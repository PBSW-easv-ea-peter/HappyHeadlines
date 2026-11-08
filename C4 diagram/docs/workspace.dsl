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

            // Applications
            // Webapp and Website are two Blazor WebAssembly apps (apps/happy-headlines-web_service:
            // HappyHeadlines.web.wasm on 8000, HappyHeadlines.reader.wasm on 8001) sharing the page
            // library HappyHeadlinesPages.web. They call the services directly from the browser.
            webapp = container "Webapp" "Editorial application used by publishers (port 8000). Implemented: draft dashboard, create/edit, submit/approve/reject, publishing via PublisherService, article overview with comments." "Web Application (Blazor WebAssembly)" "Implemented"
            website = container "Website" "Public website for readers (port 8001). Implemented: articles per region with comments. Missing: newsletter subscription." "Web Application (Blazor WebAssembly)" "Partial"

            // Draft
            !include models/services/draft.dsl

            // Comment
            !include models/services/comment.dsl

            // Profanity
            !include models/services/profanity.dsl

            // Observability
            !include models/observability.dsl

            // No database of its own - it reads drafts from DraftService and writes to ArticleQueue.
            group "Publisher" {
                publisherService = container "PublisherService" "Checks approved drafts for profanity and publishes them as articles via ArticleQueue. Code lives in apps/publish_service (to be renamed)." "Service" "Implemented" {
                    publishDraftEndpoint = component "PublishDraftEndpoint" "Exposes REST endpoint POST /api/v1/publish-draft/{id}." "ASP.NET Core Minimal API"
                    publishDraftHandler = component "PublishDraftHandler" "Checks that the draft is Approved and free of profanity, queues a PublishedArticleEvent, then marks the draft as published. Banned words: 422, words stored on the draft. ProfanityService down: 503 (fail-closed). Queues before marking, so publishing again after a failure is safe." "Component"
                    publisherDraftClient = component "DraftClient" "Gets the draft, stores flagged words and marks it as published in DraftService over HTTP." "Component"
                    publisherProfanityClient = component "ProfanityClient" "Calls ProfanityService over HTTP, wrapped in a Polly retry + circuit breaker. Reports 'not checked' on any failure, so the handler can fail closed." "Component"
                    rabbitMqService = component "RabbitMqService" "Publishes events to RabbitMQ with publisher confirms." "Component"
                    publisherObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

                    publishDraftEndpoint -> publishDraftHandler "Delegates publishing to"
                    publishDraftHandler -> publisherDraftClient "Gets, flags and marks drafts via"
                    publishDraftHandler -> publisherProfanityClient "Checks title and body via"
                    publishDraftHandler -> rabbitMqService "Queues PublishedArticleEvent via"
                }
            }

            // x-axis split (week 36): not a container of its own, but part of the Swarm deployment.
            articleServiceLB = container "ArticleService Load Balancer" "Docker Swarm's built-in routing mesh distributes requests across the ArticleService replicas. Not a separate container." "Docker Swarm routing mesh" "Implemented"

            subscriberService = container "SubscriberService" "Manages newsletter subscriptions." "Service"
            newsletterService = container "NewsletterService" "Sends newsletters." "Service"

            // Suggested - not part of the original design (see L4/README.md)
//            userService = container "UserService" "Manages users: User, Journalist, Publisher." "Service" "Suggested"

            // Databases (not yet implemented)
            subscriberDb = container "SubscriberDatabase" "Stores subscriber information." "Database" "Database"

            // Queues
            !include models/rabbitmq.dsl

            // Each consumer binds its own named queue to the exchange, so every consumer gets
            // every article. Replicas of one service share that service's queue (competing consumers).
            articleQueue = container "ArticleQueue" "Exchange published_articles (routing key article.published) for newly published articles. ArticleService consumes from its own queue article_service.published_articles." "RabbitMQ" "Queue,Implemented"
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
        // Early feedback for the editor on submit. The gate before publishing is PublisherService's
        // check below (docs/logging.md, open question 8).
        draftProfanityClient -> profanityService "Pre-checks draft text on submit via HTTP POST /api/profanity/check" "HTTP"

        ui -> publisherService "Publishes article"

        publisherService -> profanityService "Checks article content"

        publisherService -> publishedArticlesExchange "Publishes approved article"

        // PublisherService component-level relations (imply the container-level
        // Webapp -> PublisherService -> DraftService/ProfanityService/ArticleQueue relations)
        webapp -> publishDraftEndpoint "Publishes approved draft via POST /api/v1/publish-draft/{id}" "HTTP"
        // Article overview page in the editorial app (Pages/Articles) - same reads as the Website.
        webapp -> articleServiceLB "Lists published articles per region" "HTTP"
        webapp -> commentService "Reads comments on published articles" "HTTP"
        publisherDraftClient -> draftsController "Gets draft, flags banned words (PUT api/drafts/{id}/flagged-words) and marks it published" "HTTP"
        publisherProfanityClient -> profanityService "Checks title and body before publishing via HTTP POST /api/profanity/check" "HTTP"
        rabbitMqService -> articleQueue "Publishes PublishedArticleEvent to" "AMQP"

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
        // DraftService/CommentService/ProfanityService/ArticleService/PublisherService -> OTel Collector relations.
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
            include draftService commentService profanityService articleService publisherService otelCollector loki tempo grafana developer
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

        component publisherService "PublisherServiceComponents" {
            include *
            autolayout lr
        }

        // C4 Level 5 - Deployment diagram
        deployment happyHeadlines "Production" "ArticleServiceDeployment" {
            include articleServiceInstance webServerInstance
            autolayout lr
        }

        deployment happyHeadlines "Production" "ArticleDatabaseDeployment" {
            include articleServiceInstance northAmericaDb southAmericaDb australiaDb antarcticaDb africaDb asiaDb europeDb globalDb
            autolayout lr
        }

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
    }
}
