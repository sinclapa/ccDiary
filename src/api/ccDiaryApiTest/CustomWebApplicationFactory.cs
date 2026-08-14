// <copyright file="CustomWebApplicationFactory.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using ccDiaryApi.Services;
    using ccDiaryApiTest.Storage;
    using global::Azure.Data.Tables;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;

    /// <summary>
    /// Boots the real application against Azurite.
    /// </summary>
    /// <remarks>
    /// Each factory instance gets its own table and container name prefix, so parallel
    /// test classes and repeated local runs cannot see each other's data. That is a
    /// stronger guarantee than the single shared in-memory database this replaced.
    /// </remarks>
    /// <typeparam name="TProgram">The application entry point type.</typeparam>
    public class CustomWebApplicationFactory<TProgram>
         : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        private readonly string _prefix = "t" + Guid.NewGuid().ToString("N")[..8];

        public string DefaultUserId { get; set; } = "TestUser";

        /// <summary>
        /// Gets or sets the redeem URL returned by the mocked <see cref="IGraphService"/>.
        /// Set to <see cref="string.Empty"/> to simulate Graph not configured.
        /// </summary>
        public string GraphRedeemUrl { get; set; } = "https://test-redeem.example.com";

        /// <summary>
        /// Removes every diary, entry, user and access request, plus the blobs that
        /// belong to them. Call from [TestInitialize] to get a clean state.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearDatabaseAsync()
        {
            var tables = Services.GetRequiredService<ITableStore>();
            var blobs = Services.GetRequiredService<IBlobStore>();
            var options = Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;

            // The caches are cleared too. The previous implementation left them behind,
            // which only stayed harmless because the cache tests used a separate database.
            // AppInfo is excluded: it is written once by the bootstrapper at startup and
            // is startup state rather than test data — clearing it makes the app info
            // endpoint 404 and the health check report an incomplete bootstrap.
            foreach (var table in tables.All.Where(t => t != tables.AppInfo))
            {
                var rows = await TableJson.QueryAsync(table);
                foreach (var partition in rows.GroupBy(r => r.PartitionKey))
                {
                    await TableJson.DeleteBatchAsync(table, partition.Key, partition.Select(r => r.RowKey));
                }
            }

            foreach (var container in new[] { options.ImagesContainer, options.ContentContainer, options.MapCacheContainer })
            {
                await blobs.DeleteByPrefixAsync(container, string.Empty);
            }
        }

        /// <summary>
        /// Seeds an AppUser and returns the user's OID string.
        /// </summary>
        /// <param name="oid">The Entra Object ID for the user.</param>
        /// <param name="role">The role to assign to the user.</param>
        /// <returns>The OID string passed in.</returns>
        /// <remarks>
        /// Roles come from the database rather than the token, so a policy test has to
        /// seed a real user; setting a role claim directly would bypass the enrichment
        /// middleware and prove nothing.
        /// </remarks>
        public async Task<string> CreateAppUserAsync(string oid, AppRole role)
        {
            var tables = Services.GetRequiredService<ITableStore>();
            var user = new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = oid,
                DisplayName = $"Test {role}",
                Email = $"{oid}@test.com",
                Role = role,
                CreatedAt = DateTime.UtcNow,
            };

            var entity = TableJson.ToEntity(
                StorageKeys.UserPartition,
                StorageKeys.SanitiseKey(oid),
                user,
                e =>
                {
                    e["UserId"] = user.UserId.ToString();
                    e["Email"] = user.Email;
                    e["Role"] = user.Role.ToStoredValue();
                });

            await tables.AppUsers.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            return oid;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            StorageTestFixture.RequireAzurite();

            builder.UseEnvironment("Development");
            builder.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", string.Empty);
            builder.UseSetting("Storage:ConnectionString", StorageTestFixture.AzuriteConnectionString);
            builder.UseSetting("Storage:TableNamePrefix", _prefix);
            builder.UseSetting("Storage:ContainerPrefix", _prefix + "-");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<TestAuthHandlerOptions>(options => options.DefaultUserId = DefaultUserId);

                services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });

                var graphMock = new Mock<IGraphService>();
                graphMock
                    .Setup(g => g.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(() => GraphRedeemUrl);
                services.AddScoped<IGraphService>(_ => graphMock.Object);

                var emailMock = new Mock<IEmailService>();
                emailMock
                    .Setup(e => e.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
                services.AddScoped<IEmailService>(_ => emailMock.Object);
            });
        }

        /// <summary>Removes the tables and containers this factory created.</summary>
        /// <param name="disposing">Whether managed resources should be released.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TryCleanup();
            }

            base.Dispose(disposing);
        }

        private void TryCleanup()
        {
            try
            {
                var tables = Services.GetRequiredService<ITableStore>();
                var blobs = Services.GetRequiredService<IBlobStore>();
                var options = Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;

                foreach (var table in tables.All)
                {
                    table.Delete();
                }

                foreach (var container in new[] { options.ImagesContainer, options.ContentContainer, options.MapCacheContainer })
                {
                    blobs.Container(container).DeleteIfExists();
                }
            }
            catch (Exception)
            {
                // Teardown is best effort; a leaked emulator table must not fail a green run.
            }
        }
    }
}
