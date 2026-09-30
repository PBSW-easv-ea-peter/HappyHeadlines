editingVM = deploymentNode "Comment and profanity VM" "Virtual Machine" "Alpine" {

    deploymentNode "Docker Swarm Cluster" "Happy Headlines runtime" "Docker Swarm" {

        // Draft
        deploymentNode "draft-service" "" "Docker" {
            draftServiceInstance = containerInstance draftService
        }

        deploymentNode "draft-db" "Draft DB" "PostgreSQL 18" {
            draftDbInstance = containerInstance draftDb
        }

        // Profanity
        deploymentNode "profanity-service" "" "Docker" {
            profanityServiceInstance = containerInstance profanityService
        }

        deploymentNode "profanity-db" "Profanity DB" "PostgreSQL 18" {
            profanityDbInstance = containerInstance profanityDb
        }

        // Publish
        deploymentNode "publisher-service" "" "Docker" {
            publisherServiceInstance = containerInstance publisherService
        }

        // WebApp
        deploymentNode "happyheadlines" "Docker container" {
            webappInstance = containerInstance webapp
        }

        // Quees
        deploymentNode "Published Articles Exchange" "Message Broker" "RabbitMQ" {
            publishedArticlesExchange = containerInstance articleQueue
        }

    }
}
