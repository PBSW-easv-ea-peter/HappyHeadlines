#!/usr/bin/env bash
# Simulates the whole HappyHeadlines flow through the real service APIs, so logs, traces
# and metrics in Grafana show realistic traffic. Runs inside the flow-simulator container
# (see README.md), so it behaves the same on Windows and Linux.
#
#   Newsroom - create draft -> submit for approval (profanity check) -> approve
#              -> publish via PublishService -> RabbitMQ -> ArticleService
#   Readers  - front page -> one article -> its comments -> sometimes a new comment
#
# Every PROFANITY_EVERY'th draft and comment contains BANNED_WORD, so the rejection paths
# show up too. A failing request is logged and counted - it never stops the run.
set -uo pipefail

DURATION_MINUTES=${DURATION_MINUTES:-60}
PUBLISH_EVERY_SECONDS=${PUBLISH_EVERY_SECONDS:-15}
READ_EVERY_SECONDS=${READ_EVERY_SECONDS:-1}
COMMENT_EVERY=${COMMENT_EVERY:-5}           # 1 in N reads posts a comment
PROFANITY_EVERY=${PROFANITY_EVERY:-10}      # 1 in N drafts/comments contains BANNED_WORD
BANNED_WORD=${BANNED_WORD:-idiot}

DRAFT_URL=${DRAFT_URL:-http://draftservice:8083}
PUBLISH_URL=${PUBLISH_URL:-http://publishservice:8084}
ARTICLE_URL=${ARTICLE_URL:-http://articleservice:8080}
COMMENT_URL=${COMMENT_URL:-http://commentservice:8082}

REGIONS=(EU NA SA AU AS AN AF)
JOURNALISTS=5   # ids 1-5, seeded by the draft migrations
SECTIONS=5      # ids 1-5, see DraftService Models/Sections.cs

drafts=0; published=0; stopped=0
reads=0; read_errors=0; comments=0; comment_errors=0

log() { echo "$(date '+%H:%M:%S') $*"; }

# request METHOD URL [JSON] - sets STATUS and BODY. Not run in a subshell, so the globals stick.
request() {
    local method=$1 url=$2 body=${3:-} out
    if [ -n "$body" ]; then
        out=$(curl -s -m 10 -w '\n%{http_code}' -X "$method" -H 'Content-Type: application/json' -d "$body" "$url")
    else
        out=$(curl -s -m 10 -w '\n%{http_code}' -X "$method" "$url")
    fi
    STATUS=${out##*$'\n'}
    BODY=${out%$'\n'*}
}

ok() { [[ $STATUS == 2* ]]; }

# Stops this draft's flow and logs which step it stopped at.
stop_draft() {
    stopped=$((stopped + 1))
    log "draft $drafts stopped at '$1': HTTP $STATUS ${BODY:0:120}"
}

publish_one() {
    drafts=$((drafts + 1))
    local journalist=$((RANDOM % JOURNALISTS + 1))
    local approver=$((journalist % JOURNALISTS + 1))
    local section=$((RANDOM % SECTIONS + 1))
    local location=${REGIONS[RANDOM % ${#REGIONS[@]}]}
    local text="Simulated article number $drafts about everyday life in region $location."
    if ((drafts % PROFANITY_EVERY == 0)); then
        # No punctuation right after the word - ProfanityChecker only splits on whitespace
        text="$text Written by an $BANNED_WORD today"
    fi

    request POST "$DRAFT_URL/api/drafts" "$(jq -n \
        --argjson j "$journalist" --argjson s "$section" --arg loc "$location" \
        --arg title "Simulated article $drafts" --arg text "$text" \
        '{journalistId: $j, title: $title, breadtext: $text, location: $loc, sectionId: $s, creditedJournalistIds: [$j]}')"
    ok || { stop_draft "create"; return; }
    local id
    id=$(jq -r '.id' <<<"$BODY")

    request POST "$DRAFT_URL/api/drafts/$id/submit-for-approval"
    ok || { stop_draft "submit"; return; }

    request POST "$DRAFT_URL/api/drafts/$id/approve" "{\"journalistId\": $approver}"
    ok || { stop_draft "approve"; return; }

    request POST "$PUBLISH_URL/api/v1/publish-draft/$id"
    ok || { stop_draft "publish"; return; }
    published=$((published + 1))
}

read_once() {
    reads=$((reads + 1))
    # Most readers start on the global front page, which is the cached one
    local location=GO
    if ((RANDOM % 10 < 3)); then
        location=${REGIONS[RANDOM % ${#REGIONS[@]}]}
    fi

    request GET "$ARTICLE_URL/api/articles/$location"
    ok || { read_errors=$((read_errors + 1)); return; }

    local ids count id
    ids=$(jq -r '.[].id' <<<"$BODY" 2>/dev/null)
    count=$(grep -c . <<<"$ids")
    ((count > 0)) || return
    id=$(sed -n "$((RANDOM % count + 1))p" <<<"$ids")

    request GET "$ARTICLE_URL/api/articles/$location/$id"
    ok || { read_errors=$((read_errors + 1)); return; }

    request GET "$COMMENT_URL/api/comments/$location/$id"
    ok || { read_errors=$((read_errors + 1)); return; }

    if ((RANDOM % COMMENT_EVERY == 0)); then
        comments=$((comments + 1))
        local text="Interesting read, thanks!"
        if ((comments % PROFANITY_EVERY == 0)); then
            text="What an $BANNED_WORD wrote this?"
        fi
        request POST "$COMMENT_URL/api/comments/$location/$id" \
            "$(jq -n --arg text "$text" --arg name "Reader $((RANDOM % 100))" '{authorName: $name, text: $text}')"
        ok || comment_errors=$((comment_errors + 1))
    fi
}

log "Simulating the full flow for $DURATION_MINUTES min: publish every ${PUBLISH_EVERY_SECONDS}s, read every ${READ_EVERY_SECONDS}s"

start=$(date +%s)
end=$((start + DURATION_MINUTES * 60))
next_publish=$start
next_status=$((start + 60))

while (($(date +%s) < end)); do
    now=$(date +%s)
    if ((now >= next_publish)); then
        publish_one
        next_publish=$((now + PUBLISH_EVERY_SECONDS))
    fi

    read_once

    if ((now >= next_status)); then
        log "drafts=$drafts published=$published stopped=$stopped | reads=$reads read_errors=$read_errors | comments=$comments comment_errors=$comment_errors"
        next_status=$((now + 60))
    fi

    sleep "$READ_EVERY_SECONDS"
done

log "Done. drafts=$drafts published=$published stopped=$stopped | reads=$reads read_errors=$read_errors | comments=$comments comment_errors=$comment_errors"
