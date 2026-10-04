---
course: Development of Large Systems
type: service-contract
week: 40
company: Happy Headlines
service: DraftService
---

# DraftService

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** Håndterer udkast og deres workflow: WorkInProgress → PendingApproval → Approved → Published (+ Archived/Reactivate).
- **Ejer data:** DraftDatabase → `drafts`, `status`, `journalists`, `draft_journalists`.
- **Commands:** Opret/redigér draft, submit, approve, reject, publish, archive, reactivate.
- **Queries:** Alle drafts (evt. filtreret på `createdBy`), én draft, "filled-in" draft til publicering, alle journalister.
- **Publicerer:** –
- **Afhænger af:** ProfanityService (`POST /api/profanity/check`) ved submit-for-approval → markerede ord. Fortsætter uden, hvis circuit er åben.
- **Afvigelse:** Profanity-tjek ligger her i stedet for i PublishService. Sektionsnavne er hardkodet i `Models/Sections.cs` og skal matche ArticleServices `sections`-tabel.

## Endpoint-map

Base-URL (lokalt): `http://localhost:8083`

| Type | Metode | Route / Emne                              | Beskrivelse |
|------|--------|-------------------------------------------|-------------|
| REST | GET    | /api/drafts?createdBy={id}                | Alle drafts (filter valgfrit) |
| REST | GET    | /api/drafts/{id}                          | Én draft |
| REST | GET    | /api/drafts/filled-in-draft/{id}          | Draft i format til PublishService (byline, sektionsnavn, status). 422 ved ukendt sektion |
| REST | POST   | /api/drafts                               | Opret draft |
| REST | PUT    | /api/drafts/{id}                          | Redigér indhold (kun WorkInProgress) |
| REST | POST   | /api/drafts/{id}/submit-for-approval      | WorkInProgress → PendingApproval (+ profanity-tjek) |
| REST | POST   | /api/drafts/{id}/approve                  | PendingApproval → Approved |
| REST | POST   | /api/drafts/{id}/reject                   | PendingApproval → WorkInProgress |
| REST | POST   | /api/drafts/{id}/publish                  | Approved → Published |
| REST | POST   | /api/drafts/{id}/archive                  | → Archived |
| REST | POST   | /api/drafts/{id}/reactivate               | Archived → WorkInProgress |
| REST | GET    | /api/journalists                          | Alle journalister |

Ugyldige statusskift giver `409 Conflict`.
