# Opgaveliste – status mod kravene

Gennemgang af master (`c3450f4`, 2026-11-11) mod ugekravene i `docs/handouts/`. Første version blev lavet mod `1e4a4d2` (2026-11-06). Siden er #20 (subscriber-exchange), #21 (status-oprydning) og #22 (article-cache i prod) merget, og de er medregnet her. Koden er læst, men ikke bygget eller kørt. Derfor skal punkter markeret "formentlig" verificeres.

Opgaverne er grupperet efter område, så de kan tages, når man alligevel arbejder i den del af koden. Hold én task pr. PR, så reviews forbliver små. Lukkede tasks står stadig i listen, krydset af med PR-nummer.

**Prioritet** (vores egen inddeling, ikke fra kurset):
- **P1**: et ugekrav fra handouts er ikke opfyldt. Skal laves før aflevering eller præsentation.
- **P2**: en fejl i noget, der allerede er bygget (virker forkert eller er skrøbeligt).
- **P3**: docs, oprydning eller nice-to-have.

**C4-niveauer:** L1 = Context, L2 = Container, L3 = Component, L4 = Code.

## Status pr. krav

| Uge | Krav | Status |
|---|---|---|
| 35 | C4 L1 + L2 | OK. `workspace.dsl` blev opdateret i #21 |
| 36 | ArticleService CRUD, x-akse (3 instanser + load balancer), z-akse (7 kontinenter + global DB) | OK. Load balancer = Swarm routing mesh. Dev kører med 1 instans |
| 37 | Comment + Profanity, swimlanes, circuit breaker | OK. Breaker og retry blev rettet i #21 |
| 38 | DraftService + central logging og tracing | OK. Logs blev ensrettet med `docs/logging.md` i #21 |
| 39 | PublisherService → Profanity → kø → ArticleService, ubrudt tracing | **Næsten**: Publisher tjekker profanity (#21). Ubrudt trace over RabbitMQ er ikke verificeret i Tempo. NewsletterService findes kun på `feature/NewsletterService` |
| 40 | ArticleCache (offline, 14 dage), CommentCache (LRU, 30 artikler), dashboard | Delvis: ArticleCache kører i prod, og nye artikler kommer i cachen (#21/#22). En miss og et hit giver stadig forskellige data, og seed-datoerne er for gamle |
| 43 | CI/CD for alle services, versionerede images i et registry | Delvis / i stykker (CI ejes af en i gruppen, se nederst og `docs/ci-status.md`) |
| 44 | Subscriber + DB + kø, Newsletter i egen swimlane, release toggle | **Ikke startet**. Kun `NewSubscriberEvent`, `SubscribersExchange` og routing keys er på master (#20). `feature/NewsletterService` er ikke merget |
| 45 | 3 tactics fra Green Architecture Framework (GAF) + før/efter-kodesnippets | **Ikke startet** |

## Tasks

### Docker compose (dev + prod)
- [x] **P1** Tilføj `article-cache` (Redis) til prod `docker-compose.yaml`. (#21/#22)
- [x] **P2** CommentService i dev peger på containernavne via env-overrides i compose. (#21)
- [x] **P2** Fjern `container_name: redis` på dev article-cache. (#21)
- [ ] **P2** Flyway-migrate-jobs i prod har ingen `deploy.restart_policy`. Under Swarm genstarter de derfor efter hver vellykket kørsel. Sæt `condition: on-failure`.
- [x] **P2** Prod `articleservice` mangler `depends_on: rabbitmq`. (#22)
- [ ] **P3** RabbitMQ-volumen er udkommenteret i dev og mangler i prod, så køer og beskeder forsvinder, når containeren genskabes.

### RabbitMQ / shared Messaging
- [x] **P2** `published_articles`-exchangen er nu durable. (#21) NB: en broker, der stadig kører med den gamle exchange, svarer `PRECONDITION_FAILED`. Genskab rabbitmq-containeren én gang.
- [x] **P2** Retry ved opstart, når RabbitMQ er nede (`RabbitMqConnection.CreateWithRetryAsync`, `SharedConnection`). (#21)
- [ ] **P2** `SubscribersExchange` (`apps/shared/src/Messaging/Exchanges/SubscribersExchange.cs:14`) er nu på master og er stadig `durable: false`. Ret den, før NewsletterService/SubscriberService bruger den.
- [ ] **P3** Ingen dead-letter-exchange: poison messages kasseres med kun en Warning (`ArticleQueueConsumer.cs`), og andre fejl requeues uendeligt.

### PublishService
- [x] **P1** PublisherService konsulterer ProfanityService før publicering (uge 39): 422 med ordene ved fund, 503 hvis ProfanityService er nede. DraftService' tjek ved submit er bevaret som tidlig feedback. (#21)
- [x] **P2** Én langlivet RabbitMQ-forbindelse i stedet for én pr. publish (`SharedConnection`). (#21)
- [x] **P2** Retry og circuit breaker på DraftService-klienten. (#21)
- [ ] **P2** En 422 fra DraftService ender stadig som en 503 "DraftService is unavailable". `HttpDraftClient.GetAsync` kalder `EnsureSuccessStatusCode`, og den `HttpRequestException` mappes til "unavailable" (`PublishDraftHandler.cs:41`, `:140`).
- [x] **P3** Død kode: `Setup/OpenTelemetry.cs` og `Setup/ServicesRegistration.cs` er slettet. (#21)

### Tracing (uge 39)
- [ ] **P1** Verificér i Grafana/Tempo, om publish → consume er ét trace. RabbitMQ.Client 7 linker formentlig consumer-spannet i stedet for at gøre det til child, så det får et nyt trace-id. Hvis det er tilfældet, skal `traceparent` udtrækkes fra headers i `ArticleQueueConsumer.HandleAsync`, og en child-span startes. `C4 diagram/docs/L4/observability.md` beskriver det også som "not yet verified in Tempo".
- [ ] **P3** Ingen Redis-instrumentering, så cache-kald er usynlige i traces.
- [ ] **P3** Tempo → logs er ikke sat op i Grafana (`tracesToLogsV2`). Logs → trace virker.

### ArticleService + ArticleCache (uge 40)
- [ ] **P1** En cache-miss returnerer **alle** artikler i shardet (også upublicerede, usorteret), mens et hit returnerer publicerede fra de sidste 14 dage (`ArticlesController.cs:37-43`, `ArticleReadRepository.cs`). En miss skal give det samme resultat.
- [x] **P1** Nye artikler fra køen kommer i cachen (`RefreshArticleAsync` i consumeren). (#21)
- [ ] **P2** Alle seed-datoer ligger i september 2026 (`ArticleSeeder.cs:129ff`), mere end 14 dage tilbage, så GO-indekset er tomt i demoer. Gør datoerne relative til i dag.
- [ ] **P3** Hit ratio er skæv: Redis keyspace tæller et hit som 1 + N (ZRANGE + MGET), og regionale lokationer giver misses, der aldrig kan blive til hits. Tæl i appen med en metrics-counter i stedet.
- [ ] **P3** "Cache fill level"-panelet kræver `maxmemory`, som ingen af de to Redis-instanser har sat.
- [ ] **P3** `CacheRefreshService` kører i hver replika, så der fyldes 3× i timen.

### CommentService (uge 37)
- [x] **P1** Circuit breakeren virker ved fejl i ProfanityService: default `ShouldHandle` (5xx tæller), 2 s timeout pr. forsøg og 10 s backstop. Samme rettelse i DraftService. (#21)
- [ ] **P2** Ingen test af den rigtige Polly-pipeline. ArticleService' `ShardResilienceTests` kan bruges som skabelon.
- [ ] **P2** Race i CommentCache: en fill kan overskrive en nyere godkendt kommentar. Location normaliseres heller ikke, så `eu` og `EU` er forskellige nøgler.
- [ ] **P2** `ProfanityChecker.cs:21` splitter kun på whitespace, så `"idiot,"` fanges ikke.
- [x] **P3** Udkommenteret pipeline fjernet, og Pollys egne logs er dæmpet i alle services. (#21)

### Logging (uge 38)
- [x] **P3** Feltnavne følger listen i `docs/logging.md` (`Reason`, `ArticleId`/`DraftId`, `Location`; `EventId`, `DeliveryTag` og `Queue` er tilføjet til listen). (#21)
- [x] **P3** Ingen response-body eller fulde exceptions på Warning-niveau. Fulde exceptions logges kun på Error. (#21)

### Docs, C4, repo
- [x] **P3** README: build-context, `monitoring/`-stacken og en "sådan kører du det"-sektion. (#21)
- [x] **P3** Forældet tekst i `docs/*.md` og `C4 diagram/docs/L4/*` er opdateret. (#21)
- [ ] **P3** C4 `workspace.dsl` er bragt ajour (#21), men mangler stadig L3 for web og ArticleCache og L4 for profanity og publish.
- [x] **P3** `C4 diagram/docs/workspace.json` er untracked og står i `.gitignore`. (#21)
- [x] **P3** `PublishService.Tests` er med i `apps/HappyHeadlines.slnx`. (#21)
- [x] **P3** Ubrugte `.dockerignore` i `apps/article_service/` og `apps/publish_service/` er slettet. (#21)

### Uge 44 (designes af gruppen)
- [ ] **P1** SubscriberService + SubscriberDatabase + SubscriberQueue. Event og exchange findes på master (#20).
- [ ] **P1** NewsletterService i egen swimlane med fault isolation mod SubscriberService. Påbegyndt på `feature/NewsletterService` (ikke merget). Afstem status med den, der arbejder på den.
- [ ] **P1** Release toggle: SubscriberService skal kunne slås til og fra uden redeploy
- [ ] **P1** Alle nye services med i CI/CD (koordineres med CI-ejeren)

### Uge 45 – Green Architecture Framework (GAF)
Handout: "Pick three tactics from GAF, you haven't been actively working on yet and improve Happy Headlines based on these tactics. Make sure to document your process with before and after code snippets."
- [ ] **P1** Vælg 3 tactics, vi ikke har arbejdet med. Allerede brugt: caching og geografisk sharding (Fetch Data from the Proximity), x-akse-skalering (Independent Scaling), delt RabbitMQ-forbindelse (Pool Resources). Kandidater:
  - **Defer Fetching / pagination**: `GetAll` returnerer alle artikler i shardet.
  - **Reduce Bundle Size**: .NET-services kører på `aspnet:10.0`. Alpine/chiseled er mindre.
  - **Reduce Logging**: logniveauer pr. service, afvejet mod observability (uge 38).
  - **Shut Down when Idle**: `CacheRefreshService` kører i alle 3 replikaer.
- [ ] **P1** Mål eller dokumentér "før", inden der ændres (fx image-størrelse, responsstørrelse, CPU i Grafana). Gem før/efter-kodesnippets undervejs.

## Noter til CI-ejeren (uge 43)
Kun til orientering. Vi ændrer ikke selv i pipelinen (`.github/workflows/ci-apps.yml`). Se også `docs/ci-status.md`.
- Push-triggeren er udkommenteret. Pipelinen kører kun på `pull_request: closed`, uden et `merged == true`-tjek, så også lukkede, ikke-mergede PR'er pusher images.
- Den bygger kun ændrede mapper. En ændring i `apps/shared` giver matrix-værdien `shared`, der ikke har en Dockerfile, og de services, der afhænger af den, bygges ikke.
- Web-imaget bygges med `apps/` som context, men dets Dockerfile forventer sin egen mappe. `Dockerfile.reader` bygges slet ikke.
- Images tagges med git-SHA og `latest` i GHCR. Compose bruger lokale image-navne, ikke registry-imagesne.
- Ingen `dotnet build`/`dotnet test`, og ingen build-tjek på åbne PR'er.

## Foreslået rækkefølge
1. Uge 44, som er det største hul
2. Uge 45 (vælg tactics og mål "før" tidligt)
3. Cache-miss skal give samme resultat som et hit (uge 40)
4. Tjek tracing i Tempo (kræver at stacken kører)
5. P2/P3 tages løbende, når man alligevel er i området

## Verifikation pr. task
- `dotnet test apps/HappyHeadlines.slnx`
- `docker compose -f docker-compose.dev.yaml up` + stacken i `monitoring/`, og derefter et manuelt flow: opret draft → godkend → publicér → artiklen vises på forsiden → kommentér
- Tracing: find publish-requestet i Grafana/Tempo, og tjek at consumer-spannet har samme trace-id
- Cache: `cache-dashboard` viser hit ratio for begge caches
