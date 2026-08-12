// <copyright file="ITableStore.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using global::Azure.Data.Tables;

    /// <summary>
    /// Provides the table clients used by the storage-backed services.
    /// </summary>
    public interface ITableStore
    {
        /// <summary>Gets a value indicating whether storage configuration was supplied.</summary>
        bool IsConfigured { get; }

        /// <summary>Gets the diary table.</summary>
        TableClient Diaries { get; }

        /// <summary>Gets the diary entry table.</summary>
        TableClient DiaryEntries { get; }

        /// <summary>Gets the application user table.</summary>
        TableClient AppUsers { get; }

        /// <summary>Gets the access request table.</summary>
        TableClient AccessRequests { get; }

        /// <summary>Gets the singleton application info table.</summary>
        TableClient AppInfo { get; }

        /// <summary>Gets the geocoding cache table.</summary>
        TableClient GeocodingCache { get; }

        /// <summary>Gets every table this store manages, for bootstrap and teardown.</summary>
        IReadOnlyList<TableClient> All { get; }
    }
}
