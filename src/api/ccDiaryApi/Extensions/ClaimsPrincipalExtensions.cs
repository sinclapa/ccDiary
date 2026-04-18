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

        public static string? GetOid(this ClaimsPrincipal user) =>
            user.FindFirst(OidClaimType)?.Value
            ?? user.FindFirst(OidClaimTypeAlt)?.Value;
    }
}
