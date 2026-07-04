using Cosmere.Lightweave.Blocks;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class CarouselMathTests {
    [Theory]
    [InlineData(3, 1, 1)]
    [InlineData(3, 3, 3)]
    [InlineData(3, 5, 3)]
    [InlineData(3, 0, 1)]
    [InlineData(0, 3, 1)]
    public void ClampVisible_clamps_to_one_through_count(int count, int visible, int expected) {
        Assert.Equal(expected, CarouselMath.ClampVisible(visible, count));
    }

    [Fact]
    public void LoopActive_is_false_when_all_slides_fit() {
        // Regression: 3 storytellers, visible:3, loop:true used to wrap the last
        // slide off-screen-left and cull it (Randy went missing). Looping must be
        // inactive when there is nothing to scroll.
        Assert.False(CarouselMath.LoopActive(3, 3, true));
    }

    [Fact]
    public void LoopActive_is_true_when_slides_overflow() {
        Assert.True(CarouselMath.LoopActive(5, 3, true));
    }

    [Fact]
    public void LoopActive_is_false_when_fewer_slides_than_visible() {
        Assert.False(CarouselMath.LoopActive(2, 3, true));
    }

    [Fact]
    public void LoopActive_is_false_when_loop_disabled() {
        Assert.False(CarouselMath.LoopActive(5, 3, false));
    }
}
