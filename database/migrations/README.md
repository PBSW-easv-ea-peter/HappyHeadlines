# Migrations

## Pulling this after running the old (pre-Flyway) compose stack

If you ever ran `docker compose up` on this project before the Flyway switch, `down`
alone won't clean up: Postgres's own image declares an anonymous volume for its data
directory, and `docker compose up` carries that volume forward into the recreated
`articledb-*`/`draftdb` containers even though nothing in this repo's compose files
names it. The old init-script schema is still sitting there, Flyway finds a non-empty
database with no `flyway_schema_history` table, and refuses to migrate - the
migrate containers exit 1 and `articleservice`/`draftservice` never start (verified
by reproducing it: same failure both times).

Fix: `docker compose down -v` (the `-v` is what actually drops the volume) before
your next `docker compose up` on this branch or later.

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

## If this were a real, already-running system

Everything above is written as if the deploy order is the only thing standing
between us and safety. That's true for this project, because our "deploy" is a
single `docker compose up` that runs `V1` through `V3` atomically before any app
code ever starts - there's no window where two versions of a service are live at
once, no real data to lose, and no team of ops people we'd wake up at 3am. A real,
already-running system with real traffic and real data would still need everything
above, plus several things this project doesn't have and isn't pretending to:

**Adopting Flyway on top of data that already exists.** We reproduced this exact
failure by accident (see "Pulling this after running the old (pre-Flyway) compose
stack" above): Flyway finds a non-empty schema with no `flyway_schema_history`
table and refuses to migrate. Locally the fix is `docker compose down -v` - throw
the data away and start clean. A real system can't throw its data away. The actual
fix is `flyway baseline -baselineVersion=1`, which tells Flyway "trust me, `V1` is
already applied, just record it without running it." That's a one-time, unverified
assertion that `article/V1`/`draft/V1` exactly match whatever the live database
actually looks like after however many years of ad-hoc changes. If they don't
match, nothing catches it - Flyway just starts applying `V2` onward on top of a
wrong assumption.

**There is no undo.** Flyway OSS (`flyway/flyway:10-alpine`, what we run here) has
no automated undo migrations - that's a paid Teams-edition feature. So "rolling
back" a contract step like `article/V3` (`DROP TABLE journalists`,
`DROP COLUMN journalist_id`) isn't a migration operation at all; it's restoring
from a database backup taken immediately beforehand. That has real costs a
migration rollback wouldn't: every article created or edited between the backup
and the restore is gone or needs manual reconciliation; ArticleService is sharded
across 8 independent databases, so a *consistent* restore means landing all 8 on
the same point in time, not just the one that had the problem; and DraftService's
`journalists` table (a one-time frozen copy from the export script above, not a
live sync) doesn't roll back with it - a restored ArticleService and DraftService's
already-existing copy could end up disagreeing about who's a journalist.

**Step 4 above isn't actually safe yet.** The deploy order says the ArticleService
app-code cutover (step 4) is safe once `article/V2` is live (step 3) - but
`ArticleWriteRepository.CreateAsync` already never populates `journalist_id`, which
is still `NOT NULL` until `V3` (step 6) drops it. In our all-at-once deploy model
this never surfaces, because steps 3 and 6 both finish before step 4's code is
ever asked to insert anything. In a real rolling deploy - old and new ArticleService
replicas serving live traffic side by side while the rollout is in progress - every
article create or edit routed to a new-code replica during that window would throw
a `NOT NULL` violation. This is a real gap in the current code, not just a
documentation nitpick: before this deploy order could be trusted against a real
system, `journalist_id` would need to stay nullable (or default to something) through
the expand window, with the write path populated until `V3` actually ships.

**A contract step needs proof, not confidence.** Step 6 says it "only ships once
every deployed ArticleService instance is confirmed on step 4" - confirmed how?
A real system would want a monitoring signal (e.g. zero requests observed touching
`journalist_id` over some soak window across every instance) before running
anything destructive, plus a fresh backup taken right before, precisely because of
the previous point: there's no undo if that confidence turns out to be wrong.
Neither exists here.

**A shard can silently fall behind.** The export script above already treats
"the 8 shards disagree" as a real, expected failure mode worth guarding against.
The same is true one level up: if one of the 8 `articledb-*-migrate` jobs fails
partway through a rollout (disk full, a network blip) while the other 7 succeed,
there's no cross-shard transaction tying them together - that region is now
silently running a different schema version than everywhere else, and application
code written for one schema breaks only in that region. `depends_on:
service_completed_successfully` only checks "did this one migrate container exit
0 just now" - nothing here would notice "one region has drifted" later.

**The tempting shortcut is also the trap.** `FLYWAY_BASELINE_ON_MIGRATE: true`
would make the adoption problem in the first point above go away quietly instead
of failing loudly - Flyway would just assume the existing schema matches `V1` and
proceed. That's exactly backwards from what you want: today's failure is safe
because it's loud and stops before anything happens; a wrong baseline assumption
under `baselineOnMigrate` fails silently, potentially applying `V2`/`V3` against a
schema they were never written for.
