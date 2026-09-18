# HappyHeadlines

# Applications

Denne mappe indeholder alle applikationer og services i **HappyHeadlines**-projektet.

Projektet er struktureret, så hver individuel service har sin egen mappe under `apps/`.

## Projektstruktur

Den overordnede struktur er:

```text
apps/
├── HappyHeadlines.slnx
│
├── article_service/
│   ├── ArticleService.slnx
│   ├── Dockerfile
│   ├── .dockerignore
│   ├── src/
│   │   └── ArticleService/
│   │       └── ArticleService.csproj
│   └── tests/
│       └── ArticleService.Tests/
│           └── ArticleService.Tests.csproj
│
├── comment_service/
│   ├── CommentService.slnx
│   ├── Dockerfile
│   ├── .dockerignore
│   ├── src/
│   │   └── CommentService/
│   │       └── CommentService.csproj
│   └── tests/
│       └── CommentService.Tests/
│           └── CommentService.Tests.csproj
│
├── profanity_service/
│   └── ...
│
└── happy-headlines-web_service/
    └── ...
```

Hver service er isoleret i sin egen mappe og følger samme overordnede struktur.

## Solution files

Der findes en samlet solution for applikationerne:

```text
apps/HappyHeadlines.slnx
```

Derudover har de enkelte services aktuelt deres egen solution-fil:

```text
apps/article_service/ArticleService.slnx
apps/comment_service/CommentService.slnx
```

De individuelle solution-filer gør det muligt at arbejde med en enkelt service isoleret fra resten af projektet.

Det er endnu ikke besluttet, om de individuelle solution-filer skal beholdes permanent.

## Naming conventions

Følgende konventioner anvendes:

| Type                | Convention       | Eksempel                        |
| ------------------- | ---------------- | ------------------------------- |
| Service             | `[name]_service` | `article_service`               |
| Source directory    | `src/`           | `article_service/src/`          |
| Test directory      | `tests/`         | `article_service/tests/`        |
| Application project | `[Name]`         | `ArticleService`                |
| Test project        | `[Name].Tests`   | `ArticleService.Tests`          |
| Dockerfile          | `Dockerfile`     | `article_service/Dockerfile`    |
| Docker ignore       | `.dockerignore`  | `article_service/.dockerignore` |
| Service solution    | `[Name].slnx`    | `ArticleService.slnx`           |

## Nye services

Når en ny service oprettes, bør den følge samme struktur:

```text
apps/
└── new_service/
    ├── Dockerfile
    ├── .dockerignore
    ├── NewService.slnx
    ├── src/
    │   └── NewService/
    │       └── NewService.csproj
    └── tests/
        └── NewService.Tests/
            └── NewService.Tests.csproj
```