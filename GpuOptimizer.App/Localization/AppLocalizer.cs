using Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace GpuOptimizer.App.Localization;

public sealed class AppLocalizer
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppTitle"] = "GPU Optimizer",
            ["InfoText"] = "How it works: GPU Optimizer reads Windows GPU performance counters, including GPU Process Memory dedicated usage, and uses DXGI to map adapter IDs to real GPU names and integrated/discrete types. When you select apps and apply optimization, it writes the current user's Windows graphics preference under HKCU\\Software\\Microsoft\\DirectX\\UserGpuPreferences with GpuPreference=1, which asks Windows to use the power-saving GPU after the target apps are restarted. The result is lower discrete GPU memory pressure for apps that can run on the integrated GPU.",
            ["RefreshButton"] = "Refresh",
            ["SelectAllButton"] = "Select all",
            ["OptimizeButton"] = "Optimize",
            ["RestoreButton"] = "Restore",
            ["LanguageLabel"] = "Language",
            ["ThemeLabel"] = "Theme: System",
            ["CheckUpdatesButton"] = "Check updates",
            ["OpenReleaseButton"] = "Open release",
            ["ProcessesSuffix"] = "processes",
            ["SelectedSuffix"] = "selected",
            ["OptimizableSuffix"] = "optimizable",
            ["SelectedSavingPrefix"] = "Selected saving",
            ["GpuMemoryLabel"] = "GPU Memory",
            ["SelectHeader"] = "Select",
            ["ProcessNameHeader"] = "Process Name",
            ["GpuHeader"] = "GPU",
            ["MemoryHeader"] = "Memory",
            ["PreferenceHeader"] = "Preference",
            ["PowerSavingTarget"] = "Power-saving target",
            ["PotentialSavingPool"] = "Potential saving pool",
            ["GpuMemoryKind"] = "GPU memory",
            ["ReadyStatus"] = "Ready",
            ["RefreshingStatus"] = "Refreshing GPU usage...",
            ["RefreshFailedPrefix"] = "Refresh failed",
            ["RefreshedStatusFormat"] = "Refreshed at {0:HH:mm:ss} ({1} processes)",
            ["NoSelectionStatus"] = "No selected, optimizable process found.",
            ["ApplyingStatus"] = "Applying power-saving preference...",
            ["OptimizationAppliedStatus"] = "Optimization applied. Please restart target applications.",
            ["OptimizationHadErrorsStatus"] = "Optimization had errors. Check row status messages.",
            ["OptimizationFailedPrefix"] = "Optimization failed",
            ["RestoringStatus"] = "Restoring original preferences...",
            ["RestoreCompletedStatus"] = "Restore completed.",
            ["RestoreHadErrorsStatus"] = "Restore had errors. Check row status messages.",
            ["RestoreFailedPrefix"] = "Restore failed",
            ["CheckingUpdatesStatus"] = "Checking for updates...",
            ["UpdateAvailableFormat"] = "Update available: {0} (current {1})",
            ["NoUpdateAvailableFormat"] = "You're up to date ({0})",
            ["UpdateCheckFailedPrefix"] = "Update check failed",
            ["PreferenceUnknown"] = "Unknown",
            ["PreferenceSystemDefault"] = "SystemDefault",
            ["PreferencePowerSaving"] = "PowerSaving",
            ["PreferenceHighPerformance"] = "HighPerformance",
        },
        ["zh"] = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppTitle"] = "GPU 优化器",
            ["InfoText"] = "工作原理：GPU 优化器通过 Windows GPU 性能计数器读取当前进程的 GPU 显存占用，包括 GPU Process Memory 的 Dedicated Usage，并使用 DXGI 将适配器 ID 映射为真实显卡名称和集显/独显类型。选择应用并执行优化后，工具会写入当前用户的 Windows 图形首选项 HKCU\\Software\\Microsoft\\DirectX\\UserGpuPreferences，设置 GpuPreference=1，让 Windows 在目标应用重启后优先使用节能 GPU，从而降低可运行在集显上的应用对独显显存的占用。",
            ["RefreshButton"] = "刷新",
            ["SelectAllButton"] = "全选",
            ["OptimizeButton"] = "优化",
            ["RestoreButton"] = "恢复",
            ["LanguageLabel"] = "语言",
            ["ThemeLabel"] = "主题：跟随系统",
            ["CheckUpdatesButton"] = "检查更新",
            ["OpenReleaseButton"] = "打开发布页",
            ["ProcessesSuffix"] = "个进程",
            ["SelectedSuffix"] = "已选择",
            ["OptimizableSuffix"] = "可优化",
            ["SelectedSavingPrefix"] = "选中可节省",
            ["GpuMemoryLabel"] = "GPU 显存",
            ["SelectHeader"] = "选择",
            ["ProcessNameHeader"] = "进程名",
            ["GpuHeader"] = "显卡",
            ["MemoryHeader"] = "显存",
            ["PreferenceHeader"] = "首选项",
            ["PowerSavingTarget"] = "节能目标",
            ["PotentialSavingPool"] = "可节省显存池",
            ["GpuMemoryKind"] = "GPU 显存",
            ["ReadyStatus"] = "就绪",
            ["RefreshingStatus"] = "正在刷新 GPU 占用...",
            ["RefreshFailedPrefix"] = "刷新失败",
            ["RefreshedStatusFormat"] = "{0:HH:mm:ss} 已刷新（{1} 个进程）",
            ["NoSelectionStatus"] = "没有选中的可优化进程。",
            ["ApplyingStatus"] = "正在应用节能 GPU 首选项...",
            ["OptimizationAppliedStatus"] = "优化已应用。请重启目标应用后生效。",
            ["OptimizationHadErrorsStatus"] = "部分优化失败。请检查行状态信息。",
            ["OptimizationFailedPrefix"] = "优化失败",
            ["RestoringStatus"] = "正在恢复原始首选项...",
            ["RestoreCompletedStatus"] = "恢复完成。",
            ["RestoreHadErrorsStatus"] = "部分恢复失败。请检查行状态信息。",
            ["RestoreFailedPrefix"] = "恢复失败",
            ["CheckingUpdatesStatus"] = "正在检查更新...",
            ["UpdateAvailableFormat"] = "发现新版本：{0}（当前 {1}）",
            ["NoUpdateAvailableFormat"] = "已是最新版本（{0}）",
            ["UpdateCheckFailedPrefix"] = "检查更新失败",
            ["PreferenceUnknown"] = "未知",
            ["PreferenceSystemDefault"] = "系统默认",
            ["PreferencePowerSaving"] = "节能",
            ["PreferenceHighPerformance"] = "高性能",
        }
    };

    public static AppLocalizer Current { get; } = new();

    public event EventHandler? LanguageChanged;

    public string CurrentLanguageCode { get; private set; } = DetectSystemLanguageCode();

    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("en", "English"),
        new("zh", "简体中文"),
    ];

    public void Initialize()
    {
        ApplyToApplicationResources();
    }

    public void SetLanguage(string languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        if (CurrentLanguageCode.Equals(normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentLanguageCode = normalized;
        ApplyToApplicationResources();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key)
    {
        return _resources[CurrentLanguageCode].TryGetValue(key, out var value) ||
            _resources["en"].TryGetValue(key, out value)
            ? value
            : key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), args);
    }

    private void ApplyToApplicationResources()
    {
        if (Application.Current is null)
        {
            return;
        }

        foreach (var item in _resources[CurrentLanguageCode])
        {
            Application.Current.Resources[item.Key] = item.Value;
        }
    }

    private static string DetectSystemLanguageCode()
    {
        return NormalizeLanguageCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        return languageCode.Equals("zh", StringComparison.OrdinalIgnoreCase) ? "zh" : "en";
    }
}
