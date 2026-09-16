-- Verifies check_continent rejects an invalid location code. Expect: ERROR (constraint violation).
begin;
insert into articles (journalist_id, title, breadtext, location, section_id)
values (1, 'Invalid location test', 'Should be rejected by check_continent.', 'ZZ', 1);
rollback;
