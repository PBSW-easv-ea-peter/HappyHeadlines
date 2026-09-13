CREATE TABLE banned_words (
    id   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    word VARCHAR(255) NOT NULL UNIQUE
);

-- Placeholder seed list - see "Kendt gæld" in docs/comment_and_profanity_services.md.
INSERT INTO banned_words (word)
VALUES ('idiot'), ('stupid'), ('dumb')
ON CONFLICT (word) DO NOTHING;
