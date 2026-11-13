# GAF tactic: Defer Fetching (færre rækker)

Hører sammen med [reduce-transmitted-data.md](reduce-transmitted-data.md). Begge bruger det samme endpoint, men måles hver for sig: denne tactic handler om **hvor mange artikler** der hentes, den anden om **hvor meget af hver artikel**.

## Problem

Reader-forsiden kaldte `GET /api/articles/{location}`, som returnerer **alle** artikler i shardet med fuld brødtekst. Der er ingen `LIMIT`. Forsiden viser højst 21 af dem (1 fremhævet, 5 populære og 3 pr. sektion i 5 sektioner), og browseren smed resten væk. Svaret vokser lineært med antallet af artikler. Baseline: se [baseline.md](baseline.md).

## Ændring

Et nyt endpoint henter præcis det, siden viser, i ét kald:

```
GET /api/articles/{location}/frontpage?top=6&perSection=3[&section=Health]
```

- **`top`**: de N nyeste (hero + populære). **`perSection`**: de N nyeste i hver sektion. **`section`**: hvis forsiden er filtreret, de `top` nyeste i den sektion.
- **Klienten bestemmer tallene ud fra sit eget layout**, så der er ingen magic numbers: `top: 1 + PopularCount`, `perSection: SectionCardCount` (`FrontPage.razor.cs`). Serveren kender ikke siden, kun "top N + N pr. sektion".
- **Én SQL med window functions**, så kun de rækker, der skal bruges, forlader databasen:

**Før** (`ArticleReadRepository.GetAllAsync`)
```sql
select a.id, a.byline, a.title, a.breadtext, ...
from articles a
inner join sections s on s.id = a.section_id
-- ingen order by, ingen limit
```

**Efter** (`ArticleReadRepository.GetFrontPageAsync`)
```sql
with ranked as (
    select ...,
        row_number() over (order by coalesce(a.publish_date, a.created_date) desc, a.id) as rn_all,
        row_number() over (partition by a.section_id
                           order by coalesce(a.publish_date, a.created_date) desc, a.id) as rn_section
    from articles a inner join sections s on s.id = a.section_id
)
select ... from ranked
where rn_all <= @Top
   or rn_section <= @PerSection
   or (lower(section_name) = lower(@Section) and rn_section <= @Top)
```

**Klient** (`FrontPage.razor.cs`)
```csharp
// Før: hele regionen
var articles = await ArticleCatalog.GetRegionArticlesAsync(HttpClientFactory, Logger, region);

// Efter: kun det, siden viser
var articles = await ArticleCatalog.GetFrontPageAsync(HttpClientFactory, Logger, region,
    top: 1 + PopularCount,
    perSection: SectionCardCount,
    section: ActiveSection, ...);
```

"More news" på artikelsiden bruger samme endpoint (`top` og `perSection` = `MoreNewsCount + 1`). `simulate-flow.sh` sender samme forespørgsel som forsiden.

Da forsiden nu henter pr. sektion, er et skift af sektionsfilter et nyt (lille) kald i stedet for en filtrering af alt, der allerede er hentet.

## Caching

Hele det sammensatte svar caches i Redis (`frontpage:{region}:{top}:{perSection}:{section}:{excerpt}:{heroExcerpt}`, TTL 5 min) for **alle** regioner:

- **Cache-aside:** et miss læser Postgres og gemmer svaret.
- **Invalidering:** når en artikel i regionen publiceres, redigeres eller fjernes (`RefreshArticleAsync`/`RemoveArticleAsync`), slettes regionens forsider. Hver region har et Redis-set med sine forside-nøgler, så det kan ske uden `SCAN`.
- **TTL'en** sætter en grænse for, hvor gammel en forside kan blive, hvis en invalidering går tabt, fx hvis Redis var nede et øjeblik.
- Hits og misses ses i det eksisterende cache-dashboard (`redis_keyspace_hits/misses`).

**Hvorfor ikke bare genbruge den eksisterende artikel-cache?** Den eksisterende forside-cache (`articles:{region}:index` + én nøgle pr. artikel) er bygget til "alle artikler fra de seneste 14 dage" og fyldes kun for `GO`. Under målingen viste det sig, at **indekset ikke findes i dev**: `GO`'s seed-artikler er ældre end 14 dage, så den timelige refresh laver et tomt indeks, og de andre regioner får aldrig et, fordi publicering kun tilføjer til et eksisterende indeks. Alle forsidekald gik derfor til Postgres. Den gamle cache er urørt og bruges stadig af `GetAll` og af artikelsiden (`GetById`).

## Måling

Samme data i alle målinger: EU-shardet med **1.008 artikler** (8 seed + 1.000 fra [`seed-1000-articles.sql`](seed-1000-articles.sql), ca. 2.000 tegns brødtekst). Simulatorens korte testartikler blev fjernet, så de ikke gjorde svarene kunstigt små. Hver måling er 3 opvarmningskald + 50 kald mod `localhost:8080`.

| | Før: `GetAll` | Efter: `frontpage?top=6&perSection=3` |
|---|---|---|
| Artikler i svaret | 1.008 | 15 |
| **Bytes pr. svar** | **2.312.484 (2,31 MB)** | **34.543 (34,5 KB), −98,5 %** |
| Gns. svartid, 50 kald (1 miss, resten cache-hit) | 38,6 ms | 11,5 ms |
| Gns. svartid ved cache-miss (DB), 5 kald | | 37,7 ms |
| NetIO for 50 kald (ArticleService ind/ud) | ≈ 109 / 117 MB | ≈ 1 / 1 MB |

15 og ikke 21: de 6 nyeste er også blandt de 3 nyeste i deres sektion, så de tæller kun én gang.

Bemærk svartiden ved cache-miss: 37,7 ms er næsten det samme som `GetAll`. Uden index på `publish_date` skal Postgres stadig sortere og nummerere **hele** shardet for at finde de 15. Bytes-gevinsten er stor, men databasearbejdet er ikke mindre før der kommer et index. Cachen skjuler det for alle undtagen det første kald efter en publicering.

### Virker det stadig?

`simulate-flow.sh` i 15 min med forside-endpointet (samme forespørgsel som reader-forsiden): 60 drafts, 54 publiceret, 6 stoppet af profanity-check, **838 læsninger med 0 fejl**, 166 kommentarer med 0 fejl. ArticleService loggede ingen `fail`/`crit`. Samme billede som ved baseline.

Bagefter lå der en cachet forside for hver region, der var publiceret i. GO's forside manglede, fordi dens faste TTL på 5 min var udløbet 2 sekunder før kørslen sluttede. TTL'en forlænges ikke ved læsning. Det næste kald genopbyggede den.

## Estimeret CO2

Læserens browser henter forsiden direkte fra ArticleService, så alle tre led i Sustainable Web Design Model v4 tælles med: 0,055 + 0,059 + 0,080 = **0,194 kWh/GB**. Dansk elmix 2025 er ca. 124 g CO2e/kWh.

- Før: 0,002312 GB × 0,194 × 124 ≈ **0,056 g CO2e pr. forsidevisning** (55,6 kg pr. million)
- Efter: 0,0000345 GB × 0,194 × 124 ≈ **0,00083 g CO2e pr. forsidevisning** (0,83 kg pr. million)

Det er et estimat. Bytes og ms er målt.

## Trade-offs

- **API'et kender lidt til forsiden.** "Top N + N pr. sektion" er generelt nok til forside og "More news", men en ny sidetype kan kræve en ny forespørgsel. Det er prisen for at hente præcis det rigtige i ét kald i stedet for at hente for meget eller hente flere gange.
- **Uden index på `publish_date`** sorterer Postgres hele shardet ved hvert cache-miss. Næste skridt: `create index on articles (section_id, publish_date desc)` og `(publish_date desc)`.
- **Sektionsfilteret er et nyt kald** i stedet for en filtrering i browseren. Det er et lille kald (få KB), og det caches.
- **Forsiden kan være op til 5 minutter gammel**, hvis en invalidering går tabt. Normalt slettes den med det samme ved publicering.
- **Journalist-web og drafts-siden** bruger stadig `GetAll` og henter hele shardet. De er uændrede.
