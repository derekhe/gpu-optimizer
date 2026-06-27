using System;
using System.Collections.Generic;
using System.Linq;
using GpuOptimizer.Core.Models;

namespace GpuOptimizer.Core.Services;

public static class GpuPreferenceValue
{
    private const string PreferencePrefix = "GpuPreference=";
    private static readonly IReadOnlyDictionary<int, GpuPreferenceKind> KnownValues =
        new Dictionary<int, GpuPreferenceKind>
        {
            [(int)GpuPreferenceKind.SystemDefault] = GpuPreferenceKind.SystemDefault,
            [(int)GpuPreferenceKind.PowerSaving] = GpuPreferenceKind.PowerSaving,
            [(int)GpuPreferenceKind.HighPerformance] = GpuPreferenceKind.HighPerformance,
        };

    public static GpuPreferenceKind Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GpuPreferenceKind.SystemDefault;
        }

        var tokens = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (!token.StartsWith(PreferencePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var numericPart = token.AsSpan()[PreferencePrefix.Length..];
            if (int.TryParse(numericPart, out var numeric) &&
                KnownValues.TryGetValue(numeric, out var known))
            {
                return known;
            }

            return GpuPreferenceKind.Unknown;
        }

        return GpuPreferenceKind.SystemDefault;
    }

    public static string EnsurePowerSaving(string? existingValue)
    {
        var target = $"{PreferencePrefix}{(int)GpuPreferenceKind.PowerSaving}";
        if (string.IsNullOrWhiteSpace(existingValue))
        {
            return $"{target};";
        }

        var tokens = existingValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var found = false;
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!tokens[i].StartsWith(PreferencePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            tokens[i] = target;
            found = true;
            break;
        }

        if (!found)
        {
            tokens.Add(target);
        }

        return string.Join(";", tokens) + ";";
    }
}
