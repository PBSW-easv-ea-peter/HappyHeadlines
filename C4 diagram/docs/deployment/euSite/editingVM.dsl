editingVM = deploymentNode "Editing VM" "Virtual Machine" "Alpine" {

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

        // UI - Blazor WASM running in browser
        // (webServer serves the files, but the UI runs in browser and has the relations)
        // deploymentNode for webServer would be separate if needed

        // Quees
        deploymentNode "RabbitMQ message broker" "Message Broker" "RabbitMQ" {
            rabbitmqInstance = containerInstance rabbitmq
        }

    }
}
