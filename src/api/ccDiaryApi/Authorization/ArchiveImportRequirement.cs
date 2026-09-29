// <copyright file="ArchiveImportRequirement.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Authorization
{
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// Who may import a diary archive: an admin, or the application itself.
    /// </summary>
    /// <remarks>
    /// An import can overwrite any diary it names, so a signed-in reader or contributor may not.
    /// The deploy pipeline seeds each environment before its end-to-end run with an app-only
    /// token for the environment's own app registration, obtained through a federated credential
    /// that only this repository's workflows can use. That identity has no AppUser row and so no
    /// role, but it is the application acting for itself, and is trusted for this.
    /// </remarks>
    public sealed class ArchiveImportRequirement : IAuthorizationRequirement
    {
        /// <summary>The name the policy is registered under.</summary>
        public const string PolicyName = "ArchiveImport";
    }
}
