CREATE TABLE status (
    id   SMALLINT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE comments (
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    article_id       UUID NOT NULL, -- articles.id in the article shard for article_location (no FK: separate database)
    article_location VARCHAR NOT NULL,
    author_name      VARCHAR(25) NOT NULL,
    text             VARCHAR(500) NOT NULL,
    created_date     TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status           SMALLINT NOT NULL DEFAULT 1, -- 0 = Approved, 1 = PendingProfanityCheck, 2 = Rejected

    CONSTRAINT fk_comments_status
        FOREIGN KEY (status)
        REFERENCES status (id)
);

-- Seed data
INSERT INTO status (id, name)
VALUES
    (0, 'Approved'),
    (1, 'PendingProfanityCheck'),
    (2, 'Rejected')
ON CONFLICT (id) DO NOTHING;

do $$
begin
    if not exists (select 1 from comments where author_name = 'Alice' and text = 'Great overview, thanks for the summary!') then
        insert into comments (article_id, article_location, author_name, text, status) values
            (md5('GO:Global Climate Summit 2026: Key Takeaways')::uuid, 'GO', 'Alice', 'Great overview, thanks for the summary!', 0),
            (md5('GO:Global Climate Summit 2026: Key Takeaways')::uuid, 'GO', 'Bob', 'Not sure I agree with all the projections here.', 0),
            (md5('EU:EU Energy Crisis: Solutions and Challenges')::uuid, 'EU', 'Carla', 'Nuclear as a "temporary solution" feels like a stretch.', 1),
            (md5('NA:US Election 2026: Early Predictions')::uuid, 'NA', 'Dave', 'This is such an idiot take.', 2),
            (md5('SA:Amazon Rainforest: Deforestation at a Record Low')::uuid, 'SA', 'Elena', 'Good to see deforestation numbers finally improving.', 0);
    end if;
end $$;
