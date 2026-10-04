-- TRUE when the draft was submitted for approval while ProfanityService couldn't be asked
-- (circuit open, unreachable, timeout, error). flagged_words is then empty without the text
-- having been checked, so the editor must not be told "no issues found".
-- Defaults to FALSE so existing drafts keep their current meaning. Reset when the content is edited.
ALTER TABLE drafts
    ADD COLUMN profanity_check_skipped BOOLEAN NOT NULL DEFAULT FALSE;
