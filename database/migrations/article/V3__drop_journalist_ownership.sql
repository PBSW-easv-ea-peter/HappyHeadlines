-- Contract phase of the journalist-ownership move (see database/migrations/README.md
-- for the full deploy order). Destructive - only ships once every deployed
-- ArticleService instance is confirmed reading/writing byline (never journalist_id)
-- and DraftService already has its own copy of the journalist data (see
-- database/migrations/draft/tools/export_journalists_from_article.sh).

ALTER TABLE articles ALTER COLUMN byline SET NOT NULL;

ALTER TABLE articles DROP CONSTRAINT fk_articles_journalist;
ALTER TABLE articles DROP COLUMN journalist_id;
DROP TABLE journalists;
