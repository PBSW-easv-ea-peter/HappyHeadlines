articleVM = deploymentNode "Article VM" "Virtual Machine" "Alpine" {

    deploymentNode "Docker Swarm Cluster" "Happy Headlines runtime" "Docker Swarm" {

        // Article
        deploymentNode "article-service" "" "Docker" {
            instances 3
            articleServiceInstance = containerInstance articleService
        }

    //    deploymentNode "NewsletterService" "Docker container" {
    //        newsletterServiceInstance = containerInstance newsletterService
    //    }

        deploymentNode "articledb-na" "Article DB shard for North America" "PostgreSQL 18" {
            northAmericaDb = containerInstance articleDb
        }

        deploymentNode "articledb-sa" "Article DB shard for South America" "PostgreSQL 18" {
            southAmericaDb = containerInstance articleDb
        }

        deploymentNode "articledb-au" "Article DB shard for Australia" "PostgreSQL 18" {
            australiaDb = containerInstance articleDb
        }

        deploymentNode "articledb-an" "Article DB shard for Antarctica" "PostgreSQL 18" {
            antarcticaDb = containerInstance articleDb
        }

        deploymentNode "articledb-af" "Article DB shard for Africa" "PostgreSQL 18" {
            africaDb = containerInstance articleDb
        }

        deploymentNode "articledb-as" "Article DB shard for Asia" "PostgreSQL 18" {
            asiaDb = containerInstance articleDb
        }

        deploymentNode "articledb-eu" "Article DB shard for Europe" "PostgreSQL 18" {
            europeDb = containerInstance articleDb
        }

        deploymentNode "articledb-global" "Global Article DB" "PostgreSQL 18" {
            globalDb = containerInstance articleDb
        }

        // Comment
        deploymentNode "comment-service" "" "Docker" {
            instances 2
            commentServiceInstance = containerInstance commentService
        }

        deploymentNode "comment-db" "Comment DB" "PostgreSQL 18" {
            commentDbInstance = containerInstance commentDb
        }

        // Website
        deploymentNode "happyheadlines" "Docker container" {
            websiteInstance = containerInstance website
        }
    }
}
