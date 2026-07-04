namespace Cosmere.Lightweave.Blocks;

public static class CarouselMath {
    public static int ClampVisible(int visible, int count) {
        if (count <= 0) {
            return 1;
        }
        if (visible < 1) {
            return 1;
        }
        if (visible > count) {
            return count;
        }
        return visible;
    }

    public static bool LoopActive(int count, int visible, bool loop) {
        return loop && count > ClampVisible(visible, count);
    }
}
