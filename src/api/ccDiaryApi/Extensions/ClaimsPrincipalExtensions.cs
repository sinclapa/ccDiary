// <copyright file="ClaimsPrincipalExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Extensions
{
    using System.Security.Claims;

    public static class ClaimsPrincipalExtensions
    {
        private const string OidClaimType = "oid";
        private const string OidClaimTypeAlt = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        // v2 tokens name the calling client "azp", v1 tokens "appid"; a delegated (user) token
        // carries scopes in "scp", mapped or not, and an app-only token carries none.
        private static readonly string[] ClientIdClaimTypes = { "azp", "appid" };
        private static readonly string[] ScopeClaimTypes = { "scp", "http://schemas.microsoft.com/identity/claims/scope" };

        public static string? GetOid(this ClaimsPrincipal user) =>
            user.FindFirst(OidClaimType)?.Value
            ?? user.FindFirst(OidClaimTypeAlt)?.Value;

        /// <summary>
        /// Whether the caller is the application's own registration, calling for itself with an
        /// app-only token rather than on behalf of a signed-in user.
        /// </summary>
        /// <param name="user">The authenticated principal.</param>
        /// <param name="applicationClientId">The application's own client id; no id matches nothing.</param>
        /// <returns>True for an app-only token issued to <paramref name="applicationClientId"/>.</returns>
        public static bool IsApplicationItself(this ClaimsPrincipal user, string? applicationClientId)
        {
            if (string.IsNullOrWhiteSpace(applicationClientId)
                || ScopeClaimTypes.Any(type => user.HasClaim(c => c.Type == type)))
            {
                return false;
            }

            return ClientIdClaimTypes
                .Select(type => user.FindFirst(type)?.Value)
                .Any(value => string.Equals(value, applicationClientId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
