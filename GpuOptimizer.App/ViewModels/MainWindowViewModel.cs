using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private int totalProcessCount;

    [ObservableProperty]
    private int selectedProcessCount;

    [ObservableProperty]
    private int optimizableProcessCount;

    [ObservableProperty]
    private double totalDedicatedMemoryMb;

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

        _ = RefreshAsync();
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        SetBusyState(true);
        StatusMessage = "Refreshing GPU usage...";

        try
        {
            await ReloadProcessesAsync(cancellationToken);
            StatusMessage = $"Refreshed at {DateTime.Now:HH:mm:ss} ({Processes.Count} processes)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Refresh failed: {ex.Message}";
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
                CurrentPreference = process.IsOptimizable && preferenceByPath.TryGetValue(process.ExecutablePath, out var preference)
                    ? preference.ToString()
                    : GpuPreferenceKind.Unknown.ToString(),
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
                DedicatedMemoryMb = group.Sum(memory => memory.DedicatedMemoryMb)
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
            StatusMessage = "No selected, optimizable process found.";
            return;
        }

        SetBusyState(true);
        StatusMessage = "Applying power-saving preference...";

        try
        {
            var result = await _gpuPreferenceService.ApplyPowerSavingAsync(selectedPaths, cancellationToken);
            await ReloadProcessesAsync(cancellationToken);
            ApplyOperationMessages(result.Items);
            StatusMessage = result.AllSucceeded
                ? "Optimization applied. Please restart target applications."
                : "Optimization had errors. Check row status messages.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Optimization failed: {ex.Message}";
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
            StatusMessage = "No selected, optimizable process found.";
            return;
        }

        SetBusyState(true);
        StatusMessage = "Restoring original preferences...";

        try
        {
            var result = await _gpuPreferenceService.RestoreAsync(selectedPaths, cancellationToken);
            await ReloadProcessesAsync(cancellationToken);
            ApplyOperationMessages(result.Items);
            StatusMessage = result.AllSucceeded
                ? "Restore completed."
                : "Restore had errors. Check row status messages.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
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
