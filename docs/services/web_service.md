---
course: Development of Large Systems
type: service-contract
week: 40
company: Happy Headlines
service: HappyHeadlines Web
---

# HappyHeadlines Web

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** Blazor-frontend til læsere (artikler, kommentarer) og journalister/redaktører (drafts-workflow).
- **Ejer data:** Ingen.
- **Commands / Queries:** Udstiller ingen API – er kun klient.
- **Publicerer:** –
- **Afhænger af:** ArticleService, CommentService, DraftService (se nedenfor).
- **Afvigelse:** Kalder PublishService ikke. Service-URL'er er hardcodet til `localhost`.

## Kalder

Serveres på `http://localhost:8000`

| Service        | Metode | Route |
|----------------|--------|-------|
| ArticleService | GET    | /api/articles/{location} |
| CommentService | GET    | /api/comments/{location}/{articleId} |
| DraftService   | GET    | /api/drafts, /api/drafts/{id}, /api/journalists |
| DraftService   | POST   | /api/drafts |
| DraftService   | PUT    | /api/drafts/{id} |
| DraftService   | POST   | /api/drafts/{id}/submit-for-approval, /approve, /reject, /reactivate |
