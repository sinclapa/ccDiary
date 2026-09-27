// <copyright file="ArchiveImportHandler.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Authorization
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Extensions;
    using Microsoft.AspNetCore.Authorization;

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
