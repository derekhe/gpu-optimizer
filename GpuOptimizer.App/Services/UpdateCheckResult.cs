using System;

namespace GpuOptimizer.App.Services;

public sealed record UpdateCheckResult(
    Version CurrentVersion,
    Version LatestVersion,
    string LatestTag,
    string ReleaseUrl,
    bool IsUpdateAvailable);
