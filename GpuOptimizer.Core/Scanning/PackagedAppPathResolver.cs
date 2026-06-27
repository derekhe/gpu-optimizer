using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GpuOptimizer.Core.Scanning;

internal static class PackagedAppPathResolver
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> ExecutablePathByName = new(BuildExecutableMap);

    public static string ResolveExecutablePath(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return string.Empty;
        }

        var executableName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName
            : $"{processName}.exe";

        return ExecutablePathByName.Value.TryGetValue(executableName, out var path) ? path : string.Empty;
    }

    private static IReadOnlyDictionary<string, string> BuildExecutableMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var windowsAppsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "WindowsApps");

        if (!Directory.Exists(windowsAppsPath))
        {
            return map;
        }

        IEnumerable<string> packageDirectories;
        try
        {
            packageDirectories = Directory
                .EnumerateDirectories(windowsAppsPath)
                .OrderByDescending(GetLastWriteTimeUtc)
                .ToArray();
        }
        catch
        {
            return map;
        }

        foreach (var packageDirectory in packageDirectories)
        {
            AddPackageExecutables(packageDirectory, map);
        }

        return map;
    }

    private static void AddPackageExecutables(string packageDirectory, Dictionary<string, string> map)
    {
        try
        {
            var manifestPath = Path.Combine(packageDirectory, "AppxManifest.xml");
            if (!File.Exists(manifestPath))
            {
                return;
            }

            var document = XDocument.Load(manifestPath);
            var applications = document.Descendants().Where(element => element.Name.LocalName == "Application");
            foreach (var application in applications)
            {
                var executable = application.Attribute("Executable")?.Value;
                if (string.IsNullOrWhiteSpace(executable))
                {
                    continue;
                }

                var executablePath = Path.Combine(packageDirectory, executable);
                if (!File.Exists(executablePath))
                {
                    continue;
                }

                map.TryAdd(Path.GetFileName(executablePath), executablePath);
            }
        }
        catch
        {
            // Some package directories are not readable; ignore them.
        }
    }

    private static DateTime GetLastWriteTimeUtc(string path)
    {
        try
        {
            return Directory.GetLastWriteTimeUtc(path);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
