-- Reference data required for ArticleSeeder.cs's mock articles (journalist_id / section_id 1-8)
-- to satisfy the fk_articles_journalist / fk_articles_section constraints in both prod and dev.
do $$
begin
    if not exists (select 1 from journalists) then
        insert into journalists (name, operations) values
            ('Emma Johnson', 'Global News'),
            ('Liam Chen', 'Tech Today'),
            ('Sophia Garcia', 'Green Earth'),
            ('Noah Wilson', 'Health Watch'),
            ('Olivia Brown', 'Culture Pulse'),
            ('James Lee', 'Political Insight'),
            ('Ava Martinez', 'Science Daily'),
            ('Ethan Davis', 'World Affairs');
    end if;

    if not exists (select 1 from sections) then
        insert into sections (name) values
            ('Politics'),
            ('Technology'),
            ('Environment'),
            ('Health'),
            ('Culture');
    end if;
end $$;
