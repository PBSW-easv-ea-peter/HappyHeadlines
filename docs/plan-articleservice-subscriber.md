# Plan: ArticleService abonnerer på og gemmer publicerede artikler

## Kontekst
Ugekravet (`docs/handouts/Femte uge.md`) er, at traces ikke må brydes på tværs af services og skal samles centralt. PublishService publicerer allerede `PublishedArticleEvent` på topic-exchangen `published_articles` med routing key `article.published` (`apps/shared/src/Messaging`). ArticleService har kun en tom stub (`Queue/ArticleQueueConsumer.cs`).

Målet er, at ArticleService lytter på køen og gemmer artiklen i den rigtige shard. Tracen skal fortsætte hele vejen: PublishService → RabbitMQ → ArticleService → Postgres.

**Afhængighed:** `plan-observability-publishservice.md` dækker den side der publicerer. Den skal være på plads, for uden en `traceparent` på beskeden er der ingen trace at fortsætte. Den fælles `AddObservability()` abonnerer allerede på `RabbitMQ.Client.Subscriber` (`apps/shared/HappyHeadlines.Observability/ObservabilityExtensions.cs`).

## Uden for scope
- CommentCache, web-klienten og kommentarer.
- De PublishService-fejl, der er nævnt i `plan-observability-publishservice.md`.

## Uoverensstemmelser mellem event og domæne (besluttes først)
| Event (`PublishedArticleEvent`) | ArticleService i dag | Beslutning |
|---|---|---|
| `Id: Guid`, `DraftId: Guid` | `articles.id BIGINT IDENTITY` | Hvordan undgår vi dubletter, når en besked leveres igen? Fx en ny kolonne `draft_id UUID UNIQUE` via migration `V4__...sql` og `ON CONFLICT DO NOTHING`. |
| `SectionName: string` | `section_id` er en FK til `sections` | Opslag på navn. Hvad sker der, hvis navnet ikke findes? |
| `JournalistName` | `byline` (V2/V3) | Direkte mapping |
| `Location: string` | Shard-nøgle i `ArticleShardResolver` | Validering. En ukendt location er en poison message. |
| `DateTime` | `DateTimeOffset` | Antag UTC |

## Trin
1. **Projekt-referencer** i `apps/article_service/src/ArticleService/ArticleService.csproj`: tilføj `Messaging` og `HappyHeadlines.Observability`, samme mønster som i `DraftService.csproj`. Kald `builder.AddObservability()` i `Program.cs`.
2. **Konfiguration** i `appsettings.Development.json` og `appsettings.Production.json`: tilføj en `RabbitMQ`-sektion og `OpenTelemetry:Endpoint`. Formen kan kopieres fra `apps/publish_service/src/PublishService/appsettings.Development.jsonc`.
3. **Connection og topologi:**
   - `ConnectionFactory` registreres som singleton, som i `apps/publish_service/src/PublishService/Setup/RabbitMq.cs`.
   - Ved opstart kaldes `PublishedArticlesExchange.ConfigureAsync(channel)`, som genbruges.
   - Declare en navngivet, durable kø, som ArticleService ejer.
   - Bind køen med `PublishedArticleRKeys.ArticlePublished`.
4. **Consumer** i `ArticleQueueConsumer.ExecuteAsync`:
   - Brug `AsyncEventingBasicConsumer` med `autoAck: false`.
   - Deserialisér `PublishedArticleEvent` og map til `UpsertArticleRequest`.
   - Kald write-repositoriet i et DI-scope via `IServiceScopeFactory`. Repositoriet er scoped, mens en BackgroundService er singleton.
   - `BasicAck`, når skrivningen er lykkedes. `BasicNack` uden requeue ved poison.
5. **Idempotens:** migration og repo-metode, jf. tabellen ovenfor.
6. **Container:** `apps/article_service/Dockerfile` har build-context `apps/article_service/` og kan derfor ikke se `apps/shared`.
   - Omlæg til DraftService-mønstret med context `./apps` i begge compose-filer.
   - Tilføj `depends_on: rabbitmq: condition: service_healthy`.
7. **Docs:** opdatér `docs/services/article_service.md` (Afhænger af, Afvigelse og Endpoint-map).

## Fault isolation: circuit breaker pr. shard
Skrivninger fra consumeren går gennem en Polly-pipeline pr. shard (`Resilience/ShardResilience.cs`). Pipelinen har en kort retry og en circuit breaker. Er ét shard nede (fx EU), fejler dets skrivninger hurtigt, mens de andre shards fortsætter som normalt. Køen isolerer i forvejen PublishService fra fejl i ArticleService.

**Beslutning (nuværende løsning):** Når et shard's circuit er åbent, bliver beskeden requeued med det samme (`Nack(requeue: true)`). Beskeden ryger så frem og tilbage mellem RabbitMQ og consumeren, indtil circuiten går half-open (højst 30s). Databasen bliver ikke ramt imens. Det er acceptabelt, fordi ArticleService kun får få beskeder i minuttet.

**Ved behov: retry-kø.** Stiger antallet af beskeder, eller varer nedbrud længere, skal requeue erstattes af en forsinket retry-kø:
- Main-køen får en dead-letter exchange, så `Nack(requeue: false)` sender beskeden til en retry-kø med `x-message-ttl`.
- Når tiden udløber, sendes beskeden tilbage til main-køen via default exchange.
- En parking-lot-kø samler beskeder, der har fejlet N gange (tælles via headeren `x-death`), og poison-beskeder.

Det er samme overvejelse, der gør en retry-kø nødvendig ved høj trafik, fx i CommentService.

## Verifikation
1. Kør `dotnet build` på ArticleService.
2. Start via dev-compose: `rabbitmq`, article-shards og deres migrate-containere, `otel-collector`, `tempo`, `grafana`, `draftservice` og `publishservice`. Kør derefter ArticleService.
3. Tjek i RabbitMQ Management (port 15672), at køen findes og er bundet til `published_articles` med `article.published`. Der skal være 1 consumer (3 med replicas).
4. Publicér et draft via PublishService. Artiklen skal kunne hentes med `GET /api/articles/{location}`.
5. Publicér samme event igen, eller genstart ArticleService midt i behandlingen. Der må ikke opstå en dublet.
6. Stop `articledb-eu`, og publicér en EU-artikel og en NA-artikel. NA gemmes, og "Circuit ArticleShard:EU opened" logges én gang. Start `articledb-eu` igen. Inden for 30s gemmes EU-artiklen.
7. Tjek i Grafana/Tempo, at der er én trace med: HTTP-request i PublishService → DraftService → RabbitMQ publish → deliver i ArticleService → Npgsql insert.

## Refleksionsspørgsmål
1. ArticleService kører i 3 replicas. Skal de dele én navngivet kø, eller skal hver replica have sin egen? Hvad sker der med artiklen i hvert tilfælde? (Competing consumers vs. fan-out.)
2. Hvorfor ack'er vi efter DB-skrivningen og ikke før? Hvilken leveringsgaranti giver det, og hvorfor gør det idempotens nødvendig?
3. Exchangen er `durable: false`. Hvad betyder det for den durable kø, hvis RabbitMQ genstarter?
4. Hvem bør eje kønavnet: `Messaging`-libbet eller ArticleService? Hvorfor?
5. Bliver deliver-spanet child af publish-spanet, eller får det et link? Det kan nu ses direkte i Tempo.
