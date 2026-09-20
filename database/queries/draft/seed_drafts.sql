-- Mock drafts for local testing of DraftService.
-- NB: DraftDatabase is a separate Postgres instance from ArticleService's databases -
-- created_by_journalist_id/section_id here are NOT enforced by a foreign key. The values
-- below just reuse journalist ids (1-8) and section ids (1-5) from ArticleService's
-- ArticleSeeder.cs mock data, so they resolve to something real if you seed that too.

INSERT INTO status (id, name)
VALUES
    (0, 'WorkInProgress'),
    (1, 'PendingApproval'),
    (2, 'Approved'),
    (3, 'Published'),
    (4, 'Archived')
ON CONFLICT (id) DO NOTHING;

do $$
begin
    if not exists (select 1 from drafts where title = 'Local Elections: What to Watch For') then
        insert into drafts
            (title, breadtext, location, section_id, created_by_journalist_id, last_edited_by_journalist_id, status)
        values
            ('Local Elections: What to Watch For',
             'A first pass at covering the upcoming local elections - still needs a second source.',
             'EU', 1, 1, 1, 0), -- WorkInProgress

            ('New Bridge Opens After Years of Delays',
             'The long-awaited bridge finally opened to traffic this morning. Only a stupid person would have doubted it.',
             'EU', 3, 2, 2, 1), -- PendingApproval

            ('Startup Raises Record Funding Round',
             'A local startup has raised the largest seed round in the region''s history.',
             'NA', 2, 3, 3, 2), -- Approved

            ('City Council Approves New Park',
             'The city council voted unanimously to approve funding for a new public park.',
             'NA', 1, 4, 4, 4); -- Archived (scrapped after the story fell through)

        update drafts
        set approved_by_journalist_id = 5, approved_date = current_timestamp
        where title = 'Startup Raises Record Funding Round';

        -- Shows what a real profanity-on-submit check would have flagged for the editor.
        update drafts
        set flagged_words = '{stupid}'
        where title = 'New Bridge Opens After Years of Delays';
    end if;
end $$;
