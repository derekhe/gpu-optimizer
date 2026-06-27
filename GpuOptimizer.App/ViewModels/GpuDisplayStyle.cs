using System;
using GpuOptimizer.App.Localization;

namespace GpuOptimizer.App.ViewModels;

internal static class GpuDisplayStyle
{
    public static bool IsIntegratedGpu(string gpuName)
    {
        return gpuName.Contains("(Integrated)", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDiscreteGpu(string gpuName)
    {
        return gpuName.Contains("(Discrete)", StringComparison.OrdinalIgnoreCase) ||
            (!IsIntegratedGpu(gpuName) && gpuName.Contains("(GPU)", StringComparison.OrdinalIgnoreCase));
    }

    public static string GetKindLabel(string gpuName)
    {
        if (IsIntegratedGpu(gpuName))
        {
            return AppLocalizer.Current.Get("PowerSavingTarget");
        }

        if (IsDiscreteGpu(gpuName))
        {
            return AppLocalizer.Current.Get("PotentialSavingPool");
        }

        return AppLocalizer.Current.Get("GpuMemoryKind");
    }

    public static string GetAccentBrush(string gpuName)
    {
        if (IsIntegratedGpu(gpuName))
        {
            return "#059669";
        }

        if (IsDiscreteGpu(gpuName))
        {
            return "#EA580C";
        }

        return "#6B7280";
    }

    public static string GetBackgroundBrush(string gpuName)
    {
        if (IsIntegratedGpu(gpuName))
        {
            return "#22059669";
        }

        if (IsDiscreteGpu(gpuName))
        {
            return "#22EA580C";
        }

        return "#F9FAFB";
    }

    public static string GetBorderBrush(string gpuName)
    {
        if (IsIntegratedGpu(gpuName))
        {
            return "#66059669";
        }

        if (IsDiscreteGpu(gpuName))
        {
            return "#66EA580C";
        }

        return "#E5E7EB";
    }
}
