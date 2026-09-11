---
course: Development of Large Systems
type: architecture-doc
week: 36
company: Happy Headlines
service: ArticleService
---

# ArticleService — arkitektur

## Ansvar

ArticleService eksponerer en REST-baseret CRUD-API for artikler og er både x-axis og
z-axis splittet, jf. kravene for denne uge:

- **x-axis split**: 3 replicas af ArticleService, load-balanceret.
- **z-axis split**: ArticleDatabase er delt i 8 uafhængige databaser — én pr. kontinent
  (Europa, Nordamerika, Sydamerika, Australien, Asien, Antarktis, Afrika) samt én global
  database for artikler der er relevante på tværs af hele verden.

## Modulær monolit

Dette er den første service i systemet. For at kunne trække repository/controller-laget
ud i sin egen service senere, er koden opdelt i en "vanilla" læse-gren og en CUD-gren,
hver med sit eget repository-lag (`IArticleReadRepository` / `IArticleWriteRepository`),
bag én fælles `ArticlesController`. Det er en modulær-monolit-udgave af CQRS
(Command Query Responsibility Segregation) — læs og skriv er adskilt i koden, selvom de
kører i samme proces og mod samme database i denne omgang.

## Konsistens-model: Create og Update er kø-drevne, Delete er direkte REST

Underviseren har afklaret (forumtråd "Spørgsmål til arkitekturbeslutning for 2. uge",
uge 36) at CUD ikke er tre ligeværdige REST-operationer:

- **Create**: sker ved at ArticleService **konsumerer** ArticleQueue — matcher det
  oprindelige uge 35-diagram, hvor `PublisherService -> ArticleQueue` og
  `ArticleService -> ArticleQueue` (subscriber). Det er PublisherService, der lægger en
  godkendt artikel i køen; ArticleService's rolle er at forbruge beskeden og persistere
  den i ArticleDatabase.
- **Update**: gruppen har besluttet at Update følger samme spor som Create (kø-drevet),
  frem for Delete-sporet. Dette er **ikke** bekræftet af underviseren — kun Create og
  Delete er eksplicit afklaret i forumtråden. Bør nævnes som en eksplicit antagelse i
  rapporten/præsentationen, og gerne følges op med et opklarende spørgsmål til
  underviseren.
- **Delete**: bekræftet af underviseren som en direkte REST-operation — "tager et Id og
  sletter artiklen baseret på det Id".

**Konsekvens for denne uges implementering:** hverken PublisherService eller en rigtig
ArticleQueue findes endnu, så Create og Update kan ikke reelt trigges via kø-forbrug i
denne omgang. Opgaveteksten kræver stadig fire REST-endpoints, så `POST` og `PUT` er
bevaret som **midlertidige REST-stand-ins**, der skriver direkte til den relevante
shard-database — men den egentlige, tiltænkte trigger for Create og Update er en
kø-konsument. Boilerplate-seamet er derfor lavet som en konsument (`ArticleQueueConsumer`,
en `BackgroundService`), ikke en publisher: når ArticleQueue og PublisherService findes,
er planen at `ArticleQueueConsumer` kalder ind i de samme `IArticleWriteRepository`-
metoder (`CreateAsync`/`UpdateAsync`), som `ArticlesController` også bruger til
stand-in-endpointsne. Den er idle nu, da der intet er at konsumere endnu.

`DELETE` er upåvirket af ovenstående — den er og forbliver en ægte, direkte REST-operation.

## Sharding

`articles.location` (CHECK-constrained til `EU, NA, SA, AU, AS, AN, AF, GO`) er
sharding-nøglen. Fordi hver shard har sin egen identity-sekvens, er artikel-ID'er **ikke**
globalt unikke på tværs af de 8 databaser — et ID skal derfor altid læses sammen med en
`location`.

REST-ressourcen inkluderer derfor `location` eksplicit i stien:

| Metode | Rute                              | Beskrivelse                         |
|--------|------------------------------------|--------------------------------------|
| GET    | `/api/articles/{location}`         | Alle artikler i én shard             |
| GET    | `/api/articles/{location}/{id}`    | Én artikel i en given shard          |
| POST   | `/api/articles/{location}`         | Opret artikel i en given shard       |
| PUT    | `/api/articles/{location}/{id}`    | Opdatér artikel i en given shard     |
| DELETE | `/api/articles/{location}/{id}`    | Slet artikel i en given shard        |

`IArticleShardResolver` slår `location` op i konfiguration (`ArticleShards`-sektionen i
`appsettings.json`) og returnerer den rigtige Postgres-connection string. Repository-laget
åbner en ny `NpgsqlConnection` pr. kald ud fra den resolverede connection string.

**Kendt gæld:** de gyldige location-koder findes nu tre steder — CHECK-constrainten i
`relational_baseline.sql`, `ArticleShards`-nøglerne i `appsettings.json`, og
`ValidLocations` i `ArticlesController`. Det er fint for denne uges omfang, men bør
konsolideres til én kilde (fx et enum delt mellem validering og konfiguration), hvis
flere shards eller en ny service skal bruge samme liste.

## Deployment

Kørslen sker via Docker Swarm (`docker stack deploy`), ikke almindelig
`docker compose up`. Det betyder:

- `deploy.replicas: 3` på `articleservice` fungerer som forventet under Swarm — Swarms
  indbyggede routing mesh fordeler trafik mellem de 3 replicas.
- `ArticleService Load Balancer` fra C4-deployment-diagrammet er **ikke** en selvstændig
  container — den er kun tegnet for at visualisere Swarms indbyggede load-balancering.
  Der bygges ingen separat LB-container.
- `docker stack deploy` bygger ikke images. Workflowet er:
  1. `docker compose build` (eller `docker build -t happyheadlines/articleservice ./HappyHeadlines/ArticleService/`)
  2. `docker swarm init` (én gang)
  3. `docker stack deploy -c docker-compose.yaml happyheadlines`
- De 8 shard-databaser (`articledb-eu`, `articledb-na`, `articledb-sa`, `articledb-au`,
  `articledb-as`, `articledb-an`, `articledb-af`, `articledb-global`) kører hver som sin
  egen Postgres-container, alle initialiseret med samme `relational_baseline.sql`.

## Teknologivalg

- ASP.NET Core Web API (controller-baseret, ikke minimal API) — matcher jeres eget
  "controller-repository"-udgangspunkt.
- Npgsql + Dapper til dataadgang — holder repository-laget simpelt uden
  migrations-overhead fra en fuld ORM, og gør det trivielt at skifte connection string pr.
  shard.
