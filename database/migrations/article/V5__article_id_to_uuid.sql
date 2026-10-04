-- Article ids become UUIDs instead of a BIGINT identity per shard.
--
-- Not split into expand/contract (see database/migrations/README.md): the id is part of
-- every article URL, so old and new ArticleService/CommentService/clients can't run side
-- by side anyway. Ship this together with the services and clients that use Guid ids.
--
-- Existing rows get a deterministic id, md5('<location>:<old id>')::uuid, rather than a
-- random one. CommentService's database is a separate Postgres instance and can't look the
-- new ids up, but it can compute the same value from (article_location, article_id) - see
-- database/queries/comment/migrate_article_id_to_uuid.sql.
--
-- New rows get gen_random_uuid() unless the inserter supplies an id (ArticleSeeder does).

ALTER TABLE articles ADD COLUMN new_id UUID;
UPDATE articles SET new_id = md5(location || ':' || id)::uuid;
ALTER TABLE articles ALTER COLUMN new_id SET NOT NULL;
ALTER TABLE articles ALTER COLUMN new_id SET DEFAULT gen_random_uuid();

-- photos.article_id follows its article.
ALTER TABLE photos ADD COLUMN new_article_id UUID;
UPDATE photos p SET new_article_id = a.new_id FROM articles a WHERE p.article_id = a.id;
ALTER TABLE photos DROP CONSTRAINT fk_photos_article;
ALTER TABLE photos DROP COLUMN article_id;
ALTER TABLE photos RENAME COLUMN new_article_id TO article_id;
ALTER TABLE photos ALTER COLUMN article_id SET NOT NULL;

ALTER TABLE articles DROP CONSTRAINT articles_pkey;
ALTER TABLE articles DROP COLUMN id;
ALTER TABLE articles RENAME COLUMN new_id TO id;
ALTER TABLE articles ADD CONSTRAINT articles_pkey PRIMARY KEY (id);

ALTER TABLE photos
    ADD CONSTRAINT fk_photos_article
        FOREIGN KEY (article_id) REFERENCES articles(id);
