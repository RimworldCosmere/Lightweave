namespace Cosmere.Lightweave.Tokens;

// Display-resolution tiers, named after the major 16:9 standards. Resolved once per frame
// from the actual screen width (UI.screenWidth), not the surface/window width - a primitive
// in a narrow pane on a 4K display reads P2160, because the tier describes the monitor, not
// the rect it happens to occupy.
public enum Breakpoint {
    P720 = 0,
    P1080 = 1,
    P1440 = 2,
    P2160 = 3,
}

public static class Breakpoints {
    public const float P1080MinPx = 1920f;
    public const float P1440MinPx = 2560f;
    public const float P2160MinPx = 3840f;

    public static Breakpoint For(float widthPx) {
        if (widthPx >= P2160MinPx) return Breakpoint.P2160;
        if (widthPx >= P1440MinPx) return Breakpoint.P1440;
        if (widthPx >= P1080MinPx) return Breakpoint.P1080;
        return Breakpoint.P720;
    }

    public static Breakpoint Current => Runtime.RenderContext.CurrentOrNull?.Breakpoint ?? Breakpoint.P720;

    public static T Pick<T>(
        T p720,
        T? p1080 = null,
        T? p1440 = null,
        T? p2160 = null
    ) where T : struct {
        Breakpoint current = Current;
        if (p2160.HasValue && current >= Breakpoint.P2160) return p2160.Value;
        if (p1440.HasValue && current >= Breakpoint.P1440) return p1440.Value;
        if (p1080.HasValue && current >= Breakpoint.P1080) return p1080.Value;
        return p720;
    }

    public static T PickRef<T>(
        T p720,
        T? p1080 = null,
        T? p1440 = null,
        T? p2160 = null
    ) where T : class {
        Breakpoint current = Current;
        if (p2160 != null && current >= Breakpoint.P2160) return p2160;
        if (p1440 != null && current >= Breakpoint.P1440) return p1440;
        if (p1080 != null && current >= Breakpoint.P1080) return p1080;
        return p720;
    }
}
