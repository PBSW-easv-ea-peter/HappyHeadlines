-- Articles arriving from PublishService (published_articles exchange) carry the DraftId
-- they were published from. The unique constraint makes the queue consumer idempotent:
-- a redelivered message hits ON CONFLICT and is ignored instead of creating a duplicate.
-- Nullable because seeded articles and REST-created articles have no draft.

ALTER TABLE articles ADD COLUMN draft_id UUID UNIQUE;
