using Cosmere.Lightweave.Rendering;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class BlurPipelineMathTests {
    [Fact]
    public void DownsampledSize_quarters_and_clamps() {
        Assert.Equal((480, 270), BlurPipelineMath.DownsampledSize(1920, 1080, 4));
        Assert.Equal((1920, 1080), BlurPipelineMath.DownsampledSize(1920, 1080, 1));
        Assert.Equal((1, 1), BlurPipelineMath.DownsampledSize(2, 2, 99)); // never zero
        Assert.Equal((1920, 1080), BlurPipelineMath.DownsampledSize(1920, 1080, 0)); // factor clamps to 1
    }

    [Fact]
    public void NeedsRebuild_only_on_frame_change() {
        Assert.True(BlurPipelineMath.NeedsRebuild(10, 11));
        Assert.False(BlurPipelineMath.NeedsRebuild(11, 11));
    }

    [Fact]
    public void NeedsRealloc_on_any_dim_change() {
        Assert.False(BlurPipelineMath.NeedsRealloc(480, 270, 480, 270));
        Assert.True(BlurPipelineMath.NeedsRealloc(0, 0, 480, 270));
        Assert.True(BlurPipelineMath.NeedsRealloc(480, 270, 481, 270));
    }

    [Fact]
    public void ScreenUvSubRect_maps_pixels_to_uv() {
        (float x, float y, float w, float h) = BlurPipelineMath.ScreenUvSubRect(960, 540, 480, 270, 1920, 1080, flipY: false);
        Assert.Equal(0.5f, x, 4);
        Assert.Equal(0.5f, y, 4);
        Assert.Equal(0.25f, w, 4);
        Assert.Equal(0.25f, h, 4);
    }

    [Fact]
    public void ScreenUvSubRect_flipY_inverts_v() {
        (float _, float normalY, float _, float _) = BlurPipelineMath.ScreenUvSubRect(0, 0, 1920, 270, 1920, 1080, flipY: false);
        (float _, float flippedY, float _, float _) = BlurPipelineMath.ScreenUvSubRect(0, 0, 1920, 270, 1920, 1080, flipY: true);
        Assert.Equal(0f, normalY, 4);
        Assert.Equal(0.75f, flippedY, 4); // top band flips to top of flipped V space
    }

    [Fact]
    public void EffectiveCornerRadius_zero_when_clipped() {
        Assert.Equal(8f, BlurPipelineMath.EffectiveCornerRadius(0, 0, 100, 100, 0, 0, 100, 100, 8f), 4);
        Assert.Equal(0f, BlurPipelineMath.EffectiveCornerRadius(0, 0, 100, 100, 0, 10, 100, 90, 8f), 4);
    }
}
