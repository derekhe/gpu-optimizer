using GpuOptimizer.Core.Models;
using GpuOptimizer.Core.Services;
using Xunit;

namespace GpuOptimizer.Core.Tests;

public class GpuPreferenceValueTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ReturnsSystemDefault_WhenValueIsMissing(string? value)
    {
        var result = GpuPreferenceValue.Parse(value);
        Assert.Equal(GpuPreferenceKind.SystemDefault, result);
    }

    [Fact]
    public void Parse_ReturnsPowerSaving_WhenValueIsExplicit()
    {
        var result = GpuPreferenceValue.Parse("GpuPreference=1;");
        Assert.Equal(GpuPreferenceKind.PowerSaving, result);
    }

    [Fact]
    public void Parse_ReturnsHighPerformance_WhenValueIsExplicit()
    {
        var result = GpuPreferenceValue.Parse("GpuPreference=2;");
        Assert.Equal(GpuPreferenceKind.HighPerformance, result);
    }

    [Fact]
    public void Parse_ReturnsUnknown_WhenNumericIsUnexpected()
    {
        var result = GpuPreferenceValue.Parse("GpuPreference=9;");
        Assert.Equal(GpuPreferenceKind.Unknown, result);
    }

    [Fact]
    public void Parse_IsCaseInsensitiveForPrefix()
    {
        var result = GpuPreferenceValue.Parse("other=abc;gPUpReFeReNcE=1;foo=bar;");
        Assert.Equal(GpuPreferenceKind.PowerSaving, result);
    }

    [Fact]
    public void EnsurePowerSaving_UpdatesOnlyGpuPreferenceToken()
    {
        var result = GpuPreferenceValue.EnsurePowerSaving("foo=bar;GpuPreference=2;hello=1");
        Assert.Equal("foo=bar;GpuPreference=1;hello=1;", result);
    }

    [Fact]
    public void EnsurePowerSaving_UpdatesCaseInsensitivePrefix()
    {
        var result = GpuPreferenceValue.EnsurePowerSaving("foo=bar;gPUpReFeReNcE=2;hello=1;");
        Assert.Equal("foo=bar;GpuPreference=1;hello=1;", result);
    }

    [Fact]
    public void EnsurePowerSaving_AppendsWhenMissing()
    {
        var result = GpuPreferenceValue.EnsurePowerSaving("foo=bar;hello=1;");
        Assert.Equal("foo=bar;hello=1;GpuPreference=1;", result);
    }
}
