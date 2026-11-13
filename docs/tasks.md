# Opgaveliste – status mod kravene

Gennemgang af master (`fb6c266`, 2026-11-13) mod ugekravene i `docs/handouts/` og afleveringen af Compulsory Assignment 2. Første version blev lavet mod `1e4a4d2` (2026-11-06). Siden er #20–#36 merget, og de er medregnet her.

Den 2026-11-11 blev hele dev-stacken + `monitoring/` kørt med `monitoring/scripts/simulate-flow.sh` (ca. 20 min: 72 drafts, 65 publiceret, 7 afvist for profanity, ~1000 læsninger og ~190 kommentarer uden fejl). Punkter markeret **(verificeret)** er bekræftet i den kørsel. Punkter markeret "formentlig" er kun læst i koden.

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
| 36 | ArticleService CRUD, x-akse (3 instanser + load balancer), z-akse (7 kontinenter + global DB) | OK i compose (Swarm routing mesh). k8s: `replicas: 3` bag en k8s Service. Dev kører med 1 instans |
| 37 | Comment + Profanity, swimlanes, circuit breaker | OK. Breaker og retry blev rettet i #21 |
| 38 | DraftService + central logging og tracing | **Regression**: `AddObservability()` i DraftService er kommenteret ud i #23, så DraftService mangler i Loki og Tempo (verificeret) |
| 39 | PublisherService → Profanity → kø → ArticleService, ubrudt tracing | OK **(verificeret)**: ét trace fra `POST publish-draft` over ProfanityService og RabbitMQ (`publish`/`deliver article.published`) til ArticleService og shardet. Rettelserne fra #23-afsnittet er merget (#29) |
| 40 | ArticleCache (offline, 14 dage), CommentCache (LRU, 30 artikler), dashboard | Delvis: begge caches fyldes, og dashboardet virker (verificeret). En miss og et hit giver stadig forskellige data, og seed-datoerne er for gamle |
| 43 | CI/CD for alle services, versionerede images i et registry | OK: CI (build + test, #24) og CD (GHCR, #25) er grønne. k8s henter fra GHCR (lokal ændring, ikke merget) |
| 44 | Subscriber + DB + kø, Newsletter i egen swimlane, release toggle | **Delvis**: NewsletterService og toggle (`IOptionsMonitor<FeatureTogglesOptions>`, kan ændres uden redeploy) er merget i #26/#27, og toggle-registrering og interval-config er rettet i #31/#32. Ingen SubscriberService endnu |
| 45 | 3 tactics fra Green Architecture Framework (GAF) + før/efter-kodesnippets | OK: Reduce Bundle Size (#33), Defer Fetching (#34) og Reduce Transmitted Data (#36). Dokumentation og billeder i `docs/gaf/` |

## Compulsory Assignment 2 (frist mandag 16/11 kl. 08:00)

| Afsnit | Status |
|---|---|
| GitHub-link (public) | Klar |
| C4 L2 + L3 ArticleService (screenshots) | Views findes i `workspace.dsl` (opdateret i #30). Load balanceren flyttes fra L2 til deployment-diagrammet (`c4/remove-load-balancer`). Mangler at blive renderet bagefter |
| Scale cube: z-akse (koordinering), x-akse (config + load balancer) | Kodeblokke skrevet ind. Forklaringen skal passe til den valgte opsætning (compose eller k8s) |
| Fault isolation: circuit breaker + swimlanes | Kodeblok skrevet ind |
| Monitoring: screenshots af metrics, logs, tracing | Klar. Screenshots fra kørslen 11/11 er taget |
| CI + CD | Klar |
| Feature toggle i NewsletterService | Merget (#27): `NewsletterExecutor` og `NewsletterBackgroundService` |
| GAF: mindst 3 billeder | Klar: ét billede pr. tactic i `docs/gaf/images/` (#33, #34, #36) |
| Video, højst 5 min | **Ikke startet**. Laves til sidst |

## Tasks

### Regressioner fra #23 (k8s) – fundet ved kørslen 11/11
Rettelserne er merget i #29.
- [x] **P1** `ArticleQueueConsumer`, `CacheRefreshService` og RabbitMQ-opsætningen var kommenteret ud i `apps/article_service/src/ArticleService/Program.cs`. Publicerede artikler nåede aldrig frem, i alle miljøer inkl. k8s. Genaktiveret. (#29)
- [ ] **P1** DraftService: `builder.AddObservability()` er kommenteret ud (`apps/draft_service/src/DraftService/Program.cs:30`), så DraftService mangler i logs og traces. **Ikke rettet.**
- [ ] **P2** ArticleService kræver `JournalistWeb:BaseUrl`, men `appsettings.Development.json` og `appsettings.Production.json` havde nøglen `WebApp`, så servicen crashede ved opstart. Development er rettet (#29). **Production er ikke rettet** (k8s-configmap'en har den rigtige nøgle).
- [ ] **P2** `POSTGRES_DB_PWD` sættes i `Program.cs` (ArticleService), *efter* configuration er læst, så `ArticleShardResolver` erstatter adgangskoden med `null`. Dev-shards peger desuden på `localhost`. Dev er omgået med env-overrides på `articleservice` i `docker-compose.dev.yaml` (#29). **Selve kodefejlen er ikke rettet** og rammer stadig, når servicen køres fra IDE'en.
- [x] **P2** CommentService læser DB-adgangskoden fra `ARTICLE_DB_PWD` (forvirrende navn), som ikke var sat i dev, så alle kald gav 500. Env sat på `commentservice` i `docker-compose.dev.yaml`. (#29)
- [ ] **P3** Prod-compose (`docker-compose.yaml`) starter formentlig ikke længere: `WebApp`-nøglen, manglende `POSTGRES_DB_PWD` og web-context peger på den slettede `apps/happy-headlines-web_service/` (l. 408/415). Afklar om prod-compose stadig skal bruges, eller om k8s har overtaget.

### Docker compose (dev + prod)
- [x] **P1** Tilføj `article-cache` (Redis) til prod `docker-compose.yaml`. (#21/#22, dubletten fjernet bagefter)
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
- [x] **P1** PublisherService konsulterer ProfanityService før publicering (uge 39): 422 med ordene ved fund, 503 hvis ProfanityService er nede. (#21) **(verificeret: 7 af 7 drafts med bandeord afvist med 422)**
- [x] **P2** Én langlivet RabbitMQ-forbindelse i stedet for én pr. publish (`SharedConnection`). (#21)
- [x] **P2** Retry og circuit breaker på DraftService-klienten. (#21)
- [ ] **P2** En 422 fra DraftService ender stadig som en 503 "DraftService is unavailable". `HttpDraftClient.GetAsync` kalder `EnsureSuccessStatusCode`, og den `HttpRequestException` mappes til "unavailable" (`PublishDraftHandler.cs:41`, `:140`).
- [x] **P3** Død kode: `Setup/OpenTelemetry.cs` og `Setup/ServicesRegistration.cs` er slettet. (#21)

### Tracing (uge 39)
- [x] **P1** Verificér i Grafana/Tempo, om publish → consume er ét trace. **(verificeret 11/11)** Trace indeholder PublishService, ProfanityService og ArticleService med `publish article.published` → `deliver article.published` → DB-spans. Ingen `traceparent`-kode var nødvendig.
- [ ] **P3** Ingen Redis-instrumentering, så cache-kald er usynlige i traces.
- [ ] **P3** Tempo → logs er ikke sat op i Grafana (`tracesToLogsV2`). Logs → trace virker.
- [ ] **P3** ProfanityService har traces i Tempo, men ingen logs i Loki inden for en time. Tjek logniveau eller om servicen bare ikke logger på Information.

### ArticleService + ArticleCache (uge 40)
- [ ] **P1** En cache-miss returnerer **alle** artikler i shardet (også upublicerede, usorteret), mens et hit returnerer publicerede fra de sidste 14 dage (`ArticlesController.cs:37-43`, `ArticleReadRepository.cs`). En miss skal give det samme resultat.
- [x] **P1** Nye artikler fra køen kommer i cachen (`RefreshArticleAsync` i consumeren). (#21) NB: kun GO har et cachet indeks, og simulatoren publicerer kun til regioner.
- [ ] **P2** Alle seed-datoer ligger i september 2026 (`ArticleSeeder.cs:129ff`), mere end 14 dage tilbage, så GO-indekset er tomt i demoer. Gør datoerne relative til i dag.
- [ ] **P3** Hit ratio er skæv: Redis keyspace tæller et hit som 1 + N (ZRANGE + MGET), og regionale lokationer giver misses, der aldrig kan blive til hits. Tæl i appen med en metrics-counter i stedet.
- [ ] **P3** "Cache fill level"-panelet kræver `maxmemory`, som ingen af de to Redis-instanser har sat.
- [ ] **P3** `CacheRefreshService` kører i hver replika, så der fyldes 3× i timen.

### CommentService (uge 37)
- [x] **P1** Circuit breakeren virker ved fejl i ProfanityService: default `ShouldHandle` (5xx tæller), 2 s timeout pr. forsøg og 10 s backstop. Samme rettelse i DraftService. (#21)
- [ ] **P2** Ingen test af den rigtige Polly-pipeline. ArticleService' `ShardResilienceTests` kan bruges som skabelon.
- [ ] **P2** Race i CommentCache: en fill kan overskrive en nyere godkendt kommentar. Location normaliseres heller ikke, så `eu` og `EU` er forskellige nøgler.
- [ ] **P2** `ProfanityChecker.cs:21` splitter kun på whitespace, så `"idiot."` fanges ikke. **(verificeret 11/11: en draft med "idiot." blev publiceret)**
- [ ] **P2** Når ProfanityService er nede, gemmes kommentaren som `PendingProfanityCheck`, men CommentService svarer 422, så klienten tror, den er afvist. Svar fx 202 Accepted. **(verificeret 13/11 ved et live-udfald)**
- [x] **P3** Udkommenteret pipeline fjernet, og Pollys egne logs er dæmpet i alle services. (#21)

### Logging (uge 38)
- [x] **P3** Feltnavne følger listen i `docs/logging.md` (`Reason`, `ArticleId`/`DraftId`, `Location`; `EventId`, `DeliveryTag` og `Queue` er tilføjet til listen). (#21)
- [x] **P3** Ingen response-body eller fulde exceptions på Warning-niveau. Fulde exceptions logges kun på Error. (#21)

### CI/CD (uge 43)
- [x] **P1** CI-pipeline: restore, build og test af hele `apps/HappyHeadlines.slnx` på PR'er og push til master (`continuous_integration.yml`). (#24)
- [x] **P1** CD bygger kun mapper med en Dockerfile, genbygger services der afhænger af `shared`, `fail-fast: false`, små bogstaver i image-navne og egen build context for web-apps (`continuous_delivery.yml`). (#25)
- [x] **P2** CD: manuel trigger (`workflow_dispatch`), der bygger alle services. (`d6771ab`) Kør den én gang efter merge, så alle images findes i GHCR.
- [x] **P2** k8s-deployments henter `ghcr.io/pbsw-easv-ea-peter/happyheadlines/<service>:latest` i stedet for Docker Hub. (`d6771ab`) Tjek at GHCR-pakkerne er public.
- [ ] **P2** CD kører på `pull_request: closed` uden `merged == true`-tjek, så lukkede, ikke-mergede PR'er pusher også images.
- [ ] **P3** CD bruger two-dot diff (`base.sha head.sha`). Three-dot (`base...head`) differ mod merge-basen.
- [ ] **P3** Versionering er kun git-SHA og `latest`. `docs/ci-status.md` er bagud i forhold til #24/#25.
- [ ] **P3** Branch protection på master med CI som påkrævet check (indstilling i GitHub).

### Docs, C4, repo
- [x] **P3** README: build-context, `monitoring/`-stacken og en "sådan kører du det"-sektion. (#21)
- [x] **P3** Forældet tekst i `docs/*.md` og `C4 diagram/docs/L4/*` er opdateret. (#21)
- [ ] **P3** C4 `workspace.dsl` er bragt ajour (#21), men mangler stadig L3 for web og ArticleCache og L4 for profanity og publish.
- [x] **P3** `C4 diagram/docs/workspace.json` er untracked og står i `.gitignore`. (#21)
- [x] **P3** `PublishService.Tests` er med i `apps/HappyHeadlines.slnx`. (#21)
- [x] **P3** Ubrugte `.dockerignore` i `apps/article_service/` og `apps/publish_service/` er slettet. (#21)
- [ ] **P3** `monitoring/README.md` nævner ikke `--profile flow` og `simulate-flow.sh`.
- [x] **P3** C4 er bragt ajour med NewsletterService, web-apps og k8s-load balanceren. (#30)
- [ ] **P3** Load balanceren fjernes fra L2 og bliver en infrastructure node i deployment-diagrammet, da den ikke kører egen applikationskode. Branch `c4/remove-load-balancer` (validerer med `structurizr/structurizr validate`).

### Uge 44 (designes af gruppen)
- [ ] **P1** SubscriberService + SubscriberDatabase + SubscriberQueue. Event og exchange findes på master (#20).
- [ ] **P1** NewsletterService i egen swimlane med fault isolation mod SubscriberService. NewsletterService er merget (#26/#27), men bruger stadig en in-memory SubscriberService.
- [x] **P1** Release toggle: `IOptionsMonitor<FeatureTogglesOptions>` i NewsletterService, læses ved hvert brug. (#27)
- [ ] **P1** Alle nye services med i CI/CD. CD finder dem automatisk, hvis mappen under `apps/` har en Dockerfile. CI kræver at projekterne er med i `HappyHeadlines.slnx`.
- [x] **P2** NewsletterService: interval-nøglen i config og look-back-perioden (`TimeSpan.FromDays`) er rettet. (#31)
- [x] **P2** Feature toggles registreres i NewsletterService' `Program.cs`. (#32)
- [ ] **P2** `InMemorySubscriberService` forventer interval 0/1, men enum'en har værdierne 10/20, så den crasher, når toggle'en er slået til.
- [ ] **P2** `NewSubscriberMessageHandler` logger subscriberens e-mail. Det er en personoplysning (GDPR). Log et id i stedet.

### Uge 45 – Green Architecture Framework (GAF)
Handout: "Pick three tactics from GAF, you haven't been actively working on yet and improve Happy Headlines based on these tactics. Make sure to document your process with before and after code snippets." Afleveringen kræver mindst 3 billeder, ét pr. tactic.
- [x] **P1** Reduce Bundle Size: chiselede runtime-images til .NET-services. (#33)
- [x] **P1** Defer Fetching: forside-endpoint, der kun henter det, siden viser. (#34)
- [x] **P1** Reduce Transmitted Data: send kun den brødtekst, siden viser. (#36)
- [x] **P1** "Før" er målt og dokumenteret i `docs/gaf/baseline.md`. Før/efter pr. tactic ligger i `docs/gaf/*.md` og `docs/gaf/images/`.

## Foreslået rækkefølge (frem til 16/11)
1. Merge `c4/remove-load-balancer`, og render derefter C4-screenshots (L2, L3 ArticleService, deployment)
2. Videoen (højst 5 min)
3. P1: `AddObservability()` i DraftService
4. P2-bugs fra 13/11: `InMemorySubscriberService` 0/1, PII-log og 422 på pending-kommentarer
5. Øvrige P2/P3 løbende

## Verifikation pr. task
- `dotnet test apps/HappyHeadlines.slnx` (kører også i CI)
- `docker compose -f docker-compose.dev.yaml up -d --build` + `docker compose -f monitoring/docker-compose.yaml up -d`, derefter `--profile flow up -d flow-simulator`. Simulatoren skriver en statuslinje hvert minut (`docker logs -f monitoring-flow-simulator-1`).
- Tracing: find `POST /api/v1/publish-draft/{id}` i Tempo, og tjek at ArticleService' `deliver article.published` ligger i samme trace
- Cache: `cache-dashboard` viser hit ratio for begge caches
