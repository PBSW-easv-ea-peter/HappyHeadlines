
otelForwarderNode = deploymentNode "otel-forwarder" "OTEL Collector" "OpenTelemetry" {
    containerInstance otelForwarder
}
