# L4 – DraftService: models

Code-level view of `apps/draft_service/src/DraftService/Models/Draft.cs`,
`Handlers/DraftActionResult.cs` and the `ProfanityCheckResult` record in `Profanity/IProfanityClient.cs`.
See the L3 view `DraftServiceComponents` in Structurizr for how the components use these models.

```mermaid
classDiagram
    direction LR

    class Draft {
        <<entity>>
        +long Id
        +string Title
        +string Breadtext
        +string Location
        +long SectionId
        +long CreatedByJournalistId
        +DateTimeOffset CreatedDate
        +long LastEditedByJournalistId
        +DateTimeOffset LastEditedDate
        +long? ApprovedByJournalistId
        +DateTimeOffset? ApprovedDate
        +string[] FlaggedWords
        +DraftStatus Status
        +string? ReviewNote
    }

    class DraftStatus {
        <<enumeration>>
        WorkInProgress = 0
        PendingApproval = 1
        Approved = 2
        Published = 3
        Archived = 4
    }

    class CreateDraftRequest {
        <<request>>
        +long JournalistId
        +string Title
        +string Breadtext
        +string Location
        +long SectionId
    }

    class EditDraftRequest {
        <<request>>
        +long JournalistId
        +string Title
        +string Breadtext
        +string Location
        +long SectionId
    }

    class ApproveDraftRequest {
        <<request>>
        +long JournalistId
        +string? Note
    }

    class RejectDraftRequest {
        <<request>>
        +string? Note
    }

    class DraftActionResult {
        <<record>>
        +DraftActionOutcome Outcome
        +Draft? Draft
        +string? Message
        +Success(Draft draft)$ DraftActionResult
        +NotFound()$ DraftActionResult
        +ValidationFailed(string message)$ DraftActionResult
        +IllegalTransition(string message)$ DraftActionResult
        +ConcurrentChange()$ DraftActionResult
    }

    class DraftActionOutcome {
        <<enumeration>>
        Success
        NotFound
        ValidationFailed
        IllegalTransition
        ConcurrentChange
    }

    class ProfanityCheckResult {
        <<record>>
        +IReadOnlyList~string~ BannedWords
        +bool CircuitOpen
        +bool IsProfane
    }

    class Journalist {
        <<external>>
    }

    class Section {
        <<external>>
    }

    CreateDraftRequest ..> Draft : creates
    EditDraftRequest ..> Draft : updates content
    ApproveDraftRequest ..> Draft : approves
    RejectDraftRequest ..> Draft : rejects
    Draft --> DraftStatus
    DraftActionResult --> DraftActionOutcome
    DraftActionResult o-- "0..1" Draft
    ProfanityCheckResult ..> Draft : BannedWords -> FlaggedWords (on submit)

    Draft "*" --> "1" Journalist : created by
    Draft "*" --> "1" Journalist : last edited by
    Draft "*" --> "0..1" Journalist : approved by
    Draft "*" --> "1" Section : SectionId

    note for Journalist "Owned by ArticleService today. Only referenced by id - DraftService never calls ArticleService. See README: UserService."
```

## Notes

- **Many journalists per draft:** a draft is created, edited and approved by (potentially) different
  journalists. This is not covered by the original design and is one of the reasons behind the
  suggested UserService (see [README](README.md)).
- **`Journalist` / `Section`** (`<<external>>`) are owned by ArticleService. DraftService only stores
  their ids, so nothing enforces that the ids exist.
- **Legal status transitions** between `DraftStatus` values are defined in `Workflow/DraftStatusTransitions.cs`
  (L3 component `DraftStatusTransitions`), not in the model itself.
- **`DraftActionResult`** carries a use-case outcome without depending on ASP.NET Core; `DraftsController`
  maps `DraftActionOutcome` to HTTP status codes.
- **`FlaggedWords`** is filled from ProfanityService on submit. If the circuit is open, the draft is still
  submitted, just without flagged words.
