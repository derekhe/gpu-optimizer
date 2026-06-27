using GpuOptimizer.Core.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GpuOptimizer.Core.Services;

public sealed class RegistryGpuPreferenceService : IGpuPreferenceService
{
    private const string SubKeyPath = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private const string BackupFileName = "backups.json";
    private readonly string _backupFilePath;
    private readonly string _userGpuPreferencesSubKey;

    public RegistryGpuPreferenceService(string? userGpuPreferencesSubKey = null, string? backupFilePath = null)
    {
        _userGpuPreferencesSubKey = userGpuPreferencesSubKey ?? SubKeyPath;
        _backupFilePath = backupFilePath ?? BuildDefaultBackupFilePath();
    }

    private static string BuildDefaultBackupFilePath()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GpuOptimizer");
        return Path.Combine(basePath, BackupFileName);
    }

    public Task<GpuPreferenceKind> GetPreferenceAsync(string executablePath, CancellationToken cancellationToken = default)
    {
        var value = ReadRawValue(executablePath);
        return Task.FromResult(GpuPreferenceValue.Parse(value));
    }

    public async Task<IReadOnlyDictionary<string, GpuPreferenceKind>> GetPreferencesAsync(
        IEnumerable<string> executablePaths,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, GpuPreferenceKind>(StringComparer.OrdinalIgnoreCase);
        foreach (var executablePath in executablePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                continue;
            }

            result[executablePath] = await GetPreferenceAsync(executablePath, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<PreferenceOperationResult> ApplyPowerSavingAsync(
        IEnumerable<string> executablePaths,
        CancellationToken cancellationToken = default)
    {
        return await ApplyPreferenceAsync(
            executablePaths,
            requestMessage: "Set to PowerSaving (restart target process)",
            writeAction: path => SetPreferenceValue(path, GpuPreferenceValue.EnsurePowerSaving(ReadRawValue(path))),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<PreferenceOperationResult> RestoreAsync(
        IEnumerable<string> executablePaths,
        CancellationToken cancellationToken = default)
    {
        var normalizedPaths = NormalizePaths(executablePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (normalizedPaths.Length == 0)
        {
            return new PreferenceOperationResult
            {
                AllSucceeded = true,
                Items = []
            };
        }

        var backup = await LoadBackupAsync(cancellationToken).ConfigureAwait(false);
        var items = new List<PreferenceOperationItem>();
        var changed = false;
        var allSucceeded = true;

        foreach (var path in normalizedPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!backup.OriginalValues.TryGetValue(path, out var original))
            {
                items.Add(new PreferenceOperationItem
                {
                    ExecutablePath = path,
                    Success = false,
                    Message = "No backup record for this app"
                });
                allSucceeded = false;
                continue;
            }

            try
            {
                if (original is null)
                {
                    DeleteValue(path);
                    items.Add(new PreferenceOperationItem
                    {
                        ExecutablePath = path,
                        Success = true,
                        Message = "Deleted preference value"
                    });
                }
                else
                {
                    SetValue(path, original);
                    items.Add(new PreferenceOperationItem
                    {
                        ExecutablePath = path,
                        Success = true,
                        Message = "Restored original preference"
                    });
                }

                backup.OriginalValues.Remove(path);
                changed = true;
            }
            catch (Exception ex)
            {
                items.Add(new PreferenceOperationItem
                {
                    ExecutablePath = path,
                    Success = false,
                    Message = ex.Message
                });
                allSucceeded = false;
            }
        }

        if (changed)
        {
            backup.LastUpdatedUtc = DateTime.UtcNow;
            await SaveBackupAsync(backup, cancellationToken).ConfigureAwait(false);
        }

        return new PreferenceOperationResult
        {
            AllSucceeded = allSucceeded,
            Items = items
        };
    }

    private async Task<PreferenceOperationResult> ApplyPreferenceAsync(
        IEnumerable<string> executablePaths,
        string requestMessage,
        Action<string> writeAction,
        CancellationToken cancellationToken)
    {
        var normalizedPaths = NormalizePaths(executablePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (normalizedPaths.Length == 0)
        {
            return new PreferenceOperationResult
            {
                AllSucceeded = true,
                Items = []
            };
        }

        var backup = await LoadBackupAsync(cancellationToken).ConfigureAwait(false);
        foreach (var path in normalizedPaths)
        {
            if (!backup.OriginalValues.ContainsKey(path))
            {
                backup.OriginalValues[path] = ReadRawValue(path);
            }
        }

        var items = new List<PreferenceOperationItem>();
        var allSucceeded = true;

        foreach (var path in normalizedPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                writeAction(path);
                items.Add(new PreferenceOperationItem
                {
                    ExecutablePath = path,
                    Success = true,
                    Message = requestMessage
                });
            }
            catch (Exception ex)
            {
                items.Add(new PreferenceOperationItem
                {
                    ExecutablePath = path,
                    Success = false,
                    Message = ex.Message
                });
                allSucceeded = false;
            }
        }

        backup.LastUpdatedUtc = DateTime.UtcNow;
        await SaveBackupAsync(backup, cancellationToken).ConfigureAwait(false);

        return new PreferenceOperationResult
        {
            AllSucceeded = allSucceeded,
            Items = items
        };
    }

    private string? ReadRawValue(string executablePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_userGpuPreferencesSubKey, writable: false);
            if (key is null)
            {
                return null;
            }

            return key.GetValue(executablePath) as string;
        }
        catch
        {
            return null;
        }
    }

    private void DeleteValue(string executablePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_userGpuPreferencesSubKey, writable: true);
        key?.DeleteValue(executablePath, throwOnMissingValue: false);
    }

    private void SetValue(string executablePath, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_userGpuPreferencesSubKey, writable: true);
        key?.SetValue(executablePath, value, RegistryValueKind.String);
    }

    private void SetPreferenceValue(string executablePath, string value)
    {
        SetValue(executablePath, value);
    }

    private static IEnumerable<string> NormalizePaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            yield return path.Trim();
        }
    }

    private async Task<PreferenceBackupStore> LoadBackupAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_backupFilePath))
        {
            return new PreferenceBackupStore
            {
                LastUpdatedUtc = DateTime.UtcNow,
                OriginalValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            };
        }

        await using var stream = new FileStream(_backupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var model = await JsonSerializer.DeserializeAsync<PreferenceBackupStore>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (model is null)
        {
            return new PreferenceBackupStore
            {
                LastUpdatedUtc = DateTime.UtcNow,
                OriginalValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            };
        }

        return new PreferenceBackupStore
        {
            LastUpdatedUtc = model.LastUpdatedUtc,
            OriginalValues = new Dictionary<string, string?>(model.OriginalValues ?? new Dictionary<string, string?>(), StringComparer.OrdinalIgnoreCase)
        };
    }

    private async Task SaveBackupAsync(PreferenceBackupStore backup, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_backupFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        await using var stream = new FileStream(_backupFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, backup, options, cancellationToken).ConfigureAwait(false);
    }
}
