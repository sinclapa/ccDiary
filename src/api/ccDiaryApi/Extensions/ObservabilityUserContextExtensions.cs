// <copyright file="ObservabilityUserContextExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Extensions
{
    using System.Diagnostics;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using Serilog.Context;

    /// <summary>
    /// Adds per-request user context enrichment for observability.
    /// </summary>
    public static class ObservabilityUserContextExtensions
    {
        /// <summary>
        /// Enriches OpenTelemetry spans and Serilog context with a pseudonymous user id.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for fluent chaining.</returns>
        public static IApplicationBuilder UseObservabilityUserContext(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                var user = context.User;
                var isAuthenticated = user.Identity?.IsAuthenticated ?? false;

                Activity.Current?.SetTag("enduser.authenticated", isAuthenticated);

                if (!isAuthenticated)
                {
                    await next();
                    return;
                }

                var rawUserId = ResolveUserIdentifier(user);
                if (string.IsNullOrEmpty(rawUserId))
                {
                    await next();
                    return;
                }

                var pseudonymousUserId = CreatePseudonymousUserId(rawUserId);
                Activity.Current?.SetTag("enduser.id", pseudonymousUserId);

                using (LogContext.PushProperty("EndUserId", pseudonymousUserId))
                {
                    await next();
                }
            });

        /// <summary>
        /// Resolves a stable identity claim for correlation.
        /// </summary>
        /// <param name="user">The authenticated principal.</param>
        /// <returns>A stable identifier value when available; otherwise <c>null</c>.</returns>
        public static string? ResolveUserIdentifier(ClaimsPrincipal user) =>
            user.FindFirst("oid")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        /// <summary>
        /// Produces a non-reversible, short hash suitable for logs and telemetry tags.
        /// </summary>
        /// <param name="rawUserId">The original stable user identifier claim value.</param>
        /// <returns>A deterministic pseudonymous id.</returns>
        public static string CreatePseudonymousUserId(string rawUserId)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawUserId));
            return Convert.ToHexString(hashBytes[..12]).ToLowerInvariant();
        }
    }
}
