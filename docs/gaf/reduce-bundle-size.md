# GAF tactic: Reduce Bundle Size

## Problem

Alle 6 .NET-services brugte `mcr.microsoft.com/dotnet/aspnet:10.0` som runtime-image. Det er et fuldt Debian med shell, pakkemanager og systemværktøjer, som ingen af servicerne bruger. Det er det image, der pushes til GHCR ved hver CD-kørsel og pulles af hver k8s-node, der starter en pod. Baseline: se [baseline.md](baseline.md).

## Ændring

Én linje i final-stagen af hver Dockerfile (`article`, `comment`, `draft`, `newsletter`, `profanity` og `publish`):

**Før**
```dockerfile
# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ArticleService.dll"]
```

**Efter**
```dockerfile
# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ArticleService.dll"]
```

Build-stagen (`sdk:10.0`) er uændret. Den kommer ikke med i det færdige image.

## Resultat

Målt 12/11-2026. Før og efter er bygget på samme Docker-version (29.9.3). "Pull" er målt som `docker save <image> | gzip | wc -c` og svarer til de komprimerede lag, der sendes ved push og pull.

| Service | Disk før | Disk efter | Pull før | Pull efter |
|---|---|---|---|---|
| article_service | 353 MB | 193 MB | 100,4 MB | 59,7 MB (−40,5 %) |
| comment_service | 353 MB | 193 MB | 100,5 MB | 59,8 MB (−40,4 %) |
| draft_service | 350 MB | 190 MB | 99,5 MB | 58,8 MB (−40,9 %) |
| newsletter_service | 346 MB | 186 MB | 98,0 MB | 57,4 MB (−41,5 %) |
| profanity_service | 348 MB | 188 MB | 99,0 MB | 58,4 MB (−41,0 %) |
| publish_service | 350 MB | 190 MB | 99,5 MB | 58,8 MB (−40,9 %) |
| **I alt** | **2.100 MB** | **1.140 MB (−45,7 %)** | **596,8 MB** | **353,0 MB (−40,9 %)** |

Pull-tallene er målt i bytes og afrundet til én decimal (fx article: 100.375.218 → 59.744.063 bytes). "Disk" er `docker images`' afrundede visning. Article_service efter er bygget med forside-endpointet, men det ændrer imaget med under 0,1 MB.

### Virker det stadig?

Samme workload som baseline (`simulate-flow.sh`, standardindstillinger, 15 min, frisk stack):

| | Før | Efter |
|---|---|---|
| Publiceret / stoppet af profanity | 54 / 6 | 54 / 6 |
| Læsninger / fejl | 821 / 0 | 831 / 0 |
| Kommentarer / fejl | 183 / 0 | 171 / 0 |
| ArticleService CPU gns. | 2,56 % | 2,43 % |
| ArticleService RAM ved slut | 111 MiB | 109 MiB |

CPU og RAM er uændrede inden for måleusikkerheden. Det var forventeligt, fordi runtime og app er de samme, og kun styresystemet omkring dem er skåret ned. Gevinsten ligger i overførsel og lagring, ikke i drift. Servicernes logs indeholder kun exceptions fra OpenTelemetry-exporteren, som ikke kunne nå `otel-collector`, fordi monitoring-stacken ikke kørte. De opstod også ved baseline og har ikke noget med ændringen at gøre.

Rådata: `docs/gaf/data/reduce-bundle-size-stats.csv`.

## Estimeret CO2

Besparelse pr. fuld deploy af de 6 services: 596,8 − 353,0 = **243,8 MB ≈ 0,244 GB** overført.

- Energi: Sustainable Web Design Model v4, datacenter (0,055) + netværk (0,059) = 0,114 kWh/GB. Brugerens enhed er udeladt, da et image-pull går mellem registry og server. → 0,244 × 0,114 ≈ **0,028 kWh**
- CO2: dansk elmix 2025 ca. 124 g CO2e/kWh (Electricity Maps). → **ca. 3,4 g CO2e pr. deploy pr. node** (8,4 → 5,0 g)
- Pr. service (article): 0,100 GB → 1,42 g og 0,060 GB → 0,84 g, altså 0,57 g sparet pr. pull.

Det er et estimat, ikke en måling. kWh/GB-modellen er kritiseret for at overvurdere effekten af færre bytes, da netværksudstyr bruger det meste af sin strøm uanset trafik. Den målte evidens er image-størrelsen. Besparelsen ganges op med antal noder, antal deploys (CD ved hver merge) og nye pods ved skalering. Desuden gemmer GHCR hver pushet version, så lageret vokser 41 % langsommere.

## Trade-offs

- **Ingen shell eller pakkemanager.** `docker exec -it <container> bash` virker ikke. Fejlsøgning skal ske via logs, traces eller en debug-sidecar. Ingen healthchecks eller k8s-probes på .NET-services kører kommandoer i containeren, så intet er brudt.
- **Kører som non-root** (UID 1654). Det er en sikkerhedsgevinst og samtidig en begrænsning: servicen kan ikke skrive uden for sine egne mapper eller binde porte under 1024. Alle services bruger 8080–8086.
- **Invariant globalization** (`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true`, ingen ICU eller tzdata). Ingen backend-service bruger `CultureInfo` eller `TimeZoneInfo`. Får det betydning senere, findes `10.0-noble-chiseled-extra` med ICU.
- **Mindre angrebsflade.** Færre pakker giver færre CVE'er at holde styr på. Det er en sidegevinst, der ikke har med GAF at gøre.
