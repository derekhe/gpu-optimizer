using GpuOptimizer.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GpuOptimizer.Core.Scanning;

public sealed class WindowsPerformanceCounterGpuScanner : IGpuScannerService
{
    private const string EngineCategory = "GPU Engine";
    private const string MemoryCategory = "GPU Process Memory";
    private const string EngineUtilizationCounter = "Utilization Percentage";
    private const string DedicatedUsageCounter = "Dedicated Usage";
    private const string SharedUsageCounter = "Shared Usage";
    private const string UnknownAdapterLabel = "Other GPU";
    public Task<IReadOnlyList<GpuProcessInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Scan(), cancellationToken);
    }

    private IReadOnlyList<GpuProcessInfo> Scan()
    {
        var stats = new Dictionary<int, GpuProcessStats>(capacity: 64);
        var adapterLabels = BuildAdapterLabelMap();

        TryCollectEngineData(stats, adapterLabels);
        TryCollectMemoryData(stats, adapterLabels);

        var results = new List<GpuProcessInfo>();
        foreach (var kvp in stats)
        {
            if (kvp.Key <= 0)
            {
                continue;
            }

            var processInfo = ResolveProcessInfo(kvp.Key, kvp.Value);
            results.Add(processInfo);
        }

        return results.OrderBy(process => process.ProcessName)
                      .ThenBy(process => process.ProcessId)
                      .ToList();
    }

    private void TryCollectEngineData(Dictionary<int, GpuProcessStats> stats, IReadOnlyDictionary<string, string> adapterLabels)
    {
        if (!PerformanceCounterCategoryExists(EngineCategory))
        {
            return;
        }

        var instanceNames = GetCounterInstances(EngineCategory);
        var counters = new List<EngineCounterSample>();
        foreach (var instance in instanceNames)
        {
            if (!GpuCounterInstanceParser.TryGetProcessIdFromInstance(instance, out var pid))
            {
                continue;
            }

            try
            {
                var adapterLabel = ResolveAdapterLabel(instance, adapterLabels);
                var counter = new PerformanceCounter(EngineCategory, EngineUtilizationCounter, instance, true);
                counters.Add(new EngineCounterSample(
                    pid,
                    adapterLabel,
                    GpuCounterInstanceParser.ParseEngineName(instance),
                    counter,
                    counter.NextSample()));
            }
            catch
            {
                // Counter instances can disappear while processes exit.
            }
        }

        if (counters.Count == 0)
        {
            return;
        }

        Thread.Sleep(TimeSpan.FromSeconds(1));

        foreach (var sample in counters)
        {
            using var counter = sample.Counter;
            var utilization = ReadUtilizationValue(counter, sample.InitialSample);
            if (utilization is null || utilization.Value <= 0)
            {
                continue;
            }

            var accumulator = GetOrCreate(stats, sample.ProcessId);
            accumulator.UtilizationPercent += utilization.Value;
            accumulator.GpuAdapters.Add(sample.AdapterLabel);
            accumulator.Engines.Add($"{sample.AdapterLabel} {sample.EngineName}");
        }
    }

    private static double? ReadUtilizationValue(PerformanceCounter counter, CounterSample initialSample)
    {
        try
        {
            var value = CounterSampleCalculator.ComputeCounterValue(initialSample, counter.NextSample());
            if (value < 0 || double.IsNaN(value) || double.IsInfinity(value))
            {
                return null;
            }

            return value;
        }
        catch
        {
            return null;
        }
    }

    private void TryCollectMemoryData(Dictionary<int, GpuProcessStats> stats, IReadOnlyDictionary<string, string> adapterLabels)
    {
        if (!PerformanceCounterCategoryExists(MemoryCategory))
        {
            return;
        }

        var instanceNames = GetCounterInstances(MemoryCategory);
        foreach (var instance in instanceNames)
        {
            if (!GpuCounterInstanceParser.TryGetProcessIdFromInstance(instance, out var pid))
            {
                continue;
            }

            var dedicated = ReadCounterValue(MemoryCategory, DedicatedUsageCounter, instance);
            var shared = ReadCounterValue(MemoryCategory, SharedUsageCounter, instance);
            if (dedicated is null && shared is null)
            {
                continue;
            }

            var accumulator = GetOrCreate(stats, pid);
            var adapterLabel = ResolveAdapterLabel(instance, adapterLabels);
            accumulator.GpuAdapters.Add(adapterLabel);
            if (dedicated is not null)
            {
                accumulator.DedicatedMemoryBytes += dedicated.Value;
                accumulator.AddDedicatedMemory(adapterLabel, dedicated.Value);
            }

            if (shared is not null)
            {
                accumulator.SharedMemoryBytes += shared.Value;
            }
        }
    }

    private static IReadOnlyDictionary<string, string> BuildAdapterLabelMap()
    {
        return DxgiGpuAdapterEnumerator.GetHardwareAdapterLabels();
    }

    private static string ResolveAdapterLabel(string instanceName, IReadOnlyDictionary<string, string> adapterLabels)
    {
        if (GpuCounterInstanceParser.TryGetAdapterKeyFromInstance(instanceName, out var adapterKey) &&
            adapterLabels.TryGetValue(adapterKey, out var label))
        {
            return label;
        }

        return UnknownAdapterLabel;
    }

    private static List<string> GetCounterInstances(string categoryName)
    {
        try
        {
            var category = new PerformanceCounterCategory(categoryName);
            return [.. category.GetInstanceNames()];
        }
        catch
        {
            return [];
        }
    }

    private static bool PerformanceCounterCategoryExists(string categoryName)
    {
        try
        {
            return PerformanceCounterCategory.Exists(categoryName);
        }
        catch
        {
            return false;
        }
    }

    private static double? ReadCounterValue(string categoryName, string counterName, string instanceName)
    {
        try
        {
            using var counter = new PerformanceCounter(categoryName, counterName, instanceName, true);
            var value = counter.NextValue();
            if (value < 0)
            {
                return null;
            }

            return value;
        }
        catch
        {
            return null;
        }
    }

    private static GpuProcessStats GetOrCreate(Dictionary<int, GpuProcessStats> stats, int pid)
    {
        if (!stats.TryGetValue(pid, out var accumulator))
        {
            accumulator = new GpuProcessStats();
            stats[pid] = accumulator;
        }

        return accumulator;
    }

    private GpuProcessInfo ResolveProcessInfo(int processId, GpuProcessStats stats)
    {
        var (processName, executablePath, status) = ResolveProcessMetadata(processId);
        var isOptimizable = !string.IsNullOrWhiteSpace(executablePath);
        var displayAdapters = stats.GpuAdapters
            .Where(IsDisplayAdapter)
            .OrderBy(value => value)
            .ToList();

        return new GpuProcessInfo
        {
            ProcessId = processId,
            ProcessName = processName,
            ExecutablePath = executablePath,
            GpuAdapters = displayAdapters.Count > 0 ? string.Join(", ", displayAdapters) : "Unknown",
            GpuEngines = stats.Engines.Count > 0 ? string.Join(", ", stats.Engines.OrderBy(value => value)) : "Unknown",
            GpuUtilizationPercent = stats.UtilizationPercent,
            DedicatedMemoryMb = stats.DedicatedMemoryBytes / 1024d / 1024d,
            AdapterMemoryUsage = stats.DedicatedMemoryBytesByAdapter
                .Where(item => IsDisplayAdapter(item.Key))
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => new GpuAdapterMemoryUsage(item.Key, BytesToMegabytes(item.Value)))
                .ToList(),
            SharedMemoryMb = stats.SharedMemoryBytes / 1024d / 1024d,
            IsOptimizable = isOptimizable,
            Status = status
        };
    }

    private static bool IsDisplayAdapter(string adapterLabel)
    {
        return !adapterLabel.Equals(UnknownAdapterLabel, StringComparison.OrdinalIgnoreCase);
    }

    private static double BytesToMegabytes(double bytes)
    {
        return bytes / 1024d / 1024d;
    }

    private static (string ProcessName, string ExecutablePath, string Status) ResolveProcessMetadata(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            var processName = process.ProcessName;
            string executablePath;
            try
            {
                executablePath = process.MainModule?.FileName ?? string.Empty;
            }
            catch (Exception)
            {
                executablePath = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                executablePath = NativeProcessPathResolver.ResolveExecutablePath(processId);
            }

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                executablePath = SystemExecutablePathResolver.ResolveExecutablePath(processName);
            }

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                executablePath = PackagedAppPathResolver.ResolveExecutablePath(processName);
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    return (processName, string.Empty, "No executable path access");
                }
            }

            return (processName, executablePath, string.Empty);
        }
        catch (ArgumentException)
        {
            return ("Unknown", string.Empty, "Process exited");
        }
        catch
        {
            return ("Unknown", string.Empty, "Process inaccessible");
        }
    }

    private sealed class GpuProcessStats
    {
        public double UtilizationPercent { get; set; }
        public double DedicatedMemoryBytes { get; set; }
        public double SharedMemoryBytes { get; set; }
        public Dictionary<string, double> DedicatedMemoryBytesByAdapter { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> GpuAdapters { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Engines { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void AddDedicatedMemory(string adapterLabel, double bytes)
        {
            DedicatedMemoryBytesByAdapter.TryGetValue(adapterLabel, out var current);
            DedicatedMemoryBytesByAdapter[adapterLabel] = current + bytes;
        }
    }

    private sealed record EngineCounterSample(
        int ProcessId,
        string AdapterLabel,
        string EngineName,
        PerformanceCounter Counter,
        CounterSample InitialSample);
}
