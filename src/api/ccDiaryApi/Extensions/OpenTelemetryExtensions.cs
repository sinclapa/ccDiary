// <copyright file="OpenTelemetryExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Extensions
{
    using System.Reflection;
    using ccDiaryApi.Utilities;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Instrumentation.AspNetCore;
    using OpenTelemetry.Instrumentation.SqlClient;
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
                .ConfigureResource(resource => ConfigureOtelResource(resource, serviceName, serviceVersion, environment))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(ConfigureAspNetCoreTracing)
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(ConfigureSqlClientTracing)
                    .AddOtlpExporter(ConfigureTracingOtlpExporter))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(ConfigureMetricsOtlpExporter));

            return services;
        }

        /// <summary>
        /// Configures the OpenTelemetry resource with service name, version, and deployment environment.
        /// </summary>
        /// <param name="resource">The resource builder to configure.</param>
        /// <param name="serviceName">The logical service name.</param>
        /// <param name="serviceVersion">The service version.</param>
        /// <param name="environment">The deployment environment (e.g. Production, Development).</param>
        /// <returns>The configured <paramref name="resource"/> for fluent chaining.</returns>
        public static ResourceBuilder ConfigureOtelResource(
            ResourceBuilder resource,
            string serviceName,
            string serviceVersion,
            string environment) =>
            resource
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", environment),
                });

        /// <summary>
        /// Returns <c>true</c> when the request should be traced.
        /// Excludes infrastructure paths (Swagger UI, actuator, health check, and assembly-info)
        /// that produce high-volume, low-value spans.
        /// </summary>
        /// <param name="ctx">The HTTP context for the incoming request.</param>
        /// <returns><c>true</c> if the request should be traced; otherwise <c>false</c>.</returns>
        public static bool ShouldTraceRequest(HttpContext ctx) =>
            !ctx.Request.Path.StartsWithSegments("/swagger") &&
            !ctx.Request.Path.StartsWithSegments("/actuator") &&
            !ctx.Request.Path.StartsWithSegments("/api/assembly-info") &&
            !ctx.Request.Path.StartsWithSegments("/health");

        /// <summary>
        /// Configures ASP.NET Core tracing instrumentation: enables exception recording and
        /// applies <see cref="ShouldTraceRequest"/> as the sampling filter.
        /// </summary>
        /// <param name="options">The instrumentation options to configure.</param>
        public static void ConfigureAspNetCoreTracing(AspNetCoreTraceInstrumentationOptions options)
        {
            options.RecordException = true;
            options.Filter = ShouldTraceRequest;
        }

        /// <summary>
        /// Configures SQL Client tracing instrumentation to capture the full query text in spans.
        /// </summary>
        /// <param name="options">The instrumentation options to configure.</param>
        public static void ConfigureSqlClientTracing(SqlClientTraceInstrumentationOptions options)
        {
            options.SetDbStatementForText = true;
        }

        /// <summary>
        /// Configures the OTLP exporter used for tracing: HTTP/Protobuf protocol with a batch
        /// processor (5 s flush interval, 2 048-span queue).
        /// </summary>
        /// <param name="options">The exporter options to configure.</param>
        public static void ConfigureTracingOtlpExporter(OtlpExporterOptions options)
        {
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
            options.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
            options.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<System.Diagnostics.Activity>
            {
                ScheduledDelayMilliseconds = 5000,
                MaxQueueSize = 2048,
            };
        }

        /// <summary>
        /// Configures the OTLP exporter used for metrics: HTTP/Protobuf protocol.
        /// </summary>
        /// <param name="options">The exporter options to configure.</param>
        public static void ConfigureMetricsOtlpExporter(OtlpExporterOptions options)
        {
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
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
                var environment = config["ASPNETCORE_ENVIRONMENT"] ?? "unknown";
                var serviceVersion = AssemblyVersionInfo.GetInformationalVersion(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());

                o.Endpoint = endpoint.TrimEnd('/') + "/v1/logs";
                o.Protocol = OtlpProtocol.HttpProtobuf;
                o.ResourceAttributes["service.name"] = "ccDiaryApi";
                o.ResourceAttributes["service.version"] = serviceVersion;

                // Align log resource attributes with trace resource attributes.
                o.ResourceAttributes["deployment.environment"] = environment;
                o.ResourceAttributes["deployment.environment.name"] = environment;

                var headers = config["OTEL_EXPORTER_OTLP_HEADERS"];
                if (!string.IsNullOrEmpty(headers))
                {
                    foreach (var part in headers.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var idx = part.IndexOf('=', StringComparison.Ordinal);
                        if (idx > 0)
                        {
                            var key = part[..idx].Trim();
                            var value = part.Substring(idx + 1).Trim();
                            o.Headers.Add(key, value);
                        }
                    }
                }
            }
        }
    }
}
