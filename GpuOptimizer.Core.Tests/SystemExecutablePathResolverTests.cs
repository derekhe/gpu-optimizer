using GpuOptimizer.Core.Scanning;
using System;
using System.IO;
using Xunit;

namespace GpuOptimizer.Core.Tests;

public class SystemExecutablePathResolverTests
{
    [Fact]
    public void ResolveExecutablePath_ReturnsSystem32Executable_WhenProcessApiCannotExposePath()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("GpuOptimizer-SystemPath-");
        try
        {
            var executablePath = Path.Combine(tempDirectory.FullName, "dwm.exe");
            File.WriteAllText(executablePath, string.Empty);

            var resolved = SystemExecutablePathResolver.ResolveExecutablePath("dwm", tempDirectory.FullName);

            Assert.Equal(executablePath, resolved);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData(@"C:\Windows\System32\dwm.exe")]
    [InlineData(@"..\dwm")]
    public void ResolveExecutablePath_RejectsPathLikeProcessNames(string processName)
    {
        var resolved = SystemExecutablePathResolver.ResolveExecutablePath(processName, Environment.SystemDirectory);

        Assert.Equal(string.Empty, resolved);
    }
}
