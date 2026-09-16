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
    photographer  VARCHAR(255) NOT NULL,
    title         VARCHAR(255),
    article_id    BIGINT NOT NULL
);

-- Photos
do $$
begin
	if not exists (select 1 from pg_constraint where conname = 'fk_photos_article') then
        alter table photos
            add constraint fk_photos_article
                foreign key (article_id) references articles(id);
    end if;
end $$;


-- Articles
do $$
begin
if not exists (select 1 from pg_constraint where conname = 'fk_articles_journalist') then
        alter table articles
            add constraint fk_articles_journalist
                foreign key (journalist_id) references journalists(id);
    end if;

	if not exists (select 1 from pg_constraint where conname = 'fk_articles_section') then
        alter table articles
            add constraint fk_articles_section
                foreign key (section_id) references sections(id);
    end if;

    if not exists (select 1 from pg_constraint where conname = 'check_continent') then
        alter table articles
            add constraint check_continent
                -- EU = Europe,
                -- NA = North America,
                -- SA = South America,
                -- AU = Australia,
                -- AS = Asia,
                -- AN = Antarctica,
                -- AF = Africa,
                -- GO = Global
                check (location IN ('EU', 'NA', 'SA', 'AU', 'AS', 'AN', 'AF', 'GO'));
    end if;
end $$;

-- Mock Data --
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
