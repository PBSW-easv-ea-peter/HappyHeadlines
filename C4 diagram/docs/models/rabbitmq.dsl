group "RabbitMQ" {
    rabbitmq = container "RabbitMQ" "Message broker for newly published articles." "RabbitMQ" "Queue" {
        publishedArticlesExchange = component "PublishedArticlesExchange" "Exchange for newly published articles." "Exchange"
    }
}
