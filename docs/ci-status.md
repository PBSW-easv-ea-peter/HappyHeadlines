---
course: Development of Large Systems
type: status-note
week: 43
company: Happy Headlines
---

# CI/CD-pipeline — status og fund

Gennemgang af `.github/workflows/ci-apps.yml` (commit `bcbe156`) mod uge 43-kravet: *"build and deliver
[all services] everytime something new arrives on the master/main-branch"* og *"store the Docker Images
with a version number in a central repository"*.

Noten er kun en beskrivelse til ejeren af pipelinen. Workflowet er ikke ændret. Fundene er fra
læsning af filerne — pipelinen er ikke kørt, så punkter markeret *sandsynligvis* bør bekræftes i en
Actions-kørsel.

## Hvad virker

- Images bygges og pushes til GitHub Container Registry (`ghcr.io/<repo>/<service>`).
- Kun ændrede services bygges (matrix ud fra `git diff`).
- Login til ghcr med `GITHUB_TOKEN` og `packages: write`.

## Fund

### 1. Trigger: lukkede PR'er deployer også

`ci-apps.yml:7-9` trigger på `pull_request: types: [closed]` uden at tjekke, om PR'en blev merget.

- En PR, der lukkes **uden** merge, bygger og pusher stadig `:latest` — med kode, der aldrig kom på master.
- Et direkte push til master trigger ikke (push-triggeren l.4-6 er udkommenteret).

**Forslag:** trig på `push` til `master` (det er præcis "something new arrives on master"), eller tilføj
`if: github.event.pull_request.merged == true` på jobbet.

### 2. Change-detection: `shared` og filer i roden af `apps/`

`ci-apps.yml:54` finder services med `cut -d'/' -f2`, så alt direkte under `apps/` bliver en "service":

- En ændring i `apps/shared/` giver matrix-værdien `shared`. Der findes ingen `apps/shared/Dockerfile`,
  så jobbet fejler.
- Samtidig bliver de services, der afhænger af `shared` (alle .NET-services via `ProjectReference`),
  **ikke** genbygget — selvom deres image reelt er ændret.
- Ændringer i `apps/HappyHeadlines.slnx` eller `apps/.dockerignore` giver tilsvarende ugyldige værdier
  (`HappyHeadlines.slnx`, `.dockerignore`).

**Forslag:** map stier til services i stedet for at tage mappenavnet direkte: `apps/shared/**` ⇒ alle
.NET-services; filtrér værdier fra, der ikke har en Dockerfile.

### 3. Build context: web-appen bygges forkert, reader bygges aldrig

`ci-apps.yml:88-92` bygger alt med context `apps/`. Det passer til .NET-services (de skal kunne nå
`apps/shared`), men ikke til web-appen:

- `apps/happy-headlines-web_service/Dockerfile` kopierer `src/...` og forventer sin egen mappe som context
  — det gør compose også (`docker-compose.yaml:392`). Med context `apps/` findes `src/` ikke, så
  web-buildet fejler *sandsynligvis*.
- `apps/happy-headlines-web_service/Dockerfile.reader` (reader-sitet, `docker-compose.yaml:397-401`)
  bygges aldrig — matrixen kender kun én Dockerfile pr. mappe.

**Forslag:** lad matrixen indeholde objekter med `name`, `context` og `dockerfile`, så web og reader kan
have deres egen context og Dockerfile.

### 4. Ingen build- eller test-trin

Workflowet kører hverken `dotnet build` eller `dotnet test`. Der findes testprojekter for Article,
Comment, Draft, Profanity og Publish, men et image kan pushes, selvom testene fejler.

Bemærk: `PublishService.Tests` er ikke med i `apps/HappyHeadlines.slnx` (l.39-41 har kun `src`), så
`dotnet test apps/HappyHeadlines.slnx` ville i dag springe den over.

**Forslag:** et `test`-job (`dotnet test apps/HappyHeadlines.slnx`), som `build` afhænger af
(`needs: test`), og tilføj `PublishService.Tests` til `.slnx`.

### 5. Versionering og levering

- `ci-apps.yml:90-91` tagger med `:<git-sha>` og `:latest`. SHA er unik, men ikke en læsbar
  versionsrække — opgaven siger "with a version number". Fx `:1.0.<run_number>` eller semver ved git-tag
  ville opfylde det tydeligere.
- `docker-compose.yaml` bruger lokale images (`happyheadlines/<service>` med `build:`), ikke ghcr.
  Images fra pipelinen bliver altså aldrig brugt af en deployment. Det er måske løst på
  `feature/deployment_in_prod` (Kubernetes) — tjek, hvilke images manifesterne der peger på.

**Forslag:** versionstag ud over SHA, og lad deployment-manifesterne pege på `ghcr.io/...:<version>`.

## Prioritet

| # | Fund | Konsekvens | Prioritet |
|---|---|---|---|
| 1 | Lukkede PR'er deployer | Kode uden for master kan ende som `:latest` | Høj |
| 3 | Web-context / reader mangler | To af de deploybare apps leveres ikke | Høj |
| 2 | `shared` i matrix | Fejlende job + forældede images | Høj |
| 4 | Ingen tests | Fejl opdages først efter deploy | Middel |
| 5 | Versionering / levering | Kravet om versionsnummer er kun delvist opfyldt | Middel |
