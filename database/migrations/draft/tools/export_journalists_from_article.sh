#!/usr/bin/env bash
# One-time operational tool for the journalist-ownership cutover - NOT a Flyway
# migration. A migration has to stay valid forever (every fresh environment replays
# the full history from V1), but this depends on live data in ArticleService's
# database that won't exist anymore once ArticleService's own contract migration
# (V3) has dropped the table. So this has to run once, against the real database,
# at the actual moment of cutover - its output becomes the frozen INSERT statement
# committed into DraftService's migration.
#
# ArticleService is sharded 8 ways (eu/na/sa/au/as/an/af/global) and each shard has
# its own copy of the journalists reference table - they're supposed to be
# identical (seeded the same everywhere), but nothing enforces that, so a
# journalist added to only one shard would be silently missed if this only read
# one. This checks every shard and only proceeds if they all agree; if they don't,
# it refuses to guess and prints the disagreement instead, since deciding which
# shard is "right" is a business call, not something a script should resolve on
# its own.
#
# Re-run this against the live ArticleService shards whenever journalists change
# there and before authoring/updating the DraftService migration - it is
# deliberately not wired into `flyway migrate` itself.
#
# Usage: ./export_journalists_from_article.sh [source-db]
set -euo pipefail

SHARDS=(articledb-eu articledb-na articledb-sa articledb-au articledb-as articledb-an articledb-af articledb-global)
SOURCE_DB="${1:-articles}"
export PGPASSWORD=Postgres1234

QUERY="SELECT id, name, operations FROM journalists ORDER BY id"

echo "Checking journalists agree across all ${#SHARDS[@]} shards..." >&2

declare -A SHARD_ROWS
for shard in "${SHARDS[@]}"; do
    SHARD_ROWS["$shard"]=$(psql -h "$shard" -U postgres -d "$SOURCE_DB" -t -A -F'|' -c "$QUERY")
done

REFERENCE_SHARD="${SHARDS[0]}"
REFERENCE_ROWS="${SHARD_ROWS[$REFERENCE_SHARD]}"
DISAGREEMENT=0

for shard in "${SHARDS[@]:1}"; do
    if [ "${SHARD_ROWS[$shard]}" != "$REFERENCE_ROWS" ]; then
        echo "DISAGREEMENT: ${shard} does not match ${REFERENCE_SHARD}:" >&2
        diff <(echo "$REFERENCE_ROWS") <(echo "${SHARD_ROWS[$shard]}") >&2 || true
        DISAGREEMENT=1
    fi
done

if [ "$DISAGREEMENT" -eq 1 ]; then
    echo "" >&2
    echo "Shards disagree on who the journalists are - refusing to generate an INSERT." >&2
    echo "Resolve which shard is authoritative by hand, then re-run this script." >&2
    exit 1
fi

echo "All ${#SHARDS[@]} shards agree. Generating INSERT from ${REFERENCE_SHARD}..." >&2

psql -h "$REFERENCE_SHARD" -U postgres -d "$SOURCE_DB" -t -A -c "
    SELECT 'INSERT INTO journalists (id, name, operations, email, phone) OVERRIDING SYSTEM VALUE VALUES' || E'\n'
        || string_agg(format('    (%s, %L, %L, NULL, NULL)', id, name, operations), E',\n' ORDER BY id)
        || ';' || E'\n\n'
        || format('SELECT setval(pg_get_serial_sequence(''journalists'', ''id''), %s);', max(id))
    FROM journalists;
"
