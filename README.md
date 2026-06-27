# GPU Optimizer

GPU Optimizer is a Windows desktop utility for finding processes that are using GPU dedicated memory and moving selected apps to the Windows power-saving GPU preference. It is intended for hybrid-GPU laptops and desktops where integrated graphics can handle lightweight apps while the discrete GPU memory should be kept free for heavier workloads.

## Features

- Lists active GPU processes with process name, GPU adapter, dedicated memory usage, and current Windows GPU preference.
- Maps GPU counter adapter IDs to real GPU names using DXGI.
- Distinguishes integrated and discrete GPUs when Windows reports minimum-power and high-performance adapters.
- Shows per-GPU dedicated memory totals and selected discrete-GPU memory that may be saved.
- Applies Windows power-saving GPU preference (`GpuPreference=1`) to selected executable paths.
- Restores preferences to the original value when a backup exists, or resets to SystemDefault by deleting the current preference value.
- Supports English and Simplified Chinese.
- Follows the system light/dark theme.

## Requirements

- Windows 10/11 with GPU performance counters available.
- .NET 10 runtime for framework-dependent builds, or use the self-contained release package.
- The target app usually needs to be restarted before Windows applies a changed GPU preference.

## Usage

1. Start GPU Optimizer.
2. Review the list of processes using GPU dedicated memory.
3. Select apps that can run on the integrated GPU.
4. Click **Optimize** to set those apps to the Windows power-saving GPU preference.
5. Restart the target apps.
6. Use **Restore** to return selected apps to their original preference, or SystemDefault when no original backup exists.

## How It Works

GPU Optimizer reads Windows GPU performance counters, including `GPU Process Memory(*)\Dedicated Usage`, and uses DXGI to map adapter LUIDs to real GPU names and integrated/discrete types. When optimization is applied, it writes the current user's Windows graphics preference under:

```text
HKCU\Software\Microsoft\DirectX\UserGpuPreferences
```

For selected executable paths, it writes:

```text
GpuPreference=1;
```

This is Windows' power-saving GPU preference. Windows applies this preference after the target app is restarted.

## Safety Notes

- GPU Optimizer only writes to the current user's GPU preference registry key.
- It does not terminate target processes.
- It backs up original preference values before applying optimization.
- System and protected processes may not always expose executable paths through Windows process APIs; the app hides entries that cannot be safely mapped.

## Development

Build and test:

```powershell
dotnet build
dotnet test --no-build
```

Run the app:

```powershell
dotnet run --project GpuOptimizer.App
```

Publish a Windows x64 self-contained single-file executable:

```powershell
dotnet publish GpuOptimizer.App -c Release
```

The distributable executable is `GpuOptimizer.exe` in the publish directory. Native debug symbol files, if emitted by dependent packages, are not required for running the app.

## License

MIT License. See [LICENSE](LICENSE).
