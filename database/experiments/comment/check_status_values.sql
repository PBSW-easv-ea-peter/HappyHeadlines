-- Verifies check_comment_status rejects a status outside 0/1/2. Expect: ERROR (constraint violation).
begin;
insert into comments (article_id, article_location, author_name, text, status)
values (999, 'GO', 'Test', 'Invalid status test.', 3);
rollback;

-- Sanity check: status distribution of the current seed data.
select status, count(*) from comments group by status order by status;
