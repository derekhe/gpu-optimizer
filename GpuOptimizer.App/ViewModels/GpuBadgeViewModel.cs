using CommunityToolkit.Mvvm.ComponentModel;

namespace GpuOptimizer.App.ViewModels;

public partial class GpuBadgeViewModel : ObservableObject
{
    [ObservableProperty]
    private string gpuName = string.Empty;

    [ObservableProperty]
    private string accentBrush = "#6B7280";

    [ObservableProperty]
    private string backgroundBrush = "#F9FAFB";

    [ObservableProperty]
    private string borderBrush = "#E5E7EB";
}
