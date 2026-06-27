using System;
using System.Collections.Generic;
using Vortice.DXGI;

namespace GpuOptimizer.Core.Scanning;

internal static class DxgiGpuAdapterEnumerator
{
    public static IReadOnlyDictionary<string, string> GetHardwareAdapterLabels()
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory6>();
            var integratedAdapterKey = GetPreferredAdapterKey(factory, GpuPreference.MinimumPower);
            var discreteAdapterKey = GetPreferredAdapterKey(factory, GpuPreference.HighPerformance);

            for (uint adapterIndex = 0; factory.EnumAdapters1(adapterIndex, out var adapter).Success; adapterIndex++)
            {
                using (adapter)
                {
                    var description = adapter.Description1;
                    if (description.Flags.HasFlag(AdapterFlags.Software) ||
                        IsVirtualAdapter(description.Description))
                    {
                        continue;
                    }

                    var adapterKey = GpuCounterInstanceParser.FormatAdapterKey(description.Luid.HighPart, description.Luid.LowPart);
                    labels[adapterKey] = $"{description.Description} ({ClassifyAdapter(adapterKey, integratedAdapterKey, discreteAdapterKey)})";
                }
            }
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return labels;
    }

    private static string GetPreferredAdapterKey(IDXGIFactory6 factory, GpuPreference preference)
    {
        try
        {
            if (factory.EnumAdapterByGpuPreference<IDXGIAdapter1>(0, preference, out var adapter).Failure ||
                adapter is null)
            {
                return string.Empty;
            }

            using (adapter)
            {
                var description = adapter.Description1;
                return GpuCounterInstanceParser.FormatAdapterKey(description.Luid.HighPart, description.Luid.LowPart);
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ClassifyAdapter(string adapterKey, string integratedAdapterKey, string discreteAdapterKey)
    {
        if (!string.IsNullOrWhiteSpace(integratedAdapterKey) &&
            adapterKey.Equals(integratedAdapterKey, StringComparison.OrdinalIgnoreCase))
        {
            return "Integrated";
        }

        if (!string.IsNullOrWhiteSpace(discreteAdapterKey) &&
            adapterKey.Equals(discreteAdapterKey, StringComparison.OrdinalIgnoreCase))
        {
            return "Discrete";
        }

        return "GPU";
    }

    private static bool IsVirtualAdapter(string description)
    {
        return description.Contains("virtual", StringComparison.OrdinalIgnoreCase) ||
               description.Contains("software", StringComparison.OrdinalIgnoreCase) ||
               description.Contains("basic render", StringComparison.OrdinalIgnoreCase);
    }
}
