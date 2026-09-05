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

## Konsistens-model: direkte skrivning, ikke via kø

Det oprindelige container-diagram (uge 1) viser artikler blive skrevet til
ArticleDatabase via ArticleQueue (fra PublisherService). Denne uge er ArticleService den
første service, der bygges, så der findes endnu ikke nogen rigtig ArticleQueue at koble
op på. Vi har derfor besluttet:

- Create/Update/Delete skriver **direkte** til den relevante shard-database.
- Der er lagt et boilerplate-seam ind (`IArticleQueuePublisher`, med en no-op
  implementation `NoOpArticleQueuePublisher`), som kaldes efter en vellykket oprettelse.
  Når ArticleQueue findes, er planen at skifte DI-registreringen ud med en rigtig
  implementering, uden at ændre repository- eller controller-koden.
- Konsekvens: da både læs og skriv går direkte mod databasen, er der ingen eventual
  consistency at forholde sig til i denne uge. Det ændrer sig, når publicering fra
  PublisherService kobles på via køen i en senere uge.

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
