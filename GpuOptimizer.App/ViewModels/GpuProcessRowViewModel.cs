using CommunityToolkit.Mvvm.ComponentModel;
using GpuOptimizer.Core.Models;

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

    [ObservableProperty]
    private string gpuAdapters = string.Empty;

    [ObservableProperty]
    private string gpuEngines = string.Empty;

    [ObservableProperty]
    private double gpuUtilizationPercent;

    [ObservableProperty]
    private double dedicatedMemoryMb;

    [ObservableProperty]
    private double sharedMemoryMb;

    [ObservableProperty]
    private string currentPreference = GpuPreferenceKind.Unknown.ToString();

    [ObservableProperty]
    private bool isOptimizable;

    [ObservableProperty]
    private string status = string.Empty;
}
