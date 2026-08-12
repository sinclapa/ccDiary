// <copyright file="RequestLimits.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers
{
    /// <summary>
    /// Explicit request body size limits for the endpoints that accept large payloads.
    /// </summary>
    /// <remarks>
    /// Diary entries embed images as base64, so these endpoints are the only ones that
    /// can receive a large body. Without an explicit limit they fall back to Kestrel's
    /// 30 MB default, which is not obvious from the code and leaves no headroom margin
    /// documented anywhere. The largest real image in the sample data is ~3.3 MB base64
    /// and the largest real archive is ~25 MB, so both limits below are set to clear
    /// those with room to spare while still bounding memory on a 0.5 GiB container.
    /// </remarks>
    public static class RequestLimits
    {
        /// <summary>Maximum body size for a whole-diary archive import.</summary>
        public const long ArchiveImportBytes = 32L * 1024 * 1024;

        /// <summary>Maximum body size for a single diary entry create or update.</summary>
        public const long DiaryEntryBytes = 16L * 1024 * 1024;
    }
}
