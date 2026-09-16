-- Placeholder seed list - see "Kendt gæld" i docs/comment_and_profanity_service.md.
INSERT INTO banned_words (word)
VALUES ('idiot'), ('stupid'), ('dumb')
ON CONFLICT (word) DO NOTHING;
