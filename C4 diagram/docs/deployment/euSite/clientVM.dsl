clientVM = deploymentNode "Client VM" "Virtual Machine" "Alpine" {

    deploymentNode "Docker Swarm Cluster" "Happy Headlines runtime" "Docker Swarm" {

        deploymentNode "webserver" "" "Docker" {
            instances 3
            webServerInstance = containerInstance webServer
        }
    }
}
