using CommunityToolkit.Mvvm.ComponentModel;

namespace GpuOptimizer.App.ViewModels;

public partial class GpuMemorySummaryViewModel : ObservableObject
{
    [ObservableProperty]
    private string gpuName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayMemory))]
    private double dedicatedMemoryMb;

    public string DisplayMemory => $"{DedicatedMemoryMb:F1} MB";
}
