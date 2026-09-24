# Migrations

Each service (`article/`, `draft/`) has its own independent Flyway migration history
against its own database. Flyway doesn't enforce any ordering *between* the two
histories, and it can't - `articledb-*-migrate` and `draftdb-migrate` are separate
one-shot containers that only know about their own schema. The safety of a
multi-service change like the journalist-ownership move comes entirely from the
order these are actually *deployed* in, which this document exists to capture.

## Expand/contract

Any change that replaces an existing shape with a new one is split into two
migrations instead of one atomic change:

- **Expand** - add the new shape without touching the old one. Safe to deploy any
  time, on its own, since nothing yet depends on it.
- **Contract** - remove the old shape. Destructive, and only safe once every
  consumer (every deployed instance of every service, and the frontend) has been
  confirmed to have moved off the old shape.

A service that never had an old representation to begin with only needs an expand
step - there's nothing to contract.

## The journalist-ownership move, in real deploy order

1. **DraftService `V2`** (expand, additive) - creates `journalists` (with nullable
   `email`/`phone`, since the copied rows never have that data), `draft_journalists`,
   and `drafts.byline`. Safe to ship any time; nothing depends on it yet.
2. **DraftService app code** (`Journalist`/`JournalistRepository`/
   `JournalistsController`, the credited-authors additions to `Draft`) - safe once
   (1) is live. Old WebApp code simply never calls the new `/api/journalists`
   endpoint or sends the new optional `CreditedJournalistIds`/`Byline` request
   fields, so it keeps working unmodified.
3. **ArticleService `V2`** (expand, additive) - adds nullable `byline`, backfills it
   from the existing `journalist_id` join. Independent of (1)/(2), safe any time;
   old ArticleService code never selects `byline`.
4. **ArticleService app code cutover** - `ArticleReadRepository`/
   `ArticleWriteRepository`/`ArticleSeeder` switch to reading/writing `byline`
   instead of `journalist_id`. Safe once (3) is live.
5. **WebApp** - drops the hardcoded journalist list for DraftService's
   `/api/journalists`, switches from the old `journalistName` field to `byline`,
   adds the credited-authors editor. Safe only once (2) *and* (4) are both live -
   this is the "frontend has to look in the right place" dependency.
6. **ArticleService `V3`** (contract, destructive) - makes `byline` `NOT NULL`,
   drops `journalist_id`/`journalists`. Only ships once every deployed
   ArticleService instance is confirmed on step (4) and nothing else still reads
   the old columns.

DraftService needs no contract step of its own - it never had journalist data
before this move, so there's no old shape to remove.

## Copying the journalist rows themselves

`journalist_id`/`journalists` in ArticleService and the new `journalists` table in
DraftService are two separate databases - there's no way to `SELECT ... FROM` one
into the other directly. `draft/tools/export_journalists_from_article.sh` queries
whatever journalists actually exist in ArticleService's database *right now* and
prints the `INSERT` statement that becomes `draft/V2`'s seed data.

This is deliberately a manual, one-off tool, not something `flyway migrate` runs
automatically: a migration has to stay valid forever, since every fresh environment
replays the full history from `V1` onward - but by the time a new environment does
that, ArticleService's `journalists` table won't exist anymore (step 6 above has
dropped it). So the copy can only happen once, for real, at the actual moment of
cutover, and its output gets frozen into `draft/V2` as an ordinary, replayable
migration from then on.

Practical implication: if a journalist is added on the ArticleService side after
this move has already happened elsewhere, they are **not** picked up automatically.
Re-run the export script against the live ArticleService database and update
`draft/V2` (or a new migration, if `draft/V2` has already shipped anywhere) before
that journalist needs to show up in DraftService.

### One more wrinkle: ArticleService is sharded

`journalists` isn't one table - ArticleService has 8 regional shards
(`articledb-eu/na/sa/au/as/an/af/global`), each with its own copy, seeded the same
way everywhere. They're supposed to always agree, but nothing enforces that, so a
journalist added to only one shard would be silently missed if the export only read
one of them. The script queries all 8 and only generates the `INSERT` if every
shard agrees; if they don't, it prints exactly which shard disagrees and how, and
exits without generating anything - resolving a real disagreement between shards is
a business decision (which one is "right"?), not something the script should guess
at. Verified by seeding 8 throwaway containers identically (script succeeds),
then adding one extra journalist to a single shard (script catches it, names the
shard, shows the diff, and refuses to proceed).
