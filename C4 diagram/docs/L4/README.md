# C4 Level 4 – Code (model layer)

Structurizr DSL does not model the code level, so L4 is written as Mermaid class diagrams.
They cover the **model layer** of the implemented services; controllers, handlers and
repositories are already covered by the L3 component views.

| Service | L4 | L3 view in Structurizr |
|---|---|---|
| ArticleService | [article.md](article.md) | `ArticleServiceComponents` |
| CommentService | [comment.md](comment.md) | `CommentServiceComponents` |
| DraftService | [draft.md](draft.md) | `DraftServiceComponents` |
| ProfanityService | – (see below) | `ProfanityServiceComponents` |

## Stereotypes

| Stereotype | Meaning |
|---|---|
| `<<prepared>>` | Exists in code but is not used yet – prepared for later services. |
| `<<external>>` | Owned by another service; only referenced by id. |
| `<<entity>>` / `<<dto>>` / `<<request>>` / `<<read model>>` | Persisted row / API response / API input / query projection. |

## ProfanityService

The model layer is a single class, `ProfanityCheckRequest { string Text }`, so it has no diagram.
The service returns a plain `List<string>` of banned words found in the text; callers wrap it in
their own `ProfanityCheckResult` (see [comment.md](comment.md) and [draft.md](draft.md)).

## Missing: UserService (suggested)

The original design has no service that owns users. Today `Journalist` lives in ArticleService
(`<<prepared>>`, unused), and DraftService references journalists by id only.

A **UserService** is suggested (tagged `Suggested` in the Structurizr model) owning the user types:

- **User** – reader who comments and subscribes
- **Journalist** – writes and edits drafts
- **Publisher** – approves and publishes articles

**Why:**
- DraftService assumes *many* journalists per article (created by, last edited by, approved by),
  which the original design does not account for.
- DraftService stores journalist ids that are owned by ArticleService, without ever calling it –
  nothing guarantees the ids are valid.

**Open question:** in L1, *Publisher* is a separate person, but in DraftService a *journalist*
approves drafts (`ApprovedByJournalistId`). Are Publisher and Journalist separate user types,
or is Publisher a journalist with extra permissions (one user with roles)?
