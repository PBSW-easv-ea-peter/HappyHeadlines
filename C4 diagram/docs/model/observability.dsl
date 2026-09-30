// Central logging and tracing (week 38). Runs in docker-compose.dev.yaml only;
// services send telemetry asynchronously, so an outage here never stops a service.
group "Observability" {
    otelCollector-eu = container "OTel Collector EU" "Receives logs and traces over OTLP and routes them to Loki and Tempo." "OpenTelemetry Collector" "Implemented"
    otelCollector-na = container "OTel Collector NA" "Receives logs and traces over OTLP and routes them 'otelCollector-eu'." "OpenTelemetry Collector" "Implemented"


    loki = container "Loki" "Stores logs, incl. trace_id per log line." "Grafana Loki" "Database,Implemented"
    tempo = container "Tempo" "Stores distributed traces." "Grafana Tempo" "Database,Implemented"
    grafana = container "Grafana" "UI for searching logs and traces; links a log line to its trace." "Grafana" "Implemented"
}
