using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GpuOptimizer.App.ViewModels;
using System;
using System.Collections.Specialized;

namespace GpuOptimizer.App.Views;

public partial class MainWindow : Window
{
    private bool _autoSizeQueued;
    private INotifyCollectionChanged? _observedProcesses;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AttachProcessesObserver(DataContext as MainWindowViewModel);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        AttachProcessesObserver(DataContext as MainWindowViewModel);
    }

    private void AttachProcessesObserver(MainWindowViewModel? viewModel)
    {
        if (_observedProcesses is not null)
        {
            _observedProcesses.CollectionChanged -= OnProcessesCollectionChanged;
        }

        _observedProcesses = viewModel?.Processes;

        if (_observedProcesses is not null)
        {
            _observedProcesses.CollectionChanged += OnProcessesCollectionChanged;
            ScheduleAutoSizeColumns();
        }
    }

    private void OnProcessesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScheduleAutoSizeColumns();
    }

    private void ProcessGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control source &&
            source.FindAncestorOfType<DataGridColumnHeader>() is not null)
        {
            AutoSizeColumns();
            e.Handled = true;
        }
    }

    private void ScheduleAutoSizeColumns()
    {
        if (_autoSizeQueued)
        {
            return;
        }

        _autoSizeQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _autoSizeQueued = false;
            AutoSizeColumns();
        }, DispatcherPriority.Background);
    }

    private void AutoSizeColumns()
    {
        foreach (var column in ProcessGrid.Columns)
        {
            column.Width = DataGridLength.Auto;
        }

        ProcessGrid.InvalidateMeasure();
        ProcessGrid.InvalidateArrange();
    }
}
