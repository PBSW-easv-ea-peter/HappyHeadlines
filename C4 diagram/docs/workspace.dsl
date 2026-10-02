workspace "Happy Headlines" "Positive news platform" {

    model {

        // People
        publisher = person "Publisher" "Writes and publishes articles."
        reader = person "Reader" "Reads articles, comments, and subscribes to newsletters."
        developer = person "Developer" "Builds and operates Happy Headlines; follows logs and traces across services."

        // Software system
        happyHeadlines = softwareSystem "Happy Headlines" "Positive news platform." {

            // Article
            !include models/services/article.dsl

            // UI and Web Server - Separated to show that the UI in browser has relations, but the web server does not
            ui = container "UI" "Blazor WASM UI running in browser. Editorial interface for publishers and reading interface for readers." "Blazor WASM" "Partial"
            webServer = container "Web Server" "Serves Blazor WASM files to browsers." "NGINX" "Partial"

            // Draft
            !include models/services/draft.dsl

            // Comment
            !include models/services/comment.dsl

            // Profanity
            !include models/services/profanity.dsl

            // Observability
            !include models/observability.dsl

            // Services (not yet implemented)
            publisherService = container "PublisherService" "Publishes approved articles." "Service"
            subscriberService = container "SubscriberService" "Manages newsletter subscriptions." "Service"
            newsletterService = container "NewsletterService" "Sends newsletters." "Service"

            // Suggested - not part of the original design (see L4/README.md)
//            userService = container "UserService" "Manages users: User, Journalist, Publisher." "Service" "Suggested"

            // Databases (not yet implemented)
            subscriberDb = container "SubscriberDatabase" "Stores subscriber information." "Database" "Database"

            // Queues
            !include models/rabbitmq.dsl
            subscriberQueue = container "SubscriberQueue" "Queue for new newsletter subscriptions." "Queue" "Queue"
        }

        // Relations

        // Article
        !include relations/article.dsl

        // Publisher workflow
        publisher -> ui "Creates drafts and publishes articles"

        // DraftService component-level relations (imply the container-level relations
        // UI -> DraftService, DraftService -> DraftDatabase/ProfanityService)
        ui -> draftsController "Saves and retrieves drafts"
        draftRepository -> draftDb "Reads and writes drafts in"
        // Known deviation: per the handout, PublisherService owns the profanity check before
        // publishing. Kept here until PublisherService exists (docs/logging.md, open question 8).
        // Declared at container level first: implied relationships do not copy tags, so without
        // this line the L2 view would not show the Deviation style.
        draftService -> profanityService "Checks draft text (known deviation - belongs to PublisherService)" "HTTP" "Deviation"
        draftProfanityClient -> profanityService "Checks draft text via HTTP POST /api/profanity/check (known deviation - belongs to PublisherService)" "HTTP" "Deviation"

        ui -> publisherService "Publishes article"

        publisherService -> profanityService "Checks article content"

        publisherService -> publishedArticlesExchange "Publishes approved article"

        // ProfanityService component-level relations (imply the ProfanityService
        // container-level relation to ProfanityDatabase, so no separate one here)
        profanityRepository -> profanityDb "Reads prohibited words from"


        // Reader - articles
        reader -> ui "Reads articles"
        // ui -> articleService "Requests articles"
        ui -> articleService "Requests articles"
        // articleServiceLB -> articleService is implied by articleServiceLB -> articlesController above

        // Reader - comments
        reader -> ui "Posts comments"
        ui -> commentService "Creates and retrieves comments"

        // CommentService component-level relations (imply the CommentService container-level
        // relations to ProfanityService/CommentDatabase, so no separate container-level ones here)
        profanityClient -> profanityService "Checks comment text via HTTP POST /api/profanity/check"
        commentRepository -> commentDb "Reads and writes comments in"


        // Reader - newsletter subscription
        reader -> ui "Subscribes to newsletter"
        ui -> subscriberService "Registers subscriber"

        subscriberService -> subscriberDb "Reads and writes subscriber data"
        subscriberService -> subscriberQueue "Queues new subscribers"


        // Newsletter
        !include relations/newsletter.dsl

        // Suggested UserService
        //articleService -> userService "Looks up journalists (suggested)"
        //draftService -> userService "Looks up journalists (suggested)"


        // Observability (week 38). Component-level relations imply the container-level
        // DraftService/CommentService/ProfanityService -> OTel Collector relations.
        draftObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        commentObservability -> otelForwarder "Exports logs and traces via" "OTLP/HTTP"
        otelForwarder -> otelCollector "Exports logs and traces via" "OTLP/HTTP"

        profanityObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"

        // Observability
        !include relations/observability.dsl

        // Shared deployment variables
        //routingMesh = infrastructureNode "Swarm routing mesh" "Built-in ingress load balancing across service replicas"

        // Deployments: Production
        deploymentEnvironment "Production" {
            !include deployment/euSite/eu.dsl
        }
    }

    views {

        // Level 1 - System Context
        systemContext happyHeadlines "SystemContext" {
            include *
            autolayout lr
        }

        // Level 2 - Container diagram
        container happyHeadlines "Containers" {
            include *
            autolayout lr
        }

        // Observability
        container happyHeadlines "Observability" {
            include draftService commentService profanityService otelCollector loki tempo grafana developer
            autolayout lr
        }

        // Level 3 - Component
        !include views/level_3.dsl

        // Level 4 - Code

        // Level 5 - Deployment diagram
        !include views/level_5.dsl

        // Styles
        !include styles.dsl

        theme default
    }
}
