using System;

namespace GpuOptimizer.Core.Models;

public enum GpuPreferenceKind
{
    Unknown = -1,
    SystemDefault = 0,
    PowerSaving = 1,
    HighPerformance = 2,
}

public sealed class GpuProcessInfo
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public string GpuAdapters { get; init; } = "N/A";
    public string GpuEngines { get; init; } = "N/A";
    public double GpuUtilizationPercent { get; init; }
    public double DedicatedMemoryMb { get; init; }
    public IReadOnlyList<GpuAdapterMemoryUsage> AdapterMemoryUsage { get; init; } = [];
    public double SharedMemoryMb { get; init; }
    public GpuPreferenceKind CurrentPreference { get; init; } = GpuPreferenceKind.Unknown;
    public bool IsOptimizable { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsAccessibleProcessPath => !string.IsNullOrWhiteSpace(ExecutablePath);

    public override string ToString() =>
        $"{ProcessName} ({ProcessId})";
}

public sealed record GpuAdapterMemoryUsage(string AdapterName, double DedicatedMemoryMb);
