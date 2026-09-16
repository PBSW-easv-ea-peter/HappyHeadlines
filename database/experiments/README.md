# Experiments

Ad-hoc SQL til manuel afprøvning af databaselogik (constraints, lookup-mønstre osv.) —
ikke automatiserede tests, og ikke en del af `init/` eller `queries/`. Køres manuelt via
`psql` mod den relevante database, når man vil verificere at et constraint eller mønster
opfører sig som forventet.
