-- Mock comments for local testing of CommentService.
-- NB: CommentDb and ArticleDb are separate Postgres instances - article_id/article_location
-- here are NOT enforced by a foreign key. The values below match the insertion order of the
-- mock articles seeded by ArticleService's ArticleSeeder.cs (run it first if you want the
-- ids to actually resolve to real articles via the API).

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
            (1, 'GO', 'Alice', 'Great overview, thanks for the summary!', 0),
            (1, 'GO', 'Bob', 'Not sure I agree with all the projections here.', 0),
            (4, 'EU', 'Carla', 'Nuclear as a "temporary solution" feels like a stretch.', 1),
            (7, 'NA', 'Dave', 'This is such an idiot take.', 2),
            (10, 'SA', 'Elena', 'Good to see deforestation numbers finally improving.', 0);
    end if;
end $$;
