CREATE TABLE status (
    id   SMALLINT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE drafts (
    id                            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
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

    -- As of V1, created_by_journalist_id/last_edited_by_journalist_id/approved_by_journalist_id
    -- are plain BIGINT, not FKs: journalist data still lives in ArticleService's database at
    -- this point. V2 changes this once journalist ownership moves into DraftService.
    CONSTRAINT check_draft_location
        CHECK (location IN ('EU', 'NA', 'SA', 'AU', 'AS', 'AN', 'AF', 'GO'))
);

INSERT INTO status (id, name) VALUES
    (0, 'WorkInProgress'),
    (1, 'PendingApproval'),
    (2, 'Approved'),
    (3, 'Published'),
    (4, 'Archived');
