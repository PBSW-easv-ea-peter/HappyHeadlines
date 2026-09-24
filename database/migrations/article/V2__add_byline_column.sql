-- Expand phase of the journalist-ownership move (see database/migrations/README.md
-- for the full deploy order). Deployable on its own, any time: journalist_id and
-- journalists are untouched, so old, not-yet-updated ArticleService app code keeps
-- working unmodified - it never selects byline.

ALTER TABLE articles ADD COLUMN byline VARCHAR(255);

-- Nullable on purpose: old app code (pre-cutover) still inserts rows without a
-- byline, so this can't be NOT NULL yet - that tightening happens in the contract
-- migration (V3), once every instance is confirmed writing byline.
UPDATE articles a
SET byline = j.name
FROM journalists j
WHERE a.journalist_id = j.id;
