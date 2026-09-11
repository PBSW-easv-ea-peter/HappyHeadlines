CREATE TABLE comments (
    id           BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    article_id   BIGINT NOT NULL,
    author_name  VARCHAR(255) NOT NULL,
    text         TEXT NOT NULL,
    created_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status       SMALLINT NOT NULL DEFAULT 1 -- 0 = Approved, 1 = PendingProfanityCheck, 2 = Rejected
);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'check_comment_status') then
        alter table comments
            add constraint check_comment_status
                check (status in (0, 1, 2));
    end if;
end $$;
