// <copyright file="AssemblyInfoEndpoint.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Endpoints
{
    using System.Reflection;
    using ccDiaryApi.Utilities;
    using Microsoft.AspNetCore.Builder;

    public static class AssemblyInfoEndpoint
    {
        public static IEndpointRouteBuilder MapAssemblyInfo(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/assembly-info", () =>
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var assemblyName = assembly.GetName().Name ?? "Unknown";
                var assemblyVersion = AssemblyVersionInfo.GetInformationalVersion(assembly);

                return Results.Ok(new
                {
                    assemblyName,
                    assemblyVersion,
                });
            }).AllowAnonymous();

            return endpoints;
        }
    }
}
