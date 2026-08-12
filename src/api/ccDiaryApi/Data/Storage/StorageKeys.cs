// <copyright file="StorageKeys.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Derives every partition key, row key and blob name used by the storage layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These functions are <b>frozen</b>. There is no schema migration step in a
    /// key-value store: changing how a key is derived does not move existing rows, it
    /// orphans them silently, because lookups start addressing a location nothing was
    /// ever written to. Adding a new key shape is fine; altering an existing one is a
    /// data migration, not a refactor.
    /// </para>
    /// <para>
    /// Table partition and row keys may not contain <c>/</c>, <c>\</c>, <c>#</c>,
    /// <c>?</c> or control characters, and are capped at 1024 characters.
    /// </para>
    /// </remarks>
    public static class StorageKeys
    {
        /// <summary>Partition key for the diary table; the set is small enough for one partition.</summary>
        public const string DiaryPartition = "diary";

        /// <summary>Partition key for the app user table.</summary>
        public const string UserPartition = "user";

        /// <summary>
        /// Partition key for the access request table.
        /// </summary>
        /// <remarks>
        /// Deliberately constant rather than partitioned by status: status is mutable,
        /// and status-as-partition-key would turn every approval into a non-atomic
        /// cross-partition write-then-delete.
        /// </remarks>
        public const string RequestPartition = "request";

        /// <summary>Partition key for the singleton app info row.</summary>
        public const string AppInfoPartition = "appinfo";

        /// <summary>Row key for the singleton app info row.</summary>
        public const string AppInfoRow = "1";

        /// <summary>Partition key for the geocoding cache table.</summary>
        public const string GeocodePartition = "geo";

        /// <summary>
        /// Sortable, fixed-width UTC timestamp format used as the diary entry row key prefix.
        /// </summary>
        /// <remarks>
        /// Fixed width matters: Table Storage orders row keys lexicographically, so a
        /// zero-padded timestamp makes date ordering and <c>RowKey ge/lt</c> range
        /// queries work without any secondary index.
        /// </remarks>
        private const string DateFormat = "yyyyMMddHHmmssfffffff";

        private static readonly char[] IllegalKeyChars = new[] { '/', '\\', '#', '?' };

        /// <summary>
        /// Builds the row key for a diary entry: a sortable UTC timestamp followed by
        /// the entry id, so entries sort by date and ties break deterministically.
        /// </summary>
        /// <param name="date">The entry date; null sorts before all real dates.</param>
        /// <param name="entryId">The entry identifier, guaranteeing uniqueness.</param>
        /// <returns>The row key.</returns>
        public static string EntryRowKey(DateTime? date, Guid entryId)
        {
            var prefix = date.HasValue
                ? FormatDate(date.Value)
                : new string('0', DateFormat.Length);
            return $"{prefix}-{entryId:N}";
        }

        /// <summary>
        /// Builds the row key prefix for a point in time, for use in <c>RowKey ge</c> /
        /// <c>RowKey lt</c> range filters.
        /// </summary>
        /// <param name="date">The boundary date.</param>
        /// <returns>The row key prefix.</returns>
        public static string EntryRowKeyPrefix(DateTime date) => FormatDate(date);

        /// <summary>
        /// Builds the row key for a cached geocoding query.
        /// </summary>
        /// <param name="query">The raw search text.</param>
        /// <returns>A deterministic, always-legal row key.</returns>
        /// <remarks>
        /// Free text routinely contains characters a row key forbids, so the normalised
        /// query is hashed rather than escaped. The raw query is stored alongside as a
        /// column so rows stay readable.
        /// </remarks>
        public static string GeocodeKey(string query)
        {
            var normalised = (query ?? string.Empty).Trim().ToLowerInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
            return Convert.ToHexString(hash)[..32];
        }

        /// <summary>
        /// Builds the blob name for a cached route.
        /// </summary>
        /// <param name="profile">The routing profile (for example <c>driving</c>).</param>
        /// <param name="fromLat">Origin latitude.</param>
        /// <param name="fromLon">Origin longitude.</param>
        /// <param name="toLat">Destination latitude.</param>
        /// <param name="toLon">Destination longitude.</param>
        /// <returns>The blob name.</returns>
        /// <remarks>
        /// Coordinates are quantised to six decimal places (about 0.1 m) and rendered as
        /// integers, which makes the lookup an exact-match blob fetch. The previous
        /// relational implementation compared with a tolerance, which forced a full table
        /// scan because the comparison could not use the index.
        /// </remarks>
        public static string RouteBlobKey(string profile, double fromLat, double fromLon, double toLat, double toLon)
        {
            var safeProfile = SanitiseKey(profile);
            return $"routes/{safeProfile}/{Quantise(fromLat)}_{Quantise(fromLon)}_{Quantise(toLat)}_{Quantise(toLon)}.json";
        }

        /// <summary>Builds the blob name for a cached map tile.</summary>
        /// <param name="source">The upstream tile source.</param>
        /// <param name="z">Zoom level.</param>
        /// <param name="x">Tile X coordinate.</param>
        /// <param name="y">Tile Y coordinate.</param>
        /// <returns>The blob name.</returns>
        public static string TileBlobKey(string source, int z, int x, int y) =>
            $"tiles/{SanitiseKey(source)}/{z}/{x}/{y}";

        /// <summary>Builds the blob name for a diary entry image.</summary>
        /// <param name="diaryId">The owning diary, whose prefix makes cascade delete a prefix scan.</param>
        /// <param name="entryId">The diary entry.</param>
        /// <returns>The blob name.</returns>
        public static string ImageBlobKey(Guid diaryId, Guid entryId) => $"{diaryId:N}/{entryId:N}";

        /// <summary>Builds the blob name for a diary entry whose JSON exceeded the spill threshold.</summary>
        /// <param name="entryId">The diary entry.</param>
        /// <returns>The blob name.</returns>
        public static string EntryJsonBlobKey(Guid entryId) => $"entries/{entryId:N}.json";

        /// <summary>
        /// Replaces characters that are illegal in a table key with underscores and
        /// truncates to the 1024 character key limit.
        /// </summary>
        /// <param name="value">The candidate key fragment.</param>
        /// <returns>A legal key fragment.</returns>
        public static string SanitiseKey(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                builder.Append(char.IsControl(c) || Array.IndexOf(IllegalKeyChars, c) >= 0 ? '_' : c);
            }

            var sanitised = builder.ToString();
            return sanitised.Length > 1024 ? sanitised[..1024] : sanitised;
        }

        /// <summary>
        /// Normalises a date to UTC without depending on the server's time zone for
        /// values whose kind is unspecified.
        /// </summary>
        private static string FormatDate(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

            return utc.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        private static string Quantise(double value) =>
            ((long)Math.Round(value * 1_000_000, MidpointRounding.AwayFromZero))
                .ToString(CultureInfo.InvariantCulture);
    }
}
