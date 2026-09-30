observabilityVM = deploymentNode "Observability VM" "Virtual Machine" "Alpine" {

    deploymentNode "Docker Swarm Cluster" "Happy Headlines runtime" "Docker Swarm" {

        // Otel Collector
        deploymentNode "otel-collector" "" "Docker" {
            instances 2
            otelCollectorInstance-eu = containerInstance otelCollector-eu
        }

        // Loki
        deploymentNode "loki" "Time Scale DB for logs" {
            lokiInstance = containerInstance loki
        }

        // Tempo
        deploymentNode "tempo" "Object Storace for traces" {
            tempoInstance = containerInstance tempo
        }

        // Grafana
        deploymentNode "grafana" {
            grafanaInstance = containerInstance grafana
        }
    }
}
