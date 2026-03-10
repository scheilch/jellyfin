using MediaBrowser.Model.Configuration;
using Xunit;

namespace Jellyfin.Model.Tests.Configuration;

public class EncodingOptionsTests
{
    [Fact]
    public void Constructor_IntelLowPowerH264_DefaultsToTrue()
    {
        var options = new EncodingOptions();
        Assert.True(options.EnableIntelLowPowerH264HwEncoder);
    }

    [Fact]
    public void Constructor_IntelLowPowerHevc_DefaultsToTrue()
    {
        var options = new EncodingOptions();
        Assert.True(options.EnableIntelLowPowerHevcHwEncoder);
    }
}
