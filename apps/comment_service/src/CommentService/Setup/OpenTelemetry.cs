using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;

namespace CommentService.Setup;

public static class OpenTelemetry
{

    public static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder)
    {
        builder.ConfigureOTelExporter();

        return builder;
    }

    public static WebApplicationBuilder ConfigureOTelExporter(this WebApplicationBuilder builder)
    {
        var openTelemetryBuilder = builder.Services.AddOpenTelemetry();
        var serviceName = builder.Environment.ApplicationName;
//
//        openTelemetryBuilder.ConfigureResource(resource => resource
//            .AddService(builder.Environment.ApplicationName)
//            .AddTelemetrySdk());
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
//        openTelemetryBuilder.WithTracing(tracing => tracing
//            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
//            .AddAspNetCoreInstrumentation()
//            .AddHttpClientInstrumentation()
//            .AddSqlClientInstrumentation()
//            .AddOtlpExporter(options =>
//            {
//                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:TracesEndpoint"]); ;
//                options.Protocol = OtlpExportProtocol.HttpProtobuf;
//            })
//        );

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

        return builder;
    }
}
