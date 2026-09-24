-- Repeatable migration: Flyway reruns this automatically whenever its content changes.
-- Mock drafts for local testing/demo of DraftService. Reuses journalist ids 1-8 seeded in V2.

DELETE FROM drafts;

INSERT INTO drafts
    (title, breadtext, location, section_id, created_by_journalist_id, last_edited_by_journalist_id, status)
VALUES
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

UPDATE drafts
SET approved_by_journalist_id = 5, approved_date = current_timestamp
WHERE title = 'Startup Raises Record Funding Round';

-- Shows what a real profanity-on-submit check would have flagged for the editor.
UPDATE drafts
SET flagged_words = '{stupid}'
WHERE title = 'New Bridge Opens After Years of Delays';
