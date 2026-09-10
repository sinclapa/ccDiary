// <copyright file="StorageCredentialFactory.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Storage
{
    using global::Azure.Core;
    using global::Azure.Identity;

    /// <summary>
    /// Builds the token credential used to reach storage when no connection string is configured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The chain is deliberately narrowed to the two sources that can actually succeed
    /// here: managed identity in the Container App, and the Azure CLI for a developer
    /// or the migration tool. Every other source in the default chain is a failed
    /// network or process probe on the first storage call, which is paid on a cold start.
    /// </para>
    /// <para>
    /// One instance is shared for the same reason. The token cache belongs to the
    /// credential, so a credential per store meant the first table call and the first blob
    /// call each acquired their own token from IMDS — two round trips on the startup path
    /// to obtain the same token. <see cref="DefaultAzureCredential"/> is thread-safe, so
    /// sharing it is what the SDK expects.
    /// </para>
    /// </remarks>
    public static class StorageCredentialFactory
    {
        private static readonly Lazy<TokenCredential> Shared = new (
            () => new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeWorkloadIdentityCredential = true,
            }),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>Gets the narrowed credential, shared across every storage client.</summary>
        /// <returns>A token credential for the storage data plane.</returns>
        public static TokenCredential Create() => Shared.Value;
    }
}
