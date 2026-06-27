using GpuOptimizer.Core.Services;
using GpuOptimizer.Core.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace GpuOptimizer.Core.Tests;

public class RegistryGpuPreferenceServiceTests
{
    [Fact]
    public async Task ApplyPowerSaving_WritesGpuPreference_WhenValueExists()
    {
        var context = CreateTestContext();
        try
        {
            var exePath = @"C:\Program Files\GpuOptimizer\App.exe";
            SetRawPreference(context.SubKey, exePath, "foo=bar;GpuPreference=2;");

            var service = CreateService(context);
            var result = await service.ApplyPowerSavingAsync(new[] { exePath });

            Assert.True(result.AllSucceeded);
            Assert.Single(result.Items);
            Assert.Equal(exePath, result.Items.First().ExecutablePath);
            Assert.Contains("Set to PowerSaving", result.Items.First().Message);

            var written = GetRawPreference(context.SubKey, exePath);
            Assert.Equal("foo=bar;GpuPreference=1;", written);
            var backup = ReadBackup(context.BackupFilePath);
            Assert.True(backup.OriginalValues.ContainsKey(exePath));
            Assert.Equal("foo=bar;GpuPreference=2;", backup.OriginalValues[exePath]);
        }
        finally
        {
            Cleanup(context);
        }
    }

    [Fact]
    public async Task Restore_DeletesValue_WhenOriginalWasMissing()
    {
        var context = CreateTestContext();
        try
        {
            var exePath = @"C:\Program Files\GpuOptimizer\NotExistingValue.exe";
            var service = CreateService(context);

            await service.ApplyPowerSavingAsync(new[] { exePath });
            Assert.Equal("GpuPreference=1;", GetRawPreference(context.SubKey, exePath));
            var backupBeforeRestore = ReadBackup(context.BackupFilePath);
            Assert.True(backupBeforeRestore.OriginalValues.ContainsKey(exePath));
            Assert.Null(backupBeforeRestore.OriginalValues[exePath]);

            var restore = await service.RestoreAsync(new[] { exePath });
            Assert.True(restore.AllSucceeded);
            Assert.Single(restore.Items);
            Assert.True(restore.Items[0].Success);
            Assert.Equal("Reset to SystemDefault", restore.Items[0].Message);
            Assert.Null(GetRawPreference(context.SubKey, exePath));
            var backupAfterRestore = ReadBackup(context.BackupFilePath);
            Assert.False(backupAfterRestore.OriginalValues.ContainsKey(exePath));
        }
        finally
        {
            Cleanup(context);
        }
    }

    [Fact]
    public async Task Restore_ReturnsOriginalValue_WhenPreviouslySaved()
    {
        var context = CreateTestContext();
        try
        {
            var exePath = @"C:\Program Files\GpuOptimizer\ExistingValue.exe";
            var original = "GpuPreference=2;foo=1;";
            var changed = "GpuPreference=1;foo=changed;";

            SetRawPreference(context.SubKey, exePath, original);
            var service = CreateService(context);

            await service.ApplyPowerSavingAsync(new[] { exePath });
            SetRawPreference(context.SubKey, exePath, changed);
            await service.ApplyPowerSavingAsync(new[] { exePath });

            var restore = await service.RestoreAsync(new[] { exePath });
            var restored = GetRawPreference(context.SubKey, exePath);

            Assert.True(restore.AllSucceeded);
            Assert.Single(restore.Items);
            Assert.True(restore.Items[0].Success);
            Assert.Equal(original, restored);
        }
        finally
        {
            Cleanup(context);
        }
    }

    [Fact]
    public async Task ApplyOrRestore_HandlesDuplicatePathsCaseInsensitiveOnce()
    {
        var context = CreateTestContext();
        try
        {
            var exePathLower = @"C:\Program Files\GpuOptimizer\Case.exe";
            var exePathUpper = @"c:\program files\gpuoptimizer\case.exe";
            var original = "GpuPreference=2;";
            SetRawPreference(context.SubKey, exePathLower, original);

            var service = CreateService(context);
            var apply = await service.ApplyPowerSavingAsync(new[] { exePathLower, exePathUpper });
            var backupAfterApply = ReadBackup(context.BackupFilePath);
            var restore = await service.RestoreAsync(new[] { exePathLower, exePathUpper });

            Assert.True(apply.AllSucceeded);
            Assert.True(restore.AllSucceeded);
            Assert.Single(apply.Items);
            Assert.Single(restore.Items);
            Assert.Single(backupAfterApply.OriginalValues);
            Assert.True(backupAfterApply.OriginalValues.ContainsKey(exePathLower));
            Assert.Equal(original, GetRawPreference(context.SubKey, exePathLower));
        }
        finally
        {
            Cleanup(context);
        }
    }

    [Fact]
    public async Task RestoreWithoutBackup_DeletesCurrentValue()
    {
        var context = CreateTestContext();
        try
        {
            var exePath = @"C:\Program Files\GpuOptimizer\NoBackup.exe";
            SetRawPreference(context.SubKey, exePath, "GpuPreference=1;");
            var service = CreateService(context);

            var restore = await service.RestoreAsync(new[] { exePath });

            Assert.True(restore.AllSucceeded);
            var item = Assert.Single(restore.Items);
            Assert.True(item.Success);
            Assert.Equal("Reset to SystemDefault", item.Message);
            Assert.Null(GetRawPreference(context.SubKey, exePath));
        }
        finally
        {
            Cleanup(context);
        }
    }

    [Fact]
    public async Task RestoreWithoutBackup_ReturnsSuccess_WhenAlreadySystemDefault()
    {
        var context = CreateTestContext();
        try
        {
            var exePath = @"C:\Program Files\GpuOptimizer\NoBackupDefault.exe";
            var service = CreateService(context);

            var restore = await service.RestoreAsync(new[] { exePath });

            Assert.True(restore.AllSucceeded);
            var item = Assert.Single(restore.Items);
            Assert.True(item.Success);
            Assert.Equal("Already SystemDefault", item.Message);
        }
        finally
        {
            Cleanup(context);
        }
    }

    private static string? GetRawPreference(string subKey, string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKey, writable: false);
        return key?.GetValue(exePath)?.ToString();
    }

    private static PreferenceBackupStore ReadBackup(string backupPath)
    {
        using var stream = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var backup = JsonSerializer.Deserialize<PreferenceBackupStore>(stream);
        return backup ?? new PreferenceBackupStore();
    }

    private static void SetRawPreference(string subKey, string exePath, string? value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(subKey, writable: true);
        if (value is null)
        {
            key?.DeleteValue(exePath, throwOnMissingValue: false);
            return;
        }

        key?.SetValue(exePath, value, RegistryValueKind.String);
    }

    private static RegistryPreferenceTestContext CreateTestContext()
    {
        var scope = Guid.NewGuid().ToString("N");
        return new RegistryPreferenceTestContext(
            $@"Software\GpuOptimizer.Tests\{scope}",
            Path.Combine(Path.GetTempPath(), $"GpuOptimizer.Tests-{scope}.json"));
    }

    private static RegistryGpuPreferenceService CreateService(RegistryPreferenceTestContext context) =>
        new RegistryGpuPreferenceService(context.SubKey, context.BackupFilePath);

    private static void Cleanup(RegistryPreferenceTestContext context)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(context.SubKey, throwOnMissingSubKey: false);
        }
        catch
        {
            // best effort cleanup
        }

        try
        {
            if (File.Exists(context.BackupFilePath))
            {
                File.Delete(context.BackupFilePath);
            }
        }
        catch
        {
            // best effort cleanup
        }
    }

    private sealed record RegistryPreferenceTestContext(string SubKey, string BackupFilePath);
}
