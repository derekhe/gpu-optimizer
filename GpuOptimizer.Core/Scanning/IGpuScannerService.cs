using GpuOptimizer.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GpuOptimizer.Core.Scanning;

public interface IGpuScannerService
{
    Task<IReadOnlyList<GpuProcessInfo>> ScanAsync(CancellationToken cancellationToken = default);
}

