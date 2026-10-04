# Monitoring - status og plan (1. okt 2026)

## Status: Cache-dashboard (uge 40) - færdigt og testet

Alt ligger i `monitoring/` og kører standalone (se `monitoring/README.md`).

- Prometheus, Grafana, 2 × redis_exporter, dummy-Redis (profil `dummy`)
- Dashboard *Cache hit ratio*: redis_up, hit ratio (5m), keys pr. cache, hit ratio over tid, hits/misses pr. sekund
- `scripts/simulate-traffic.sh` i containeren `traffic-simulator` (profil `simulate`) - virker på Windows og Linux
- Verificeret: Redis- og Prometheus-tal stemmer overens, LRU holder 30 keys, faserne giver det forventede mønster
- Ingen eksisterende filer er ændret. Intet er committet endnu - der skal laves en branch først.

### Åbent
- [ ] Tilføj `monitoring/.gitattributes` med `*.sh text eol=lf`. Ellers får Windows-brugere med
      `core.autocrlf=true` scriptet checket ud med CRLF, og det fejler i containeren.
- [ ] Grafana logger `Could not find plugin definition for data source` for Loki - tjek om det også sker i dev-compose.
- [ ] Afklar i gruppen: navne på de rigtige Redis-containere (for `REDIS_ADDR`), og hvad "cache-status" i pie-diagrammet skal betyde.
- [ ] Forberedelse til fremlæggelse: Hvorfor bruger panel 2 `[5m]` og panel 4 `[$__rate_interval]`?

## Plan: HappyHeadlines Overview (dashboard nr. 2) - kun forslag, ikke godkendt

Et 2. dashboard ved siden af cache-dashboardet, plus et simuleringsscript. Mockups ligger i denne mappe.

### 1. Metrics - CPU / RAM / Network-tærter pr. database
- Alle 11 DB'er: 8 × `articledb-*`, `commentdb`, `profanitydb`, `draftdb`.
- Variabel `$database` + repeated row pr. DB med 3 tærter (matcher mockup: kolonne = ressource, række = DB).
- Kilde: cAdvisor → Prometheus. Queries filtrerer på `container_label_com_docker_compose_service`,
  så de virker uændret på tværs af dummy/dev/prod.
- Tærten viser andel af tidsintervallet i hvert belastningsbånd. Forslag: <60 % blå, 60-80 % gul, 80-90 % orange, >90 % rød.
- Forudsætninger:
  - RAM i % kræver `mem_limit` på containeren (dummy-DB'er får det, dev/prod skal have det ved migrering).
  - CPU i % måles mod CPU-limit eller værtens kerner.
  - Netværk har ingen naturlig 100 % → variabel `$net_capacity` (fx 100 Mbit/s).

### 2. Log-hændelser pr. niveau
- Kilde: Loki. Simulatoren sender til Lokis OTLP-endpoint, så labels svarer til de rigtige .NET-logs.
- Stablede søjler, `count_over_time` grupperet pr. niveau: info blå, warning gul, error orange, critical rød.
- Variabel `$bucket`: 1m i dummy, 1d i prod.

### 3. Kontrolkort - comments / articles
- Rettelse ift. mockup: UCL/LCL = gennemsnit ± 3σ over en baseline-periode. UCL er altid over LCL,
  og Today bør normalt ligge mellem dem (i mockup'et krydser linjerne, og Today ligger under LCL hele dagen).
- Kilde: counters `comments_created_total` / `articles_published_total` i Prometheus
  (`avg_over_time` + `stddev_over_time`). LCL klippes ved 0.
- Dummy: simulatoren pusher counters til en Pushgateway.
- Migrering: services udsender counters via OTel Metrics → collectorens (udkommenterede) Prometheus-exporter.
  Kræver `WithMetrics` i `ObservabilityExtensions.cs` og counter-kald i Comment-/ArticleService - **skal aftales i gruppen**.
- Tidsskala: `$bucket` = 1m / `$baseline` = 30m i dummy; 1h / 7d i prod. Samme queries, kun variabler skiftes.

### 4. Simuleringsscript (ca. 10 min, profil `simulate`, postgres-image med pgbench/psql + curl)

| Fase | DB-belastning | Logs | Comments/Articles |
|---|---|---|---|
| Normal | Lav på alle | Mest info | Stabil - baseline bygges |
| Peak i Europa | Høj på `articledb-eu` + `commentdb` | Flere warnings | Comments mod UCL |
| Incident | `commentdb` > 90 % | Error/critical-spike | Comments under LCL |
| Recovery | Normaliseres | Tilbage til info | Tilbage i båndet |

### 5. Filer
```
monitoring/
├── docker-compose.yaml            # + cadvisor, loki, pushgateway, 11 dummy-DB'er, overview-simulator
├── prometheus/prometheus.yml      # + cadvisor, pushgateway
├── loki/loki-config.yaml          # kopi af configs/loki-config.yaml
├── grafana/dashboards/happyheadlines-overview.json
├── scripts/simulate-overview.sh
└── README.md
```

### 6. Rækkefølge (verifikation efter hvert trin)
0. Spike: virker cAdvisor på Docker Desktop/Windows (WSL2)? Største tekniske risiko - afklares først.
1. Metrics: dummy-DB'er + cAdvisor + tærter (tjek mod `docker stats`).
2. Logs: Loki + log-generering (tjek antal pr. niveau).
3. Kontrolkort: Pushgateway + counters + UCL/LCL (tjek mod kendt serie).
4. Samlet scenarie + README.

### 7. Risici
- Ressourcer: 11 Postgres + pgbench på en laptop → 256 MB pr. DB, få pgbench-klienter.
- Log-niveau-label i Loki: `detected_level` eller `severity_text` - afklares i trin 2.
- Kontrolkortets migrering kræver kodeændringer i services.

### Beslutninger der mangler
1. Båndgrænser og farver til tærterne.
2. Pushgateway i dummy + OTel-counters ved migrering til kontrolkortet?
3. Rækkefølgen, med cAdvisor-spike først?
