// <copyright file="AuthenticationLoggingExtensions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Extensions
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Serilog;

    /// <summary>
    /// Authentication logging configuration helpers.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Authentication event wiring is infrastructure configuration and not meaningful for line-level coverage.")]
    public static class AuthenticationLoggingExtensions
    {
        /// <summary>
        /// Configures JWT bearer event handlers for authentication observability.
        /// </summary>
        /// <param name="jwtBearerOptions">JWT bearer options to configure.</param>
        public static void ConfigureJwtBearerEvents(JwtBearerOptions jwtBearerOptions)
        {
            jwtBearerOptions.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var sanitizedPath = context.Request.Path.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty);
                    Log.Logger.Warning(context.Exception, "JWT authentication failed for {Path}", sanitizedPath);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var sanitizedPath = context.Request.Path.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty);
                    Log.Logger.Information("JWT authentication challenge for {Path}", sanitizedPath);
                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    var sanitizedPath = context.Request.Path.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty);
                    Log.Logger.Warning("JWT authorization forbidden for {Path}", sanitizedPath);
                    return Task.CompletedTask;
                },
            };
        }
    }
}
