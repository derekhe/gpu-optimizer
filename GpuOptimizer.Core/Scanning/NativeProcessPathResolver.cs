using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GpuOptimizer.Core.Scanning;

internal static class NativeProcessPathResolver
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public static string ResolveExecutablePath(int processId)
    {
        var processHandle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (processHandle == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var path = new StringBuilder(32768);
            var size = path.Capacity;
            return QueryFullProcessImageName(processHandle, 0, path, ref size)
                ? path.ToString()
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr processHandle, int flags, StringBuilder executablePath, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
