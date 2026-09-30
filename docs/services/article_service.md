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
- **Afhænger af:** Ingen i dag. (Planlagt: `published_articles` / `article.published` fra PublishService.)
- **Afvigelse:** `ArticleQueueConsumer` er en tom stub – Create/Update sker via REST, ikke via kø som beskrevet i arkitekturen.

## Endpoint-map

Base-URL (lokalt): `http://localhost:8080` · `{location}` ∈ `EU|NA|SA|AU|AS|AN|AF|GO`

| Type  | Metode   | Route / Emne                            | Beskrivelse |
|-------|----------|-----------------------------------------|-------------|
| REST  | GET      | /api/articles/{location}                | Alle artikler i location |
| REST  | GET      | /api/articles/{location}/{id}           | Én artikel (404 hvis ukendt) |
| REST  | POST     | /api/articles/{location}                | Opret artikel |
| REST  | PUT      | /api/articles/{location}/{id}           | Opdatér artikel |
| REST  | DELETE   | /api/articles/{location}/{id}           | Slet artikel |
| Queue | consume  | *(ikke implementeret)*                  | Stub – logger kun ved opstart |

Uddybning: [article_service.md](../article_service.md)
