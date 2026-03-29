// <copyright file="OpenTelemetryExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Extensions
{
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
    using Serilog.Sinks.OpenTelemetry;

    /// <summary>
    /// Extension methods for registering OpenTelemetry SDK services.
    /// </summary>
    public static class OpenTelemetryExtensions
    {
        /// <summary>
        /// Registers OpenTelemetry tracing and metrics exporters when an OTLP endpoint is configured.
        /// When <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is absent or empty the method returns without
        /// registering any exporters, making this a no-op for environments that have no Grafana
        /// credentials (e.g. local development without a <c>.env</c> override).
        /// </summary>
        /// <param name="services">The service collection to register OpenTelemetry into.</param>
        /// <param name="configuration">Application configuration, used to read the OTLP endpoint.</param>
        /// <param name="serviceName">The logical service name reported to the tracing backend.</param>
        /// <param name="serviceVersion">The service version reported to the tracing backend.</param>
        /// <returns>The original <paramref name="services"/> for fluent chaining.</returns>
        public static IServiceCollection AddCcDiaryOpenTelemetry(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName,
            string serviceVersion)
        {
            var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (string.IsNullOrEmpty(otlpEndpoint))
            {
                return services;
            }

            var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "unknown";

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName, serviceVersion: serviceVersion)
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", environment),
                    }))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/swagger") &&
                            !ctx.Request.Path.StartsWithSegments("/actuator") &&
                            !ctx.Request.Path.StartsWithSegments("/api/assembly-info") &&
                            !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                    })
                    .AddOtlpExporter(options =>
                    {
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                        options.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
                        options.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
                        {
                            ScheduledDelayMilliseconds = 5000,
                            MaxQueueSize = 2048,
                        };
                    }))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    }));

            return services;
        }

        /// <summary>
        /// Configures the Serilog OpenTelemetry sink with the OTLP endpoint and headers from
        /// application configuration. When <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is absent or empty
        /// the options are left unchanged, making this a no-op.
        /// </summary>
        /// <param name="o">The sink options to configure.</param>
        /// <param name="config">Application configuration used to read the endpoint and headers.</param>
        public static void ConfigureSerilogOtelSink(OpenTelemetrySinkOptions o, IConfiguration config)
        {
            var endpoint = config["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrEmpty(endpoint))
            {
                o.Endpoint = endpoint.TrimEnd('/') + "/v1/logs";
                o.Protocol = OtlpProtocol.HttpProtobuf;
                o.ResourceAttributes["service.name"] = "ccDiaryApi";
                var headers = config["OTEL_EXPORTER_OTLP_HEADERS"];
                if (!string.IsNullOrEmpty(headers))
                {
                    foreach (var part in headers.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var idx = part.IndexOf('=', StringComparison.Ordinal);
                        if (idx > 0)
                        {
                            o.Headers.Add(part[..idx].Trim(), part[(idx + 1)..].Trim());
                        }
                    }
                }
            }
        }
    }
}
