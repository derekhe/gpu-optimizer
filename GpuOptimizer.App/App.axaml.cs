using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using GpuOptimizer.Core.Scanning;
using GpuOptimizer.Core.Services;
using GpuOptimizer.App.Localization;
using GpuOptimizer.App.ViewModels;
using GpuOptimizer.App.Views;

namespace GpuOptimizer.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        AppLocalizer.Current.Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    new WindowsPerformanceCounterGpuScanner(),
                    new RegistryGpuPreferenceService()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
