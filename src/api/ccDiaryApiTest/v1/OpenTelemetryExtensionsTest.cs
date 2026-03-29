// <copyright file="OpenTelemetryExtensionsTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
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
