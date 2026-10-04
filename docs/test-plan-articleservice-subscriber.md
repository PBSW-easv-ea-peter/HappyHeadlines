# Testplan: ArticleService som subscriber

Manuel end-to-end-test af `ArticleQueueConsumer`: at artikler gemmes, at dubletter afvises, at poison-beskeder håndteres, og at circuit breakeren virker pr. shard. Baggrund: [plan-articleservice-subscriber.md](plan-articleservice-subscriber.md).

Consumeren testes isoleret. Vi publicerer beskederne selv via RabbitMQ Management API, så PublishService og DraftService behøver ikke køre.

Unit tests af ack/nack-logikken (`ArticleQueueConsumerTests`) og af hvilke fejl der åbner circuit breakeren (`ShardResilienceTests`) ligger i `apps/article_service/tests/ArticleService.Tests` og køres med `dotnet test apps/article_service/tests/ArticleService.Tests`.

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

Hjælpefunktioner, som genbruges i testene nedenfor:

```bash
# Kort alias for compose-filen
dc() { docker compose -f docker-compose.dev.yaml "$@"; }

# Gyldigt test-GUID ud fra et tal: id 6 -> aaaaaaaa-0000-0000-0000-000000000006
id() { printf 'aaaaaaaa-0000-0000-0000-%012d' "$1"; }

publish() {  # $1 = draftId, $2 = location, $3 = section
  # The payload is a JSON string inside the API's JSON body, hence the escaped quotes (\\\").
  curl -s -u admin:admin -H "content-type: application/json" \
    -X POST http://localhost:15672/api/exchanges/%2F/published_articles/publish \
    -d "{\"properties\":{},\"routing_key\":\"article.published\",\"payload_encoding\":\"string\",\"payload\":\"{\\\"Id\\\":\\\"$1\\\",\\\"DraftId\\\":\\\"$1\\\",\\\"JournalistName\\\":\\\"Test Journalist\\\",\\\"SectionName\\\":\\\"$3\\\",\\\"Title\\\":\\\"Test $2 $1\\\",\\\"Location\\\":\\\"$2\\\",\\\"CreatedDate\\\":\\\"2026-09-30T10:00:00Z\\\",\\\"PublishDate\\\":\\\"2026-09-30T12:00:00Z\\\",\\\"BreadText\\\":\\\"Test body\\\"}\"}"
  echo
}
```

Brug `$(id N)` i stedet for at skrive GUID'et i hånden. En tastefejl giver et ugyldigt GUID, og så bliver beskeden kasseret som poison i stedet for at teste det, den skulle.

Alternativt kan du publicere via Management UI: *Exchanges* → `published_articles` → *Publish message*, med routing key `article.published` og artiklens JSON som payload. Publicér fra exchangens side, ikke køens: køens *Publish message* bruger default exchange og springer bindingen over.

Hvert kald skal svare `{"routed":true}`. Svarer det `false`, er køen ikke bundet endnu. `true` betyder kun, at beskeden landede i køen. Om den blev gemt, ses i loggen.

Shard-containerne har intet `container_name`, så brug compose-navnet (`dc stop articledb-eu`), ikke `docker stop articledb-eu`.

## Testcases

| # | Test | Handling | Forventet |
|---|---|---|---|
| 1 | Gyldig artikel gemmes | `publish $(id 1) EU Politics` og derefter `curl -s http://localhost:8080/api/articles/EU` | Artiklen "Test EU aaaaaaaa-…01" er med i listen. Loggen viser `Stored article for draft … in EU`. |
| 2 | Dublet afvises | Kør samme `publish` som i #1 igen | Loggen viser `already stored - ignoring redelivery`. Artiklen står der stadig kun én gang. Der opstår et hul i artikel-id'erne, fordi `on conflict do nothing` bruger en sekvensværdi. |
| 3 | Ukendt location | `publish $(id 3) XX Politics` | Loggen viser en warning om `Discarding unprocessable message` (Unknown location). Køen er tom bagefter. |
| 4 | Ukendt section | `publish $(id 4) EU Sport` | Samme som #3 (Unknown section). Ingen artikel oprettes. |
| 4b | Poison åbner ikke breakeren | `publish $(id 41) EU Sport`, samme med 42 og 43, derefter `publish $(id 44) EU Politics` | Tre discards, og …0044 gemmes. Ingen `Circuit ArticleShard:EU opened`. |
| 5 | Ugyldig JSON | Management UI: publicér payload `hej` | Warning om `Discarding unprocessable message` (`'h' is an invalid start of a value`). |
| 6 | Et shard nede påvirker ikke de andre | `dc stop articledb-eu`. Derefter `publish $(id 6) EU Politics` og `publish $(id 7) NA Politics` | Loggen viser `Circuit ArticleShard:EU opened. Reason: SocketException` én gang pr. break-periode (30s). Ingen `Failed to store`. NA-artiklen gemmes (`GET /api/articles/NA`). EU-beskeden requeues cirka hvert 5. sekund (redeliver-rate ~0,2/s). |
| 7 | Shardet kommer tilbage | `dc start articledb-eu` og vent op til cirka 35s | Loggen viser `half-open` og derefter `closed`. EU-artiklen …0006 gemmes, og køen er tom. |
| 8 | Crash midt i behandlingen | `dc stop articledb-eu`, `publish $(id 8) EU Politics`, `dc restart articleservice` og derefter `dc start articledb-eu` | Beskeden står som unacked efter genstarten, fordi den aldrig blev ack'et, og bliver gemt, når shardet er tilbage. |
| 9 | Traces | Grafana (http://localhost:3000, admin/admin) → Explore → Tempo → service `ArticleService` | Et deliver-span fra `RabbitMQ.Client.Subscriber` med Npgsql-spans som children (section-opslag og insert). |

**Om #9:** vores egne test-beskeder har ingen `traceparent`, så tracen starter i ArticleService. Den sammenhængende trace fra PublishService kan først testes, når [plan-observability-publishservice.md](plan-observability-publishservice.md) er implementeret. Så publicerer man via PublishService-endpointet i stedet for `publish()`.

## Hvis noget fejler

| Symptom | Sandsynlig årsag |
|---|---|
| `articleservice` genstarter hele tiden | RabbitMQ var ikke klar, eller `RabbitMQ:*`/`OpenTelemetry:Endpoint` mangler i config. Se loggen. |
| `PRECONDITION_FAILED` i loggen | Køen findes allerede med andre arguments. Slet den i Management UI og genstart. |
| `column "draft_id" does not exist` | V4-migrationen er ikke kørt. Tjek med `dc logs articledb-eu-migrate`. |
| `{"routed":false}` | ArticleService er ikke startet endnu, så køen er ikke bundet. |
| `bash: $'\302\226publish': command not found` | Et usynligt kontroltegn er kommet med ved indsættelse. Tryk Ctrl+A og Delete, eller skriv det første ord i hånden. |
| `JsonException … could not be converted to System.Guid` på en gyldig test | Tastefejl i GUID'et. Brug `$(id N)`. |
| `Failed to store message … SocketException` i løkke, og breakeren åbner aldrig | `ShardResilience` håndterer ikke `SocketException`. Rettet 2026-09-30, se resultater nedenfor. |
| `articledb-eu` kører igen efter `dc up --build articleservice` | `depends_on` starter shardet. Stop det igen før test 6. |

## Oprydning

```bash
docker compose -f docker-compose.dev.yaml down
```

Test-artiklerne ligger i article-databaserne indtil deres volumes slettes.

## Resultater 2026-09-30

| # | Resultat | Bemærkning |
|---|---|---|
| – | Bestået | Ekstra: ugyldigt GUID (tastefejl) kasseres som `JsonException`. |
| 1 | Bestået | |
| 2 | Bestået | Id 5 blev sprunget over: dubletten blev fanget af `on conflict (draft_id) do nothing` i databasen. |
| 3 | Bestået | |
| 4 | Bestået | |
| 4b | Bestået | Breakeren tæller kun "shard kan ikke nås"-fejl (`ShardResilience.Configure`). |
| 5 | Bestået | |
| 6 | Fejlet → rettet → bestået | Se fund 1 og 2. |
| 7 | Bestået | |
| 8 | Bestået | |
| 9 | Bestået | |

**Fund 1: breakeren åbnede aldrig, når et shard var nede.** En stoppet container forsvinder fra Dockers DNS, og Npgsql kaster så en rå `SocketException` ("Name or service not known") i stedet for en `NpgsqlException`. `ShouldHandle` håndterede kun transiente `NpgsqlException`, så hverken retry eller breaker reagerede. Beskeden endte i den generelle `catch` og blev requeuet i det uendelige med en error-log hver gang. Den eksisterende unit test `HandleAsync_ShardUnreachable_NacksWithRequeue` antog, at Npgsql altid pakker socket-fejl ind, og testede consumeren med en tom pipeline, så den fangede det ikke.
*Rettelse:* Prædikatet håndterer nu også `SocketException` og `TimeoutException`. Opsætningen er flyttet til `ShardResilience.Configure`, som testes i `ShardResilienceTests`.

**Fund 2: hot loop, mens circuiten var åben.** Efter rettelsen afviste den åbne circuit på under 2 ms, og nack med requeue blev leveret igen med det samme: cirka 250 redeliveries i sekundet og 287.000 loglinjer på under et minut. Polly logger selv hvert kald på info-niveau med stack trace.
*Rettelse:* Consumeren venter 5 sekunder før nack, når circuiten er åben, og `Polly` logger kun fra `Warning`. Målt igen: 45 sekunder med EU nede gav 222 loglinjer og en redeliver-rate på 0,2/s.

## Kendte begrænsninger og videre arbejde

- **Pausen blokerer de andre shards.** Consumeren behandler én besked ad gangen, så 5 sekunders pause pr. EU-besked forsinker også NA og de andre. Løsningen er en retry-kø med `x-message-ttl` og dead-letter tilbage til hovedkøen, så consumeren kan acke med det samme.
- **Poison-beskeder forsvinder.** Køen har ingen dead-letter exchange, så kasserede beskeder kan ikke undersøges eller sendes igen.
- **Ingen alerting.** At circuiten åbner ses kun i loggen. En Grafana-alert på `Circuit … opened` ville gøre et nede shard synligt.
- **`CreatedDate`** gemmes fra beskeden (hvornår draftet blev oprettet), mens seed-artiklerne har indsættelsestidspunktet. Det bør afklares, hvad feltet skal betyde i ArticleService.
- **Npgsql-traces uden forælder.** Tempo viste traces med root `articles` (Npgsql) uden et HTTP-span over sig. Formentlig fra `GET /api/articles`, hvilket tyder på, at ASP.NET Core-instrumentering mangler. Ikke undersøgt.
