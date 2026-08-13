// <copyright file="StorageBootstrapper.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Infrastructure
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using ccDiaryApi.Services;
    using ccDiaryApi.Utilities;
    using global::Azure.Data.Tables;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Creates the tables and containers the application expects, at startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This replaces EF migrations. A schema-less store has nothing to migrate, so the
    /// entire schema step is "make sure the containers exist"; the shape of a row is
    /// whatever the current code serialises, and rows written before a property existed
    /// deserialise using that property's default.
    /// </para>
    /// <para>
    /// Failing here must fail startup. When registered as a hosted service, an exception
    /// from <see cref="StartAsync"/> prevents the host from starting, which surfaces in
    /// the deployment workflow as a revision that never reaches a running state. That is
    /// the replacement for the old pending-migrations health check.
    /// </para>
    /// </remarks>
    public class StorageBootstrapper : IHostedService
    {
        private readonly ITableStore _tables;
        private readonly IBlobStore _blobs;
        private readonly StorageOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StorageBootstrapper> _logger;

        /// <summary>Initializes a new instance of the <see cref="StorageBootstrapper"/> class.</summary>
        /// <param name="tables">The table store.</param>
        /// <param name="blobs">The blob store.</param>
        /// <param name="options">The storage options.</param>
        /// <param name="scopeFactory">Resolves the scoped services used for seeding.</param>
        /// <param name="logger">The logger.</param>
        public StorageBootstrapper(
            ITableStore tables,
            IBlobStore blobs,
            IOptions<StorageOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<StorageBootstrapper> logger)
        {
            _tables = tables;
            _blobs = blobs;
            _options = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_tables.IsConfigured || !_blobs.IsConfigured)
            {
                _logger.LogWarning("Storage is not configured; skipping storage bootstrap.");
                return;
            }

            foreach (var table in _tables.All)
            {
                await table.CreateIfNotExistsAsync(cancellationToken);
                _logger.LogInformation("Table ready: {Table}", table.Name);
            }

            foreach (var container in ContainerNames())
            {
                var client = _blobs.Container(container);
                await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                _logger.LogInformation("Container ready: {Container}", client.Name);
            }

            // Ordering matters: both of these write rows, so they cannot run until the
            // tables above exist.
            await UpdateAppInfoAsync(cancellationToken);
            await SeedBootstrapAdminAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Records the running version and the time storage was last prepared.
        /// </summary>
        /// <remarks>
        /// Carried over verbatim from the EF migration manager, which did this on every
        /// boot. The health check reads this row to prove that bootstrap completed, so
        /// it is also what makes a broken deployment visible.
        /// </remarks>
        private async Task UpdateAppInfoAsync(CancellationToken cancellationToken)
        {
            var appInfo = new AppInfoDTO
            {
                Id = 1,
                InformationalVersion = AssemblyVersionInfo.GetInformationalVersion(),
                DatabaseLastUpdated = DateTime.UtcNow,
            };

            var entity = TableJson.ToEntity(
                StorageKeys.AppInfoPartition,
                StorageKeys.AppInfoRow,
                appInfo,
                e =>
                {
                    e["InformationalVersion"] = appInfo.InformationalVersion;
                    e["DatabaseLastUpdated"] = appInfo.DatabaseLastUpdated;
                });

            await _tables.AppInfo.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
            _logger.LogInformation("App info updated. Version={Version}", appInfo.InformationalVersion);
        }

        /// <summary>Creates the first administrator, if one is configured and none exists.</summary>
        private async Task SeedBootstrapAdminAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserService>();
            await users.SeedBootstrapAdminAsync();
        }

        /// <summary>Gets the containers the application requires.</summary>
        /// <returns>The unprefixed container names.</returns>
        private IEnumerable<string> ContainerNames()
        {
            yield return _options.ImagesContainer;
            yield return _options.MapCacheContainer;
            yield return _options.ContentContainer;
        }
    }
}
