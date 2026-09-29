# Logging-policy — HappyHeadlines

Denne fil beskriver, hvordan services i HappyHeadlines logger: hvad der logges, hvornår, i hvilket format, og hvad der aldrig må logges. Formatet for et log-entry er beskrevet i [Logging-template.json](Logging-template.json).

Policyen gælder for DraftService, CommentService og ProfanityService. Øvrige services følger, når de får OpenTelemetry.

## Hvor logs og traces ender

Alle services sender logs og traces via OTLP til OTel Collector. Collectoren sender logs videre til Loki og traces til Tempo. Begge dele vises i Grafana.

Opsætningen ligger i et fælles bibliotek (`HappyHeadlines.Observability`), så alle services konfigureres ens med ét kald: `builder.AddObservability()`.

Telemetri må aldrig påvirke en service' tilgængelighed. Hvis collectoren er nede, droppes logs og traces, og servicen kører videre.

## Log-niveauer

| Niveau | Bruges når | Eksempel |
|---|---|---|
| `Information` | En forventet forretningshændelse er sket | Draft skifter status fra `WorkInProgress` til `PendingApproval` |
| `Warning` | Noget gik galt, men systemet håndterede det selv | ProfanityService svarer ikke, og fallback bruges; circuit breaker åbner |
| `Error` | Noget fejlede, som et menneske skal se på | Uventet exception; database kan ikke nås |
| `Debug` / `Trace` | Kun lokalt under udvikling | Slås fra i `Production` |

Vi logger ikke almindelige læsninger (fx `GET /drafts`). Dem dækker tracing.

## Hvad logges og hvornår

| Hændelse | Niveau | Felter |
|---|---|---|
| Draft skifter status (submit, approve, reject, publish, archive, reactivate) | `Information` | `DraftId`, `FromStatus`, `ToStatus` |
| Draft oprettet | `Information` | `DraftId`, `Location` |
| Kommentar oprettet (godkendt, afvist eller afventer tjek) | `Information` | `CommentId`, `ArticleId`, `Status` |
| Profanity-tjek afsluttet (CommentService) | `Information` | `BannedWordCount` |
| ProfanityService utilgængelig, og der faldes tilbage | `Warning` | `DraftId` eller `CommentId` |
| Circuit breaker åbner, halvåbner eller lukker | `Warning` (åbner) / `Information` (halvåbner, lukker) | `Pipeline`, `Reason` (exception-type, ikke besked) |
| Samtidig ændring (optimistic concurrency) | `Warning` | `DraftId`, `Status` |
| Uventet exception | `Error` | Exception-objektet sendes med som parameter (`LogError(ex, ...)`) |

## Felter der må logges

Kun felter på denne liste må indgå i log-beskeder:

- `DraftId`, `ArticleId`, `CommentId`
- `Status`, `FromStatus`, `ToStatus`
- `Location` (kontinent-kode, fx `EU`)
- `BannedWordCount`
- `Pipeline`, `Reason`

Nye felter tilføjes her først og derefter i koden.

### Circuit breaker: kun `Pipeline` og `Reason`

Circuit breaker-hændelser logger **kun** to felter:

- `Pipeline`: navnet på den service, der kaldes (fx `ProfanityService`).
- `Reason`: **exception-typen** (fx `HttpRequestException`), aldrig exception-beskeden.

Beskeden udelades bevidst. En exception-besked kan indeholde hostnavne, URL'er med query-parametre eller dele af den payload, der blev sendt. Dermed kan brugerindhold slippe ud i loggen ad bagvejen. Typen er nok til at se, *hvorfor* kredsløbet åbnede, og detaljerne kan findes i tracen.

```csharp
logger.LogWarning("Circuit {Pipeline} opened. Reason: {Reason}",
    "ProfanityService", args.Outcome.Exception?.GetType().Name);
```

## Må aldrig logges

- Brugerindhold: `Breadtext`, titler, kommentartekst
- De forbudte ord, der blev fundet (log antallet i stedet)
- Connection strings, passwords og tokens
- Personoplysninger om brugere ud over deres interne id
- Exception-beskeder i egne felter (fx `Reason`). Fulde exceptions logges kun ved `Error` via `LogError(ex, ...)`

## Sådan skrives et log-kald

Brug message templates med navngivne felter, ikke string interpolation:

```csharp
// Rigtigt: DraftId, FromStatus og ToStatus bliver søgbare felter i Loki
_logger.LogInformation("Draft {DraftId} changed status from {FromStatus} to {ToStatus}", id, from, to);

// Forkert: bliver én flad tekststreng, og det er let at komme til at logge indhold
_logger.LogInformation($"Draft {id} changed status from {from} to {to}");
```

Listen over tilladte felter er en konvention og håndhæves ved code review, ikke med et runtime-filter.

## Kobling til Logging-template.json

| Template-felt | Kilde i OpenTelemetry | Automatisk? |
|---|---|---|
| `Timestamp` | Log record timestamp | Ja |
| `Level` | Log record severity | Ja |
| `Location.Service` | Resource-attributten `service.name` | Ja, via `AddObservability()` |
| `Location.FilePath` / `LineNumber` / `MemberName` | Sættes af OpenTelemetry ikke automatisk. Kræver et `Here()`-mønster med `[Caller*]`-attributter. *Ikke afklaret, se åbent spørgsmål 1* | Nej |
| `Tracing.TraceId` / `SpanId` | Aktiv `Activity` | Ja |
| `Tracing.ParentId` | Findes på spannet i Tempo, ikke på log-linjen | Via trace |
| `Message` | Formateret besked | Ja |
| `Payload` | Navngivne felter fra message templaten | Ja |

## Åbne spørgsmål (til studiegruppen)

1. **Hvordan får vi `FilePath`, `LineNumber` og `MemberName` med?** Felterne kommer fra `Here()`-mønstret i undervisningen (uge 39, "Serilog - Caller Context og Timede Operationer"). Det mønster bygger på Serilogs `ForContext`. Vi bruger i dag `Microsoft.Extensions.Logging` med OpenTelemetry, som ikke har `ForContext`. Der er tre muligheder:
   - (a) **`Here()` med scope:** En extension i det fælles bibliotek kalder `logger.BeginScope(...)` med de tre `Caller*`-felter. Med `IncludeScopes = true` bliver felterne til attributter i Loki. Vi beholder den nuværende stack, men kaldet bliver `using var _ = _logger.Here();` før log-linjen. Det er lidt mere klodset end i Serilog.
   - (b) **Skift til Serilog:** Vi bruger `Serilog.Sinks.OpenTelemetry` og kopierer `Here()` 1:1 fra undervisningen. Det ligger tættest på pensum, men betyder en ny logging-stack i alle tre services.
   - (c) **Drop felterne:** Vi bruger logger-kategorien (klassens navn) og stack traces ved fejl. Det kræver ingen ekstra kode, men afviger fra `Logging-template.json`.

   Bemærk: `BeginTimedOperation` fra samme note er dækket af tracing. Hvert span har allerede en varighed og et id (se uge 39, "Distributed Tracing Made Easy with .NET Core").

   Input til valget: ved exceptions indeholder stack tracen i Loki allerede fil og linje (fx `ProfanityClient.cs:line 23`). `Here()` giver altså kun noget ekstra for `Information`- og `Warning`-linjer.
2. **Nye felter:** `BannedWordCount` og `Reason` bruges allerede i koden. Skal de blive på listen over tilladte felter?
3. **Håndhævelse:** Er code review nok, eller skal der et runtime-filter til? Hvad mister vi, og hvad sparer vi?
4. **Refleksion:** Hvorfor skal policyen ligge før koden?
5. **Gælder policyen også for traces?** Uge 39-noten viser `Activity.Current?.AddTag(...)` og baggage. Bør de samme tilladte og forbudte felter gælde for span-tags og baggage? Baggage sendes videre i headers til alle efterfølgende services.
6. **Sampling i produktion:** Instrumentering koster performance (uge 39). Skal vi sample traces ned i `Production`, eller er alt fint i projektets skala?
7. **Observability i prod-compose:** Collector, Loki, Tempo og Grafana ligger kun i `docker-compose.dev.yaml`. Skal de også med i `docker-compose.yaml`?
8. **Kendt afvigelse fra arkitekturen:** DraftService kalder ProfanityService, men ifølge handoutet er det PublisherService' ansvar. Er vi enige om at beholde det midlertidigt og flytte det, når PublisherService bygges?
9. **Dubletter fra Polly:** `Microsoft.Extensions.Http.Resilience` logger selv circuit breaker-events og retries (`Resilience event occurred. EventName: 'OnCircuitOpened'`, inkl. fulde exceptions). Vores egne `OnOpened`-, `OnHalfOpened`- og `OnClosed`-callbacks giver derfor dubletter. Skal vi beholde vores egne (de følger policyen) og dæmpe Pollys via `LogLevel`, eller omvendt? Pollys logs indeholder exception-beskeder, som strider mod reglen for `Reason`.

## Kendte fejl (ny opgave, ikke en del af logging)

- **Profanity-tjek regnes som "rent", når ProfanityService fejler, før circuit breakeren er åben.** I begge `ProfanityClient`-klasser returnerer `catch (HttpRequestException)` og `catch (TaskCanceledException)` en tom ordliste med `CircuitOpen = false`. Handleren tolker det som "tjekket og ingen forbudte ord".
  - Konsekvens: drafts submittes uden flag, og kommentarer godkendes uden tjek i de første fejlede kald.
  - Fundet under test i uge 38: drafts 8 og 9 blev submittet uden fallback-warning, mens ProfanityService var stoppet.
  - Det strider mod kommentaren i `CommentService/Profanity/IProfanityClient.cs` og mod fault isolation fra uge 37.
