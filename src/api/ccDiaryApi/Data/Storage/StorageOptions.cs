// <copyright file="StorageOptions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    /// <summary>
    /// Configuration for the Azure Table + Blob storage backend, bound from the
    /// <c>Storage</c> configuration section.
    /// </summary>
    /// <remarks>
    /// Exactly one of <see cref="ConnectionString"/> or <see cref="AccountName"/> is
    /// expected. A connection string is used locally against Azurite; in Azure the
    /// account name is combined with the Container App's managed identity so no
    /// secret is ever stored.
    /// </remarks>
    public class StorageOptions
    {
        /// <summary>The configuration section these options bind from.</summary>
        public const string SectionName = "Storage";

        /// <summary>
        /// Gets or sets the storage connection string. Set locally (Azurite); left
        /// empty in Azure, where <see cref="AccountName"/> plus managed identity is used.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the storage account name, used with a token credential when
        /// <see cref="ConnectionString"/> is not set.
        /// </summary>
        public string? AccountName { get; set; }

        /// <summary>Gets or sets the container holding diary entry images.</summary>
        public string ImagesContainer { get; set; } = "images";

        /// <summary>Gets or sets the container holding cached map tiles and routes.</summary>
        public string MapCacheContainer { get; set; } = "mapcache";

        /// <summary>Gets or sets the container holding spilled diary entry JSON.</summary>
        public string ContentContainer { get; set; } = "content";

        /// <summary>
        /// Gets or sets a prefix applied to every table name. Empty in normal use;
        /// tests set a unique value so concurrent fixtures cannot collide.
        /// </summary>
        public string TableNamePrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a prefix applied to every container name, for the same reason
        /// as <see cref="TableNamePrefix"/>.
        /// </summary>
        public string ContainerPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the serialised size above which a diary entry's JSON is written
        /// to a blob instead of the table row.
        /// </summary>
        /// <remarks>
        /// A single Table string property caps at 64 KB; this threshold leaves ample
        /// margin. Entries in the real data average about 5 KB, so the spill path is
        /// rarely taken.
        /// </remarks>
        public int JsonSpillThresholdBytes { get; set; } = 30000;

        /// <summary>Gets or sets how long a cached map tile stays valid.</summary>
        public TimeSpan TileTtl { get; set; } = TimeSpan.FromDays(90);

        /// <summary>Gets or sets how long a cached geocoding result stays valid.</summary>
        public TimeSpan GeocodingTtl { get; set; } = TimeSpan.FromDays(180);

        /// <summary>Gets or sets how long a cached route stays valid.</summary>
        public TimeSpan RoutingTtl { get; set; } = TimeSpan.FromDays(90);

        /// <summary>
        /// Gets a value indicating whether enough configuration is present to reach storage.
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ConnectionString) || !string.IsNullOrWhiteSpace(AccountName);
    }
}
