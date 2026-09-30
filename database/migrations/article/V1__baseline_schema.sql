CREATE TABLE journalists (
    id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name       VARCHAR(255) NOT NULL,
    operations VARCHAR(255) NOT NULL
);

CREATE TABLE sections (
    id   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

CREATE TABLE articles (
    id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    journalist_id BIGINT NOT NULL,
    title         VARCHAR(255) NOT NULL,
    breadtext     TEXT NOT NULL,
    created_date  TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    publish_date  TIMESTAMPTZ,
    location      VARCHAR(2) NOT NULL,
    section_id    BIGINT NOT NULL
);

CREATE TABLE photos (
    id           BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    photographer VARCHAR(255) NOT NULL,
    title        VARCHAR(255),
    article_id   BIGINT NOT NULL
);

ALTER TABLE photos
    ADD CONSTRAINT fk_photos_article
        FOREIGN KEY (article_id) REFERENCES articles(id);

ALTER TABLE articles
    ADD CONSTRAINT fk_articles_journalist
        FOREIGN KEY (journalist_id) REFERENCES journalists(id);

ALTER TABLE articles
    ADD CONSTRAINT fk_articles_section
        FOREIGN KEY (section_id) REFERENCES sections(id);

ALTER TABLE articles
    ADD CONSTRAINT check_continent
        -- EU = Europe, NA = North America, SA = South America, AU = Australia,
        -- AS = Asia, AN = Antarctica, AF = Africa, GO = Global
        CHECK (location IN ('EU', 'NA', 'SA', 'AU', 'AS', 'AN', 'AF', 'GO'));

-- Reference data required by ArticleSeeder.cs's mock articles (journalist_id/section_id 1-8)
-- to satisfy the fk_articles_journalist / fk_articles_section constraints.
INSERT INTO journalists (name, operations) VALUES
    ('Emma Johnson', 'Global News'),
    ('Liam Chen', 'Tech Today'),
    ('Sophia Garcia', 'Green Earth'),
    ('Noah Wilson', 'Health Watch'),
    ('Olivia Brown', 'Culture Pulse'),
    ('James Lee', 'Political Insight'),
    ('Ava Martinez', 'Science Daily'),
    ('Ethan Davis', 'World Affairs');

INSERT INTO sections (name) VALUES
    ('Politics'),
    ('Technology'),
    ('Environment'),
    ('Health'),
    ('Culture');
