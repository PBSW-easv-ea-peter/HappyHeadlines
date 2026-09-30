---
course: Development of Large Systems
type: service-contract
week: 40
company: Happy Headlines
service: PublishService
---

# PublishService

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** Tager et draft og sender det ud som en publiceret artikel på message brokeren (RabbitMQ).
- **Ejer data:** Ingen.
- **Commands:** Publicér et draft.
- **Queries:** –
- **Publicerer:** `PublishedArticleEvent` på topic-exchange `published_articles`, routing key `article.published`.
- **Afhænger af:** DraftService (`GET /api/drafts/filled-in-draft/{id}`) → titel, brødtekst, location, journalist, sektion, oprettelsesdato.
- **Afvigelse:** Ingen consumer på exchangen endnu. Mangler i `docker-compose.yaml`. Opdaterer ikke draftets status i DraftService.

## Endpoint-map

Base-URL (lokalt): `http://localhost:8084`

| Type  | Metode   | Route / Emne                           | Beskrivelse |
|-------|----------|----------------------------------------|-------------|
| REST  | POST     | /api/publish-draft/{id}                | Hent draft og publicér event. 404 hvis ukendt, 503 hvis DraftService er nede |
| Queue | publish  | `published_articles` / `article.published` | `PublishedArticleEvent` (Id, DraftId, JournalistName, SectionName, Title, Location, CreatedDate, PublishDate, BreadText) |
