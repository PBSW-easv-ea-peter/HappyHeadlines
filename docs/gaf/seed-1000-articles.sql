-- GAF / Defer Fetching: 1.000 testartikler i EU-shardet, så effekten af pagination kan måles
-- ved en realistisk datamængde. Deterministisk (ingen tilfældighed), så før og efter får samme data.
--
--   docker exec -i happyheadlines-dev-articledb-eu-1 psql -U postgres -d articles < docs/gaf/seed-1000-articles.sql
--
-- Brødteksten er ca. 2.000 tegn (en kort nyhedsartikel). Artiklerne er publiceret med en times
-- mellemrum bagud i tid, så de nyeste ligger først på forsiden.

INSERT INTO articles (title, breadtext, created_date, publish_date, location, section_id, byline)
SELECT
    'GAF testartikel ' || n,
    repeat('Happy Headlines bringer gode nyheder fra hele verden. Denne testartikel bruges til at maale svarstoerrelse. ', 19),
    now() - (n || ' hours')::interval,
    now() - (n || ' hours')::interval,
    'EU',
    1 + (n % 5),
    'GAF Seeder'
FROM generate_series(1, 1000) AS n;
