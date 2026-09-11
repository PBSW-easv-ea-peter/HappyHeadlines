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
- **ProfanityService** eksponerer en REST-API til at tjekke enkeltord mod en liste af
  forbudte ord, opslået i ProfanityDatabase.

Kravet for denne uge er, at de to services er fault-isolerede efter swim lane-principperne
(kap. 21), at CommentService kalder ProfanityService **direkte** (ingen gateway eller UI
som mellemled), og at CommentService har en circuit breaker, der overtager, hvis
ProfanityService ikke er tilgængelig.

## Fault isolation: to swim lanes, én bevidst synkron grænse

CommentService og ProfanityService er hver deres swim lane: egen container, egen database,
egen fejl-domæne (Princip 1 — intet deles). Ingen af dem deler database eller proces med
ArticleService.

Der er dog én ting, der ikke passer ind i den rene teori: **Princip 2** (kap. 21) siger, at
intet bør krydse en swim lane-grænse synkront. Kaldet fra CommentService til
ProfanityService er netop synkront — det er et direkte HTTP POST-kald, ikke en kø. Det er
et bevidst brud, fordi opgaveteksten kræver direkte kommunikation uden mellemled, og fordi
en asynkron "fire and forget"-besked ikke giver mening her: CommentService skal bruge
svaret (er ordet forbudt eller ej), før den kan afgøre, om kommentaren må vises.

Circuit breakeren er den kompensering, som VMware-artiklen ("Should That Be a Microservice
- Part 5: Failure Isolation") peger på i præcis den situation: når et synkront kald på
tværs af en grænse ikke kan undgås, er en circuit breaker det værktøj, der forhindrer en
fejlende ProfanityService i at brede sig (cascading failure) til CommentService — i stedet
for at CommentService selv går ned eller hænger, mens den venter på svar.

## Circuit breaker-design

`ProfanityClient` (i CommentService) bruger Polly til at kombinere to policies, sammensat
med `Wrap`, nøjagtig samme mønster som i vores noter om Polly:

1. **Retry**: op til 2 forsøg med kort backoff (200 ms, 400 ms) for at overleve et
   kortvarigt netværksudsving.
2. **Circuit breaker**: efter 3 fejl i træk "tripper" kredsløbet og blokerer alle kald til
   ProfanityService i 30 sekunder, så en presset eller nede service får ro til at komme sig,
   i stedet for at blive oversvømmet af CommentService's forsøg.

Derudover har `HttpClient` en 2-sekunders timeout, så et hængende kald til ProfanityService
aldrig kan blokere CommentService's egen swim lane på ubestemt tid.

### Hvad sker der, når kredsløbet er åbent?

Vi har bevidst valgt **ikke** at fejle hele kommentar-postningen, hvis ProfanityService er
nede. I stedet:

- Kommentaren gemmes med status `PendingProfanityCheck` i stedet for `Approved` eller
  `Rejected`.
- `GET /api/comments/{articleId}` returnerer kun `Approved`-kommentarer, så en
  ikke-verificeret kommentar aldrig vises til læserne.
- CommentService selv forbliver oppe og fortsætter med at modtage kommentarer.

Det er en konkret anvendelse af to af de arkitekturprincipper, vi har gennemgået denne uge:
**Design to be disabled** (det er kun *visningen* af nye, endnu ikke tjekkede kommentarer,
der "slukkes" — ikke hele CommentService) og **Isolate faults** (en fejlende
ProfanityService rammer aldrig andet end retfærdiggørelsen af nye kommentarer).

**Kendt gæld:** der er endnu ingen baggrundsproces, der genforsøger `PendingProfanityCheck`
-kommentarer, når ProfanityService er oppe igen. Det naturlige næste skridt er en
`BackgroundService`, i stil med `ArticleQueueConsumer` i ArticleService, der periodisk
tjekker de afventende kommentarer igen. Den er ikke lavet i denne uge, da opgaveteksten kun
kræver circuit breakeren selv, ikke reconciliation-logikken bagefter.

## REST API

### ProfanityService

| Metode | Rute                  | Beskrivelse                              |
|--------|------------------------|-------------------------------------------|
| POST   | `/api/profanity/check` | Tjek om et enkelt ord er forbudt (bool)   |

**Hvorfor POST og ikke GET eller PUT?** Kaldet er en beregning/handling ("tjek dette ord"),
ikke en oprettelse eller erstatning af en ressource — det udelukker PUT. GET ville kræve
ordet som query-parameter (`?word=...`), hvilket kræver URL-encoding og bliver upraktisk,
hvis tjekket senere udvides fra ét ord til en hel kommentar/sætning. POST bruges her som et
"command"-endpoint (a la `/search`, `/validate`), ikke i sin CRUD-forstand.

### CommentService

| Metode | Rute                            | Beskrivelse                                  |
|--------|----------------------------------|-----------------------------------------------|
| GET    | `/api/comments/{articleId}`     | Hent alle godkendte kommentarer på en artikel |
| POST   | `/api/comments/{articleId}`     | Post en kommentar på en artikel               |

## Database

**ProfanityDatabase** (`banned_words`): `id`, `word` (unik). Opslag er case-insensitivt
(`lower(@Word)`).

**CommentDatabase** (`comments`): `id`, `article_id`, `author_name`, `text`,
`created_date`, `status` (smallint: 0 = Approved, 1 = PendingProfanityCheck, 2 = Rejected).
Status er gemt som tal, ikke tekst, så Dapper kan mappe direkte til `CommentStatus`-enummet
uden ekstra type-håndtering.

**Kendt gæld:** `banned_words` er seedet med et lille eksempel-ord-sæt
(`profanity_baseline.sql`) — der er ingen admin-endpoints til at tilføje/fjerne ord endnu.
Det er uden for denne uges scope (fault isolation og circuit breaker), men er et oplagt
næste skridt, hvis ProfanityService skal kunne vedligeholdes uden en ny deployment.

## Deployment

Ligesom ArticleService kører CommentService og ProfanityService som deres egne containere
i `docker-compose.yaml`, hver med sin egen Postgres-database (`commentdb`, `profanitydb`),
initialiseret via deres egne baseline-SQL-filer i `database/postgres/comment/` og
`database/postgres/profanity/` (holdt adskilt fra ArticleService's shard-mount, så
kommentar-/profanity-skemaet ikke ender i artikel-databaserne).

Ingen af de to services er x- eller z-akse-splittet i denne omgang — det er ikke et krav
denne uge, og med kun to swim lanes at holde styr på er det nemmere at se, om
fault-isolationen reelt virker. Jf. **Design for at least two axes of scale**: næste
naturlige skalerings-akse for CommentService ville være x-axis (flere replicas, som
ArticleService), hvis kommentar-trafikken vokser.

`commentservice` afhænger af `profanityservice` i `depends_on` — men det styrer kun
opstartsrækkefølgen, ikke om ProfanityService faktisk er klar til at modtage kald. Det er
netop derfor, retry- og circuit breaker-logikken i `ProfanityClient` er relevant fra første
opstart, ikke kun ved senere nedbrud.

## Teknologivalg

- Samme stack som ArticleService: ASP.NET Core Web API (controller-baseret), Npgsql +
  Dapper.
- **Polly** (v7-syntaks) til retry + circuit breaker i CommentService — det eneste sted i
  systemet, hvor et service-til-service-kald skal kunne håndtere, at modparten er nede,
  uden at fejle hele requesten.
