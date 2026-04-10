// <copyright file="DatabaseHealthContributor.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Health
{
    using System.Diagnostics.CodeAnalysis;
    using ccDiaryApi.Data.Context;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Steeltoe.Common.HealthChecks;

    /// <summary>
    /// Health contributor that checks SQL Server database connectivity.
    /// </summary>
    public class DatabaseHealthContributor : IHealthContributor
    {
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHealthContributor"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory used to resolve scoped services.</param>
        public DatabaseHealthContributor(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <inheritdoc/>
        public string Id => "db";

        /// <inheritdoc/>
        public HealthCheckResult Health()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DiaryDatabaseContext>();
                dbContext.Database.ExecuteSqlRaw("SELECT 1");

                var pendingMigrationError = CheckPendingMigrations(dbContext);
                if (pendingMigrationError != null)
                {
                    return pendingMigrationError;
                }

                return new HealthCheckResult
                {
                    Status = HealthStatus.UP,
                    Details = new Dictionary<string, object>
                    {
                        { "status", HealthStatus.UP.ToString() },
                        { "database", "SQL Server" },
                    },
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    Status = HealthStatus.DOWN,
                    Details = new Dictionary<string, object>
                    {
                        { "status", HealthStatus.DOWN.ToString() },
                        { "database", "SQL Server" },
                        { "error", ex.Message },
                    },
                };
            }
        }

        /// <summary>
        /// Returns a DOWN result if SQL Server has pending EF Core migrations, null otherwise.
        /// SQLite (used in tests via EnsureCreated) is intentionally skipped.
        /// Requires a live SQL Server; not testable in unit/integration tests.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Requires a live SQL Server with pending migrations; not testable in unit/integration tests.")]
        private static HealthCheckResult? CheckPendingMigrations(DiaryDatabaseContext dbContext)
        {
            if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.SqlServer")
            {
                return null;
            }

            var pending = dbContext.Database.GetPendingMigrations().ToList();
            if (pending.Count == 0)
            {
                return null;
            }

            return new HealthCheckResult
            {
                Status = HealthStatus.DOWN,
                Details = new Dictionary<string, object>
                {
                    { "status", HealthStatus.DOWN.ToString() },
                    { "database", "SQL Server" },
                    { "error", $"Pending migrations: {string.Join(", ", pending)}" },
                },
            };
        }
    }
}
