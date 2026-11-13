workspace "Happy Headlines" "Positive news platform" {

    model {

        // People
        publisher = person "Publisher" "Writes and publishes articles."
        reader = person "Reader" "Reads articles, comments, and subscribes to newsletters."
        developer = person "Developer" "Builds and operates Happy Headlines; follows logs and traces across services."


        // Software system
        happyHeadlines = softwareSystem "Happy Headlines" "Positive news platform." {

            // Applications
            // Webapp and Website are two separate Blazor WebAssembly apps, each with its own page
            // library: apps/journalist-web_service (Journalist.web.wasm + JournalistPages.web, port 8000)
            // and apps/reader-web_service (Reader.web.wasm + ReaderPages.web, port 8001).
            // They call the services directly from the browser.
            webapp = container "Webapp" "Editorial application used by publishers (apps/journalist-web_service, port 8000). Implemented: draft dashboard, create/edit, submit/approve/reject, publishing via PublisherService, article overview with comments." "Web Application (Blazor WebAssembly)" "Implemented"
            website = container "Website" "Public website for readers (apps/reader-web_service, port 8001). Implemented: articles per region with comments. Missing: newsletter subscription." "Web Application (Blazor WebAssembly)" "Partial"

            // Swimlanes (fault isolation): each implemented service is grouped with its own
            // database, so a failure in one lane cannot take down another lane's data.
            group "Draft" {
                draftService = container "DraftService" "Manages article drafts and their editorial workflow." "Service" "Implemented" {
                    draftsController = component "DraftsController" "Exposes REST endpoints for drafts and workflow actions (submit, approve, reject, publish, archive, reactivate)." "ASP.NET Core Controller"
                    draftHandler = component "DraftHandler" "Orchestrates draft business rules, profanity pre-check on submit, and persistence. Logs every status transition (docs/logging.md)." "Component"
                    draftStatusTransitions = component "DraftStatusTransitions" "Single source of truth for legal draft status transitions." "Component"
                    draftRepository = component "DraftRepository" "Reads and writes drafts via Dapper." "Repository"
                    draftProfanityClient = component "ProfanityClient" "Calls ProfanityService directly over HTTP, wrapped in a Polly retry + circuit breaker + per-attempt timeout. Fallback: submission succeeds and the draft is marked profanityCheckSkipped, so the editor is warned instead of told 'no issues found'." "Component"
                    draftObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

                    draftsController -> draftHandler "Delegates requests to"
                    draftHandler -> draftStatusTransitions "Validates status transitions via"
                    draftHandler -> draftProfanityClient "Pre-flags words on submit via"
                    draftHandler -> draftRepository "Persists and reads drafts via"
                }
                draftDb = container "DraftDatabase" "Stores article drafts." "PostgreSQL" "Database,Implemented"
            }

            group "Article" {
                articleService = container "ArticleService" "Provides published articles. (x-axis split: 3 load-balanced replicas, infrastructure/kubernetes/deployments/article-service.yaml)" "Service" "Implemented" {
                    articlesController = component "ArticlesController" "Exposes REST CRUD endpoints for articles, scoped by location." "ASP.NET Core Controller"
                    articleReadRepository = component "ArticleReadRepository" "Reads articles from the resolved shard." "Repository"
                    articleWriteRepository = component "ArticleWriteRepository" "Creates, updates and deletes articles in the resolved shard (REST stand-ins for Create/Update)." "Repository"
                    articleShardResolver = component "ArticleShardResolver" "Resolves a location code to the correct shard connection string." "Component"
                    articleQueueConsumer = component "ArticleQueueConsumer" "Consumes PublishedArticleEvent and stores the article in its shard. Replicas compete on one queue; storing is idempotent on DraftId. A circuit breaker per shard requeues messages while a shard is down." "Background Service"
                    articleCache = component "ArticleCache" "Implements IArticleCache: each article is stored once per key, and the front page is an index (sorted set) of article ids per location. Only articles from the last 14 days." "Component"
                    cacheRefreshService = component "CacheRefreshService" "Refills the cache every hour. Currently only location GO (global)." "Background Service"
                    articleObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

                    articlesController -> articleCache "Reads articles from cache first, invalidates on update/delete via"
                    articlesController -> articleReadRepository "Falls back to on cache miss"
                    articlesController -> articleWriteRepository "Delegates Create/Update/Delete REST stand-ins to"
                    articleReadRepository -> articleShardResolver "Resolves shard via"
                    articleWriteRepository -> articleShardResolver "Resolves shard via"
                    articleQueueConsumer -> articleWriteRepository "Stores consumed articles via"
                    cacheRefreshService -> articleCache "Refreshes every hour via"
                    articleQueueConsumer -> articleCache "Puts newly published articles on the cached front page via"
                    articleCache -> articleReadRepository "Loads articles for refresh via"
                }
                articleDb = container "ArticleDatabase" "Stores published articles. (z-axis split: sharded per continent, 8 instances)" "PostgreSQL" "Database,Implemented"
                articleCacheDb = container "ArticleCache" "Offline cache of articles from the last 14 days, refilled every hour. A miss reads the database but does not fill the cache. Shared by all ArticleService replicas." "Redis" "Database,Implemented"
            }

            group "Comment" {
                commentService = container "CommentService" "Manages comments." "Service" "Implemented" {
                    commentsController = component "CommentsController" "Exposes REST endpoints for posting and retrieving comments, scoped by article location." "ASP.NET Core Controller"
                    commentHandler = component "CommentHandler" "Classifies comment text via ProfanityService (Approved / Rejected / PendingProfanityCheck) and orchestrates persistence." "Component"
                    commentRepository = component "CommentRepository" "Reads and writes comments (incl. article_id/article_location) via Dapper." "Repository"
                    profanityClient = component "ProfanityClient" "Calls ProfanityService directly over HTTP, wrapped in a Polly retry + circuit breaker + per-attempt timeout. Fallback: comment is stored as PendingProfanityCheck." "Component"
                    commentCacheClient = component "RedisCommentCache" "Cache-aside per article: filled on a miss, write-through for approved comments, LRU over the 30 most recently read articles. Falls back to the database if Redis is down." "Component"
                    commentObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

                    commentsController -> commentHandler "Delegates classification and persistence to"
                    commentHandler -> profanityClient "Checks comment text via"
                    commentHandler -> commentRepository "Persists and reads comments via"
                    commentHandler -> commentCacheClient "Reads cached comments first, fills and writes through via"
                }
                commentDb = container "CommentDatabase" "Stores comments." "PostgreSQL" "Database,Implemented"
                commentCache = container "CommentCache" "Cache-aside: filled on a miss, keeps comments for the 30 most recently used articles (LRU). The service falls back to the database if it is down." "Redis" "Database,Implemented"
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

            // Central logging and tracing (week 38). Runs as its own stack (monitoring/docker-compose.yaml)
            // that joins the app network; services send telemetry asynchronously, so an outage here
            // never stops a service.
            group "Observability" {
                otelCollector = container "OTel Collector" "Receives logs and traces over OTLP and routes them to Loki and Tempo." "OpenTelemetry Collector" "Implemented"
                loki = container "Loki" "Stores logs, incl. trace_id per log line." "Grafana Loki" "Database,Implemented"
                tempo = container "Tempo" "Stores distributed traces." "Grafana Tempo" "Database,Implemented"
                grafana = container "Grafana" "UI for searching logs and traces; links a log line to its trace." "Grafana" "Implemented"
            }

            // NewsletterService (week 44). Its own swimlane, isolated from SubscriberService: the
            // UseSubscriberService release toggle decides whether it calls SubscriberService at all.
            // Articles, subscribers and e-mail are still in-memory fakes, so its calls to
            // ArticleService and SubscriberService below are tagged Planned.
            group "Newsletter" {
                newsletterService = container "NewsletterService" "Sends daily and weekly newsletters and a welcome e-mail to new subscribers. Behind the release toggle UseSubscriberService (read on every run, so it can change without a redeploy). Articles, subscribers and e-mail are still in-memory fakes." "Service" "Partial" {
                    newsletterBackgroundService = component "NewsletterBackgroundService" "Wakes up every full hour and runs the intervals that are due (daily, weekly - NewsletterIntervals in appsettings)." "Background Service"
                    newsletterExecutor = component "NewsletterExecutor" "Runs one interval: if UseSubscriberService is on, gets subscribers and articles since the last newsletter and sends the newsletters. If it is off, logs that SubscriberService is turned off and sends nothing." "Component"
                    featureToggles = component "FeatureToggles" "Binds the FeatureToggles configuration section to FeatureTogglesOptions. Read via IOptionsMonitor, so a changed value is used on the next run." "Options"
                    newSubscriberConsumer = component "NewSubscriberConsumer" "Consumes NewSubscriberEvent from its own queue newsletter_service.new_subscribers." "Background Service"
                    newSubscriberMessageHandler = component "NewSubscriberMessageHandler" "Sends a welcome e-mail to the new subscriber." "Component"
                    emailFactory = component "EmailFactory" "Builds welcome, daily and weekly newsletter e-mails." "Component"
                    newsletterEmailService = component "EmailService" "FakeEmailService: logs the e-mails and keeps them in an in-memory sink instead of sending them." "Component (fake)"
                    newsletterSubscriberClient = component "SubscriberService client" "InMemorySubscriberService: hard-coded subscribers until SubscriberService exists." "Component (fake)"
                    newsletterArticleClient = component "ArticleService client" "FakeArticleService: hard-coded articles until ArticleService is called over HTTP." "Component (fake)"
                    newsletterObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing." "Shared library"

                    newsletterBackgroundService -> newsletterExecutor "Runs due intervals via"
                    newsletterExecutor -> featureToggles "Reads UseSubscriberService from"
                    newsletterExecutor -> newsletterSubscriberClient "Gets subscribers via (toggle on)"
                    newsletterExecutor -> newsletterArticleClient "Gets articles since the last newsletter via (toggle on)"
                    newsletterExecutor -> emailFactory "Builds newsletters via"
                    newsletterExecutor -> newsletterEmailService "Sends newsletters via"
                    newSubscriberConsumer -> newSubscriberMessageHandler "Hands each NewSubscriberEvent to"
                    newSubscriberMessageHandler -> emailFactory "Builds the welcome e-mail via"
                    newSubscriberMessageHandler -> newsletterEmailService "Sends the welcome e-mail via"
                }
            }

            // The ArticleService load balancer (x-axis split, week 36) is not a container here: it runs
            // no application code of its own, so it belongs to the deployment view. See the
            // "Load Balancer" infrastructure node in the Production deployment environment below.

            // Services (not yet implemented)
            subscriberService = container "SubscriberService" "Manages newsletter subscriptions." "Service"

            // Suggested - not part of the original design (see L4/README.md)
            userService = container "UserService" "Manages users: User, Journalist, Publisher." "Service" "Suggested"

            // Databases (not yet implemented)
            subscriberDb = container "SubscriberDatabase" "Stores subscriber information." "Database" "Database"

            // Queues
            // Each consumer binds its own named queue to the exchange, so every consumer gets
            // every article. Replicas of one service share that service's queue (competing consumers).
            articleQueue = container "ArticleQueue" "Exchange published_articles (routing key article.published) for newly published articles. ArticleService consumes from its own queue article_service.published_articles." "RabbitMQ" "Queue,Implemented"
            subscriberQueue = container "SubscriberQueue" "Exchange subscribers (SubscribersExchange, routing key for new subscribers) for NewSubscriberEvent. NewsletterService consumes from its own queue newsletter_service.new_subscribers. No producer yet - SubscriberService is not implemented." "RabbitMQ" "Queue,Partial"
        }


        // Publisher workflow
        publisher -> webapp "Creates drafts and publishes articles"

        // DraftService component-level relations (imply the container-level relations
        // Webapp -> DraftService, DraftService -> DraftDatabase/ProfanityService)
        webapp -> draftsController "Saves and retrieves drafts"
        draftRepository -> draftDb "Reads and writes drafts in"
        // Early feedback for the editor on submit. The gate before publishing is PublisherService's
        // check below (docs/logging.md, open question 8).
        draftProfanityClient -> profanityService "Pre-checks draft text on submit via HTTP POST /api/profanity/check" "HTTP"

        // PublisherService component-level relations (imply the container-level
        // Webapp -> PublisherService -> DraftService/ProfanityService/ArticleQueue relations)
        webapp -> publishDraftEndpoint "Publishes approved draft via POST /api/v1/publish-draft/{id}" "HTTP"
        // Article overview page in the editorial app (Pages/Articles) - same reads as the Website.
        webapp -> articlesController "Lists published articles per region" "HTTP"
        webapp -> commentService "Reads comments on published articles" "HTTP"
        publisherDraftClient -> draftsController "Gets draft, flags banned words (PUT api/drafts/{id}/flagged-words) and marks it published" "HTTP"
        publisherProfanityClient -> profanityService "Checks title and body before publishing via HTTP POST /api/profanity/check" "HTTP"
        rabbitMqService -> articleQueue "Publishes PublishedArticleEvent to" "AMQP"

        // ArticleService component-level relations (imply the ArticleService container-level
        // relations to ArticleQueue/ArticleDatabase, so no separate container-level ones here)
        articleReadRepository -> articleDb "Reads articles from"
        articleWriteRepository -> articleDb "Writes articles to"
        articleQueueConsumer -> articleQueue "Consumes published articles from" "AMQP"
        articleCache -> articleCacheDb "Reads and writes cached articles in" "Redis"

        // ProfanityService component-level relations (imply the ProfanityService
        // container-level relation to ProfanityDatabase, so no separate one here)
        profanityRepository -> profanityDb "Reads prohibited words from"


        // Reader - articles
        reader -> website "Reads articles"
        // Implies website -> articleService at container level
        website -> articlesController "Requests articles" "HTTP"

        // Reader - comments
        reader -> website "Posts comments"
        website -> commentService "Creates and retrieves comments"

        // CommentService component-level relations (imply the CommentService container-level
        // relations to ProfanityService/CommentDatabase, so no separate container-level ones here)
        profanityClient -> profanityService "Checks comment text via HTTP POST /api/profanity/check"
        commentRepository -> commentDb "Reads and writes comments in"
        commentCacheClient -> commentCache "Reads and fills cached comments in" "Redis"


        // Reader - newsletter subscription
        reader -> website "Subscribes to newsletter"
        website -> subscriberService "Registers subscriber"

        subscriberService -> subscriberDb "Reads and writes subscriber data"
        subscriberService -> subscriberQueue "Queues new subscribers"


        // Newsletter. Component-level relations imply the NewsletterService container-level ones.
        // Planned: the target design - today the components use in-memory fakes instead.
        newsletterArticleClient -> articlesController "Retrieves articles (planned - FakeArticleService today)" "HTTP" "Planned"
        newsletterSubscriberClient -> subscriberService "Retrieves subscribers, only while UseSubscriberService is on (planned - InMemorySubscriberService today)" "HTTP" "Planned"
        newSubscriberConsumer -> subscriberQueue "Consumes new subscribers from" "AMQP"
        newsletterEmailService -> reader "Sends daily/weekly newsletter and welcome e-mail (planned - FakeEmailService logs them today)" "E-mail" "Planned"


        // Suggested UserService
        articleService -> userService "Looks up journalists (suggested)"
        draftService -> userService "Looks up journalists (suggested)"


        // Observability (week 38). Component-level relations imply the container-level
        // DraftService/CommentService/ProfanityService/ArticleService/PublisherService/NewsletterService -> OTel Collector relations.
        // Deviation: AddObservability() is commented out in DraftService's Program.cs since #23 (k8s),
        // so DraftService is missing from Loki and Tempo until it is enabled again.
        draftObservability -> otelCollector "Exports logs and traces via (currently disabled, see Program.cs)" "OTLP/HTTP" "Deviation"
        newsletterObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        articleObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        publisherObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        commentObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        profanityObservability -> otelCollector "Exports logs and traces via" "OTLP/HTTP"
        otelCollector -> loki "Forwards logs to" "OTLP/HTTP"
        otelCollector -> tempo "Forwards traces to" "OTLP/gRPC"
        grafana -> loki "Queries logs from"
        grafana -> tempo "Queries traces from"
        developer -> grafana "Searches logs and follows traces in"


        // NB: this deployment environment shows the earlier Docker Swarm / compose set-up with all
        // 8 article shards. The Kubernetes mock deployment (infrastructure/kubernetes/) only runs
        // the EU shard, and the load balancer there is nginx in front of the cluster (NodePort 30000,
        // loadbalancer/services-nginx.conf) + the article-service Service (kube-proxy).
        // Kept as it is on purpose - explain the difference in the video.
        deploymentEnvironment "Production" {

            // Infrastructure, not a container: it runs no application code of its own.
            deploymentNode "Docker Swarm" "Container orchestrator" {
                loadBalancer = infrastructureNode "Load Balancer" "Spreads requests over the 3 ArticleService replicas (x-axis split, week 36)." "Docker Swarm routing mesh"
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

            loadBalancer -> articleServiceInstance1 "Routes requests to" "HTTP"
            loadBalancer -> articleServiceInstance2 "Routes requests to" "HTTP"
            loadBalancer -> articleServiceInstance3 "Routes requests to" "HTTP"

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


        // C4 Level 2 - Container diagram. Observability is left out here; it has its own view below.
        container happyHeadlines "Containers" {
            include *
            exclude otelCollector loki tempo grafana developer
            autolayout lr
        }

        // C4 Level 2 - Observability only (week 38), so it does not drown in "Containers"
        container happyHeadlines "Observability" {
            include draftService commentService profanityService articleService publisherService newsletterService otelCollector loki tempo grafana developer
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

        component publisherService "PublisherServiceComponents" {
            include *
            autolayout lr
        }

        component newsletterService "NewsletterServiceComponents" {
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
            relationship "Planned" {
                color "#9E9E9E"
                dashed true
            }
        }
    }
}
