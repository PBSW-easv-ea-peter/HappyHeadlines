
otelCollector -> loki "Forwards logs to" "OTLP/HTTP"
otelCollector -> tempo "Forwards traces to" "OTLP/gRPC"
grafana -> loki "Queries logs from"
grafana -> tempo "Queries traces from"
developer -> grafana "Searches logs and follows traces in"
