//northAmericaSite = deploymentNode "Data Center: North America" {
//    description "Deployment site for application services."
//
//    deploymentNode "Comment Service" {
//        containerInstance commentService
//    }
//
//    deploymentNode "Website" "Docker container" {
//        websiteInstance = containerInstance website
//    }
//
//    deploymentNode "Docker Swarm Cluster" "Happy Headlines runtime" "Docker Swarm" {
//        deploymentNode "Swarm Worker" "Worker node" "Docker" {
//            instances 3
//            articleServiceInstance = containerInstance articleService
//        }
//    }
//
//    deploymentNode "North America" "PostgreSQL 18" {
//        northAmericaDb = containerInstance articleDb
//    }
//
//    deploymentNode "South America" "PostgreSQL 18" {
//        southAmericaDb = containerInstance articleDb
//    }
//
//    deploymentNode "Australia" "PostgreSQL 18" {
//        australiaDb = containerInstance articleDb
//    }
//
//    deploymentNode "Antarctica" "PostgreSQL 18" {
//        antarcticaDb = containerInstance articleDb
//    }
//}
