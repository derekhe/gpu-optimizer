using GpuOptimizer.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GpuOptimizer.Core.Services;

public interface IGpuPreferenceService
{
    Task<GpuPreferenceKind> GetPreferenceAsync(string executablePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, GpuPreferenceKind>> GetPreferencesAsync(IEnumerable<string> executablePaths, CancellationToken cancellationToken = default);
    Task<PreferenceOperationResult> ApplyPowerSavingAsync(IEnumerable<string> executablePaths, CancellationToken cancellationToken = default);
    Task<PreferenceOperationResult> RestoreAsync(IEnumerable<string> executablePaths, CancellationToken cancellationToken = default);
}

