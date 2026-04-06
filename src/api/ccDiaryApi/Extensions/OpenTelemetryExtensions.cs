// <copyright file="OpenTelemetryExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Extensions
{
    using System.Data;
    using System.Reflection;
    using ccDiaryApi.Utilities;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Instrumentation.AspNetCore;
    using OpenTelemetry.Instrumentation.EntityFrameworkCore;
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

            var otlpHeaders = configuration["OTEL_EXPORTER_OTLP_HEADERS"] ?? string.Empty;
            var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "unknown";

            services.AddOpenTelemetry()
                .ConfigureResource(resource => ConfigureOtelResource(resource, serviceName, serviceVersion, environment))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(ConfigureAspNetCoreTracing)
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(ConfigureEntityFrameworkCoreTracing)
                    .AddSqlClientInstrumentation(ConfigureSqlClientTracing)
                    .AddOtlpExporter(opts =>
                    {
                        ConfigureTracingOtlpExporter(opts);
                        ApplyOtlpEndpointAndHeaders(opts, otlpEndpoint, otlpHeaders, "/v1/traces");
                    }))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(opts =>
                    {
                        ConfigureMetricsOtlpExporter(opts);
                        ApplyOtlpEndpointAndHeaders(opts, otlpEndpoint, otlpHeaders, "/v1/metrics");
                    }));

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
                    new KeyValuePair<string, object>("deployment.environment.name", environment),
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
        /// Configures EF Core tracing instrumentation to capture generated SQL text and record exceptions.
        /// </summary>
        /// <param name="options">The instrumentation options to configure.</param>
        public static void ConfigureEntityFrameworkCoreTracing(EntityFrameworkInstrumentationOptions options)
        {
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
            options.Filter = ShouldTraceDbCommand;
        }

        /// <summary>
        /// Filters out low-value probe queries from EF Core database spans.
        /// </summary>
        /// <param name="providerName">The EF provider name.</param>
        /// <param name="dbCommand">The current database command.</param>
        /// <returns><c>true</c> when the command should be traced; otherwise <c>false</c>.</returns>
        public static bool ShouldTraceDbCommand(string? providerName, IDbCommand dbCommand)
        {
            _ = providerName;

            var commandText = dbCommand.CommandText?.Trim();
            if (string.IsNullOrEmpty(commandText))
            {
                return true;
            }

            return !IsLowValueProbeQuery(commandText);
        }

        /// <summary>
        /// Returns <c>true</c> for DB probe statements that are usually emitted by health checks.
        /// </summary>
        /// <param name="commandText">The SQL command text.</param>
        /// <returns><c>true</c> if this statement is likely a low-value probe; otherwise <c>false</c>.</returns>
        public static bool IsLowValueProbeQuery(string commandText)
        {
            var normalized = commandText.Trim();
            while (normalized.EndsWith(';'))
            {
                normalized = normalized[..^1].TrimEnd();
            }

            if (string.Equals(normalized, "SELECT 1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "SELECT 1 FROM DUAL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "SELECT DB_NAME()", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalized.StartsWith("SELECT TOP(1) 1", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("SELECT TOP (1) 1", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("SELECT SERVERPROPERTY(", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Configures the OTLP exporter used for tracing: HTTP/Protobuf protocol with a simple
        /// processor for immediate export. Batch processing is avoided to ensure spans are exported
        /// immediately, even in scale-to-zero environments where the process may terminate before
        /// a batch flush completes.
        /// </summary>
        /// <param name="options">The exporter options to configure.</param>
        public static void ConfigureTracingOtlpExporter(OtlpExporterOptions options)
        {
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
            options.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
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
        /// Applies the OTLP collector endpoint and optional authorization headers to an exporter
        /// options instance. This ensures the values from <c>IConfiguration</c> (e.g. user secrets
        /// when running via <c>dotnet run</c>) are used rather than relying solely on process
        /// environment variables.
        /// </summary>
        /// <remarks>
        /// When <see cref="OtlpExporterOptions.Endpoint"/> is set programmatically the SDK sets
        /// <c>AppendSignalPathToEndpoint = false</c>, meaning it will NOT auto-append
        /// <c>/v1/traces</c> or <c>/v1/metrics</c> to the base URL. The caller must therefore
        /// pass the appropriate <paramref name="signalPath"/> so the request reaches the correct
        /// Grafana OTLP endpoint (e.g. <c>/v1/traces</c>, <c>/v1/metrics</c>).
        /// </remarks>
        /// <param name="options">The exporter options to configure.</param>
        /// <param name="endpoint">The OTLP collector base endpoint URL (without signal path).</param>
        /// <param name="headers">Optional comma-separated key=value header string (may be empty).</param>
        /// <param name="signalPath">The signal-specific path to append (e.g. <c>/v1/traces</c>).</param>
        public static void ApplyOtlpEndpointAndHeaders(OtlpExporterOptions options, string endpoint, string headers, string signalPath)
        {
            options.Endpoint = new Uri(endpoint.TrimEnd('/') + signalPath);
            if (!string.IsNullOrEmpty(headers))
            {
                options.Headers = headers;
            }
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
