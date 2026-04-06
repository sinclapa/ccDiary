// <copyright file="CustomWebApplicationFactory.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest
{
    using System.Data.Common;
    using System.Linq;
    using ccDiaryApi.Data.Context;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;

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
        /// Removes all diary entries and diaries from the database.
        /// Call from [TestInitialize] to ensure a clean state before each test.
        /// </summary>
        public async Task ClearDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DiaryDatabaseContext>();
            db.DiaryEntries.RemoveRange(db.DiaryEntries);
            db.Diaries.RemoveRange(db.Diaries);
            await db.SaveChangesAsync();
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
            });
        }
    }
}
