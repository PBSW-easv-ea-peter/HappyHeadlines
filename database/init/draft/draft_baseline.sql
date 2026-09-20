CREATE TABLE status (
    id   SMALLINT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE drafts (
    id                            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title                         VARCHAR(255) NOT NULL,
    breadtext                     TEXT NOT NULL,
    location                      VARCHAR(2) NOT NULL,
    section_id                    BIGINT NOT NULL,
    created_by_journalist_id      BIGINT NOT NULL,
    created_date                  TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_edited_by_journalist_id  BIGINT NOT NULL,
    last_edited_date              TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    approved_by_journalist_id     BIGINT,
    approved_date                 TIMESTAMPTZ,
    flagged_words                 TEXT[] NOT NULL DEFAULT '{}', -- set by ProfanityService when submitted for approval; reset when the content is edited
    status                        SMALLINT NOT NULL DEFAULT 0, -- 0=WorkInProgress, 1=PendingApproval, 2=Approved, 3=Published, 4=Archived
    review_note                   TEXT, -- optional reviewer feedback set on approve/reject; cleared on the next submit-for-approval

    CONSTRAINT fk_drafts_status
        FOREIGN KEY (status)
        REFERENCES status (id),

    -- journalist_id and section_id are NOT foreign keys: journalists/sections live in
    -- ArticleService's own database, and DraftService intentionally doesn't reach across
    -- service boundaries to enforce that (see CommentService's comments table for the same
    -- pattern with article_id/article_location).
    CONSTRAINT check_draft_location
        CHECK (location IN ('EU', 'NA', 'SA', 'AU', 'AS', 'AN', 'AF', 'GO'))
);
