---
course: Development of Large Systems
type: service-contract
week: 40
company: Happy Headlines
service: PublishService
---

# PublishService

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** Tager et godkendt draft, lægger det i køen som en publiceret artikel (RabbitMQ) og markerer draftet som publiceret.
- **Ejer data:** Ingen.
- **Commands:** Publicér et draft (kun status `Approved`).
- **Queries:** –
- **Publicerer:** `PublishedArticleEvent` på topic-exchange `published_articles`, routing key `article.published`. Med publisher confirms, `mandatory` og persistent besked – fejler publiceringen, får klienten 503, og draftet forbliver `Approved`.
- **Afhænger af:** DraftService (`GET /api/drafts/filled-in-draft/{id}` → titel, brødtekst, location, byline, sektionsnavn, oprettelsesdato, status; `POST /api/drafts/{id}/publish` → status `Published`). RabbitMQ.
- **Rækkefølge:** Eventet lægges i køen *før* draftet markeres. Fejler markeringen, kan man publicere igen – ArticleService ignorerer et ekstra event for samme `DraftId`.
- **Forbrugere:** ArticleService (kø `article_service.published_articles`) gemmer artiklen i sin shard.

## Endpoint-map

Base-URL (lokalt): `http://localhost:8084`

| Type  | Metode   | Route / Emne                           | Beskrivelse |
|-------|----------|----------------------------------------|-------------|
| REST  | POST     | /api/v1/publish-draft/{id}             | Publicér et godkendt draft. 202 med `{ draftId, eventId, publishDate }`, 404 hvis ukendt, 409 hvis ikke `Approved`, 503 hvis DraftService eller RabbitMQ er nede |
| Queue | publish  | `published_articles` / `article.published` | `PublishedArticleEvent` (Id, DraftId, JournalistName, SectionName, Title, Location, CreatedDate, PublishDate, BreadText) |
