---
course: Development of Large Systems
type: service-contract
week: <NN>
company: Happy Headlines
service: <ServiceName>
---

# <ServiceName>

> Beskriver koden, som den er i dag. Opdateres i samme commit som ændringer i endpoints, køer eller afhængigheder.

- **Ansvar:** <én sætning>
- **Ejer data:** <database → tabeller>
- **Commands (andre kan bede den om):** <ændrer data>
- **Queries (andre kan spørge om):** <ændrer ikke data>
- **Publicerer:** <events / routing keys, eller "–">
- **Afhænger af:** <service/kø → hvilke data>
- **Afvigelse:** <hvor koden afviger fra arkitekturen, eller "–">

## Endpoint-map

Base-URL (lokalt): `http://localhost:<port>`

| Type  | Metode   | Route / Emne         | Beskrivelse |
|-------|----------|----------------------|-------------|
| REST  | GET      | /api/<resource>      | ...         |
| Queue | consume  | <exchange>/<key>     | ...         |

Uddybning: [<arkitekturdokument>](../<fil>.md)
