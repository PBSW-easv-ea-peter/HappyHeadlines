# Monitoring

Standalone sandbox for the cache hit ratio dashboard (week 40). It runs on its own,
without `docker-compose.dev.yaml`/`docker-compose.yaml` - don't run them at the same
time, both use port 3000 for Grafana.

```
redis-article ─> redis-exporter-article ─┐
                                         ├─> Prometheus ─> Grafana (Cache hit ratio)
redis-comment ─> redis-exporter-comment ─┘
```

## Running it

```sh
docker compose -f monitoring/docker-compose.yaml up -d                   # scaffolding only
docker compose -f monitoring/docker-compose.yaml --profile dummy up -d   # with two dummy Redis caches
```

- Grafana: http://localhost:3000 (admin/admin) - dashboard under *Dashboards > Caching*
- Prometheus: http://localhost:9090 - *Status > Targets* shows both exporters

Without the `dummy` profile the exporters have nothing to connect to, so the dashboard
shows `Down` and *No data*. That's expected.

### Generating dummy hits and misses

```sh
docker compose -f monitoring/docker-compose.yaml exec redis-article redis-cli SET article:1 hello
docker compose -f monitoring/docker-compose.yaml exec redis-article redis-cli GET article:1   # hit
docker compose -f monitoring/docker-compose.yaml exec redis-article redis-cli GET article:2   # miss
```

Prometheus scrapes every 15s, so allow up to a minute before the panels update.

### Simulating a usage scenario

`scripts/simulate-traffic.sh` runs inside the `traffic-simulator` container, so the same
command works on Windows and Linux - only Docker is needed. Start the stack with the
`dummy` profile first, then:

```sh
docker compose -f monitoring/docker-compose.yaml --profile dummy run --rm traffic-simulator
```

`--profile dummy` is needed on `run` too - otherwise Compose can't resolve the
simulator's dependencies on the dummy caches.

It mimics the two cache strategies: ArticleCache is prefilled with the last 14 days and
never filled on a miss, CommentCache is filled on a miss and keeps the 30 most recently
used articles. Both caches and their stats are reset when the script starts.

The run is split into four equal phases (2 minutes each by default):

| Phase | What happens | Expected on the dashboard |
|---|---|---|
| Normal traffic | Readers mostly read the latest days | Article ~90%, comment warms up from 0 |
| Breaking news | New articles published after the nightly refresh | Article drops (offline cache), comment adapts within seconds (filled on miss) |
| Long tail | Readers browse comments on many older articles | Comment drops, evictions spike |
| Midnight refresh | The batch job loads the new articles | Article recovers |

Parameters are environment variables, passed with `-e`:

| Variable | Default | |
|---|---|---|
| `DURATION_MINUTES` | `8` | Total run time, split evenly over the four phases |
| `TICK_SECONDS` | `2` | Time between request batches |
| `REQUESTS_PER_TICK` | `25` | Requests per cache per batch |

```sh
docker compose -f monitoring/docker-compose.yaml --profile dummy run --rm -e DURATION_MINUTES=10 traffic-simulator
```

The script prints the hit ratio per 10 seconds, so you can compare it with the dashboard.
Note that the dashboard's *Hit ratio (last 5m)* panel lags behind phase changes by design.

## What's in here

| Path | Purpose |
|---|---|
| `docker-compose.yaml` | Prometheus, Grafana, one redis_exporter per cache, dummy Redis (profile `dummy`) |
| `prometheus/prometheus.yml` | Scrapes the exporters and labels them `cache="article"` / `cache="comment"` |
| `grafana/provisioning/datasources/` | Prometheus (new), Loki and Tempo (copies of `configs/grafana/data-sources/`) |
| `grafana/provisioning/dashboards/provider.yaml` | Tells Grafana to load dashboards from `grafana/dashboards/` |
| `grafana/dashboards/cache-dashboard.json` | The dashboard itself |
| `scripts/simulate-traffic.sh` | Simulated reader traffic against the dummy caches (profile `simulate`) |

Loki and Tempo aren't part of this compose, so those two datasources fail their health
check here. They're included so the folder can replace `configs/grafana/` on migration.

## Migrating into the dev/prod compose

1. Move `prometheus`, `redis-exporter-article` and `redis-exporter-comment` into the compose file.
2. Point each exporter's `REDIS_ADDR` at the real ArticleCache/CommentCache container.
3. Delete the dummy `redis-article`/`redis-comment` services and `traffic-simulator`.
4. Give the existing `grafana` service the three mounts from this compose instead of
   `./configs/grafana/data-sources`, and delete `configs/grafana/` (the copies here replace it).
5. Optionally route .NET metrics through the OTel Collector's commented-out `prometheus`
   exporter and add it as a scrape target in `prometheus.yml`.

The dashboard queries by the `cache` label only, so container names can change freely
as long as `prometheus.yml` keeps the labels.
