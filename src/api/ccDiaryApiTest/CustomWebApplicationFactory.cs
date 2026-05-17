// <copyright file="CustomWebApplicationFactory.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest
{
    using System.Data.Common;
    using System.Linq;
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;

    public class CustomWebApplicationFactory<TProgram>
         : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        private readonly SqliteConnection _connection;

        public CustomWebApplicationFactory()
        {
            _connection = new SqliteConnection($"Data Source=:memory:");
            _connection.Open();
        }

        public string DefaultUserId { get; set; } = "TestUser";

        /// <summary>
        /// Gets or sets the redeem URL returned by the mocked <see cref="IGraphService"/>.
        /// Set to <see cref="string.Empty"/> to simulate Graph not configured.
        /// </summary>
        public string GraphRedeemUrl { get; set; } = "https://test-redeem.example.com";

        /// <summary>
        /// Removes all diary entries and diaries from the database.
        /// Call from [TestInitialize] to ensure a clean state before each test.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DiaryDatabaseContext>();
            db.AccessRequests.RemoveRange(db.AccessRequests);
            db.AppUsers.RemoveRange(db.AppUsers);
            db.DiaryEntries.RemoveRange(db.DiaryEntries);
            db.Diaries.RemoveRange(db.Diaries);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds an AppUser into the database and returns the user's OID string.
        /// </summary>
        /// <param name="oid">The Entra Object ID for the user.</param>
        /// <param name="role">The role to assign to the user.</param>
        /// <returns>The OID string passed in.</returns>
        public async Task<string> CreateAppUserAsync(string oid, AppRole role)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DiaryDatabaseContext>();
            var user = new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = oid,
                DisplayName = $"Test {role}",
                Email = $"{oid}@test.com",
                Role = role,
                CreatedAt = DateTime.UtcNow,
            };
            db.AppUsers.Add(user);
            await db.SaveChangesAsync();
            return oid;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("RUN_MIGRATIONS", "true");
            builder.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", string.Empty);
            builder.ConfigureServices(services =>
            {
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<DiaryDatabaseContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var dbContextFactoryDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDbContextOptionsConfiguration<DiaryDatabaseContext>));
                if (dbContextFactoryDescriptor != null)
                {
                    services.Remove(dbContextFactoryDescriptor);
                }

                services.AddDbContext<DiaryDatabaseContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
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
    }
}
