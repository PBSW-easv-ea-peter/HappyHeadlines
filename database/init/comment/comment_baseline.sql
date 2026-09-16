CREATE TABLE status (
    id   SMALLINT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE comments (
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    article_id       BIGINT NOT NULL,
    article_location VARCHAR NOT NULL,
    author_name      VARCHAR(25) NOT NULL,
    text             VARCHAR(500) NOT NULL,
    created_date     TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status           SMALLINT NOT NULL DEFAULT 1, -- 0 = Approved, 1 = PendingProfanityCheck, 2 = Rejected

    CONSTRAINT fk_comments_status
        FOREIGN KEY (status)
        REFERENCES status (id)
);
