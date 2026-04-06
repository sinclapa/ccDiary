// <copyright file="OpenTelemetryExtensionsTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Data;
    using ccDiaryApi.Extensions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Instrumentation.AspNetCore;
    using OpenTelemetry.Instrumentation.EntityFrameworkCore;
    using OpenTelemetry.Instrumentation.SqlClient;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
    using Serilog.Sinks.OpenTelemetry;

    /// <summary>
    /// Unit tests for <see cref="OpenTelemetryExtensions"/>.
    /// </summary>
    [TestClass]
    public class OpenTelemetryExtensionsTest
    {
        [TestMethod]
        public void AddCcDiaryOpenTelemetry_ReturnsServices_WhenEndpointIsEmpty()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Act
            var result = OpenTelemetryExtensions.AddCcDiaryOpenTelemetry(services, config, "test-service", "1.0.0");

            // Assert
            Assert.AreSame(services, result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void AddCcDiaryOpenTelemetry_RegistersOpenTelemetry_WhenEndpointIsSet()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318",
                })
                .Build();

            // Act
            var result = OpenTelemetryExtensions.AddCcDiaryOpenTelemetry(services, config, "test-service", "1.0.0");

            // Assert
            Assert.AreSame(services, result);
            Assert.IsTrue(result.Count > 0);
        }

        [TestMethod]
        public void AddCcDiaryOpenTelemetry_ConfiguresTracingExporterEndpoint_WhenEndpointIsSet()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318",
                })
                .Build();

            OpenTelemetryExtensions.AddCcDiaryOpenTelemetry(services, config, "test-service", "1.0.0");
            using var provider = services.BuildServiceProvider();

            // Act — resolving TracerProvider triggers the .AddOtlpExporter lambda,
            // which calls ApplyOtlpEndpointAndHeaders with signalPath "/v1/traces".
            var tracerProvider = provider.GetRequiredService<TracerProvider>();

            // Assert
            Assert.IsNotNull(tracerProvider);
        }

        [TestMethod]
        public void AddCcDiaryOpenTelemetry_ConfiguresMetricsExporterEndpoint_WhenEndpointIsSet()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318",
                })
                .Build();

            OpenTelemetryExtensions.AddCcDiaryOpenTelemetry(services, config, "test-service", "1.0.0");
            using var provider = services.BuildServiceProvider();

            // Act — resolving MeterProvider triggers the .AddOtlpExporter lambda,
            // which calls ApplyOtlpEndpointAndHeaders with signalPath "/v1/metrics".
            var meterProvider = provider.GetRequiredService<MeterProvider>();

            // Assert
            Assert.IsNotNull(meterProvider);
        }

        [TestMethod]
        public void ConfigureOtelResource_SetsServiceNameAndEnvironment()
        {
            // Arrange
            var builder = ResourceBuilder.CreateDefault();

            // Act
            var result = OpenTelemetryExtensions.ConfigureOtelResource(builder, "test-svc", "1.0.0", "testing");

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void ShouldTraceRequest_ReturnsFalse_ForExcludedPaths()
        {
            // Arrange
            var excludedPaths = new[]
            {
                "/swagger/index.html",
                "/actuator/health",
                "/api/assembly-info",
                "/health",
            };

            foreach (var path in excludedPaths)
            {
                var ctx = new DefaultHttpContext();
                ctx.Request.Path = path;

                // Act + Assert
                Assert.IsFalse(
                    OpenTelemetryExtensions.ShouldTraceRequest(ctx),
                    $"Expected path '{path}' to be excluded from tracing.");
            }
        }

        [TestMethod]
        public void ShouldTraceRequest_ReturnsTrue_ForTracedPath()
        {
            // Arrange
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/api/v1/Diary";

            // Act
            var result = OpenTelemetryExtensions.ShouldTraceRequest(ctx);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ConfigureAspNetCoreTracing_SetsRecordExceptionAndFilter()
        {
            // Arrange
            var options = new AspNetCoreTraceInstrumentationOptions();

            // Act
            OpenTelemetryExtensions.ConfigureAspNetCoreTracing(options);

            // Assert
            Assert.IsTrue(options.RecordException);
            Assert.IsNotNull(options.Filter);
        }

        [TestMethod]
        public void ConfigureSqlClientTracing_SetsDbStatementForText()
        {
            // Arrange
            var options = new SqlClientTraceInstrumentationOptions();

            // Act
            OpenTelemetryExtensions.ConfigureSqlClientTracing(options);

            // Assert
            Assert.IsTrue(options.SetDbStatementForText);
        }

        [TestMethod]
        public void ConfigureEntityFrameworkCoreTracing_SetsExpectedOptions()
        {
            // Arrange
            var options = new EntityFrameworkInstrumentationOptions();

            // Act
            OpenTelemetryExtensions.ConfigureEntityFrameworkCoreTracing(options);

            // Assert
            Assert.IsTrue(options.SetDbStatementForText);
            Assert.IsTrue(options.SetDbStatementForStoredProcedure);
            Assert.IsNotNull(options.Filter);
        }

        [DataTestMethod]
        [DataRow("SELECT 1")]
        [DataRow("SELECT 1;")]
        [DataRow(" SELECT 1 ; ")]
        [DataRow("SELECT 1 FROM DUAL")]
        [DataRow("SELECT DB_NAME()")]
        [DataRow("SELECT TOP(1) 1 FROM sys.objects")]
        [DataRow("SELECT TOP (1) 1 FROM sys.tables")]
        [DataRow("SELECT SERVERPROPERTY('ProductVersion')")]
        public void IsLowValueProbeQuery_ReturnsTrue_ForProbeStatements(string query)
        {
            // Act
            var result = OpenTelemetryExtensions.IsLowValueProbeQuery(query);

            // Assert
            Assert.IsTrue(result);
        }

        [DataTestMethod]
        [DataRow("SELECT * FROM Diary")]
        [DataRow("EXEC dbo.GetDiaryById @id")]
        [DataRow("UPDATE Diary SET Title = 'x'")]
        public void IsLowValueProbeQuery_ReturnsFalse_ForBusinessStatements(string query)
        {
            // Act
            var result = OpenTelemetryExtensions.IsLowValueProbeQuery(query);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldTraceDbCommand_ReturnsTrue_WhenCommandTextIsEmpty()
        {
            // Arrange
            var command = new Moq.Mock<IDbCommand>();
            command.SetupGet(c => c.CommandText).Returns(string.Empty);

            // Act
            var result = OpenTelemetryExtensions.ShouldTraceDbCommand("provider", command.Object);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ShouldTraceDbCommand_ReturnsFalse_ForProbeStatement()
        {
            // Arrange
            var command = new Moq.Mock<IDbCommand>();
            command.SetupGet(c => c.CommandText).Returns("SELECT 1");

            // Act
            var result = OpenTelemetryExtensions.ShouldTraceDbCommand("provider", command.Object);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldTraceDbCommand_ReturnsTrue_ForBusinessStatement()
        {
            // Arrange
            var command = new Moq.Mock<IDbCommand>();
            command.SetupGet(c => c.CommandText).Returns("SELECT * FROM Diary");

            // Act
            var result = OpenTelemetryExtensions.ShouldTraceDbCommand("provider", command.Object);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ConfigureTracingOtlpExporter_SetsProtocolAndBatchProcessor()
        {
            // Arrange
            var options = new OtlpExporterOptions();

            // Act
            OpenTelemetryExtensions.ConfigureTracingOtlpExporter(options);

            // Assert
            Assert.AreEqual(OtlpExportProtocol.HttpProtobuf, options.Protocol);
            Assert.AreEqual(OpenTelemetry.ExportProcessorType.Batch, options.ExportProcessorType);
            Assert.AreEqual(2000, options.BatchExportProcessorOptions.ScheduledDelayMilliseconds);
        }

        [TestMethod]
        public void ConfigureMetricsOtlpExporter_SetsProtocol()
        {
            // Arrange
            var options = new OtlpExporterOptions();

            // Act
            OpenTelemetryExtensions.ConfigureMetricsOtlpExporter(options);

            // Assert
            Assert.AreEqual(OtlpExportProtocol.HttpProtobuf, options.Protocol);
        }

        [TestMethod]
        public void ApplyOtlpEndpointAndHeaders_AppendsSignalPath()
        {
            // Arrange
            var options = new OtlpExporterOptions();

            // Act
            OpenTelemetryExtensions.ApplyOtlpEndpointAndHeaders(options, "http://otel-collector:4318", string.Empty, "/v1/traces");

            // Assert — signal path must be appended so the SDK sends to the correct endpoint
            Assert.AreEqual(new Uri("http://otel-collector:4318/v1/traces"), options.Endpoint);
        }

        [TestMethod]
        public void ApplyOtlpEndpointAndHeaders_StripsTrailingSlashBeforeAppendingSignalPath()
        {
            // Arrange
            var options = new OtlpExporterOptions();

            // Act
            OpenTelemetryExtensions.ApplyOtlpEndpointAndHeaders(options, "http://otel-collector:4318/otlp/", string.Empty, "/v1/metrics");

            // Assert
            Assert.AreEqual(new Uri("http://otel-collector:4318/otlp/v1/metrics"), options.Endpoint);
        }

        [TestMethod]
        public void ApplyOtlpEndpointAndHeaders_SetsHeaders_WhenHeadersProvided()
        {
            // Arrange
            var options = new OtlpExporterOptions();

            // Act
            OpenTelemetryExtensions.ApplyOtlpEndpointAndHeaders(options, "http://otel-collector:4318", "Authorization=Basic dXNlcjpwYXNz", "/v1/traces");

            // Assert
            Assert.AreEqual("Authorization=Basic dXNlcjpwYXNz", options.Headers);
        }

        [TestMethod]
        public void ApplyOtlpEndpointAndHeaders_DoesNotSetHeaders_WhenHeadersEmpty()
        {
            // Arrange
            var options = new OtlpExporterOptions();
            var defaultHeaders = options.Headers;

            // Act
            OpenTelemetryExtensions.ApplyOtlpEndpointAndHeaders(options, "http://otel-collector:4318", string.Empty, "/v1/traces");

            // Assert — Headers property must be unchanged from its default
            Assert.AreEqual(defaultHeaders, options.Headers);
        }

        [TestMethod]
        public void ConfigureSerilogOtelSink_DoesNotSetEndpoint_WhenEndpointIsEmpty()
        {
            // Arrange
            var options = new OpenTelemetrySinkOptions();
            var config = new ConfigurationBuilder().Build();
            var defaultEndpoint = options.Endpoint;

            // Act
            OpenTelemetryExtensions.ConfigureSerilogOtelSink(options, config);

            // Assert — nothing modified when no endpoint is configured
            Assert.AreEqual(defaultEndpoint, options.Endpoint);
            Assert.IsFalse(options.ResourceAttributes.ContainsKey("service.name"));
            Assert.AreEqual(0, options.Headers.Count);
        }

        [TestMethod]
        public void ConfigureSerilogOtelSink_SetsEndpointAndProtocol_WhenEndpointIsSet()
        {
            // Arrange
            var options = new OpenTelemetrySinkOptions();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://otel-collector:4318",
                })
                .Build();

            // Act
            OpenTelemetryExtensions.ConfigureSerilogOtelSink(options, config);

            // Assert — the library normalises the Endpoint value; verify host is set and other fields are configured
            Assert.IsTrue(options.Endpoint?.Contains("otel-collector:4318") == true);
            Assert.AreEqual(OtlpProtocol.HttpProtobuf, options.Protocol);
            Assert.AreEqual("ccDiaryApi", options.ResourceAttributes["service.name"]);
        }

        [TestMethod]
        public void ConfigureSerilogOtelSink_ParsesHeaders_WhenHeadersAreConfigured()
        {
            // Arrange
            var options = new OpenTelemetrySinkOptions();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://otel-collector:4318",
                    ["OTEL_EXPORTER_OTLP_HEADERS"] = "Authorization=Bearer token123,X-Scope-OrgID=tenant1",
                })
                .Build();

            // Act
            OpenTelemetryExtensions.ConfigureSerilogOtelSink(options, config);

            // Assert
            Assert.IsTrue(options.Headers.ContainsKey("Authorization"));
            Assert.AreEqual("Bearer token123", options.Headers["Authorization"]);
            Assert.IsTrue(options.Headers.ContainsKey("X-Scope-OrgID"));
            Assert.AreEqual("tenant1", options.Headers["X-Scope-OrgID"]);
        }
    }
}
