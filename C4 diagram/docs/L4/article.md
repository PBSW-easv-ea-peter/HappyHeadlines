# L4 – ArticleService: models

Code-level view of the model layer in `apps/article_service/src/ArticleService/Models/Article.cs`.
See the L3 view `ArticleServiceComponents` in Structurizr for how the components use these models.

```mermaid
classDiagram
    direction LR

    class Article {
        <<read model>>
        +long Id
        +string JournalistName
        +string Title
        +string Breadtext
        +DateTimeOffset CreatedDate
        +DateTimeOffset? PublishDate
        +string Location
        +string SectionName
    }

    class UpsertArticleRequest {
        <<request>>
        +long JournalistId
        +string Title
        +string Breadtext
        +DateTimeOffset? PublishDate
        +long SectionId
    }

    class ArticleX {
        <<prepared>>
        +long Id
        +long JournalistId
        +string Title
        +string Breadtext
        +DateTimeOffset CreatedDate
        +DateTimeOffset? PublishDate
        +string Location
        +long SectionId
    }

    class Journalist {
        <<prepared>>
        +long Id
        +string Name
        +string Operations
    }

    class Section {
        <<prepared>>
        +long Id
        +string Name
    }

    UpsertArticleRequest ..> ArticleX : creates / updates row
    ArticleX "*" --> "1" Journalist : JournalistId
    ArticleX "*" --> "1" Section : SectionId
    Article ..> ArticleX : projection of (joined with journalists + sections)

    note for Journalist "Owned by ArticleService until a UserService exists (see README)."
```

## Notes

- **`Article`** is what the API returns. `ArticleReadRepository` builds it by joining the
  `articles`, `journalists` and `sections` tables, so journalist and section are exposed by name.
- **`ArticleX`, `Journalist`, `Section`** (`<<prepared>>`) mirror the database tables but are not
  used in code yet – the repositories map straight into `Article`. They exist so the model is ready
  when other services start depending on journalists and sections.
- **`Location`** is the shard key (z-axis split): it decides which continent database the article lives in.
