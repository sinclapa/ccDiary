// <copyright file="StoredBlob.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    /// <summary>
    /// A blob's content together with the metadata the caller needs.
    /// </summary>
    public sealed record StoredBlob
    {
        /// <summary>Gets the blob content.</summary>
        required public BinaryData Content { get; init; }

        /// <summary>Gets the recorded MIME type, if any.</summary>
        public string? ContentType { get; init; }

        /// <summary>
        /// Gets the time the blob was last written.
        /// </summary>
        /// <remarks>
        /// The map caches use this as their expiry clock, which is why content and
        /// metadata are returned together rather than needing a separate properties call.
        /// </remarks>
        public DateTimeOffset LastModified { get; init; }
    }
}
