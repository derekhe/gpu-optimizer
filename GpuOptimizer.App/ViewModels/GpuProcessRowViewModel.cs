using CommunityToolkit.Mvvm.ComponentModel;
using GpuOptimizer.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace GpuOptimizer.App.ViewModels;

public partial class GpuProcessRowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private int processId;

    [ObservableProperty]
    private string processName = string.Empty;

    [ObservableProperty]
    private string executablePath = string.Empty;

    public string ProcessDetailsTooltip =>
        string.IsNullOrWhiteSpace(ExecutablePath)
            ? $"PID: {ProcessId}\nPath: unavailable"
            : $"PID: {ProcessId}\nPath: {ExecutablePath}";

    [ObservableProperty]
    private string gpuAdapters = string.Empty;

    public IReadOnlyList<GpuBadgeViewModel> GpuBadges =>
        AdapterMemoryUsage
            .Select(memory => memory.AdapterName)
            .Distinct()
            .Select(gpuName => new GpuBadgeViewModel
            {
                GpuName = gpuName,
                AccentBrush = GpuDisplayStyle.GetAccentBrush(gpuName),
                BackgroundBrush = GpuDisplayStyle.GetBackgroundBrush(gpuName),
                BorderBrush = GpuDisplayStyle.GetBorderBrush(gpuName)
            })
            .ToList();

    [ObservableProperty]
    private string gpuEngines = string.Empty;

    [ObservableProperty]
    private double gpuUtilizationPercent;

    [ObservableProperty]
    private double dedicatedMemoryMb;

    public IReadOnlyList<GpuAdapterMemoryUsage> AdapterMemoryUsage { get; init; } = [];

    [ObservableProperty]
    private double sharedMemoryMb;

    [ObservableProperty]
    private string currentPreference = GpuPreferenceKind.Unknown.ToString();

    [ObservableProperty]
    private bool isOptimizable;

    [ObservableProperty]
    private string status = string.Empty;

    partial void OnIsOptimizableChanged(bool value)
    {
        if (!value)
        {
            IsSelected = false;
        }
    }

    partial void OnProcessIdChanged(int value)
    {
        OnPropertyChanged(nameof(ProcessDetailsTooltip));
    }

    partial void OnExecutablePathChanged(string value)
    {
        OnPropertyChanged(nameof(ProcessDetailsTooltip));
    }
}
