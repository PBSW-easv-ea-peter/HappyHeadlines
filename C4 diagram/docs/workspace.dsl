workspace "Happy Headlines" "Positive news platform" {

    model {

        // People
        publisher = person "Publisher" "Writes and publishes articles."
        reader = person "Reader" "Reads articles, comments, and subscribes to newsletters."


        // Software system
        happyHeadlines = softwareSystem "Happy Headlines" "Positive news platform." {

            // Applications
            webapp = container "Webapp" "Editorial application used by publishers." "Web Application"
            website = container "Website" "Public website for readers." "Web Application"

            // Services
            draftService = container "DraftService" "Manages article drafts." "Service"
            publisherService = container "PublisherService" "Publishes approved articles." "Service"
            profanityService = container "ProfanityService" "Filters inappropriate language." "Service" "Implemented" {
                profanityController = component "ProfanityController" "Exposes REST endpoint to check a single word against the banned list." "ASP.NET Core Controller"
                profanityChecker = component "ProfanityChecker" "Normalizes and delegates word lookups." "Component"
                profanityRepository = component "ProfanityRepository" "Looks up words in banned_words via Dapper." "Repository"

                profanityController -> profanityChecker "Delegates word lookup to"
                profanityChecker -> profanityRepository "Looks up word via"
            }
            articleService = container "ArticleService" "Provides published articles. (x-axis split: 3 load-balanced replicas)" "Service" "Implemented" {
                articlesController = component "ArticlesController" "Exposes REST CRUD endpoints for articles, scoped by location." "ASP.NET Core Controller"
                articleReadRepository = component "ArticleReadRepository" "Reads articles from the resolved shard." "Repository"
                articleWriteRepository = component "ArticleWriteRepository" "Creates, updates and deletes articles in the resolved shard (REST stand-ins for Create/Update)." "Repository"
                articleShardResolver = component "ArticleShardResolver" "Resolves a location code to the correct shard connection string." "Component"
                articleQueueConsumer = component "ArticleQueueConsumer" "Will consume ArticleQueue for Create/Update once wired up. Currently idle." "Background Service"

                articlesController -> articleReadRepository "Delegates GET requests to"
                articlesController -> articleWriteRepository "Delegates Create/Update/Delete REST stand-ins to"
                articleReadRepository -> articleShardResolver "Resolves shard via"
                articleWriteRepository -> articleShardResolver "Resolves shard via"
                articleQueueConsumer -> articleWriteRepository "Will persist consumed messages via (not yet wired)"
            }
            articleServiceLB = container "ArticleService Load Balancer" "Distributes requests across ArticleService replicas." "Load Balancer"
            commentService = container "CommentService" "Manages comments." "Service" "Implemented" {
                commentsController = component "CommentsController" "Exposes REST endpoints for posting and retrieving comments, scoped by article location." "ASP.NET Core Controller"
                commentHandler = component "CommentHandler" "Classifies comment text word-by-word via ProfanityService and orchestrates persistence." "Component"
                commentRepository = component "CommentRepository" "Reads and writes comments (incl. article_id/article_location) via Dapper." "Repository"
                profanityClient = component "ProfanityClient" "Calls ProfanityService directly over HTTP, wrapped in a Polly retry + circuit breaker." "Component"

                commentsController -> commentHandler "Delegates classification and persistence to"
                commentHandler -> profanityClient "Checks each word via"
                commentHandler -> commentRepository "Persists and reads comments via"
            }
            subscriberService = container "SubscriberService" "Manages newsletter subscriptions." "Service"
            newsletterService = container "NewsletterService" "Sends newsletters." "Service"

            // Databases
            draftDb = container "DraftDatabase" "Stores article drafts." "Database"
            articleDb = container "ArticleDatabase" "Stores published articles. (z-axis split: sharded per continent, 8 instances)" "Database" "Implemented"
            commentDb = container "CommentDatabase" "Stores comments." "Database" "Implemented"
            profanityDb = container "ProfanityDatabase" "Stores prohibited words." "Database" "Implemented"
            subscriberDb = container "SubscriberDatabase" "Stores subscriber information." "Database"

            // Queues
            articleQueue = container "ArticleQueue" "Queue for newly published articles." "Queue"
            subscriberQueue = container "SubscriberQueue" "Queue for new newsletter subscriptions." "Queue"
        }


        // Publisher workflow
        publisher -> webapp "Creates drafts and publishes articles"

        webapp -> draftService "Save and retrieve drafts"
        draftService -> draftDb "Reads and writes drafts"

        webapp -> publisherService "Publishes article"

        publisherService -> profanityService "Checks article content"

        publisherService -> articleQueue "Publishes approved article"

        articleQueue -> articleDb "Stores published article"

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
        profanityClient -> profanityService "Checks word via HTTP POST /api/profanity/check"
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


        deploymentEnvironment "Production" {

            deploymentNode "Load Balancer" "Docker container" {
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

            deploymentNode "ArticleService Instance 1" "Docker container" {
                articleServiceInstance1 = containerInstance articleService
            }
            deploymentNode "ArticleService Instance 2" "Docker container" {
                articleServiceInstance2 = containerInstance articleService
            }
            deploymentNode "ArticleService Instance 3" "Docker container" {
                articleServiceInstance3 = containerInstance articleService
            }

            deploymentNode "Africa" "SQL Server" {
                africaDb = containerInstance articleDb
            }
            deploymentNode "Asia" "SQL Server" {
                asiaDb = containerInstance articleDb
            }
            deploymentNode "Europe" "SQL Server" {
                europeDb = containerInstance articleDb
            }
            deploymentNode "North America" "SQL Server" {
                northAmericaDb = containerInstance articleDb
            }
            deploymentNode "South America" "SQL Server" {
                southAmericaDb = containerInstance articleDb
            }
            deploymentNode "Australia" "SQL Server" {
                australiaDb = containerInstance articleDb
            }
            deploymentNode "Antarctica" "SQL Server" {
                antarcticaDb = containerInstance articleDb
            }
            deploymentNode "Global" "SQL Server" {
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

        // C4 Level 5 - Deployment diagram
        deployment happyHeadlines "Production" "ArticleServiceDeployment" {
            include websiteInstance newsletterServiceInstance loadBalancer articleServiceInstance1 articleServiceInstance2 articleServiceInstance3
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
        }

        theme default
    }
}