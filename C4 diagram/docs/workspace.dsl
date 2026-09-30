workspace "Happy Headlines" "Positive news platform" {

    model {

        // People
        publisher = person "Publisher" "Writes and publishes articles."
        reader = person "Reader" "Reads articles, comments, and subscribes to newsletters."
        developer = person "Developer" "Builds and operates Happy Headlines; follows logs and traces across services."


        // Software system
        happyHeadlines = softwareSystem "Happy Headlines" "Positive news platform." {
            // Applications

            // Article
            !include model/article.dsl

            // Webapp and Website are one Blazor app in code (apps/happy-headlines-web_service),
            // kept as two containers to follow the architecture handout.
            webapp = container "Webapp" "Editorial application used by publishers. Implemented: draft dashboard, create/edit, submit/approve/reject. Missing: publishing via PublisherService. Shares codebase with Website." "Web Application (Blazor)" "Partial"
            website = container "Website" "Public website for readers. Implemented: articles per region with comments. Missing: newsletter subscription. Shares codebase with Webapp." "Blazor (wasm) hosted on NGINX" "Partial"

            // Draft
            !include model/draft.dsl

            // Comment
            !include model/comment.dsl

            // Profanity
            !include model/profanity.dsl

            // Observability
            !include model/observability.dsl

            // Services (not yet implemented)
            publisherService = container "PublisherService" "Publishes approved articles." "Service"
            articleServiceLB = container "ArticleService Load Balancer" "Docker Swarm's built-in routing mesh distributes requests across the ArticleService replicas. Not a separate container." "Docker Swarm routing mesh"
            subscriberService = container "SubscriberService" "Manages newsletter subscriptions." "Service"
            newsletterService = container "NewsletterService" "Sends newsletters." "Service"

            // Suggested - not part of the original design (see L4/README.md)
//            userService = container "UserService" "Manages users: User, Journalist, Publisher." "Service" "Suggested"

            // Databases (not yet implemented)
            subscriberDb = container "SubscriberDatabase" "Stores subscriber information." "Database" "Database"

            // Queues
            articleQueue = container "ArticleQueue" "Queue for newly published articles." "Queue" "Queue"
            subscriberQueue = container "SubscriberQueue" "Queue for new newsletter subscriptions." "Queue" "Queue"
        }


        // Publisher workflow
        publisher -> webapp "Creates drafts and publishes articles"

        // DraftService component-level relations (imply the container-level relations
        // Webapp -> DraftService, DraftService -> DraftDatabase/ProfanityService)
        webapp -> draftsController "Saves and retrieves drafts"
        draftRepository -> draftDb "Reads and writes drafts in"
        // Known deviation: per the handout, PublisherService owns the profanity check before
        // publishing. Kept here until PublisherService exists (docs/logging.md, open question 8).
        // Declared at container level first: implied relationships do not copy tags, so without
        // this line the L2 view would not show the Deviation style.
        draftService -> profanityService "Checks draft text (known deviation - belongs to PublisherService)" "HTTP" "Deviation"
        draftProfanityClient -> profanityService "Checks draft text via HTTP POST /api/profanity/check (known deviation - belongs to PublisherService)" "HTTP" "Deviation"

        webapp -> publisherService "Publishes article"

        publisherService -> profanityService "Checks article content"

        publisherService -> articleQueue "Publishes approved article"

        // ArticleService component-level relations (imply the ArticleService container-level
        // relations to ArticleQueue/ArticleDatabase, so no separate container-level ones here)
        articleServiceLB -> articlesController "Routes requests to"
        articleReadRepository -> articleDb "Reads articles from"
        articleWriteRepository -> articleDb "Writes articles to"
        articleQueueConsumer -> articleQueue "Subscribes to (idle - not wired up yet)"

        // ProfanityService component-level relations (imply the ProfanityService
        // container-level relation to ProfanityDatabase, so no separate one here)
        profanityRepository -> profanityDb "Reads prohibited words from"


        // Reader - articles
        reader -> website "Reads articles"
        // website -> articleService "Requests articles"
        website -> articleServiceLB "Requests articles"
        // articleServiceLB -> articleService is implied by articleServiceLB -> articlesController above

        // Reader - comments
        reader -> website "Posts comments"
        website -> commentService "Creates and retrieves comments"

        // CommentService component-level relations (imply the CommentService container-level
        // relations to ProfanityService/CommentDatabase, so no separate container-level ones here)
        profanityClient -> profanityService "Checks comment text via HTTP POST /api/profanity/check"
        commentRepository -> commentDb "Reads and writes comments in"


        // Reader - newsletter subscription
        reader -> website "Subscribes to newsletter"
        website -> subscriberService "Registers subscriber"

        subscriberService -> subscriberDb "Reads and writes subscriber data"
        subscriberService -> subscriberQueue "Queues new subscribers"


        // Newsletter
        // newsletterService -> articleService "Retrieves articles"
        newsletterService -> articleServiceLB "Retrieves articles"
        newsletterService -> subscriberService "Retrieves subscribers"
        newsletterService -> subscriberQueue "Consumes new subscribers"
        newsletterService -> reader "Sends daily newsletter"


        // Suggested UserService
        //articleService -> userService "Looks up journalists (suggested)"
        //draftService -> userService "Looks up journalists (suggested)"


        // Observability (week 38). Component-level relations imply the container-level
        // DraftService/CommentService/ProfanityService -> OTel Collector relations.
        draftObservability -> otelCollector-eu "Exports logs and traces via" "OTLP/HTTP"
        commentObservability -> otelCollector-eu "Exports logs and traces via" "OTLP/HTTP"
        articleObservability -> otelCollector-eu "Exports logs and traces via" "OTLP/HTTP"
        profanityObservability -> otelCollector-eu "Exports logs and traces via" "OTLP/HTTP"

        otelCollector-eu -> loki "Forwards logs to" "OTLP/HTTP"
        otelCollector-eu -> tempo "Forwards traces to" "OTLP/gRPC"
        grafana -> loki "Queries logs from"
        grafana -> tempo "Queries traces from"
        developer -> grafana "Searches logs and follows traces in"

        // Shared deployment variables
        //routingMesh = infrastructureNode "Swarm routing mesh" "Built-in ingress load balancing across service replicas"

        // Deployments: Production
        !include deployment/production.dsl

    }


    views {

        // C4 Level 1 - System Context
        systemContext happyHeadlines "SystemContext" {
            include *
            autolayout lr
        }


        // C4 Level 2 - Container diagram
        container happyHeadlines "Containers" {
            include *
            autolayout lr
        }

        // C4 Level 2 - Observability only (week 38), so it does not drown in "Containers"
        container happyHeadlines "Observability" {
            include draftService commentService profanityService otelCollector-eu loki tempo grafana developer
            autolayout lr
        }

        // C4 Level 3 - Component diagram
        component articleService "ArticleServiceComponents" {
            include *
            autolayout lr
        }

        component commentService "CommentServiceComponents" {
            include *
            autolayout lr
        }

        component profanityService "ProfanityServiceComponents" {
            include *
            autolayout lr
        }

        component draftService "DraftServiceComponents" {
            include *
            autolayout lr
        }

        // C4 Level 5 - Deployment diagram
        deployment happyHeadlines "Production" "ArticleServiceDeployment" {
            include euSite
            autolayout tb
        }

 //       deployment happyHeadlines "Production" "ArticleDatabaseDeployment" {
 //           include articleServiceInstance articleQueueInstance africaDb asiaDb europeDb northAmericaDb southAmericaDb australiaDb antarcticaDb globalDb
 //           autolayout lr
 //       }

        styles {
            element "Implemented" {
                background "#1BA86B"
                color "#ffffff"
            }
            element "Database" {
                shape Cylinder
            }
            element "Queue" {
                shape Pipe
            }
            element "Suggested" {
                background "#9E9E9E"
                color "#ffffff"
                border dashed
            }
            element "Partial" {
                background "#F2B705"
                color "#000000"
            }
            relationship "Deviation" {
                color "#E8710A"
                dashed true
            }
        }

        theme default
    }
}
