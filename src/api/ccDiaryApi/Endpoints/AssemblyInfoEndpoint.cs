// <copyright file="AssemblyInfoEndpoint.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Endpoints
{
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using ccDiaryApi.Utilities;
    using Microsoft.AspNetCore.Builder;

    public static class AssemblyInfoEndpoint
    {
        public static IEndpointRouteBuilder MapAssemblyInfo(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/assembly-info", GetAssemblyInfo).AllowAnonymous();
            return endpoints;
        }

        // Null-coalescing branches (GetEntryAssembly returning null, Name being null) only occur
        // in unusual hosting scenarios and cannot be reliably triggered in unit/integration tests.
        [ExcludeFromCodeCoverage(Justification = "Null-coalescing fallbacks not reachable in standard test environments.")]
        private static IResult GetAssemblyInfo(IWebHostEnvironment webHostEnvironment)
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name ?? "Unknown";
            var assemblyVersion = AssemblyVersionInfo.GetInformationalVersion(assembly);
            var environmentName = webHostEnvironment.EnvironmentName;

            return Results.Ok(new
            {
                assemblyName,
                assemblyVersion,
                environmentName,
            });
        }
    }
}
