using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
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

    // -- Finding #2: QSV async_depth --

    [Theory]
    [InlineData("h264_qsv")]
    [InlineData("hevc_qsv")]
    [InlineData("av1_qsv")]
    public void GetVideoQualityParam_QsvEncoder_ContainsAsyncDepth(string encoder)
    {
        var state = CreateJobInfo(5_000_000);
        var options = new EncodingOptions
        {
            HardwareAccelerationType = HardwareAccelerationType.qsv
        };
        var result = _encodingHelper.GetVideoQualityParam(state, encoder, options, EncoderPreset.veryfast);
        Assert.Contains("-async_depth 8", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoQualityParam_NonQsvEncoder_DoesNotContainAsyncDepth8()
    {
        var state = CreateJobInfo(5_000_000);
        var options = new EncodingOptions();
        var result = _encodingHelper.GetVideoQualityParam(state, "libx264", options, EncoderPreset.veryfast);
        Assert.DoesNotContain("-async_depth", result, StringComparison.Ordinal);
    }

    // -- Finding #6: Thread count for HW encoders --

    [Theory]
    [InlineData("h264_qsv")]
    [InlineData("hevc_qsv")]
    [InlineData("h264_nvenc")]
    [InlineData("hevc_nvenc")]
    [InlineData("h264_vaapi")]
    [InlineData("h264_amf")]
    [InlineData("h264_videotoolbox")]
    public void GetNumberOfThreads_HwEncoder_Returns1(string codec)
    {
        var options = new EncodingOptions { EncodingThreadCount = -1 };
        var result = EncodingHelper.GetNumberOfThreads(null, options, codec);
        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData("libx264")]
    [InlineData("libx265")]
    [InlineData("libsvtav1")]
    public void GetNumberOfThreads_SwEncoder_ReturnsAutoThreading(string codec)
    {
        var options = new EncodingOptions { EncodingThreadCount = -1 };
        var result = EncodingHelper.GetNumberOfThreads(null, options, codec);
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetNumberOfThreads_ExplicitLimit_HonorsLimit()
    {
        var options = new EncodingOptions { EncodingThreadCount = 2 };
        var result = EncodingHelper.GetNumberOfThreads(null, options, "h264_qsv");
        Assert.Equal(Math.Min(2, Environment.ProcessorCount), result);
    }

    // -- Finding #12: Progressive GOP for HW encoders --
    // Tested via the GetProgressiveGopArguments helper

    [Fact]
    public void GetProgressiveGopArguments_WithFrameRate_ReturnsGopArgs()
    {
        var result = EncodingHelper.GetProgressiveGopArguments(30f);

        // 5s * 30fps = 150 frames per GOP
        Assert.Contains("-g:v:0 150", result, StringComparison.Ordinal);
        Assert.Contains("-keyint_min:v:0 150", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetProgressiveGopArguments_60fps_Returns300()
    {
        var result = EncodingHelper.GetProgressiveGopArguments(60f);

        Assert.Contains("-g:v:0 300", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetProgressiveGopArguments_23976fps_RoundsCeil()
    {
        var result = EncodingHelper.GetProgressiveGopArguments(23.976f);

        // 5 * 23.976 = 119.88 → ceil = 120
        Assert.Contains("-g:v:0 120", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetProgressiveGopArguments_NullFrameRate_ReturnsEmpty()
    {
        var result = EncodingHelper.GetProgressiveGopArguments(null);
        Assert.Equal(string.Empty, result);
    }

    // -- Finding #7: x264 rc_lookahead --

    [Fact]
    public void X264Opts_ContainsRcLookahead25()
    {
        Assert.Contains("rc_lookahead=25", EncodingHelper.X264Opts, StringComparison.Ordinal);
    }

    [Fact]
    public void X264Opts_ContainsSubme0()
    {
        Assert.Contains("subme=0", EncodingHelper.X264Opts, StringComparison.Ordinal);
    }

    // -- Finding #11: vpp_qsv async_depth --

    [Fact]
    public void VppQsvAsyncDepth_Is4()
    {
        Assert.Equal(4, EncodingHelper.VppQsvAsyncDepth);
    }
}
