// <copyright file="ProgramStartupConfigurationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Diagnostics;
    using Asp.Versioning;
    using Asp.Versioning.ApiExplorer;
    using ccDiaryApi;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Unit tests for startup configuration helpers in <see cref="Program"/>.
    /// </summary>
    [TestClass]
    public class ProgramStartupConfigurationTest
    {
        [TestMethod]
        public void ConfigureJwtBearer_SetsEvents()
        {
            // Arrange
            var options = new JwtBearerOptions();

            // Act
            Program.ConfigureJwtBearer(options);

            // Assert
            Assert.IsNotNull(options.Events);
            Assert.IsNotNull(options.Events.OnAuthenticationFailed);
            Assert.IsNotNull(options.Events.OnChallenge);
            Assert.IsNotNull(options.Events.OnForbidden);
        }

        [TestMethod]
        public void ConfigureApiVersioning_SetsExpectedOptions()
        {
            // Arrange
            var options = new ApiVersioningOptions();

            // Act
            Program.ConfigureApiVersioning(options);

            // Assert
            Assert.IsTrue(options.ReportApiVersions);
            Assert.IsInstanceOfType(options.ApiVersionReader, typeof(UrlSegmentApiVersionReader));
        }

        [TestMethod]
        public void ConfigureApiExplorer_SetsExpectedOptions()
        {
            // Arrange
            var options = new ApiExplorerOptions();

            // Act
            Program.ConfigureApiExplorer(options);

            // Assert
            Assert.AreEqual("'v'VVV", options.GroupNameFormat);
            Assert.IsTrue(options.SubstituteApiVersionInUrl);
        }

        [TestMethod]
        public void GetRequiredConnectionString_UsesAzureSqlConnectionString_WhenPresent()
        {
            // Arrange
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AZURE_SQL_CONNECTIONSTRING"] = "Server=tcp:test;Database=db;",
                    ["ConnectionStrings:SqlConnection"] = "Server=tcp:unused;Database=db;",
                })
                .Build();

            // Act
            var result = Program.GetRequiredConnectionString(configuration);

            // Assert
            Assert.AreEqual("Server=tcp:test;Database=db;", result);
        }

        [TestMethod]
        public void GetRequiredConnectionString_UsesConnectionStringsSection_WhenAzureVariableMissing()
        {
            // Arrange
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SqlConnection"] = "Server=tcp:fallback;Database=db;",
                })
                .Build();

            // Act
            var result = Program.GetRequiredConnectionString(configuration);

            // Assert
            Assert.AreEqual("Server=tcp:fallback;Database=db;", result);
        }

        [TestMethod]
        public void GetRequiredConnectionString_Throws_WhenNoConnectionStringConfigured()
        {
            // Arrange
            IConfiguration configuration = new ConfigurationBuilder().Build();

            // Act + Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => Program.GetRequiredConnectionString(configuration));
            StringAssert.Contains(exception.Message, "valid SQL connection string");
        }

        [TestMethod]
        public void RunDatabaseMigration_WorksWithoutActiveActivity()
        {
            // Arrange
            using var app = BuildMinimalWebApplication();
            using var activitySource = new ActivitySource("ProgramStartupConfigurationTest.NoListener");
            var migrationCalled = false;

            // Act
            Program.RunDatabaseMigration(app, activitySource, _ => migrationCalled = true);

            // Assert
            Assert.IsTrue(migrationCalled);
        }

        [TestMethod]
        public void RunDatabaseMigration_WorksWithActiveActivity()
        {
            // Arrange
            using var app = BuildMinimalWebApplication();
            using var activitySource = new ActivitySource("ProgramStartupConfigurationTest.WithListener");
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == activitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            };
            ActivitySource.AddActivityListener(listener);
            var migrationCalled = false;

            // Act
            Program.RunDatabaseMigration(app, activitySource, _ => migrationCalled = true);

            // Assert
            Assert.IsTrue(migrationCalled);
        }

        private static WebApplication BuildMinimalWebApplication()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Development",
            });

            return builder.Build();
        }
    }
}
