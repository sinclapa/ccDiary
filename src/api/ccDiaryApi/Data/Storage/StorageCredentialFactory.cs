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
    /// The chain is deliberately narrowed to the two sources that can actually succeed
    /// here: managed identity in the Container App, and the Azure CLI for a developer
    /// or the migration tool. Every other source in the default chain is a failed
    /// network or process probe on the first storage call, which is paid on a cold start.
    /// </remarks>
    public static class StorageCredentialFactory
    {
        /// <summary>Creates the narrowed credential.</summary>
        /// <returns>A token credential for the storage data plane.</returns>
        public static TokenCredential Create()
        {
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeWorkloadIdentityCredential = true,
            });
        }
    }
}
