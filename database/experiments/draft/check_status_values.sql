-- Verifies fk_drafts_status rejects a status outside 0-4. Expect: ERROR (constraint violation).
begin;
insert into drafts (title, breadtext, location, section_id, created_by_journalist_id, last_edited_by_journalist_id, status)
values ('Test', 'Test body', 'GO', 1, 1, 1, 5);
rollback;

-- Sanity check: status distribution of the current seed data.
select status, count(*) from drafts group by status order by status;
