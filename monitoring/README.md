# Monitoring

All monitoring for HappyHeadlines lives in this folder and runs as its own compose
project, separate from the app's `docker-compose*.yaml` in the repo root.

| File | Use it for |
|---|---|
| `docker-compose.yaml` | Monitoring the running application (dev or prod stack) |
| `docker-compose.dev.yaml` | Monitoring on its own, with dummy caches and simulated traffic |

```
.NET services ──OTLP──> OTel Collector ──┬─> Loki  (logs)   ─┐
                                         └─> Tempo (traces) ─┤
article-cache ─> redis-exporter-article ─┐                   ├─> Grafana
comment-cache ─> redis-exporter-comment ─┴─> Prometheus ─────┘
```

## Monitoring the application

The app stack creates its network - `happyheadlines-dev` or `happyheadlines-prod`, kept
apart like the stacks' project names - and this compose joins it as an external network.
`APP_NETWORK` picks which one (default: dev). Start the app first - otherwise Compose fails
with `Could not attach to network happyheadlines-dev: ... not found`.

```sh
# Dev (repo root)
docker compose -f docker-compose.dev.yaml up -d
docker compose -f monitoring/docker-compose.yaml up -d

# Prod
APP_NETWORK=happyheadlines-prod docker compose -f monitoring/docker-compose.yaml up -d
```

In PowerShell, set the variable first: `$env:APP_NETWORK = "happyheadlines-prod"`.

On the shared network the services reach the collector as `otel-collector:4318`
(see `appsettings.*.json`) and the exporters reach `article-cache` and `comment-cache` by
service name. Services you run on the host instead use `localhost:4318`.

The prod network is declared `attachable: true` in the root `docker-compose.yaml`, because
Swarm overlay networks (`docker stack deploy`) reject plain `docker compose` containers otherwise.

- Grafana: http://localhost:3000 (admin/admin) - the cache dashboard is under *Dashboards > Caching*
- Prometheus: http://localhost:9090 - *Status > Targets* shows both exporters

If a cache is renamed in the app compose, update `REDIS_ADDR` in `docker-compose.yaml`.
The dashboard queries by the `cache` label from `prometheus/prometheus.yml` only, so
container names can change freely as long as the labels stay.

## Monitoring on its own (dev)

No app needed. Two dummy Redis containers stand in for the caches.

```sh
docker compose -f monitoring/docker-compose.dev.yaml up -d
```

The two compose files have their own project names (`monitoring` and `monitoring-dev`),
but publish the same ports - run one at a time.

### Generating dummy hits and misses

```sh
docker compose -f monitoring/docker-compose.dev.yaml exec redis-article redis-cli SET article:1 hello
docker compose -f monitoring/docker-compose.dev.yaml exec redis-article redis-cli GET article:1   # hit
docker compose -f monitoring/docker-compose.dev.yaml exec redis-article redis-cli GET article:2   # miss
```

Prometheus scrapes every 15s, so allow up to a minute before the panels update.

### Simulating a usage scenario

`scripts/simulate-traffic.sh` runs inside the `traffic-simulator` container, so the same
command works on Windows and Linux - only Docker is needed:

```sh
docker compose -f monitoring/docker-compose.dev.yaml --profile simulate run --rm traffic-simulator
```

If it fails with `set: pipefail: invalid option name`, the script was checked out with
Windows line endings. `.gitattributes` forces LF for `*.sh`, but files checked out before
that rule need a fresh checkout: delete the script and run `git checkout -- monitoring/scripts/simulate-traffic.sh`.

It mimics the two cache strategies: ArticleCache is prefilled with the last 14 days and
never filled on a miss, CommentCache is filled on a miss and keeps the 30 most recently
used articles. Both caches and their stats are reset at the start of every cycle.

A cycle is split into four equal phases (2 minutes each by default):

| Phase | What happens | Expected on the dashboard |
|---|---|---|
| Normal traffic | Readers mostly read the latest days | Article ~90%, comment warms up from 0 |
| Breaking news | New articles published after the nightly refresh | Article drops (offline cache), comment adapts within seconds (filled on miss) |
| Long tail | Readers browse comments on many older articles | Comment drops, evictions spike |
| Midnight refresh | The batch job loads the new articles | Article recovers |

Parameters are environment variables, passed with `-e`:

| Variable | Default | |
|---|---|---|
| `DURATION_MINUTES` | `8` | Length of one cycle, split evenly over the four phases |
| `CYCLES` | `1` | Number of cycles. `0` repeats until the container is stopped |
| `TICK_SECONDS` | `2` | Time between request batches |
| `REQUESTS_PER_TICK` | `25` | Requests per cache per batch |
| `ARTICLE_SIZE_BYTES` | `4096` | Size of each cached article |
| `COMMENT_SIZE_BYTES` | `2048` | Size of each article's cached comments |

The `maxmemory` limits on the dummy caches are sized for the default value sizes - see
*Cache fill level* below before changing them.

```sh
docker compose -f monitoring/docker-compose.dev.yaml --profile simulate run --rm -e DURATION_MINUTES=10 traffic-simulator
```

The script prints the hit ratio per 10 seconds, so you can compare it with the dashboard.
Note that the dashboard's *Hit ratio (last 5m)* panel lags behind phase changes by design.

### Cache fill level

Each cache has a hard memory limit (`maxmemory`, with `allkeys-lru` eviction). The
*Cache fill level* gauge shows how much of it is in use. Current capacities in the dev compose:

| Cache | Capacity | Simulator keeps | Expected gauge |
|---|---|---|---|
| ArticleCache | 150 articles x 5 186 B = 780 000 B | 145 articles | ~97% |
| CommentCache | 50 articles' comments x 2 613 B = 131 000 B | 30 (its own LRU) | ~60% |

The bytes per entry are measured (payload plus Redis' per-key overhead), not calculated.

**Why the gauge measures free space.** An empty Redis already uses ~1.7 MB of its own,
which is more than all the comment data. So `used / maxmemory` would show an empty
CommentCache as ~93% full. Instead `maxmemory` = empty Redis + capacity, and the gauge
shows `1 - (maxmemory - used) / capacity`. `maxmemory - used` is the room left before Redis
starts evicting, so 0% is empty and 100% is exactly where eviction begins.

The baseline moves a little with open connections (±30 KB), so on CommentCache's small
capacity the gauge can be off by a few percentage points. With real article sizes
(megabytes of data) that noise disappears.

**Raising a limit** - the capacity lives in two places, keep them in sync:

1. `docker-compose.dev.yaml`: `--maxmemory` = baseline + new capacity (see the comments there).
2. The dashboard's hidden constants `article_capacity` / `comment_capacity` (in
   `grafana/dashboards/cache-dashboard.json`, `templating`).

To measure bytes per entry for other sizes: `FLUSHALL`, note `used_memory` from
`INFO memory`, write N entries, and divide the difference by N.

**Real caches.** The caches in the app compose have no `maxmemory` yet, so the gauge shows
*No limit set* in full mode. Once ArticleCache stores real articles, measure its bytes per
entry the same way and give it a limit plus matching dashboard constant.

### Before a presentation

Start the stack and the simulator when the session begins, so the dashboard has data
whenever it's your turn. Repeating the 8-minute scenario (instead of one long run) means
the dashboard's default 1-hour window always shows all four phases:

```sh
docker compose -f monitoring/docker-compose.dev.yaml up -d
docker compose -f monitoring/docker-compose.dev.yaml --profile simulate run -d --rm -e CYCLES=0 traffic-simulator
```

`-d` runs it in the background, so closing the terminal doesn't stop it. Follow it with
`docker logs -f <container id>` and stop it with:

```sh
docker stop $(docker ps -q --filter name=traffic-simulator)
```

Keep the machine from sleeping during the session - Docker pauses with it and leaves a gap
in the graphs.

## What's in here

| Path | Purpose |
|---|---|
| `docker-compose.yaml` | Full monitoring, joins the app's network (`APP_NETWORK`, default `happyheadlines-dev`) |
| `docker-compose.dev.yaml` | Same monitoring services plus dummy Redis and `traffic-simulator` (profile `simulate`) |
| `otel-collector/config.yaml` | Receives OTLP from the services, forwards logs to Loki and traces to Tempo |
| `loki/config.yaml`, `tempo/config.yaml` | Single-binary Loki and Tempo on the local filesystem |
| `prometheus/prometheus.yml` | Scrapes the exporters and labels them `cache="article"` / `cache="comment"` - shared by both compose files |
| `grafana/provisioning/datasources/` | Prometheus, Loki and Tempo |
| `grafana/provisioning/dashboards/provider.yaml` | Tells Grafana to load dashboards from `grafana/dashboards/` |
| `grafana/dashboards/cache-dashboard.json` | The cache dashboard |
| `scripts/simulate-traffic.sh` | Simulated reader traffic against the dummy caches |

The two compose files define the same monitoring services. When you change one of them,
change the other too.
