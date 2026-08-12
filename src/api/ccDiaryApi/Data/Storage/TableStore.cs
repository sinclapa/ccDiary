// <copyright file="TableStore.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using global::Azure.Data.Tables;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Resolves the configured table clients from either a connection string (Azurite)
    /// or an account name plus managed identity (Azure).
    /// </summary>
    /// <remarks>
    /// Registered as a singleton: <see cref="TableServiceClient"/> is thread-safe, pools
    /// its connections, and caches tokens, so sharing one instance avoids re-acquiring a
    /// token on every request.
    /// </remarks>
    public class TableStore : ITableStore
    {
        private readonly TableServiceClient? _service;
        private readonly Dictionary<string, TableClient> _clients = new (StringComparer.Ordinal);

        /// <summary>Initializes a new instance of the <see cref="TableStore"/> class.</summary>
        /// <param name="options">The storage options.</param>
        public TableStore(IOptions<StorageOptions> options)
        {
            var value = options.Value;

            if (!string.IsNullOrWhiteSpace(value.ConnectionString))
            {
                _service = new TableServiceClient(value.ConnectionString);
            }
            else if (!string.IsNullOrWhiteSpace(value.AccountName))
            {
                _service = new TableServiceClient(
                    new Uri($"https://{value.AccountName}.table.core.windows.net"),
                    StorageCredentialFactory.Create());
            }
            else
            {
                IsConfigured = false;
                return;
            }

            IsConfigured = true;

            var prefix = value.TableNamePrefix ?? string.Empty;
            Diaries = Table(prefix, "diary");
            DiaryEntries = Table(prefix, "diaryentry");
            AppUsers = Table(prefix, "appuser");
            AccessRequests = Table(prefix, "accessrequest");
            AppInfo = Table(prefix, "appinfo");
            GeocodingCache = Table(prefix, "geocodingcache");
            All = new[] { Diaries, DiaryEntries, AppUsers, AccessRequests, AppInfo, GeocodingCache };
        }

        /// <inheritdoc/>
        public bool IsConfigured { get; }

        /// <inheritdoc/>
        public TableClient Diaries { get; } = null!;

        /// <inheritdoc/>
        public TableClient DiaryEntries { get; } = null!;

        /// <inheritdoc/>
        public TableClient AppUsers { get; } = null!;

        /// <inheritdoc/>
        public TableClient AccessRequests { get; } = null!;

        /// <inheritdoc/>
        public TableClient AppInfo { get; } = null!;

        /// <inheritdoc/>
        public TableClient GeocodingCache { get; } = null!;

        /// <inheritdoc/>
        public IReadOnlyList<TableClient> All { get; } = Array.Empty<TableClient>();

        private TableClient Table(string prefix, string name)
        {
            var full = prefix + name;
            if (!_clients.TryGetValue(full, out var client))
            {
                client = _service!.GetTableClient(full);
                _clients[full] = client;
            }

            return client;
        }
    }
}
