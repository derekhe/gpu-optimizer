using System.Collections.Generic;
using System;
using System.Text.Json.Serialization;

namespace GpuOptimizer.Core.Services;

public sealed class PreferenceOperationItem
{
    public string ExecutablePath { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class PreferenceOperationResult
{
    public bool AllSucceeded { get; init; }
    public IReadOnlyList<PreferenceOperationItem> Items { get; init; } = [];
}

public sealed class PreferenceBackupStore
{
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    [JsonInclude]
    public Dictionary<string, string?> OriginalValues { get; set; } = new();
}
