// <copyright file="ArchiveImportRequirement.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Authorization
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Extensions;
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

    /// <summary>Grants <see cref="ArchiveImportRequirement"/> to an admin or to the application's own app-only token.</summary>
    public sealed class ArchiveImportHandler : AuthorizationHandler<ArchiveImportRequirement>
    {
        private readonly IConfiguration _configuration;

        /// <summary>Initializes a new instance of the <see cref="ArchiveImportHandler"/> class.</summary>
        /// <param name="configuration">Supplies <c>Entra:ClientId</c>, the application's own client id.</param>
        public ArchiveImportHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc/>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ArchiveImportRequirement requirement)
        {
            if (context.User.IsInRole(AppRole.DiaryAdmin.ToString())
                || context.User.IsApplicationItself(_configuration["Entra:ClientId"]))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
