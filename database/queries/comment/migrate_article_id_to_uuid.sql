-- One-off: converts comments.article_id from BIGINT to UUID in an existing comment database.
-- Fresh databases don't need it - comment_baseline.sql already creates the UUID column.
--
-- Run it together with article migration V5__article_id_to_uuid.sql, which gives every
-- existing article the id md5('<location>:<old id>')::uuid. Computing the same value here
-- keeps each comment attached to its article without a lookup across the two databases.
--
--   psql -U postgres -d comments -f migrate_article_id_to_uuid.sql

BEGIN;

ALTER TABLE comments
    ALTER COLUMN article_id TYPE UUID
    USING md5(upper(article_location) || ':' || article_id)::uuid;

COMMIT;
