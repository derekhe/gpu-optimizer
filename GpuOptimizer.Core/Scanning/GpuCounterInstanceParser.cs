using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GpuOptimizer.Core.Scanning;

internal static class GpuCounterInstanceParser
{
    private static readonly Regex PidRegex = new(@"(?:^|_)pid_(\d+)(?:_|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex AdapterLuidRegex = new(@"luid_(0x[0-9a-f]+)_(0x[0-9a-f]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryGetProcessIdFromInstance(string instanceName, out int processId)
    {
        var match = PidRegex.Match(instanceName);
        if (match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out processId))
        {
            return processId > 0;
        }

        processId = 0;
        return false;
    }

    public static string ParseEngineName(string instanceName)
    {
        var tokens = instanceName.Split('_');
        for (var i = 0; i < tokens.Length - 1; i++)
        {
            if (tokens[i].Equals("engtype", StringComparison.OrdinalIgnoreCase))
            {
                return tokens[i + 1];
            }
        }

        for (var i = 0; i < tokens.Length - 1; i++)
        {
            if (tokens[i].Equals("eng", StringComparison.OrdinalIgnoreCase))
            {
                return tokens[i + 1];
            }
        }

        return instanceName;
    }

    public static bool TryGetAdapterKeyFromInstance(string instanceName, out string adapterKey)
    {
        var match = AdapterLuidRegex.Match(instanceName);
        if (match.Success)
        {
            adapterKey = $"{match.Groups[1].Value.ToLowerInvariant()}_{match.Groups[2].Value.ToLowerInvariant()}";
            return true;
        }

        adapterKey = string.Empty;
        return false;
    }
}
