---
course: Development of Large Systems
type: service-contract
week: 40
company: Happy Headlines
service: ArticleService
---

# ArticleService

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** Gemmer og udstiller publicerede artikler, shardet pr. kontinent (z-axis) og kørt i 3 replicas (x-axis).
- **Ejer data:** ArticleDatabase × 8 (`EU, NA, SA, AU, AS, AN, AF, GO`) → `articles`, `sections`, `journalists`, `photos`.
- **Commands:** Opret, opdatér og slet artikel.
- **Queries:** Alle artikler for en location, én artikel pr. id.
- **Publicerer:** –
- **Afhænger af:** RabbitMQ – abonnerer på `published_articles` / `article.published` fra PublishService.
- **Fault isolation:** Circuit breaker pr. shard (`Resilience/ShardResilience.cs`). Er et shard nede, requeues dets beskeder, mens de øvrige shards skriver videre. Retry-kø er ikke implementeret – se [plan-articleservice-subscriber.md](../plan-articleservice-subscriber.md).
- **Afvigelse:** REST-endpoints for Create/Update/Delete findes stadig ved siden af køen og går ikke gennem circuit breakeren.

## Endpoint-map

Base-URL (lokalt): `http://localhost:8080` · `{location}` ∈ `EU|NA|SA|AU|AS|AN|AF|GO`

| Type  | Metode   | Route / Emne                            | Beskrivelse |
|-------|----------|-----------------------------------------|-------------|
| REST  | GET      | /api/articles/{location}                | Alle artikler i location |
| REST  | GET      | /api/articles/{location}/{id}           | Én artikel (404 hvis ukendt) |
| REST  | POST     | /api/articles/{location}                | Opret artikel |
| REST  | PUT      | /api/articles/{location}/{id}           | Opdatér artikel |
| REST  | DELETE   | /api/articles/{location}/{id}           | Slet artikel |
| Queue | consume  | `article_service.published_articles` ← `published_articles` / `article.published` | Gemmer artiklen i shard for `Location`. Idempotent på `DraftId` (`draft_id UNIQUE`, V4). Ack efter skrivning. |

Uddybning: [article_service.md](../article_service.md)
