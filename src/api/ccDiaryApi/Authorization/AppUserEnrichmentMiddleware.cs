// <copyright file="AppUserEnrichmentMiddleware.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Authorization
{
    using System.Security.Claims;
    using ccDiaryApi.Extensions;
    using ccDiaryApi.Services;

    /// <summary>
    /// Adds a role claim to the current user's identity based on their AppUser record in the database.
    /// Must run after UseAuthentication().
    /// </summary>
    public static class AppUserEnrichmentMiddleware
    {
        public static IApplicationBuilder UseAppUserEnrichment(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var oid = context.User.GetOid();
                    if (!string.IsNullOrEmpty(oid))
                    {
                        var userService = context.RequestServices.GetRequiredService<IUserService>();
                        var appUser = await userService.GetUserByOidAsync(oid);
                        if (appUser != null)
                        {
                            var identity = (ClaimsIdentity)context.User.Identity;
                            identity.AddClaim(new Claim(ClaimTypes.Role, appUser.Role.ToString()));
                        }
                    }
                }

                await next();
            });
    }
}
