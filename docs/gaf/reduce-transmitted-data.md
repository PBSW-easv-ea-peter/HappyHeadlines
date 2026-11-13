# GAF tactic: Reduce Transmitted Data (færre felter)

> **Navn:** tjek at tactic'en hedder sådan i GAF-kataloget, inden afleveringen. Pointen er "send kun de data, der faktisk bruges".

Bygger videre på [defer-fetching.md](defer-fetching.md), som allerede har skåret forsiden ned til de 15 artikler, der vises. Denne tactic handler om **hvor meget af hver artikel** der sendes.

## Problem

Efter Defer Fetching sender forsiden 15 artikler, alle med **fuld brødtekst** (ca. 2.000 tegn hver). Forsidens komponenter bruger:

| Komponent | Brødtekst |
|---|---|
| `HeroArticle` (1 artikel) | de første **400** tegn |
| `PopularList` (5) | ingen |
| `SectionCard` (15) | ingen |
| `ArticleCard` ("More news" på artikelsiden) | de første **110** tegn |

Kun 1 af 15 artikler på forsiden viser overhovedet brødtekst, og kun en femtedel af den.

## Ændring

Endpointet tager to valgfrie parametre:

```
GET /api/articles/{location}/frontpage?top=6&perSection=3&excerpt=0&heroExcerpt=400
```

- **`excerpt`**: hvor mange tegn brødtekst hver artikel får. `0` giver ingen, og udeladt giver hele teksten (som før).
- **`heroExcerpt`**: det samme for den nyeste artikel (hero).
- **Teksten skæres i databasen** (`left(breadtext, n + 1)`), så resten aldrig forlader Postgres. Det ekstra tegn gør, at klientens egen `Excerpt()` stadig kan se, at teksten fortsætter, og tilføje "…". Visningen er uændret.
- **Ingen magic numbers:** længderne er nu navngivne konstanter i komponenterne, der viser dem (`HeroArticle.ExcerptLength = 400`, `ArticleCard.ExcerptLength = 110`). Siden sender dem videre.

**Før** (komponent og forespørgsel)
```razor
@* HeroArticle.razor *@
@ArticleDisplay.Excerpt(Article.BreadText, 400)
```
```sql
select a.id, a.title, a.breadtext, ...   -- hele brødteksten for alle rækker
```

**Efter**
```razor
@* HeroArticle.razor *@
@ArticleDisplay.Excerpt(Article.BreadText, ExcerptLength)
@code { public const int ExcerptLength = 400; }
```
```csharp
// FrontPage.razor.cs
await ArticleCatalog.GetFrontPageAsync(..., excerpt: 0, heroExcerpt: HeroArticle.ExcerptLength);
```
```sql
case when excerpt_length is null then breadtext          -- udeladt: hele teksten
     when excerpt_length = 0     then ''                 -- ingen tekst
     else left(breadtext, excerpt_length + 1)            -- kun det, der vises
end as breadtext
```

Den fulde tekst hentes stadig, når læseren åbner artiklen (`GET /api/articles/{location}/{id}`). Den er bare udskudt, til den skal bruges.

## Måling

Samme data og metode som [defer-fetching.md](defer-fetching.md): EU med 1.008 artikler, 3 opvarmningskald + 50 kald.

| | Før: 15 artikler, hel tekst | Efter: 15 artikler, kun uddrag |
|---|---|---|
| **Bytes pr. svar** | **34.543** | **4.164, −87,9 %** |
| Gns. svartid, 50 kald (cache-hit) | 11,5 ms | 11,0 ms |
| Gns. svartid ved cache-miss (DB), 5 kald | 37,7 ms | 25,9 ms |

Fra det oprindelige udgangspunkt (`GetAll`, 2.312.484 bytes) er det samlet **−99,8 %**.

Ved cache-miss er der en tydelig gevinst i databasen: 37,7 → 25,9 ms. Postgres skal ikke sende ca. 30 KB brødtekst til servicen, og servicen skal ikke serialisere den.

Redis gemmer også det mindre svar, så hver cachet forside fylder ca. 4 KB i stedet for 35 KB i hukommelsen.

## Estimeret CO2

Samme model som Defer Fetching (0,194 kWh/GB, 124 g CO2e/kWh):

- Før: 0,0000345 GB → **0,00083 g CO2e pr. forsidevisning** (0,83 kg pr. million)
- Efter: 0,0000042 GB → **0,00010 g CO2e pr. forsidevisning** (0,10 kg pr. million)

## Trade-offs

- **Ét felt med to betydninger.** `breadtext` kan nu være hele teksten, et uddrag eller tom, afhængigt af forespørgslen. En klient, der tror, den har hele teksten, kan vise for lidt. Et separat `excerpt`-felt eller en egen opsummeringstype ville være tydeligere, men kræver at DTO'en ændres i både service og klienter.
- **Klientens tal skal passe.** Ændrer nogen `ExcerptLength` i en komponent, sendes det nye tal automatisk. Hardkodes et tal et andet sted, risikerer man, at uddraget bliver kortere end det, der vises.
- **Cachen får flere varianter.** Hver kombination af parametre er en nøgle. I praksis bruger reader-web to (forside og "More news") plus én pr. sektionsfilter.
- **Lille absolut gevinst.** 0,7 kg pr. million visninger. Pointen er forholdet: næsten 90 % af det, der stadig blev sendt efter Defer Fetching, blev ikke vist.
