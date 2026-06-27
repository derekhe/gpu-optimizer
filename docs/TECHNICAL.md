# Technical Design

## Project Structure

- `GpuOptimizer.App`: Avalonia desktop UI, view models, localization, and theme resources.
- `GpuOptimizer.Core`: Windows GPU scanning, GPU preference registry service, backup handling, and shared models.
- `GpuOptimizer.Core.Tests`: unit tests for parser, registry preference behavior, and path fallback logic.

## GPU Process Scanning

The scanner uses Windows performance counter categories:

- `GPU Engine(*)\Utilization Percentage`
- `GPU Process Memory(*)\Dedicated Usage`
- `GPU Process Memory(*)\Shared Usage`

Counter instance names include the process ID and the GPU adapter LUID. `GpuCounterInstanceParser` extracts those values. Memory samples are grouped by PID and by adapter so the UI can display both per-process dedicated memory and per-GPU totals.

Invalid counter samples, disappearing process instances, and inaccessible processes are skipped or marked without aborting the refresh.

## GPU Adapter Mapping

Windows performance counters identify GPUs by LUID rather than user-friendly names. `DxgiGpuAdapterEnumerator` enumerates hardware adapters through DXGI and formats adapter LUIDs to the same key shape used by performance counter instances.

The app asks DXGI for:

- `GpuPreference.MinimumPower`: treated as the integrated or power-saving GPU.
- `GpuPreference.HighPerformance`: treated as the discrete or high-performance GPU.

When DXGI cannot classify an adapter, the UI still displays the real adapter name with a generic GPU type.

## Process Path Resolution

The scanner resolves executable paths in this order:

1. `Process.MainModule.FileName`
2. Native `OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)` plus `QueryFullProcessImageName`
3. `%WINDIR%\System32\<process>.exe` fallback for restricted system processes such as `dwm`
4. MSIX/WindowsApps manifest fallback for packaged apps

Processes without a safe executable path are hidden from the UI because Windows GPU preferences are stored by executable path.

## Windows GPU Preferences

Windows stores per-user graphics preferences at:

```text
HKCU\Software\Microsoft\DirectX\UserGpuPreferences
```

Values are string entries keyed by executable path. Relevant `GpuPreference` values are:

- `GpuPreference=0;`: System default
- `GpuPreference=1;`: Power saving
- `GpuPreference=2;`: High performance

GPU Optimizer applies power saving by writing or updating `GpuPreference=1;`.

## Backup and Restore

Before writing a preference, `RegistryGpuPreferenceService` stores the original registry value in:

```text
%LocalAppData%\GpuOptimizer\backups.json
```

Restore behavior:

- If a backup exists and the original value was missing, delete the registry value.
- If a backup exists with an original value, restore that value.
- If no backup exists but a current preference value exists, delete it to return to SystemDefault.
- If no backup exists and no current value exists, report that the app is already SystemDefault.

## UI and Localization

The UI uses Avalonia with Fluent theme resources. `RequestedThemeVariant="Default"` lets Avalonia follow the operating system light/dark theme.

Localization is provided by `AppLocalizer`, a lightweight dictionary-based localizer. It supports:

- English (`en`)
- Simplified Chinese (`zh`)

The initial language follows the system UI culture. The user can switch language at runtime from the window toolbar.

## Limitations

- GPU preference changes are advisory; Windows and graphics drivers decide final scheduling behavior.
- Target apps usually need to be restarted before a preference change takes effect.
- Some protected or virtual GPU/process entries may not expose enough information and are intentionally hidden.
- The first version focuses on Windows.
