using System;
using System.IO;

namespace GpuOptimizer.Core.Scanning;

internal static class SystemExecutablePathResolver
{
    public static string ResolveExecutablePath(string processName)
    {
        return ResolveExecutablePath(processName, Environment.SystemDirectory);
    }

    internal static string ResolveExecutablePath(string processName, string systemDirectory)
    {
        if (string.IsNullOrWhiteSpace(processName) ||
            string.IsNullOrWhiteSpace(systemDirectory) ||
            processName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            return string.Empty;
        }

        var executableName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName
            : $"{processName}.exe";
        var executablePath = Path.Combine(systemDirectory, executableName);
        return File.Exists(executablePath) ? executablePath : string.Empty;
    }
}
