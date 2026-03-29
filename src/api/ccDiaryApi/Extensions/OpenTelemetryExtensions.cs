// <copyright file="OpenTelemetryExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Extensions
{
    using System.Diagnostics.CodeAnalysis;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;

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
        [ExcludeFromCodeCoverage(Justification = "Requires a live OTLP endpoint; no-op branch is covered by integration tests.")]
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
    }
}
