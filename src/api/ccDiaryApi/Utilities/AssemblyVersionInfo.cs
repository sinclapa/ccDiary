// <copyright file="AssemblyVersionInfo.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Utilities
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    public static class AssemblyVersionInfo
    {
        public static string GetInformationalVersion(Assembly? assembly = null)
        {
            assembly = ResolveAssembly(assembly);

            // Prefer AssemblyInformationalVersion (semantic + metadata)
            var infoAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(infoAttr))
            {
                return infoAttr;
            }

            return GetFileOrProductVersion(assembly) ?? GetVersionFallback(assembly);
        }

        // GetEntryAssembly() returns null only in unusual hosting scenarios (single-file trimmed,
        // native AOT). Not practically testable in unit/integration tests.
        [ExcludeFromCodeCoverage(Justification = "GetEntryAssembly() returning null is not reachable in standard test environments.")]
        private static Assembly ResolveAssembly(Assembly? assembly) =>
            assembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        // AssemblyName.Version is null only when the assembly has no version metadata at all
        // (not reachable via AssemblyBuilder.DefineDynamicAssembly which defaults to 0.0.0.0).
        // The "unknown" path is similarly unreachable in standard .NET runtimes.
        [ExcludeFromCodeCoverage(Justification = "null-Version and 'unknown' fallback not reachable in standard .NET environments.")]
        private static string GetVersionFallback(Assembly assembly) =>
            assembly.GetName().Version?.ToString() ?? "unknown";

        // These fallback paths only execute in unusual deployment scenarios (single-file trimmed
        // builds, assemblies without InformationalVersion). Not practically testable in unit tests.
        [ExcludeFromCodeCoverage(Justification = "Fallback for edge deployment scenarios; not testable without specially crafted assemblies.")]
        private static string? GetFileOrProductVersion(Assembly assembly)
        {
            try
            {
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    if (!string.IsNullOrEmpty(fvi.ProductVersion))
                    {
                        return fvi.ProductVersion;
                    }

                    if (!string.IsNullOrEmpty(fvi.FileVersion))
                    {
                        return fvi.FileVersion;
                    }
                }
            }
            catch
            {
                // ignore - some hosts (single-file trimmed) may not expose Location
            }

            return null;
        }
    }
}
