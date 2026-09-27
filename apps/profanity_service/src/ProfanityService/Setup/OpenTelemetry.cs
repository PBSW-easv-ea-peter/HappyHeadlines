using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ProfanityService.Setup;

public static class OpenTelemetry
{

    public static void ConfigureOpenTelemetry(this WebApplicationBuilder builder)
    {
        builder.ConfigureLogging();
        // builder.ConfigureMetrics();
        builder.ConfigureTracing();
    }

    private static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        var openTelemetryBuilder = builder.Services.AddOpenTelemetry();
        var serviceName = builder.Environment.ApplicationName;

        openTelemetryBuilder.ConfigureResource(resource => resource
            .AddService(builder.Environment.ApplicationName)
            .AddTelemetrySdk());

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:LogsEndpoint"]
                                           ?? throw new InvalidOperationException(
                                               "OpenTelemetry:LogsEndpoint is not configured.")
                );

                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        });
    }

    private static WebApplicationBuilder ConfigureMetrics(this WebApplicationBuilder builder)
    {
        //
//        openTelemetryBuilder.WithMetrics(metrics => metrics
//            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
//            .AddAspNetCoreInstrumentation()
//            .AddHttpClientInstrumentation()
//            .AddSqlClientInstrumentation()
//            .SetExemplarFilter(ExemplarFilterType.TraceBased)
//            .AddOtlpExporter(exporterOptions =>
//            {
//                exporterOptions.Endpoint = new Uri(builder.Configuration["OpenTelemetry:MetricsEndpoint"]);
//                exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
//            })
//        );
//
        return builder;
    }

    private static void ConfigureTracing(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(builder.Environment.ApplicationName)
                .AddTelemetrySdk())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // .AddSqlClientInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(
                        builder.Configuration["OpenTelemetry:TracesEndpoint"]
                        ?? throw new InvalidOperationException(
                            "OpenTelemetry:TracesEndpoint is not configured."));

                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                }));
    }
}
