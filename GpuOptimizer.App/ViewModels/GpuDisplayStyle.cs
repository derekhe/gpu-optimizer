using System;

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
            return "Power-saving target";
        }

        if (IsDiscreteGpu(gpuName))
        {
            return "Potential saving pool";
        }

        return "GPU memory";
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
            return "#ECFDF5";
        }

        if (IsDiscreteGpu(gpuName))
        {
            return "#FFF7ED";
        }

        return "#F9FAFB";
    }

    public static string GetBorderBrush(string gpuName)
    {
        if (IsIntegratedGpu(gpuName))
        {
            return "#A7F3D0";
        }

        if (IsDiscreteGpu(gpuName))
        {
            return "#FED7AA";
        }

        return "#E5E7EB";
    }
}
