workspace "Happy Headlines" "Positive news platform" {

    model {

        // People
        publisher = person "Publisher" "Writes and publishes articles."
        reader = person "Reader" "Reads articles, comments, and subscribes to newsletters."
        developer = person "Developer" "Builds and operates Happy Headlines; follows logs and traces across services."


        // Software system
        happyHeadlines = softwareSystem "Happy Headlines" "Positive news platform." {

            // Applications
            // Webapp and Website are one Blazor app in code (apps/happy-headlines-web_service),
            // kept as two containers to follow the architecture handout.
            webapp = container "Webapp" "Editorial application used by publishers. Implemented: draft dashboard, create/edit, submit/approve/reject. Missing: publishing via PublisherService. Shares codebase with Website." "Web Application (Blazor)" "Partial"
            website = container "Website" "Public website for readers. Implemented: articles per region with comments. Missing: newsletter subscription. Shares codebase with Webapp." "Web Application (Blazor)" "Partial"

            // Swimlanes (fault isolation): each implemented service is grouped with its own
            // database, so a failure in one lane cannot take down another lane's data.
            group "Draft" {
                draftService = container "DraftService" "Manages article drafts and their editorial workflow." "Service" "Implemented" {
                    draftsController = component "DraftsController" "Exposes REST endpoints for drafts and workflow actions (submit, approve, reject, publish, archive, reactivate)." "ASP.NET Core Controller"
                    draftHandler = component "DraftHandler" "Orchestrates draft business rules, profanity pre-check on submit, and persistence. Logs every status transition (docs/logging.md)." "Component"
                    draftStatusTransitions = component "DraftStatusTransitions" "Single source of truth for legal draft status transitions." "Component"
                    draftRepository = component "DraftRepository" "Reads and writes drafts via Dapper." "Repository"
                    draftProfanityClient = component "ProfanityClient" "Calls ProfanityService directly over HTTP, wrapped in a Polly retry + circuit breaker. Fallback: submission succeeds without flagged words." "Component"
                    draftObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

                    draftsController -> draftHandler "Delegates requests to"
                    draftHandler -> draftStatusTransitions "Validates status transitions via"
                    draftHandler -> draftProfanityClient "Pre-flags words on submit via"
                    draftHandler -> draftRepository "Persists and reads drafts via"
                }
                draftDb = container "DraftDatabase" "Stores article drafts." "PostgreSQL" "Database,Implemented"
            }

            group "Article" {
                articleService = container "ArticleService" "Provides published articles. (x-axis split: 3 load-balanced replicas via Docker Swarm, docker-compose.yaml)" "Service" "Implemented" {
                    articleCache = component "ArticleCache" "Caches articles in Redis for faster retrieval. Implements IArticleCache." "Component"
                    cacheRefreshService = component "CacheRefreshService" "Background service that refreshes the article cache hourly." "Background Service"
                    articlesController = component "ArticlesController" "Exposes REST CRUD endpoints for articles, scoped by location." "ASP.NET Core Controller"
                    articleReadRepository = component "ArticleReadRepository" "Reads articles from the resolved shard." "Repository"
                    articleWriteRepository = component "ArticleWriteRepository" "Creates, updates and deletes articles in the resolved shard (REST stand-ins for Create/Update)." "Repository"
                    articleShardResolver = component "ArticleShardResolver" "Resolves a location code to the correct shard connection string." "Component"
                    articleQueueConsumer = component "ArticleQueueConsumer" "Will consume ArticleQueue for Create/Update once wired up. Currently idle." "Background Service"

                    articlesController -> articleCache "Checks cache for articles via"
                    articlesController -> articleReadRepository "Delegates GET requests to"
                    articlesController -> articleWriteRepository "Delegates Create/Update/Delete REST stand-ins to"
                    articleReadRepository -> articleShardResolver "Resolves shard via"
                    articleWriteRepository -> articleShardResolver "Resolves shard via"
                    articleWriteRepository -> articleCache "Invalidates cache via"
                    articleQueueConsumer -> articleWriteRepository "Will persist consumed messages via (not yet wired)"
                    cacheRefreshService -> articleCache "Refreshes cache via"

                }
                articleDb = container "ArticleDatabase" "Stores published articles. (z-axis split: sharded per continent, 8 instances)" "PostgreSQL" "Database,Implemented"
                articleCacheDb = container "ArticleCache" "Redis cache for article data." "Redis" "Database,Implemented"
            }

            group "Comment" {
                commentService = container "CommentService" "Manages comments." "Service" "Implemented" {
                    commentsController = component "CommentsController" "Exposes REST endpoints for posting and retrieving comments, scoped by article location." "ASP.NET Core Controller"
                    commentHandler = component "CommentHandler" "Classifies comment text via ProfanityService (Approved / Rejected / PendingProfanityCheck) and orchestrates persistence." "Component"
                    commentRepository = component "CommentRepository" "Reads and writes comments (incl. article_id/article_location) via Dapper." "Repository"
                    profanityClient = component "ProfanityClient" "Calls ProfanityService directly over HTTP, wrapped in a Polly retry + circuit breaker. Fallback: comment is stored as PendingProfanityCheck." "Component"
                    commentObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

                    commentsController -> commentHandler "Delegates classification and persistence to"
                    commentHandler -> profanityClient "Checks comment text via"
                    commentHandler -> commentRepository "Persists and reads comments via"
                }
                commentDb = container "CommentDatabase" "Stores comments." "PostgreSQL" "Database,Implemented"
            }

            group "Profanity" {
                profanityService = container "ProfanityService" "Filters inappropriate language." "Service" "Implemented" {
                    profanityController = component "ProfanityController" "Exposes REST endpoint POST /api/profanity/check that takes a text and returns the banned words found in it." "ASP.NET Core Controller"
                    profanityChecker = component "ProfanityChecker" "Splits the text into distinct words (case-insensitive) and delegates the lookup." "Component"
                    profanityRepository = component "ProfanityRepository" "Looks up all words in banned_words in one query via Dapper." "Repository"
                    profanityObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

                    profanityController -> profanityChecker "Delegates the check to"
                    profanityChecker -> profanityRepository "Looks up words via"
                }
                profanityDb = container "ProfanityDatabase" "Stores prohibited words." "PostgreSQL" "Database,Implemented"
            }

            // Central logging and tracing (week 38). Runs in docker-compose.dev.yaml only;
            // services send telemetry asynchronously, so an outage here never stops a service.
            group "Observability" {
                otelCollector = container "OTel Collector" "Receives logs and traces over OTLP and routes them to Loki and Tempo." "OpenTelemetry Collector" "Implemented"
                loki = container "Loki" "Stores logs, incl. trace_id per log line." "Grafana Loki" "Database,Implemented"
                tempo = container "Tempo" "Stores distributed traces." "Grafana Tempo" "Database,Implemented"
                grafana = container "Grafana" "UI for searching logs and traces; links a log line to its trace." "Grafana" "Implemented"
            }

            // Services (not yet implemented)
            publisherService = container "PublisherService" "Publishes approved articles." "Service"
            articleServiceLB = container "ArticleService Load Balancer" "Docker Swarm's built-in routing mesh distributes requests across the ArticleService replicas. Not a separate container." "Docker Swarm routing mesh"
            subscriberService = container "SubscriberService" "Manages newsletter subscriptions." "Service"
            newsletterService = container "NewsletterService" "Sends newsletters." "Service"

            // Suggested - not part of the original design (see L4/README.md)
            userService = container "UserService" "Manages users: User, Journalist, Publisher." "Service" "Suggested"

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
        articleCache -> articleCacheDb "Stores cached articles in"

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
        articleService -> userService "Looks up journalists (suggested)"
        draftService -> userService "Looks up journalists (suggested)"


        // Observability (week 38). Component-level relations imply the container-level
        // DraftService/CommentService/ProfanityService -> OTel Collector relations.
        draftObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        commentObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        profanityObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        otelCollector -> loki "Forwards logs to" "OTLP/HTTP"
        otelCollector -> tempo "Forwards traces to" "OTLP/gRPC"
        grafana -> loki "Queries logs from"
        grafana -> tempo "Queries traces from"
        developer -> grafana "Searches logs and follows traces in"


        deploymentEnvironment "Production" {

            deploymentNode "Load Balancer" "Docker Swarm routing mesh" {
                loadBalancer = containerInstance articleServiceLB
            }

            deploymentNode "Website" "Docker container" {
                websiteInstance = containerInstance website
            }
            deploymentNode "NewsletterService" "Docker container" {
                newsletterServiceInstance = containerInstance newsletterService
            }
            deploymentNode "ArticleQueue" "Docker container" {
                articleQueueInstance = containerInstance articleQueue
            }
            deploymentNode "ArticleCache" "Docker container" {
                articleCacheInstance = containerInstance articleCacheDb
            }

            deploymentNode "ArticleService Instance 1" "Docker container" {
                articleServiceInstance1 = containerInstance articleService
            }
            deploymentNode "ArticleService Instance 2" "Docker container" {
                articleServiceInstance2 = containerInstance articleService
            }
            deploymentNode "ArticleService Instance 3" "Docker container" {
                articleServiceInstance3 = containerInstance articleService
            }

            deploymentNode "Africa" "PostgreSQL 18" {
                africaDb = containerInstance articleDb
            }
            deploymentNode "Asia" "PostgreSQL 18" {
                asiaDb = containerInstance articleDb
            }
            deploymentNode "Europe" "PostgreSQL 18" {
                europeDb = containerInstance articleDb
            }
            deploymentNode "North America" "PostgreSQL 18" {
                northAmericaDb = containerInstance articleDb
            }
            deploymentNode "South America" "PostgreSQL 18" {
                southAmericaDb = containerInstance articleDb
            }
            deploymentNode "Australia" "PostgreSQL 18" {
                australiaDb = containerInstance articleDb
            }
            deploymentNode "Antarctica" "PostgreSQL 18" {
                antarcticaDb = containerInstance articleDb
            }
            deploymentNode "Global" "PostgreSQL 18" {
                globalDb = containerInstance articleDb
            }
        }
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
            include draftService commentService profanityService otelCollector loki tempo grafana developer
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
            include websiteInstance newsletterServiceInstance loadBalancer articleServiceInstance1 articleServiceInstance2 articleServiceInstance3 articleCacheInstance
            autolayout lr
        }

        deployment happyHeadlines "Production" "ArticleDatabaseDeployment" {
            include articleServiceInstance1 articleQueueInstance africaDb asiaDb europeDb northAmericaDb southAmericaDb australiaDb antarcticaDb globalDb
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

        theme default
    }
}
