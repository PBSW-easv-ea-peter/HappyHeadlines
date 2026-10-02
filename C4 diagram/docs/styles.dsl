styles {

    // Status
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

    element "Implemented" {
        background "#1BA86B"
        color "#ffffff"
    }

    // Type of service
    element "Database" {
        shape Cylinder
        background "#1E90FF"
        color "#000000"
    }

    element "Observability" {
        background "#FFA500"
        color "#000000"
    }

    element "Queue" {
        shape Pipe
    }

    // Person
    element "Person" {
        shape Person
        background "#08427B"
        color "#ffffff"
    }
}
