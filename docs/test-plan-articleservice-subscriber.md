# Testplan: ArticleService som subscriber

Manuel end-to-end-test af `ArticleQueueConsumer`: at artikler gemmes, at dubletter afvises, at poison-beskeder håndteres, og at circuit breakeren virker pr. shard. Baggrund: [plan-articleservice-subscriber.md](plan-articleservice-subscriber.md).

Consumeren testes isoleret. Vi publicerer beskederne selv via RabbitMQ Management API, så PublishService og DraftService behøver ikke køre.

Unit tests af ack/nack-logikken ligger i `apps/article_service/tests/ArticleService.Tests` og køres med `dotnet test apps/article_service/tests/ArticleService.Tests`.

## Forberedelse

Kør alle kommandoer fra repo-roden i Git Bash.

```bash
# Start ArticleService. Den trækker selv shards, migrations (inkl. V4) og RabbitMQ med via depends_on.
docker compose -f docker-compose.dev.yaml up -d --build articleservice otel-collector tempo loki grafana

# Følg loggen i et separat vindue
docker compose -f docker-compose.dev.yaml logs -f articleservice
```

**Forventet i loggen:** `Consuming article_service.published_articles`.

**Tjek i RabbitMQ Management** (http://localhost:15672, admin/admin): under *Queues* står `article_service.published_articles` med 1 consumer. Under *Bindings* er køen bundet til `published_articles` med `article.published`.

Hjælpefunktion til at publicere, som genbruges i testene nedenfor:

```bash
publish() {  # $1 = draftId, $2 = location, $3 = section
  # The payload is a JSON string inside the API's JSON body, hence the escaped quotes (\\\").
  curl -s -u admin:admin -H "content-type: application/json" \
    -X POST http://localhost:15672/api/exchanges/%2F/published_articles/publish \
    -d "{\"properties\":{},\"routing_key\":\"article.published\",\"payload_encoding\":\"string\",\"payload\":\"{\\\"Id\\\":\\\"$1\\\",\\\"DraftId\\\":\\\"$1\\\",\\\"JournalistName\\\":\\\"Test Journalist\\\",\\\"SectionName\\\":\\\"$3\\\",\\\"Title\\\":\\\"Test $2 $1\\\",\\\"Location\\\":\\\"$2\\\",\\\"CreatedDate\\\":\\\"2026-09-30T10:00:00Z\\\",\\\"PublishDate\\\":\\\"2026-09-30T12:00:00Z\\\",\\\"BreadText\\\":\\\"Test body\\\"}\"}"
  echo
}
```

Alternativt kan du publicere via Management UI: *Exchanges* → `published_articles` → *Publish message*, med routing key `article.published` og artiklens JSON som payload.

Hvert kald skal svare `{"routed":true}`. Svarer det `false`, er køen ikke bundet endnu.

## Testcases

`…0006` er en forkortelse for `aaaaaaaa-0000-0000-0000-000000000006`. DraftId skal være et gyldigt GUID.

| # | Test | Handling | Forventet |
|---|---|---|---|
| 1 | Gyldig artikel gemmes | `publish aaaaaaaa-0000-0000-0000-000000000001 EU Politics` og derefter `curl -s http://localhost:8080/api/articles/EU` | Artiklen "Test EU aaaaaaaa-…01" er med i listen. Loggen viser `Stored article for draft … in EU`. |
| 2 | Dublet afvises | Kør samme `publish` som i #1 igen | Loggen viser `already stored - ignoring redelivery`. Artiklen står der stadig kun én gang. |
| 3 | Ukendt location | `publish aaaaaaaa-0000-0000-0000-000000000003 XX Politics` | Loggen viser en warning om `Discarding unprocessable message` (Unknown location). Køen er tom bagefter. |
| 4 | Ukendt section | `publish aaaaaaaa-0000-0000-0000-000000000004 EU Sport` | Samme som #3 (Unknown section). Ingen artikel oprettes. |
| 5 | Ugyldig JSON | Management UI: publicér payload `hej` | Warning om `Discarding unprocessable message` (JsonException). |
| 6 | Et shard nede påvirker ikke de andre | `docker stop articledb-eu`. Derefter `publish …0006 EU Politics` og `publish …0007 NA Politics` | Loggen viser `Circuit ArticleShard:EU opened` **én gang**. NA-artiklen gemmes (`GET /api/articles/NA`). EU-beskeden står i køen og ryger frem og tilbage (Management UI viser en høj *redeliver*-rate). |
| 7 | Shardet kommer tilbage | `docker start articledb-eu` og vent op til cirka 30s | Loggen viser `half-open` og derefter `closed`. EU-artiklen …0006 gemmes, og køen er tom. |
| 8 | Crash midt i behandlingen | `docker stop articledb-eu`, `publish …0008 EU Politics`, `docker restart <articleservice-container>` og derefter `docker start articledb-eu` | Beskeden overlever genstarten, fordi den aldrig blev ack'et, og bliver gemt, når shardet er tilbage. |
| 9 | Traces | Grafana (http://localhost:3000) → Explore → Tempo → service `ArticleService` | Et deliver-span fra `RabbitMQ.Client.Subscriber` med et Npgsql-span som child. |

**Om #9:** vores egne test-beskeder har ingen `traceparent`, så tracen starter i ArticleService. Den sammenhængende trace fra PublishService kan først testes, når [plan-observability-publishservice.md](plan-observability-publishservice.md) er implementeret. Så publicerer man via PublishService-endpointet i stedet for `publish()`.

## Hvis noget fejler

| Symptom | Sandsynlig årsag |
|---|---|
| `articleservice` genstarter hele tiden | RabbitMQ var ikke klar, eller `RabbitMQ:*`/`OpenTelemetry:Endpoint` mangler i config. Se loggen. |
| `PRECONDITION_FAILED` i loggen | Køen findes allerede med andre arguments. Slet den i Management UI og genstart. |
| `column "draft_id" does not exist` | V4-migrationen er ikke kørt. Tjek med `docker compose -f docker-compose.dev.yaml logs articledb-eu-migrate`. |
| `{"routed":false}` | ArticleService er ikke startet endnu, så køen er ikke bundet. |

## Oprydning

```bash
docker compose -f docker-compose.dev.yaml down
```

Test-artiklerne ligger i article-databaserne indtil deres volumes slettes.
