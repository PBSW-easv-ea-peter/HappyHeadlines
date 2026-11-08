---
course: Development of Large Systems
type: service-contract
week: 40
company: Happy Headlines
service: HappyHeadlines Web
---

# HappyHeadlines Web

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** To Blazor WebAssembly-apps, der deler sider fra `HappyHeadlinesPages.web`: `HappyHeadlines.web.wasm` til journalister/redaktører (drafts-workflow) og `HappyHeadlines.reader.wasm` til læsere (artikler, kommentarer).
- **Ejer data:** Ingen.
- **Commands / Queries:** Udstiller ingen API – er kun klient.
- **Publicerer:** –
- **Afhænger af:** ArticleService, CommentService, DraftService, PublishService (se nedenfor).
- **Afvigelse:** Service-URL'er er hardcodet til `localhost`.

## Kalder

Redaktionsappen serveres på `http://localhost:8000` (`Dockerfile`), læserappen på `http://localhost:8001` (`Dockerfile.reader`).

| Service        | Metode | Route |
|----------------|--------|-------|
| ArticleService | GET    | /api/articles/{location}, /api/articles/{location}/{id} |
| CommentService | GET    | /api/comments/{location}/{articleId} |
| CommentService | POST   | /api/comments/{location}/{articleId} |
| DraftService   | GET    | /api/drafts, /api/drafts/{id}, /api/journalists |
| DraftService   | POST   | /api/drafts |
| DraftService   | PUT    | /api/drafts/{id} |
| DraftService   | POST   | /api/drafts/{id}/submit-for-approval, /approve, /reject, /reactivate |
| PublishService | POST   | /api/v1/publish-draft/{id} |
