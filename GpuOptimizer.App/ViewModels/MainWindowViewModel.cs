using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GpuOptimizer.App.Localization;
using GpuOptimizer.Core.Models;
using GpuOptimizer.Core.Scanning;
using GpuOptimizer.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GpuOptimizer.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGpuScannerService _gpuScannerService;
    private readonly IGpuPreferenceService _gpuPreferenceService;

    public ObservableCollection<GpuProcessRowViewModel> Processes { get; } = [];
    public ObservableCollection<GpuMemorySummaryViewModel> GpuMemorySummaries { get; } = [];
    public IReadOnlyList<LanguageOption> Languages { get; } = AppLocalizer.Current.Languages;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = AppLocalizer.Current.Get("ReadyStatus");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTotalProcesses))]
    private int totalProcessCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplaySelectedProcesses))]
    private int selectedProcessCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayOptimizableProcesses))]
    private int optimizableProcessCount;

    [ObservableProperty]
    private double totalDedicatedMemoryMb;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplaySelectedSaving))]
    private double selectedDiscreteMemoryMb;

    [ObservableProperty]
    private LanguageOption selectedLanguage = AppLocalizer.Current.Languages
        .First(language => language.Code == AppLocalizer.Current.CurrentLanguageCode);

    public string DisplayTotalProcesses => $"{TotalProcessCount} {AppLocalizer.Current.Get("ProcessesSuffix")}";
    public string DisplaySelectedProcesses => $"{SelectedProcessCount} {AppLocalizer.Current.Get("SelectedSuffix")}";
    public string DisplayOptimizableProcesses => $"{OptimizableProcessCount} {AppLocalizer.Current.Get("OptimizableSuffix")}";
    public string DisplaySelectedSaving => $"{AppLocalizer.Current.Get("SelectedSavingPrefix")}: {SelectedDiscreteMemoryMb:F1} MB";

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand OptimizeSelectedCommand { get; }
    public IAsyncRelayCommand RestoreSelectedCommand { get; }
    public IRelayCommand SelectAllOptimizableCommand { get; }

    public MainWindowViewModel(IGpuScannerService gpuScannerService, IGpuPreferenceService gpuPreferenceService)
    {
        _gpuScannerService = gpuScannerService;
        _gpuPreferenceService = gpuPreferenceService;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        OptimizeSelectedCommand = new AsyncRelayCommand(OptimizeSelectedAsync, () => !IsBusy);
        RestoreSelectedCommand = new AsyncRelayCommand(RestoreSelectedAsync, () => !IsBusy);
        SelectAllOptimizableCommand = new RelayCommand(SelectAllOptimizable);

        AppLocalizer.Current.LanguageChanged += OnLanguageChanged;
        _ = RefreshAsync();
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        AppLocalizer.Current.SetLanguage(value.Code);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        StatusMessage = AppLocalizer.Current.Get("ReadyStatus");
        OnPropertyChanged(nameof(DisplayTotalProcesses));
        OnPropertyChanged(nameof(DisplaySelectedProcesses));
        OnPropertyChanged(nameof(DisplayOptimizableProcesses));
        OnPropertyChanged(nameof(DisplaySelectedSaving));

        foreach (var process in Processes)
        {
            process.RefreshLocalizedText();
        }

        RebuildGpuMemorySummaries();
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        SetBusyState(true);
        StatusMessage = AppLocalizer.Current.Get("RefreshingStatus");

        try
        {
            await ReloadProcessesAsync(cancellationToken);
            StatusMessage = AppLocalizer.Current.Format("RefreshedStatusFormat", DateTime.Now, Processes.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = $"{AppLocalizer.Current.Get("RefreshFailedPrefix")}: {ex.Message}";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async Task ReloadProcessesAsync(CancellationToken cancellationToken)
    {
        var processes = (await _gpuScannerService.ScanAsync(cancellationToken))
            .Where(ShouldDisplayProcess)
            .ToList();
        var processPaths = processes
            .Where(process => process.IsOptimizable)
            .Select(process => process.ExecutablePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var preferenceByPath = await _gpuPreferenceService.GetPreferencesAsync(processPaths, cancellationToken);

        foreach (var process in Processes)
        {
            process.PropertyChanged -= OnRowPropertyChanged;
        }

        Processes.Clear();

        foreach (var process in processes)
        {
            var row = new GpuProcessRowViewModel
            {
                ProcessId = process.ProcessId,
                ProcessName = process.ProcessName,
                ExecutablePath = process.ExecutablePath,
                GpuAdapters = process.GpuAdapters,
                GpuEngines = process.GpuEngines,
                GpuUtilizationPercent = process.GpuUtilizationPercent,
                DedicatedMemoryMb = process.DedicatedMemoryMb,
                AdapterMemoryUsage = process.AdapterMemoryUsage,
                SharedMemoryMb = process.SharedMemoryMb,
                CurrentPreferenceKind = process.IsOptimizable && preferenceByPath.TryGetValue(process.ExecutablePath, out var preference)
                    ? preference
                    : GpuPreferenceKind.Unknown,
                IsOptimizable = process.IsOptimizable,
                Status = process.Status
            };

            row.PropertyChanged += OnRowPropertyChanged;
            Processes.Add(row);
        }

        UpdateSummary();
    }

    private static bool ShouldDisplayProcess(GpuProcessInfo process)
    {
        return process.IsAccessibleProcessPath &&
            !process.GpuAdapters.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(GpuProcessRowViewModel.IsSelected))
        {
            return;
        }

        UpdateSummary();
        OptimizeSelectedCommand.NotifyCanExecuteChanged();
        RestoreSelectedCommand.NotifyCanExecuteChanged();
    }

    private void SetBusyState(bool value)
    {
        IsBusy = value;
        RefreshCommand.NotifyCanExecuteChanged();
        OptimizeSelectedCommand.NotifyCanExecuteChanged();
        RestoreSelectedCommand.NotifyCanExecuteChanged();
    }

    private void SelectAllOptimizable()
    {
        foreach (var process in Processes)
        {
            process.IsSelected = process.IsOptimizable;
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        TotalProcessCount = Processes.Count;
        SelectedProcessCount = Processes.Count(process => process.IsSelected);
        OptimizableProcessCount = Processes.Count(process => process.IsOptimizable);
        TotalDedicatedMemoryMb = Processes.Sum(process => process.DedicatedMemoryMb);
        SelectedDiscreteMemoryMb = Processes
            .Where(process => process.IsSelected)
            .SelectMany(process => process.AdapterMemoryUsage)
            .Where(memory => GpuDisplayStyle.IsDiscreteGpu(memory.AdapterName))
            .Sum(memory => memory.DedicatedMemoryMb);
        RebuildGpuMemorySummaries();
    }

    private void RebuildGpuMemorySummaries()
    {
        var summaries = Processes
            .SelectMany(process => process.AdapterMemoryUsage)
            .GroupBy(memory => memory.AdapterName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GpuMemorySummaryViewModel
            {
                GpuName = group.Key,
                DedicatedMemoryMb = group.Sum(memory => memory.DedicatedMemoryMb),
                KindLabel = GpuDisplayStyle.GetKindLabel(group.Key),
                AccentBrush = GpuDisplayStyle.GetAccentBrush(group.Key),
                BackgroundBrush = GpuDisplayStyle.GetBackgroundBrush(group.Key),
                BorderBrush = GpuDisplayStyle.GetBorderBrush(group.Key)
            })
            .OrderByDescending(summary => summary.DedicatedMemoryMb)
            .ThenBy(summary => summary.GpuName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        GpuMemorySummaries.Clear();
        foreach (var summary in summaries)
        {
            GpuMemorySummaries.Add(summary);
        }
    }

    private async Task OptimizeSelectedAsync(CancellationToken cancellationToken = default)
    {
        var selectedPaths = Processes
            .Where(process => process.IsSelected && process.IsOptimizable)
            .Select(process => process.ExecutablePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            StatusMessage = AppLocalizer.Current.Get("NoSelectionStatus");
            return;
        }

        SetBusyState(true);
        StatusMessage = AppLocalizer.Current.Get("ApplyingStatus");

        try
        {
            var result = await _gpuPreferenceService.ApplyPowerSavingAsync(selectedPaths, cancellationToken);
            await ReloadProcessesAsync(cancellationToken);
            ApplyOperationMessages(result.Items);
            StatusMessage = result.AllSucceeded
                ? AppLocalizer.Current.Get("OptimizationAppliedStatus")
                : AppLocalizer.Current.Get("OptimizationHadErrorsStatus");
        }
        catch (Exception ex)
        {
            StatusMessage = $"{AppLocalizer.Current.Get("OptimizationFailedPrefix")}: {ex.Message}";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async Task RestoreSelectedAsync(CancellationToken cancellationToken = default)
    {
        var selectedPaths = Processes
            .Where(process => process.IsSelected && process.IsOptimizable)
            .Select(process => process.ExecutablePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            StatusMessage = AppLocalizer.Current.Get("NoSelectionStatus");
            return;
        }

        SetBusyState(true);
        StatusMessage = AppLocalizer.Current.Get("RestoringStatus");

        try
        {
            var result = await _gpuPreferenceService.RestoreAsync(selectedPaths, cancellationToken);
            await ReloadProcessesAsync(cancellationToken);
            ApplyOperationMessages(result.Items);
            StatusMessage = result.AllSucceeded
                ? AppLocalizer.Current.Get("RestoreCompletedStatus")
                : AppLocalizer.Current.Get("RestoreHadErrorsStatus");
        }
        catch (Exception ex)
        {
            StatusMessage = $"{AppLocalizer.Current.Get("RestoreFailedPrefix")}: {ex.Message}";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void ApplyOperationMessages(IEnumerable<PreferenceOperationItem> items)
    {
        foreach (var item in items)
        {
            var matchedRows = Processes.Where(p =>
                p.ExecutablePath.Equals(item.ExecutablePath, StringComparison.OrdinalIgnoreCase));

            foreach (var matchedRow in matchedRows)
            {
                matchedRow.Status = item.Message;
            }
        }
    }
}
