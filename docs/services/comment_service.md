---
course: Development of Large Systems
type: service-contract
week: 40
company: Happy Headlines
service: CommentService
---

# CommentService

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** Modtager og udstiller kommentarer til artikler; kun godkendte kommentarer vises.
- **Ejer data:** CommentDatabase → `comments`, `status`.
- **Commands:** Post en kommentar på en artikel.
- **Queries:** Godkendte kommentarer for en artikel.
- **Publicerer:** –
- **Afhænger af:** ProfanityService (`POST /api/profanity/check`) → liste af forbudte ord. Kaldt direkte bag en circuit breaker.
- **Afvigelse:** –

## Endpoint-map

Base-URL (lokalt): `http://localhost:8082` · `{articleId}` er artiklens GUID (`comments.article_id UUID`, ingen FK – artiklen ligger i ArticleServices shard for `{location}`)

| Type | Metode | Route / Emne                              | Beskrivelse |
|------|--------|-------------------------------------------|-------------|
| REST | GET    | /api/comments/{location}/{articleId}      | Godkendte kommentarer for artikel |
| REST | POST   | /api/comments/{location}/{articleId}      | Post kommentar. 422 hvis ProfanityService er nede |

Uddybning: [comment_and_profanity_service.md](../comment_and_profanity_service.md)
