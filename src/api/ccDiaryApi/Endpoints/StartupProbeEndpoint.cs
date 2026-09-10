// <copyright file="StartupProbeEndpoint.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Endpoints
{
    using ccDiaryApi.Infrastructure;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The endpoint the Container App startup and readiness probes poll.
    /// </summary>
    /// <remarks>
    /// Mounted as a pipeline branch rather than a routed endpoint, and ahead of everything else,
    /// so a probe never runs request logging, CORS, authentication or MVC. On a cold process each
    /// of those is JIT-compiled on first use, and ingress holds a waiting user's request until
    /// this answers 200. The path sits under <c>/health</c>, which tracing and request logging
    /// already exclude.
    /// </remarks>
    public static class StartupProbeEndpoint
    {
        /// <summary>The path the probes in <c>deploy/containerApps.bicep</c> request.</summary>
        public const string Path = "/health/startup";

        /// <summary>Adds the startup probe branch to the pipeline.</summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The same builder, for chaining.</returns>
        public static IApplicationBuilder UseStartupProbe(this IApplicationBuilder app)
        {
            return app.Map(new PathString(Path), branch => branch.Run(RespondAsync));
        }

        /// <summary>Answers 200 once bootstrap has completed, and 503 until then.</summary>
        /// <param name="context">The request context.</param>
        /// <returns>A completed task.</returns>
        internal static Task RespondAsync(HttpContext context)
        {
            var readiness = context.RequestServices.GetRequiredService<StartupReadiness>();
            context.Response.StatusCode = readiness.IsReady
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        }
    }
}
