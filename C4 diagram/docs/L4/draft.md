# L4 – DraftService: models

Code-level view of `apps/draft_service/src/DraftService/Models/` (`Draft.cs`, `Journalist.cs`),
`Handlers/DraftActionResult.cs` and the `ProfanityCheckResult` record in `Profanity/IProfanityClient.cs`.
See the L3 view `DraftServiceComponents` in Structurizr for how the components use these models.

```mermaid
classDiagram
    direction LR

    class Draft {
        <<entity>>
        +Guid Id
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
        +bool ProfanityCheckSkipped
        +DraftStatus Status
        +string? ReviewNote
        +string Byline
        +List~Journalist~ CreditedJournalists
    }

    class Journalist {
        <<entity>>
        +long Id
        +string Name
        +string? Email
        +string? Phone
    }

    class DraftStatus {
        <<enumeration>>
        WorkInProgress = 0
        PendingApproval = 1
        Approved = 2
        Published = 3
        Archived = 4
    }

    class FilledInDraft {
        <<dto>>
        +Guid Id
        +string JournalistName
        +string SectionName
        +string Title
        +string Location
        +DateTime CreatedDate
        +string BreadText
        +DraftStatus Status
    }

    class CreateDraftRequest {
        <<request>>
        +long JournalistId
        +string Title
        +string Breadtext
        +string Location
        +long SectionId
        +List~long~ CreditedJournalistIds
        +string? Byline
    }

    class EditDraftRequest {
        <<request>>
        +long JournalistId
        +string Title
        +string Breadtext
        +string Location
        +long SectionId
        +List~long~ CreditedJournalistIds
        +string? Byline
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

    class FlagProfanityRequest {
        <<request>>
        +List~string~ FlaggedWords
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
        +bool Unavailable
        +bool IsProfane
    }

    class Section {
        <<external>>
    }

    CreateDraftRequest ..> Draft : creates
    EditDraftRequest ..> Draft : updates content
    ApproveDraftRequest ..> Draft : approves
    RejectDraftRequest ..> Draft : rejects
    FlagProfanityRequest ..> Draft : stores FlaggedWords (from PublisherService)
    Draft ..> FilledInDraft : projected for PublisherService
    Draft --> DraftStatus
    DraftActionResult --> DraftActionOutcome
    DraftActionResult o-- "0..1" Draft
    ProfanityCheckResult ..> Draft : BannedWords -> FlaggedWords (on submit)

    Draft "*" --> "1" Journalist : created by
    Draft "*" --> "1" Journalist : last edited by
    Draft "*" --> "0..1" Journalist : approved by
    Draft "*" --> "*" Journalist : credited (draft_journalists)
    Draft "*" --> "1" Section : SectionId

    note for Section "Names hardcoded in Models/Sections.cs - must match ArticleService's sections table."
```

## Notes

- **`Journalist`** is owned by DraftService (`journalists` table, `V2__add_journalists_and_credited_authors.sql`,
  exposed by `JournalistsController`). A draft can credit several journalists (`draft_journalists`);
  `Byline` is generated from them unless it is set manually.
- **`Id`** is a `Guid`, so the same id can follow the draft into ArticleService as `DraftId`.
- **`FilledInDraft`** is what PublisherService reads (`GET api/drafts/filled-in-draft/{id}`): byline and
  section resolved to names, so PublisherService doesn't need to know DraftService's tables.
- **Legal status transitions** between `DraftStatus` values are defined in `Workflow/DraftStatusTransitions.cs`
  (L3 component `DraftStatusTransitions`), not in the model itself.
- **`DraftActionResult`** carries a use-case outcome without depending on ASP.NET Core; `DraftsController`
  maps `DraftActionOutcome` to HTTP status codes.
- **`FlaggedWords`** is filled from ProfanityService on submit (early feedback for the editor) and by
  PublisherService when its check before publishing finds banned words. If ProfanityService can't answer
  on submit, the draft is still submitted with `ProfanityCheckSkipped = true`, so the editor is warned.
