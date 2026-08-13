// <copyright file="BlobStore.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using global::Azure;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Blob operations over the configured storage account.
    /// </summary>
    /// <remarks>
    /// Containers are created by <c>StorageBootstrapper</c> at startup, never here.
    /// Doing it in the constructor would put a blocking network
    /// call inside dependency resolution, which stalls the first request on a cold start
    /// and hides a configuration failure behind an unrelated stack trace.
    /// </remarks>
    public class BlobStore : IBlobStore
    {
        private readonly BlobServiceClient? _service;
        private readonly string _containerPrefix;

        /// <summary>Initializes a new instance of the <see cref="BlobStore"/> class.</summary>
        /// <param name="options">The storage options.</param>
        public BlobStore(IOptions<StorageOptions> options)
        {
            var value = options.Value;
            _containerPrefix = value.ContainerPrefix ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(value.ConnectionString))
            {
                _service = new BlobServiceClient(value.ConnectionString);
                IsConfigured = true;
            }
            else if (!string.IsNullOrWhiteSpace(value.AccountName))
            {
                _service = new BlobServiceClient(
                    new Uri($"https://{value.AccountName}.blob.core.windows.net"),
                    StorageCredentialFactory.Create());
                IsConfigured = true;
            }
        }

        /// <inheritdoc/>
        public bool IsConfigured { get; }

        /// <inheritdoc/>
        public BlobContainerClient Container(string container)
        {
            if (_service == null)
            {
                throw new InvalidOperationException("Blob storage is not configured.");
            }

            return _service.GetBlobContainerClient(_containerPrefix + container);
        }

        /// <inheritdoc/>
        public async Task PutAsync(
            string container,
            string name,
            BinaryData content,
            string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            var blob = Container(container).GetBlobClient(name);
            var options = new BlobUploadOptions();
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                options.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            }

            await blob.UploadAsync(content, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<StoredBlob?> TryGetAsync(
            string container,
            string name,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var blob = Container(container).GetBlobClient(name);
                var response = await blob.DownloadContentAsync(cancellationToken);
                return new StoredBlob
                {
                    Content = response.Value.Content,
                    ContentType = response.Value.Details.ContentType,
                    LastModified = response.Value.Details.LastModified,
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<string?> TryGetStringAsync(
            string container,
            string name,
            CancellationToken cancellationToken = default)
        {
            var blob = await TryGetAsync(container, name, cancellationToken);
            return blob?.Content.ToString();
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteIfExistsAsync(
            string container,
            string name,
            CancellationToken cancellationToken = default)
        {
            var blob = Container(container).GetBlobClient(name);
            var response = await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByPrefixAsync(
            string container,
            string prefix,
            CancellationToken cancellationToken = default)
        {
            var client = Container(container);
            var deleted = 0;

            await foreach (var blob in client.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                var response = await client.GetBlobClient(blob.Name)
                    .DeleteIfExistsAsync(cancellationToken: cancellationToken);
                if (response.Value)
                {
                    deleted++;
                }
            }

            return deleted;
        }
    }
}
