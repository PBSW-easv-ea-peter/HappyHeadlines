using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HappyHeadlines.Observability;

// Shared OpenTelemetry setup so every service reports logs and traces the same way
// to the OTel Collector (see docs/logging.md). Services only call AddObservability().
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Environment.ApplicationName;

        // Base URL of the collector's OTLP/HTTP receiver, e.g. http://otel-collector:4318
        var endpoint = builder.Configuration["OpenTelemetry:Endpoint"]
            ?? throw new InvalidOperationException("OpenTelemetry:Endpoint is not configured.");

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri($"{endpoint}/v1/traces");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                }));

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri($"{endpoint}/v1/logs");
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        });

        return builder;
    }
}
