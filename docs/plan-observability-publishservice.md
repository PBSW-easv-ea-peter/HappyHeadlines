# Plan: Observability i RabbitMQ og PublishService

## Kontekst
Ugekravet (`docs/handouts/Femte uge.md`) er, at traces ikke må brydes på tværs af services. PR'en samler to tickets:

1. **Add observability i RabbitMQ (færdig):** `AddSource("RabbitMQ.Client.Publisher", "RabbitMQ.Client.Subscriber")` er tilføjet i `apps/shared/HappyHeadlines.Observability/ObservabilityExtensions.cs`. RabbitMQ.Client 7 har indbyggede `ActivitySource`s. Når SDK'et abonnerer på dem, laver klienten publish- og deliver-spans og sender trace-konteksten med i beskedens headers.
2. **Add observability i PublishService:** PublishService sender i dag kun logs. Tracing er udkommenteret i `Setup/OpenTelemetry.cs`. Den skal over på den fælles `AddObservability()` ligesom Draft, Comment og Profanity.

Når begge er med, kan RabbitMQ-ændringen testes rigtigt, for PublishService er den eneste service, der publicerer.

## Ændringer (PublishService)
Mønsteret følger DraftService (`apps/draft_service/src/DraftService/DraftService.csproj` og `appsettings.*.json`).

1. **`apps/publish_service/src/PublishService/PublishService.csproj`**
   - Tilføj `ProjectReference` til `..\..\..\shared\HappyHeadlines.Observability\HappyHeadlines.Observability.csproj`.
   - Fjern `OpenTelemetry.Exporter.OpenTelemetryProtocol` og `OpenTelemetry.Extensions.Hosting` (1.19.1). De kommer nu med via det fælles projekt, og så undgår vi to forskellige versioner.
2. **`Program.cs`**: Erstat `builder.ConfigureOpenTelemetry()` med `builder.AddObservability()` og tilføj `using HappyHeadlines.Observability;`.
3. **Slet `Setup/OpenTelemetry.cs`.** Den erstattes helt af den fælles opsætning.
4. **Konfiguration:** `AddObservability()` kræver `OpenTelemetry:Endpoint` (base-URL) og kaster en exception, hvis den mangler.
   - `appsettings.Development.jsonc`: `LogsEndpoint: http://localhost:4318/v1/logs` bliver til `Endpoint: http://localhost:4318`. Den udkommenterede container-variant bevares, som filen gør i dag.
   - `appsettings.Production.jsonc`: Tilføj `"OpenTelemetry": { "Endpoint": "http://otel-collector:4318" }`.

## Uden for scope (nævnes i PR-beskrivelsen)
Følgende fejl fandtes i forvejen og bliver ikke rettet i denne PR:
- **`apps/publish_service/Dockerfile`** har build-context `src/`. Derfor kan hverken `Messaging` eller `Observability` findes, så container-buildet er allerede brudt. Det skal omlægges til DraftService-mønstret, hvor context er `./apps`.
- **`PublishService.slnx`** peger på `ArticleService.csproj`.
- **`appsettings.Production.jsonc`** indeholder `ArticleShards` og mangler `RabbitMQ` og `DraftService`, fordi den er kopieret fra en anden service.

## Verifikation
1. `dotnet build` på PublishService, Observability, Draft, Comment og Profanity.
2. **Kør hele kæden lokalt:**
   - Start `rabbitmq`, `draftdb`, `draftdb-migrate` og `draftservice` via `docker-compose.dev.yaml`, sammen med `otel-collector`, `tempo` og `grafana`.
   - Start PublishService med `dotnet run` (Development, localhost).
   - Opret en midlertidig kø bundet til `PublishedArticlesExchange` via RabbitMQ Management (port 15672).
   - Opret et draft via DraftService-API'et og publicér det via PublishService-endpointet.
3. **Tjek:**
   - Beskeden i den midlertidige kø har en `traceparent`-header (RabbitMQ Management → Get messages).
   - I Grafana/Tempo er der én trace med: HTTP-request i PublishService, HttpClient-kald til DraftService, span i DraftService og et publish-span fra `RabbitMQ.Client.Publisher`.
4. **Oprydning:** Slet den midlertidige kø og stop de containere, du har startet.

Hold `docs/Caching.md` ude af PR'en.

## Refleksionsspørgsmål
1. Hvorfor virker det automatisk over HTTP, men ikke over en kø?
2. Bliver deliver-spanet child af publish-spanet eller får det et link? Hvad giver mest mening ved asynkron messaging?
3. Hvad sker der med tracen, hvis consumeren læser beskeden en time senere?
