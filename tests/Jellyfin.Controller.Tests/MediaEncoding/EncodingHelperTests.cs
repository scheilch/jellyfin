using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperTests
{
    private readonly EncodingHelper _encodingHelper;

    public EncodingHelperTests()
    {
        _encodingHelper = new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            Mock.Of<IMediaEncoder>(),
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    private static EncodingJobInfo CreateJobInfo(int? videoBitrate)
    {
        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            OutputVideoBitrate = videoBitrate,
            BaseRequest = new BaseEncodingJobOptions()
        };
    }

    // -- Finding #1: QSV Lookahead VBR --

    [Theory]
    [InlineData("h264_qsv")]
    [InlineData("hevc_qsv")]
    public void GetVideoBitrateParam_QsvH264Hevc_ContainsLookahead(string codec)
    {
        var state = CreateJobInfo(5_000_000);
        var result = _encodingHelper.GetVideoBitrateParam(state, codec);
        Assert.Contains("-look_ahead 1", result, StringComparison.Ordinal);
        Assert.Contains("-look_ahead_depth 20", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoBitrateParam_Av1Qsv_DoesNotContainLookahead()
    {
        var state = CreateJobInfo(5_000_000);
        var result = _encodingHelper.GetVideoBitrateParam(state, "av1_qsv");
        Assert.DoesNotContain("-look_ahead", result, StringComparison.Ordinal);
    }
}
