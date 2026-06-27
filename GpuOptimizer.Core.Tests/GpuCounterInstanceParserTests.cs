using GpuOptimizer.Core.Scanning;
using Xunit;

namespace GpuOptimizer.Core.Tests;

public class GpuCounterInstanceParserTests
{
    [Theory]
    [InlineData("pid_1234", 1234)]
    [InlineData("some_pid_987", 987)]
    [InlineData("GPU_1D_ENG_TYPE_luid_0x0000_pid_42_some", 42)]
    public void TryGetProcessIdFromInstance_ParsesPid_WhenPresent(string instanceName, int expectedPid)
    {
        var result = GpuCounterInstanceParser.TryGetProcessIdFromInstance(instanceName, out var processId);

        Assert.True(result);
        Assert.Equal(expectedPid, processId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("_pid_")]
    [InlineData("pid_")]
    [InlineData("dpid_55")]
    [InlineData("_pid_abc")]
    [InlineData("abc")]
    public void TryGetProcessIdFromInstance_ReturnsFalse_WhenMissingOrInvalid(string instanceName)
    {
        var result = GpuCounterInstanceParser.TryGetProcessIdFromInstance(instanceName, out var processId);

        Assert.False(result);
        Assert.Equal(0, processId);
    }

    [Theory]
    [InlineData("luid_0x00000001_engtype_3D_pid_1234", "3D")]
    [InlineData("pid_1234_luid_0x00000000_0x00019666_phys_0_eng_0_engtype_3D", "3D")]
    [InlineData("eng_Compute_pid_5", "Compute")]
    [InlineData("something_pid_9", "something_pid_9")]
    public void ParseEngineName_ExtractsKnownTokens_WhenPossible(string instanceName, string expectedEngine)
    {
        var engine = GpuCounterInstanceParser.ParseEngineName(instanceName);
        Assert.Equal(expectedEngine, engine);
    }

    [Theory]
    [InlineData("pid_1234_luid_0x00000000_0x00019666_phys_0_eng_0_engtype_3D", "0x00000000_0x00019666")]
    [InlineData("luid_0xABCDEF01_0x0001D7CA_phys_0", "0xabcdef01_0x0001d7ca")]
    public void TryGetAdapterKeyFromInstance_ParsesLuidKey_WhenPresent(string instanceName, string expectedKey)
    {
        var result = GpuCounterInstanceParser.TryGetAdapterKeyFromInstance(instanceName, out var adapterKey);

        Assert.True(result);
        Assert.Equal(expectedKey, adapterKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pid_1234")]
    [InlineData("luid_missing")]
    public void TryGetAdapterKeyFromInstance_ReturnsFalse_WhenMissing(string instanceName)
    {
        var result = GpuCounterInstanceParser.TryGetAdapterKeyFromInstance(instanceName, out var adapterKey);

        Assert.False(result);
        Assert.Equal(string.Empty, adapterKey);
    }

    [Theory]
    [InlineData(0, 0x00019666u, "0x00000000_0x00019666")]
    [InlineData(-1, 0xabcdef01u, "0xffffffff_0xabcdef01")]
    public void FormatAdapterKey_FormatsDxgiLuidLikeCounterInstances(int highPart, uint lowPart, string expectedKey)
    {
        var adapterKey = GpuCounterInstanceParser.FormatAdapterKey(highPart, lowPart);
        Assert.Equal(expectedKey, adapterKey);
    }
}
