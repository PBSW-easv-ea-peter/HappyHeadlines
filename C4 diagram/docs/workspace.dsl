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
            profanityService = container "ProfanityService" "Filters inappropriate language." "Service"
            articleService = container "ArticleService" "Provides published articles." "Service"
            commentService = container "CommentService" "Manages comments." "Service"
            subscriberService = container "SubscriberService" "Manages newsletter subscriptions." "Service"
            newsletterService = container "NewsletterService" "Sends newsletters." "Service"

            // Databases
            draftDb = container "DraftDatabase" "Stores article drafts." "Database"
            articleDb = container "ArticleDatabase" "Stores published articles." "Database"
            commentDb = container "CommentDatabase" "Stores comments." "Database"
            profanityDb = container "ProfanityDatabase" "Stores prohibited words." "Database"
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
        profanityService -> profanityDb "Reads prohibited words"

        publisherService -> articleQueue "Publishes approved article"

        articleQueue -> articleDb "Stores published article"
        articleService -> articleQueue "Subscribes to published articles"
        articleService -> articleDb "Reads articles"


        // Reader - articles
        reader -> website "Reads articles"
        website -> articleService "Requests articles"


        // Reader - comments
        reader -> website "Posts comments"
        website -> commentService "Creates and retrieves comments"

        commentService -> profanityService "Checks comment content"
        commentService -> commentDb "Reads and writes comments"


        // Reader - newsletter subscription
        reader -> website "Subscribes to newsletter"
        website -> subscriberService "Registers subscriber"

        subscriberService -> subscriberDb "Reads and writes subscriber data"
        subscriberService -> subscriberQueue "Queues new subscribers"


        // Newsletter
        newsletterService -> articleService "Retrieves articles"
        newsletterService -> subscriberService "Retrieves subscribers"
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


        theme default
    }
}