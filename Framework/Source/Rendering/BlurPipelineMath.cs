using System;

namespace Cosmere.Lightweave.Rendering;

// Pure (UnityEngine-free) math for the backdrop blur pipeline so it can be linked
// into the net9.0 Tests project. Callers convert Rect <-> primitives at the boundary.
public static class BlurPipelineMath {
    public static (int w, int h) DownsampledSize(int screenW, int screenH, int factor) {
        int f = factor < 1 ? 1 : factor;
        int w = screenW / f;
        int h = screenH / f;
        return (w < 1 ? 1 : w, h < 1 ? 1 : h);
    }

    public static bool NeedsRebuild(int cachedFrame, int currentFrame) {
        return cachedFrame != currentFrame;
    }

    public static bool NeedsRealloc(int haveW, int haveH, int wantW, int wantH) {
        return haveW != wantW || haveH != wantH;
    }

    public static (float x, float y, float w, float h) ScreenUvSubRect(
        float surfaceX, float surfaceY, float surfaceW, float surfaceH,
        int screenW, int screenH, bool flipY) {
        float w = screenW <= 0 ? 1f : screenW;
        float h = screenH <= 0 ? 1f : screenH;
        float ux = surfaceX / w;
        float uw = surfaceW / w;
        float uh = surfaceH / h;
        float uy = flipY ? 1f - (surfaceY + surfaceH) / h : surfaceY / h;
        return (ux, uy, uw, uh);
    }

    public static float EffectiveCornerRadius(
        float snappedX, float snappedY, float snappedW, float snappedH,
        float clippedX, float clippedY, float clippedW, float clippedH,
        float requestedRadiusPx) {
        bool fullyVisible = Math.Abs(clippedX - snappedX) < 0.5f
                            && Math.Abs(clippedY - snappedY) < 0.5f
                            && Math.Abs(clippedW - snappedW) < 0.5f
                            && Math.Abs(clippedH - snappedH) < 0.5f;
        return fullyVisible ? requestedRadiusPx : 0f;
    }
}
