// <copyright file="AssemblyVersionInfo.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Utilities
{
    using System.Diagnostics;
    using System.Reflection;

    public static class AssemblyVersionInfo
    {
        public static string GetInformationalVersion(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            // Prefer AssemblyInformationalVersion (semantic + metadata)
            var infoAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(infoAttr))
            {
                return infoAttr;
            }

            // Fallback to product/file version from file metadata (works when InformationalVersion not set)
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

            // Final fallback to AssemblyName.Version
            return assembly.GetName().Version?.ToString() ?? "unknown";
        }
    }
}
