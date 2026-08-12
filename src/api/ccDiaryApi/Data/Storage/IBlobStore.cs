// <copyright file="IBlobStore.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using global::Azure.Storage.Blobs;

    /// <summary>
    /// Blob operations for the payloads that cannot live in a table row: entry images,
    /// cached map tiles and routes, and spilled entry JSON.
    /// </summary>
    public interface IBlobStore
    {
        /// <summary>Gets a value indicating whether storage configuration was supplied.</summary>
        bool IsConfigured { get; }

        /// <summary>Gets the container client for a logical container name.</summary>
        /// <param name="container">The unprefixed container name.</param>
        /// <returns>The container client.</returns>
        BlobContainerClient Container(string container);

        /// <summary>Writes a blob, overwriting any existing content.</summary>
        /// <param name="container">The unprefixed container name.</param>
        /// <param name="name">The blob name.</param>
        /// <param name="content">The content to write.</param>
        /// <param name="contentType">The MIME type to record, if any.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task PutAsync(string container, string name, BinaryData content, string? contentType = null, CancellationToken cancellationToken = default);

        /// <summary>Reads a blob, returning <c>null</c> when it does not exist.</summary>
        /// <param name="container">The unprefixed container name.</param>
        /// <param name="name">The blob name.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The blob with its metadata, or <c>null</c>.</returns>
        Task<StoredBlob?> TryGetAsync(string container, string name, CancellationToken cancellationToken = default);

        /// <summary>Reads a blob as text, returning <c>null</c> when it does not exist.</summary>
        /// <param name="container">The unprefixed container name.</param>
        /// <param name="name">The blob name.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The blob content, or <c>null</c>.</returns>
        Task<string?> TryGetStringAsync(string container, string name, CancellationToken cancellationToken = default);

        /// <summary>Deletes a blob if it exists.</summary>
        /// <param name="container">The unprefixed container name.</param>
        /// <param name="name">The blob name.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns><c>true</c> when a blob was deleted.</returns>
        Task<bool> DeleteIfExistsAsync(string container, string name, CancellationToken cancellationToken = default);

        /// <summary>Deletes every blob under a prefix.</summary>
        /// <param name="container">The unprefixed container name.</param>
        /// <param name="prefix">The blob name prefix.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The number of blobs deleted.</returns>
        /// <remarks>
        /// This is how a diary's images are removed when the diary is deleted, which the
        /// database used to do with a cascade.
        /// </remarks>
        Task<int> DeleteByPrefixAsync(string container, string prefix, CancellationToken cancellationToken = default);
    }
}
