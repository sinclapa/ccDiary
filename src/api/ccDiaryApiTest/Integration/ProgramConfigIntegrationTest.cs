// <copyright file="ProgramConfigIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;
    using ccDiaryApi.Data.Context;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;

    [TestClass]
    public class ProgramConfigIntegrationTest
    {
        [TestMethod]
        public async Task App_StartsAndResponds_WhenMigrationsDisabled()
        {
            // Arrange — RUN_MIGRATIONS=false exercises the else-branch (lines 121-123) in Program.cs
            var factory = new NoMigrationFactory();
            var client = factory.CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/api/v1/Diary/Get");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task App_StartsAndResponds_WhenSaPasswordIsSet()
        {
            // Arrange — SA_PASSWORD set as an environment variable so AddEnvironmentVariables()
            // in Program.cs picks it up and exercises the password-override branch (lines 55-58).
            Environment.SetEnvironmentVariable("SA_PASSWORD", "TestPassword123!");
            try
            {
                var factory = new SaPasswordFactory();
                var client = factory.CreateDefaultClient();

                // Act
                var response = await client.GetAsync("/api/v1/Diary/Get");

                // Assert
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SA_PASSWORD", null);
            }
        }

        [TestMethod]
        public async Task App_StartsAndResponds_WhenEnvironmentIsLocal()
        {
            // Arrange — "Local" environment exercises the AddUserSecrets branch (lines 30-32)
            var factory = new LocalEnvironmentFactory();
            var client = factory.CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/api/v1/Diary/Get");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Factory that sets RUN_MIGRATIONS=false to cover the skip-migration branch in Program.cs.
        /// Uses an in-memory database so no schema creation is needed.
        /// </summary>
        private sealed class NoMigrationFactory : WebApplicationFactory<Program>
        {
            private readonly string _dbName = "NoMigration_" + Guid.NewGuid();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("RUN_MIGRATIONS", "false");
                builder.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", string.Empty);
                builder.ConfigureServices(services =>
                {
                    var dbDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<DiaryDatabaseContext>));
                    if (dbDescriptor != null)
                    {
                        services.Remove(dbDescriptor);
                    }

                    var dbFactoryDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IDbContextOptionsConfiguration<DiaryDatabaseContext>));
                    if (dbFactoryDescriptor != null)
                    {
                        services.Remove(dbFactoryDescriptor);
                    }

                    services.AddDbContext<DiaryDatabaseContext>(o => o.UseInMemoryDatabase(_dbName));
                });
                builder.ConfigureTestServices(services =>
                {
                    services.Configure<TestAuthHandlerOptions>(options => options.DefaultUserId = "TestUser");
                    services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                        .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(
                            TestAuthHandler.AuthenticationScheme, options => { });
                });
            }
        }

        /// <summary>
        /// Factory that sets SA_PASSWORD to cover the password-override branch in Program.cs.
        /// The environment variable must be set before the factory builds the host so that
        /// AddEnvironmentVariables() (Program.cs line 28) picks it up.
        /// </summary>
        private sealed class SaPasswordFactory : CustomWebApplicationFactory<Program>
        {
        }

        /// <summary>
        /// Factory that runs in "Local" environment to cover the AddUserSecrets branch.
        /// </summary>
        private sealed class LocalEnvironmentFactory : CustomWebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.UseEnvironment("Local");
            }
        }
    }
}
