---
course: Development of Large Systems
type: architecture-doc
week: 37
company: Happy Headlines
service: CommentService & ProfanityService
---

# CommentService & ProfanityService — arkitektur

## Ansvar

- **CommentService** eksponerer en REST-API til at poste og hente kommentarer på artikler
  og gemmer dem i CommentDatabase.
- **ProfanityService** eksponerer en REST-API til at tjekke en hel kommentartekst i ét
  kald mod en liste af forbudte ord, opslået i ProfanityDatabase.

Kravet for uge 37 er, at de to services er fault-isolerede efter swim lane-principperne
(kap. 21), at CommentService kalder ProfanityService **direkte** (ingen gateway eller UI
som mellemled), og at CommentService har en circuit breaker, der overtager, hvis
ProfanityService ikke er tilgængelig.

## Lagdeling

Begge services følger et 3-lags mønster, Controller → orkestrering → Repository, så
controlleren udelukkende står for HTTP-bindings (route, statuskoder):

**CommentService:**
```
CommentsController (HTTP only)
    -> CommentHandler (orkestrering: ét klassificeringskald pr. kommentar)
        -> IProfanityClient (Polly retry + circuit breaker)
        -> ICommentRepository
```

**ProfanityService:**
```
ProfanityController (HTTP only)
    -> IProfanityChecker (splitter/dedupliker teksten, opslagslogik)
        -> IProfanityRepository (matcher flere ord i ét DB-kald)
```

API-kontrakten er adskilt fra persisteringsmodellen: `CommentDto` (API) og `CommentEntity`
(database) er to separate klasser i `CommentService/Models/`, mappet via
`CommentDto.FromEntity`. `PostCommentRequest` er request-modellen for `POST`.

## Fault isolation & circuit breaker-design

CommentService og ProfanityService er hver deres swim lane: egen container, egen database,
egen fejl-domæne (Princip 1 — intet deles). Ingen af dem deler database eller proces med
ArticleService.

Der er dog én ting, der ikke passer ind i den rene teori: **Princip 2** (kap. 21) siger, at
intet bør krydse en swim lane-grænse synkront. Kaldet fra `CommentHandler` til
ProfanityService er netop synkront — det er et direkte HTTP POST-kald, ikke en kø. Det er
et bevidst brud, fordi opgaveteksten kræver direkte kommunikation uden mellemled, og fordi
en asynkron "fire and forget"-besked ikke giver mening her: CommentService skal bruge
svaret (er ordet forbudt eller ej), før den kan afgøre, om kommentaren må vises.

Circuit breakeren er den kompensering, som VMware-artiklen ("Should That Be a Microservice
- Part 5: Failure Isolation") peger på i præcis den situation: når et synkront kald på
tværs af en grænse ikke kan undgås, er en circuit breaker det værktøj, der forhindrer en
fejlende ProfanityService i at brede sig (cascading failure) til CommentService.

`ProfanityClient` (i `CommentService/Profanity/`) bruger Polly til at kombinere to
policies, sammensat med `WrapAsync`:

1. **Retry**: op til 2 forsøg med kort backoff (200 ms, 400 ms) for at overleve et
   kortvarigt netværksudsving.
2. **Circuit breaker**: efter 3 fejl i træk "tripper" kredsløbet og blokerer alle kald til
   ProfanityService i 30 sekunder.

Derudover har `HttpClient` en 2-sekunders timeout, så et hængende kald til ProfanityService
aldrig kan blokere CommentService's egen swim lane på ubestemt tid.

### Hvad sker der, når kredsløbet er åbent?

Vi har bevidst valgt **ikke** at fejle hele kommentar-postningen, hvis ProfanityService er
nede. I stedet sætter `CommentHandler.ClassifyAsync` status til `PendingProfanityCheck` i
stedet for `Approved`/`Rejected`. `GET /api/comments/{location}/{articleId}` returnerer
kun `Approved`-kommentarer, så en ikke-verificeret kommentar aldrig vises til læserne, mens
CommentService selv forbliver oppe og fortsætter med at modtage kommentarer. Det er en
konkret anvendelse af **Design to be disabled** (kun *visningen* af nye, endnu ikke tjekkede
kommentarer "slukkes") og **Isolate faults** (en fejlende ProfanityService rammer aldrig
andet end verificeringen af nye kommentarer).

### HTTP-respons ved POST

`POST /api/comments/{location}/{articleId}` returnerer:

- **`201 Created`** — kommentaren er gemt, uanset om den blev klassificeret som
  `Approved` eller `Rejected`. Begge tilfælde er et teknisk vellykket kald; klienten kan se
  forskellen på `comment.Status` i response-body. En `Rejected`-kommentar er altså gemt,
  men flagget — ikke afvist.
- **`422 Unprocessable Entity`** — reserveret til når selve kaldet til ProfanityService
  teknisk ikke gik igennem (circuit breaker åben/timeout, status
  `PendingProfanityCheck`). Kommentaren gemmes stadig med denne status (og forbliver skjult
  via `GetApprovedAsync`), men HTTP-svaret signalerer at klienten ikke kan stole på at
  kommentaren er blevet profanitetstjekket.

## Data model & article-reference

ArticleService er Z-akse-shardet per kontinent (8 uafhængige databaser, hver med egen
identity-sekvens) — `article_id` alene er derfor **ikke globalt unikt**. En kommentar
gemmer derfor både `article_id` og `article_location`, så den entydigt kan spores til den
rigtige artikel, uanset hvilken shard den kom fra. `location` valideres i
`CommentsController` med samme `ValidLocations`-mønster som `ArticlesController` i
ArticleService (`EU, NA, SA, AU, AS, AN, AF, GO`) — samme kendte duplikation som i
ArticleService selv, konsolidering er ude af scope.

**CommentDatabase** (`comments`): `id`, `article_id`, `article_location`,
`author_name VARCHAR(25)`, `text VARCHAR(500)`, `created_date`, `status` (smallint:
0 = Approved, 1 = PendingProfanityCheck, 2 = Rejected).

**ProfanityDatabase** (`banned_words`): `id`, `word` (unik, case-insensitivt opslag via
`lower()`).

### REST API

| Service | Metode | Rute | Beskrivelse |
|---|---|---|---|
| CommentService | GET | `/api/comments/{location}/{articleId}` | Hent godkendte kommentarer på en artikel |
| CommentService | POST | `/api/comments/{location}/{articleId}` | Post en kommentar på en artikel |
| ProfanityService | POST | `/api/profanity/check` | Tjek en hel tekst; returnerer listen af matchede forbudte ord (tom liste = ren tekst) |

Ingen DELETE-endpoint: hverken opgaveteksten (`docs/Tredje uge.md`) eller underviserens
eget diagram (`docs/week37-fault-isolation-diagram.png`) kræver sletning af kommentarer —
kun "posting" og "requesting".

**Hvorfor POST og ikke GET eller PUT på ProfanityService?** Kaldet er en
beregning/handling ("tjek denne tekst"), ikke en oprettelse eller erstatning af en
ressource. GET ville kræve teksten som query-parameter, hvilket er upraktisk for en hel
sætning/kommentar — en af grundene til at endpointet blev udvidet fra ét ord til hele
kommentarteksten i ét kald.

## Tests

`CommentService.Tests` og `ProfanityService.Tests` er unit-testprojekter (xUnit + Moq),
sidestillet med de to services i solutionen:

- **`CommentHandlerTests`**: klassificeringslogikken (`Approved`/`Rejected`/
  `PendingProfanityCheck`), inkl. en eksplicit verifikation af at `IProfanityClient`
  kaldes **én gang med hele teksten** pr. kommentar, ikke ét kald pr. ord.
- **`CommentsControllerTests`**: location-/felt-validering og HTTP-statusmapping (`201`
  for både `Approved` og `Rejected`, `422` for `PendingProfanityCheck`).
- **`ProfanityCheckerTests`**: ord-splitning/dedup (case-insensitiv) og at
  repository-resultatet sendes videre uændret.
- **`ProfanityControllerTests`**: tom-tekst-validering og pass-through af resultatet.

Scope er bevidst afgrænset til unit-tests: alle afhængigheder (`ICommentRepository`,
`IProfanityClient`, `IProfanityRepository`) er mocket, så testene kører uden ægte
Postgres eller HTTP-kald og uden Docker. `CommentRepository`/`ProfanityRepository`
(rå Dapper/SQL) og cross-service-flowet er derfor **ikke** dækket — det ville kræve
integrationstests. Kør med `dotnet test CommentService.Tests` /
`dotnet test ProfanityService.Tests` fra `HappyHeadlines`-mappen.

## Kendt gæld

- **Ingen reconciliation for `PendingProfanityCheck`:** intet baggrundsjob genforsøger i
  dag kommentarer, der blev sat i venteposition, mens circuit breakeren var åben.
- **Ingen admin-endpoints for `banned_words`:** listen er seedet med 3 placeholder-ord
  (`idiot`, `stupid`, `dumb`) og kan ikke vedligeholdes uden en ny deployment.
- **Ingen auth/autorisation:** ingen af endpoints kræver godkendelse — hvem som helst kan
  poste under vilkårligt forfatternavn.
- **Kun unit-tests, ingen integrationstests:** `CommentService.Tests`/
  `ProfanityService.Tests` dækker handler-/checker-/controller-logik med mocks (se
  "Tests" ovenfor), men `CommentRepository`/`ProfanityRepository` og det faktiske
  HTTP-flow mellem de to services er utestet. ArticleService har fortsat ingen tests
  overhovedet.
- **`C4 diagram/docs/workspace.dsl`** indeholder stadig relationen
  `publisherService -> profanityService`, som ikke er implementeret — kun CommentService
  kalder i praksis ProfanityService. Opdateres i en separat, kommende omgang.

## Deployment

Ligesom ArticleService kører CommentService og ProfanityService via Docker Swarm
(`docker stack deploy`), ikke almindelig `docker compose up` — hele stacken deles med
ArticleService i samme `happyheadlines`-stack.

- `commentservice` publiceres på host-port `8083` (ændret fra oprindeligt `8081`, som var
  optaget af en lokal Structurizr-instans), `profanityservice` på `8082`.
- Hver service har sin egen Postgres-database (`commentdb`, `profanitydb`), initialiseret
  via `database/init/comment/comment_baseline.sql` og
  `database/init/profanity/profanity_baseline.sql` (seed-data i `database/queries/`).
- `commentservice` afhænger af `profanitydb` (i `depends_on`), men det styrer kun
  opstartsrækkefølgen, ikke om ProfanityService faktisk er klar til at modtage kald —
  det er netop derfor retry- og circuit breaker-logikken er relevant fra første opstart.
- Ingen af de to services er x- eller z-akse-splittet — det er ikke et krav denne uge.

## Teknologivalg

- Samme stack som ArticleService: ASP.NET Core Web API (controller-baseret), Npgsql +
  Dapper.
- **Polly** (v7-syntaks) til retry + circuit breaker i CommentService — det eneste sted i
  systemet, hvor et service-til-service-kald skal kunne håndtere, at modparten er nede,
  uden at fejle hele requesten.
