---
course: Development of Large Systems
type: service-contract
week: 40
company: Happy Headlines
service: ProfanityService
---

# ProfanityService

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** Tjekker en tekst mod listen af forbudte ord i ét kald.
- **Ejer data:** ProfanityDatabase → `banned_words`.
- **Commands:** –
- **Queries:** Hvilke forbudte ord indeholder denne tekst?
- **Publicerer:** –
- **Afhænger af:** Ingen.
- **Afvigelse:** –

## Endpoint-map

Base-URL (lokalt): `http://localhost:8081`

| Type | Metode | Route / Emne          | Beskrivelse |
|------|--------|-----------------------|-------------|
| REST | POST   | /api/profanity/check  | Body `{ "text": "..." }` → liste af fundne forbudte ord |

Kaldes af: CommentService, DraftService.

Uddybning: [comment_and_profanity_service.md](../comment_and_profanity_service.md)
