#!/usr/bin/env bash
# Simulates reader traffic against the dummy ArticleCache and CommentCache, so the
# cache dashboard shows realistic hit/miss patterns. Runs inside the traffic-simulator
# container (see README.md), so it behaves the same on Windows and Linux.
#
#   ArticleCache - offline cache, prefilled with the last 14 days. A miss does NOT fill it.
#   CommentCache - filled on miss, limited to 30 articles, least recently used is evicted.
#
# The run is split into four equal phases:
#   1. Normal traffic   - readers mostly read the latest days
#   2. Breaking news    - new articles published after the nightly refresh
#   3. Long tail        - readers browse comments on many older articles
#   4. Midnight refresh - the batch job loads today's articles into ArticleCache
#
# Both caches and their stats are reset on start.
set -euo pipefail

DURATION_MINUTES=${DURATION_MINUTES:-8}
TICK_SECONDS=${TICK_SECONDS:-2}
REQUESTS_PER_TICK=${REQUESTS_PER_TICK:-25}
ARTICLE_HOST=${ARTICLE_HOST:-redis-article}
COMMENT_HOST=${COMMENT_HOST:-redis-comment}

ARTICLES_PER_DAY=10
CACHED_DAYS=14
COMMENT_CACHE_LIMIT=30
BREAKING_ARTICLES=5

PHASE_NAMES=("Normal traffic" "Breaking news" "Long tail" "Midnight refresh")
PHASE_BREAKING_SHARE=(0 50 10 30)   # % of requests for breaking news articles
PHASE_LONG_TAIL=(0 0 1 0)

# Sends a batch of commands through a single redis-cli connection.
send() {
    local host=$1; shift
    (( $# == 0 )) && return
    printf '%s\n' "$@" | redis-cli -h "$host" > /dev/null
}

# Key pickers set KEY instead of echoing, to avoid a subshell per request.
pick_article_key() {
    if (( RANDOM % 100 < PHASE_BREAKING_SHARE[phase] )); then
        KEY="article:breaking-$(( RANDOM % BREAKING_ARTICLES + 1 ))"
        return
    fi
    # Readers mostly read the latest days; 10% go beyond the 14 cached days.
    local r=$(( RANDOM % 100 )) age
    if   (( r < 60 )); then age=$(( RANDOM % 2 + 1 ))
    elif (( r < 90 )); then age=$(( RANDOM % (CACHED_DAYS - 2) + 3 ))
    else                    age=$(( RANDOM % 45 + CACHED_DAYS + 1 ))
    fi
    KEY="article:day${age}-$(( RANDOM % ARTICLES_PER_DAY + 1 ))"
}

pick_comment_key() {
    if (( RANDOM % 100 < PHASE_BREAKING_SHARE[phase] )); then
        KEY="comments:breaking-$(( RANDOM % BREAKING_ARTICLES + 1 ))"
        return
    fi
    if (( PHASE_LONG_TAIL[phase] && RANDOM % 100 < 60 )); then
        KEY="comments:$(( RANDOM % 260 + 41 ))"
        return
    fi
    # Squaring a 0-999 random number skews towards low ids = a few popular articles.
    local x=$(( RANDOM % 1000 ))
    KEY="comments:$(( 40 * x * x / 1000000 + 1 ))"
}

percent() {
    local hits=$1 total=$(( $1 + $2 ))
    (( total == 0 )) && { echo "  n/a"; return; }
    local p=$(( hits * 1000 / total ))
    printf '%3d.%d%%' $(( p / 10 )) $(( p % 10 ))
}

# Clean slate. Prometheus handles the counter reset via rate().
send "$ARTICLE_HOST" FLUSHALL "CONFIG RESETSTAT"
send "$COMMENT_HOST" FLUSHALL "CONFIG RESETSTAT"

# ArticleCache prefill = the nightly batch job: every article from the last 14 days.
declare -A cached_articles
cmds=()
for (( day = 1; day <= CACHED_DAYS; day++ )); do
    for (( n = 1; n <= ARTICLES_PER_DAY; n++ )); do
        cached_articles["article:day${day}-${n}"]=1
        cmds+=("SET article:day${day}-${n} body")
    done
done
send "$ARTICLE_HOST" "${cmds[@]}"
echo "ArticleCache prefilled with ${#cached_articles[@]} articles. Running for ${DURATION_MINUTES} min..."

# CommentCache LRU: lru[0] is the least recently used key.
lru=()
declare -A in_lru

start=$(date +%s)
duration=$(( DURATION_MINUTES * 60 ))
current_phase=-1
tick=0
article_hits=0 article_misses=0 comment_hits=0 comment_misses=0 evictions=0

while (( $(date +%s) - start < duration )); do
    tick_start=$(date +%s%N)
    elapsed=$(( $(date +%s) - start ))
    phase=$(( elapsed * ${#PHASE_NAMES[@]} / duration ))

    if (( phase != current_phase )); then
        current_phase=$phase
        echo
        echo "=== Phase $(( phase + 1 ))/${#PHASE_NAMES[@]}: ${PHASE_NAMES[phase]} ==="
        if [[ ${PHASE_NAMES[phase]} == "Midnight refresh" ]]; then
            cmds=()
            for (( n = 1; n <= BREAKING_ARTICLES; n++ )); do
                cached_articles["article:breaking-${n}"]=1
                cmds+=("SET article:breaking-${n} body")
            done
            send "$ARTICLE_HOST" "${cmds[@]}"
            echo "Batch job loaded ${BREAKING_ARTICLES} new articles into ArticleCache."
        fi
    fi

    article_cmds=()
    comment_cmds=()
    for (( i = 0; i < REQUESTS_PER_TICK; i++ )); do
        # ArticleCache: lookup only - on a miss the service reads the database,
        # but the offline cache is not filled.
        pick_article_key
        article_cmds+=("GET $KEY")
        if [[ -v cached_articles[$KEY] ]]; then (( ++article_hits )); else (( ++article_misses )); fi

        # CommentCache: cache-aside with LRU limited to 30 articles.
        pick_comment_key
        comment_cmds+=("GET $KEY")
        if [[ -v in_lru[$KEY] ]]; then
            (( ++comment_hits ))
            # Move the key to the most recently used end.
            for j in "${!lru[@]}"; do [[ ${lru[j]} == "$KEY" ]] && unset 'lru[j]' && break; done
            lru=("${lru[@]}")
        else
            (( ++comment_misses ))
            comment_cmds+=("SET $KEY comments")
            if (( ${#lru[@]} >= COMMENT_CACHE_LIMIT )); then
                # DEL is a write, so it doesn't affect the hit/miss counters.
                comment_cmds+=("DEL ${lru[0]}")
                unset 'in_lru[${lru[0]}]'
                lru=("${lru[@]:1}")
                (( ++evictions ))
            fi
            in_lru[$KEY]=1
        fi
        lru+=("$KEY")
    done

    send "$ARTICLE_HOST" "${article_cmds[@]}"
    send "$COMMENT_HOST" "${comment_cmds[@]}"

    if (( ++tick % 5 == 0 )); then
        elapsed=$(( $(date +%s) - start ))
        printf '[%02d:%02d] article %s | comment %s | evictions %d\n' \
            $(( elapsed / 60 )) $(( elapsed % 60 )) \
            "$(percent $article_hits $article_misses)" "$(percent $comment_hits $comment_misses)" "$evictions"
        article_hits=0 article_misses=0 comment_hits=0 comment_misses=0 evictions=0
    fi

    remaining_ms=$(( TICK_SECONDS * 1000 - ($(date +%s%N) - tick_start) / 1000000 ))
    (( remaining_ms > 0 )) && sleep "$(( remaining_ms / 1000 )).$(printf '%03d' $(( remaining_ms % 1000 )))"
done

echo
echo "Done. Totals as Redis counts them:"
for host in "$ARTICLE_HOST" "$COMMENT_HOST"; do
    echo "  $host: $(redis-cli -h "$host" INFO stats | grep -E 'keyspace_(hits|misses)' | tr -d '\r' | paste -sd ' ')"
done
