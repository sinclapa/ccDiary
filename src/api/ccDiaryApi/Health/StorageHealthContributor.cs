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
    /// They run concurrently: neither depends on the other, and this sits on the path of
    /// the container's startup probe.
    /// </para>
    /// <para>
    /// A healthy result is cached briefly so that probe polling does not turn into a
    /// steady stream of storage round trips. Failures are deliberately not cached — during
    /// startup the first probes legitimately report the app info row missing, and holding
    /// on to that would keep the container out of rotation after it was ready.
    /// </para>
    /// </remarks>
    public class StorageHealthContributor : IHealthContributor
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan HealthyFor = TimeSpan.FromSeconds(5);

        private readonly ITableStore _tables;
        private readonly IBlobStore _blobs;
        private readonly StorageOptions _options;

        private HealthCheckResult? _cachedHealthy;
        private DateTime _cachedHealthyUntilUtc = DateTime.MinValue;

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
            var cached = _cachedHealthy;
            if (cached != null && DateTime.UtcNow < _cachedHealthyUntilUtc)
            {
                return cached;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Steeltoe's contributor interface is synchronous, so the async calls are
                // blocked on behind a short timeout rather than being allowed to hang the
                // actuator endpoint.
                using var cts = new CancellationTokenSource(Timeout);

                var appInfoTask = TableJson.GetIfExistsAsync(
                    _tables.AppInfo, StorageKeys.AppInfoPartition, StorageKeys.AppInfoRow, cts.Token);
                var blobTask = _blobs.Container(_options.ImagesContainer)
                    .GetPropertiesAsync(cancellationToken: cts.Token);

                Task.WhenAll(appInfoTask, blobTask).GetAwaiter().GetResult();

                if (appInfoTask.Result == null)
                {
                    return Down("storage bootstrap incomplete: app info row is missing", stopwatch);
                }

                stopwatch.Stop();
                var healthy = new HealthCheckResult
                {
                    Status = HealthStatus.UP,
                    Details = new Dictionary<string, object>
                    {
                        { "status", HealthStatus.UP.ToString() },
                        { "database", "Azure Table Storage" },
                        { "latencyMs", stopwatch.ElapsedMilliseconds },
                    },
                };

                _cachedHealthy = healthy;
                _cachedHealthyUntilUtc = DateTime.UtcNow.Add(HealthyFor);
                return healthy;
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
