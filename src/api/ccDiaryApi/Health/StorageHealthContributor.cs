// <copyright file="StorageHealthContributor.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Health
{
    using System.Diagnostics;
    using ccDiaryApi.Data.Storage;
    using Microsoft.Extensions.Options;
    using Steeltoe.Common.HealthChecks;

    /// <summary>
    /// Reports whether the storage data plane is reachable and bootstrap completed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The id stays <c>db</c>: the deployment workflow and an end-to-end test both assert
    /// on <c>details.db.status</c>, and renaming it would silently stop gating deploys.
    /// </para>
    /// <para>
    /// Two checks, because the table and blob data planes are granted by two separate
    /// role assignments and can fail independently. Reading the app info row also proves
    /// the bootstrapper ran, which is what replaces the old pending-migrations check.
    /// </para>
    /// </remarks>
    public class StorageHealthContributor : IHealthContributor
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        private readonly ITableStore _tables;
        private readonly IBlobStore _blobs;
        private readonly StorageOptions _options;

        /// <summary>Initializes a new instance of the <see cref="StorageHealthContributor"/> class.</summary>
        /// <param name="tables">The table store.</param>
        /// <param name="blobs">The blob store.</param>
        /// <param name="options">The storage options.</param>
        public StorageHealthContributor(ITableStore tables, IBlobStore blobs, IOptions<StorageOptions> options)
        {
            _tables = tables;
            _blobs = blobs;
            _options = options.Value;
        }

        /// <inheritdoc/>
        public string Id => "db";

        /// <inheritdoc/>
        public HealthCheckResult Health()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Steeltoe's contributor interface is synchronous, so the async calls are
                // blocked on behind a short timeout rather than being allowed to hang the
                // actuator endpoint.
                using var cts = new CancellationTokenSource(Timeout);

                var appInfo = TableJson
                    .GetIfExistsAsync(_tables.AppInfo, StorageKeys.AppInfoPartition, StorageKeys.AppInfoRow, cts.Token)
                    .GetAwaiter()
                    .GetResult();

                if (appInfo == null)
                {
                    return Down("storage bootstrap incomplete: app info row is missing", stopwatch);
                }

                _blobs.Container(_options.ImagesContainer)
                    .GetPropertiesAsync(cancellationToken: cts.Token)
                    .GetAwaiter()
                    .GetResult();

                stopwatch.Stop();
                return new HealthCheckResult
                {
                    Status = HealthStatus.UP,
                    Details = new Dictionary<string, object>
                    {
                        { "status", HealthStatus.UP.ToString() },
                        { "database", "Azure Table Storage" },
                        { "latencyMs", stopwatch.ElapsedMilliseconds },
                    },
                };
            }
            catch (Exception ex)
            {
                return Down(ex.Message, stopwatch);
            }
        }

        private static HealthCheckResult Down(string error, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            return new HealthCheckResult
            {
                Status = HealthStatus.DOWN,
                Details = new Dictionary<string, object>
                {
                    { "status", HealthStatus.DOWN.ToString() },
                    { "database", "Azure Table Storage" },
                    { "latencyMs", stopwatch.ElapsedMilliseconds },
                    { "error", error },
                },
            };
        }
    }
}
