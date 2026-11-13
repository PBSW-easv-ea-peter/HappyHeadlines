# GAF – før-målinger (baseline)

Målt 11/11-2026 på branchen `gaf/green-tactics` (master `30f3bff` + `fix/article-service-after-k8s`, som er nødvendig for at ArticleService kan starte i dev). Ingen GAF-ændringer er lavet endnu.

## Metode

Vi måler ikke CO2 direkte. Efter SCI-formlen (Green Software Foundation), `SCI = (E × I + M) / R`, er elnettets CO2-intensitet (I) og hardwarens indlejrede CO2 (M) de samme før og efter, når vi kører på samme maskine. Forskellen ligger derfor i energien (E) pr. enhed arbejde (R), og den måler vi gennem proxies: bytes over nettet, MB på disk og CPU-tid.

Rigtige energimålinger (Scaphandre, Kepler) kræver adgang til CPU'ens RAPL-tællere og virker ikke under Docker Desktop/WSL. Det er en kendt begrænsning.

**Miljø:** Windows 11, Docker Desktop 29.9.3 (WSL2, 7,6 GiB RAM til VM'en), `docker-compose.dev.yaml`. ArticleService kører i 1 replika i dev.

**Workload:** `monitoring/scripts/simulate-flow.sh` med standardindstillinger (en publicering hvert 15. sek., en læsning hvert sek., en kommentar pr. 5 læsninger) i 15 minutter. "Efter"-målingen skal køres med præcis samme indstillinger fra en frisk stack.

### Sådan gentages målingen

```bash
# Frisk stack (artikel-DB'erne har ingen volumes, så data starter fra seed)
docker compose -f docker-compose.dev.yaml down
docker compose -f docker-compose.dev.yaml up -d --build
# vent til ArticleService logger "Consuming article_service.published_articles"

# Image-størrelser
docker images --format '{{.Repository}}:{{.Tag}}\t{{.Size}}' | grep happyheadlines
docker image inspect happyheadlines/article_service:dev --format '{{.Size}}'   # komprimeret (pull)

# Svarstørrelse for GetAll pr. region
for l in GO EU NA SA AU AS AN AF; do
  curl -s -o /dev/null -w "$l %{size_download} bytes %{time_total}s\n" http://localhost:8080/api/articles/$l
done

# 15 min workload + CPU/RAM/net hvert 30. sek.
DURATION_MINUTES=15 docker compose -f monitoring/docker-compose.yaml --profile flow up -d flow-simulator
docker stats --no-stream --format "{{.Name}},{{.CPUPerc}},{{.MemUsage}},{{.NetIO}}"   # i en løkke
```

Slå automatisk opdatering fra i Docker Desktop, mens der måles. Første forsøg blev afbrudt af en Docker Desktop-opdatering.

## Resultater

### Reduce Bundle Size – image-størrelser

| Image | På disk | Pull (komprimeret) |
|---|---|---|
| article_service | 353 MB | 101 MB |
| comment_service | 353 MB | 101 MB |
| draft_service | 350 MB | 100 MB |
| publish_service | 350 MB | 100 MB |
| profanity_service | 348 MB | 99 MB |
| newsletter_service | 346 MB | 98 MB |
| **I alt (6 .NET-services)** | **2.100 MB** | **599 MB** |
| *Til sammenligning: web-apps på `nginx:alpine`* | *128 MB* | |

"På disk" er fra `docker images`. "Pull" er fra `docker image inspect --format '{{.Size}}'`, målt på Docker 29.7. Efter opdateringen til 29.9 rapporterer `inspect` noget andet, så brug i stedet `docker save <image> | gzip | wc -c`. Det er krydstjekket: de gamle images giver også ca. 97–100 MB med den metode.

Alle .NET-services bruger `mcr.microsoft.com/dotnet/aspnet:10.0` (fuld Debian) som runtime-image. Med 3 replikaer af ArticleService i k8s og CD, der bygger og pusher alle services ved manuel kørsel, flyttes de ca. 600 MB pr. fuld deploy pr. node.

### Defer Fetching – `GET /api/articles/{location}`

`GetAll` returnerer cachen, hvis den har indhold (kun `GO` caches), og ellers **alle** artikler i shardet med fuld `breadtext`. Der er ingen `LIMIT` og ingen pagination (`ArticleReadRepository.GetAllAsync`).

| Region | Ved start: artikler / bytes | Efter 15 min: artikler / bytes |
|---|---|---|
| GO (cache) | 8 / 11.455 | 8 / 11.455 |
| EU | 8 / 8.192 | 16 / 10.709 |
| NA | 8 / 7.457 | 13 / 9.035 |
| SA | 8 / 7.191 | 19 / 10.659 |
| AU | 8 / 7.096 | 14 / 8.991 |
| AS | 8 / 7.056 | 14 / 8.941 |
| AN | 8 / 7.070 | 18 / 10.226 |
| AF | 8 / 7.058 | 16 / 9.587 |

**Pointe:** svaret vokser lineært med antallet af artikler i shardet, ca. 0,3–1,4 KB pr. artikel afhængigt af brødtekstens længde. Med testdata er det småt. En rigtig avis med 10.000 artikler pr. region ville sende flere MB ved hvert forsidekald, selvom forsiden kun viser en håndfuld. Responstid efter 15 min: 7–17 ms.

### Workload og ressourceforbrug (15 min)

Simulator: 60 drafts, 54 publiceret, 6 stoppet af profanity-check, 821 læsninger, 183 kommentarer, 0 fejl.

| Container | CPU gns. | CPU maks. | RAM ved slut |
|---|---|---|---|
| articleservice | 2,56 % | 5,15 % | 111 MiB |
| draftservice | 2,94 % | 76,52 % (opstart) | – |
| commentservice | 1,60 % | 8,95 % | – |
| publishservice | 0,54 % | 3,99 % | – |
| profanityservice | 0,43 % | 5,64 % | – |
| newsletter-service | 0,07 % | 0,50 % | – |

ArticleService netværk (rx / tx) for hele kørslen: **10,7 MB / 12,1 MB**. Tallet er al trafik ind og ud af containeren, også til Postgres, Redis og RabbitMQ. 12,1 MB / 821 læsninger ≈ 15 KB er derfor en øvre grænse pr. læsning, ikke svarstørrelsen alene.

CPU-tallene er øjebliksbilleder fra `docker stats` (procent af én kerne) med ca. 30 sek. mellemrum. Responstiderne er ét kald pr. region og kun vejledende.

Rådata: `docs/gaf/data/baseline-stats.csv` (29 målinger pr. container med 30 sek. mellemrum).

### Tactic 3 (afventer)

`CacheRefreshService` kører i hver ArticleService-replika og opdaterer den samme delte Redis hver time. I dev er der kun 1 replika, så den dobbelte refresh viser sig kun i prod/k8s (3 replikaer). Her måles før-situationen bedst som logudtræk med tre ens `Starting full cache refresh of locations: GO` pr. time. Det kræver, at `fix/article-service-after-k8s` er merget, da servicen var kommenteret ud på master.
