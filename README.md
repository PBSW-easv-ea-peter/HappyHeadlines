# HappyHeadlines

# Applications

Denne mappe indeholder alle applikationer og services i **HappyHeadlines**-projektet.

Projektet er struktureret, så hver individuel service har sin egen mappe under `apps/`.

## Projektstruktur

Den overordnede struktur er:

```text
apps/
├── HappyHeadlines.slnx
├── .dockerignore            # bruges af services med build context ./apps
│
├── shared/
│   ├── HappyHeadlines.Observability/
│   │   └── HappyHeadlines.Observability.csproj
│   └── src/
│       └── Messaging/
│           └── Messaging.csproj
│
├── article_service/
│   ├── ArticleService.slnx
│   ├── Dockerfile
│   ├── src/
│   │   └── ArticleService/
│   │       └── ArticleService.csproj
│   └── tests/
│       └── ArticleService.Tests/
│           └── ArticleService.Tests.csproj
│
├── comment_service/
│   ├── CommentService.slnx
│   ├── Dockerfile
│   ├── src/
│   │   └── CommentService/
│   │       └── CommentService.csproj
│   └── tests/
│       └── CommentService.Tests/
│           └── CommentService.Tests.csproj
│
├── profanity_service/
│   └── ...
│
└── happy-headlines-web_service/
    └── ...
```

Hver service er isoleret i sin egen mappe og følger samme overordnede struktur.

`shared/` indeholder biblioteker, som flere services bruger ved build. Der er ingen runtime-afhængighed mellem services. Alle .NET-services (ArticleService, CommentService, DraftService, ProfanityService og PublishService) bygges derfor med build context `./apps` (se `docker-compose*.yaml`), så Dockerfilen kan se `shared/`. Kun webappen bygges stadig med sin egen mappe som context og har sin egen `.dockerignore`.

## Solution files

Der findes en samlet solution for applikationerne:

```text
apps/HappyHeadlines.slnx
```

Derudover har de enkelte services aktuelt deres egen solution-fil:

```text
apps/article_service/ArticleService.slnx
apps/comment_service/CommentService.slnx
apps/draft_service/DraftService.slnx
apps/profanity_service/ProfanityService.slnx
apps/publish_service/PublishService.slnx
apps/happy-headlines-web_service/HappyHeadlines.web.slnx
```

De individuelle solution-filer gør det muligt at arbejde med en enkelt service isoleret fra resten af projektet.

Det er endnu ikke besluttet, om de individuelle solution-filer skal beholdes permanent.

## Naming conventions

Følgende konventioner anvendes:

| Type                | Convention       | Eksempel                        |
| ------------------- | ---------------- | ------------------------------- |
| Service             | `[name]_service` | `article_service`               |
| Source directory    | `src/`           | `article_service/src/`          |
| Test directory      | `tests/`         | `article_service/tests/`        |
| Application project | `[Name]`         | `ArticleService`                |
| Test project        | `[Name].Tests`   | `ArticleService.Tests`          |
| Dockerfile          | `Dockerfile`     | `article_service/Dockerfile`    |
| Docker ignore       | `.dockerignore`  | Roden af build context: `apps/.dockerignore` eller `happy-headlines-web_service/.dockerignore` |
| Service solution    | `[Name].slnx`    | `ArticleService.slnx`           |

## Nye services

Når en ny service oprettes, bør den følge samme struktur:

```text
apps/
└── new_service/
    ├── Dockerfile
    ├── .dockerignore
    ├── NewService.slnx
    ├── src/
    │   └── NewService/
    │       └── NewService.csproj
    └── tests/
        └── NewService.Tests/
            └── NewService.Tests.csproj
```

## Observability (logs og traces)

ArticleService, DraftService, CommentService, ProfanityService og PublishService sender logs og traces via OpenTelemetry. Opsætningen ligger i `apps/shared/HappyHeadlines.Observability` og aktiveres i hver service med `builder.AddObservability()`. Endpointet sættes i `appsettings.*.json` under `OpenTelemetry:Endpoint`.

```text
service ──OTLP──▶ otel-collector ──▶ Loki  (logs)
                                 └─▶ Tempo (traces)   ──▶ Grafana
```

Observability-stacken er en separat compose-stack i `monitoring/` (se [monitoring/README.md](monitoring/README.md)). Start app-stacken først, derefter monitoring:

```bash
docker compose -f docker-compose.dev.yaml up -d
docker compose -f monitoring/docker-compose.yaml up -d
```

Grafana: <http://localhost:3000> (første login `admin` / `admin`), menuen **Explore**:

- **Traces:** vælg datasource `Tempo` → *Search* → service `DraftService`. En submit viser hele kæden DraftService → ProfanityService → databaser.
- **Logs:** vælg datasource `loki` og forespørg fx `{service_name="DraftService"}`. Fold en linje ud og klik på `TraceID` for at springe til tracen i Tempo.

Hvad der må og ikke må logges, står i [docs/logging.md](docs/logging.md).
