using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Cosmere.Lightweave.Rendering;

[StaticConstructorOnStartup]
public static class BlurPipeline {
    public const int DownsampleFactor = 2;
    public const int Iterations = 1;
    public const float BlurStepTexels = 1.0f;

    private static RenderTexture? capture;
    private static RenderTexture? ping;
    private static RenderTexture? pong;
    private static CommandBuffer? captureCb;
    private static int builtFrame = -1;
    private static RenderTexture? lastResult;

    private static readonly int BlurStepId = Shader.PropertyToID("_BlurStep");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
    private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
    private static readonly int SubRectUvId = Shader.PropertyToID("_SubRectUV");

    public static RenderTexture? EnsureFrameBlur() {
        Material mat = LightweaveShaderDatabase.BlurMaterial;
        if (mat == null) {
            return null;
        }

        int frame = Time.frameCount;
        if (!BlurPipelineMath.NeedsRebuild(builtFrame, frame)) {
            return lastResult;
        }

        int screenW = UI.screenWidth;
        int screenH = UI.screenHeight;
        if (screenW <= 0 || screenH <= 0) {
            return null;
        }

        (int w, int h) = BlurPipelineMath.DownsampledSize(screenW, screenH, DownsampleFactor);
        EnsureTargets(screenW, screenH, w, h);

        mat.SetFloat(BlurStepId, BlurStepTexels);

        // ALL GPU work goes into ONE CommandBuffer executed once. Immediate Graphics.Blit
        // calls interleaved into an IMGUI Repaint corrupt the GUI render pass (symptom: the
        // whole menu vanishes, only the backdrop draws). A single deferred CommandBuffer is
        // the IMGUI-safe pattern (it is what the verified capture spike used).
        //
        // Blitting CurrentActive straight into a smaller RT does NOT downscale (it copies the
        // screen 1:1, leaving only a sub-region) — so capture full-res first, THEN a RT->RT
        // blit downscales correctly. Skipping the full capture mis-maps the sampled region.
        RenderTexture a = ping!;
        RenderTexture b = pong!;
        captureCb!.Clear();
        captureCb.Blit(BuiltinRenderTextureType.CurrentActive, capture); // full-res faithful capture
        captureCb.Blit(capture, a);                                      // downscale full -> low-res
        for (int it = 0; it < Iterations; it++) {
            captureCb.Blit(a, b, mat, 0); // horizontal
            Swap(ref a, ref b);
            captureCb.Blit(a, b, mat, 1); // vertical
            Swap(ref a, ref b);
        }

        RenderTexture? prevActive = RenderTexture.active;
        Graphics.ExecuteCommandBuffer(captureCb);
        RenderTexture.active = prevActive;

        builtFrame = frame;
        lastResult = a;
        return a;
    }

    public static void Composite(Rect snappedPx, Rect clippedPx, RenderTexture blurred, Color tint, float cornerRadiusPx) {
        Material mat = LightweaveShaderDatabase.BlurMaterial;
        if (mat == null || blurred == null) {
            return;
        }

        float effectiveRadius = BlurPipelineMath.EffectiveCornerRadius(
            snappedPx.x, snappedPx.y, snappedPx.width, snappedPx.height,
            clippedPx.x, clippedPx.y, clippedPx.width, clippedPx.height,
            cornerRadiusPx);

        mat.SetColor(ColorId, tint);
        mat.SetFloat(CornerRadiusId, effectiveRadius);
        mat.SetVector(RectSizeId, new Vector4(snappedPx.width, snappedPx.height, 0f, 0f));

        // Map the surface's absolute screen rect to the full-screen capture's UV. flipY=true
        // because the capture RT is bottom-origin while GUI rects are top-origin.
        (float ux, float uy, float uw, float uh) = BlurPipelineMath.ScreenUvSubRect(
            clippedPx.x, clippedPx.y, clippedPx.width, clippedPx.height,
            UI.screenWidth, UI.screenHeight, flipY: true);
        mat.SetVector(SubRectUvId, new Vector4(ux, uy, uw, uh));

        Graphics.DrawTexture(clippedPx, blurred, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, Color.white, mat, 2);
    }

    public static void ReleaseAll() {
        ReleaseTargets();
        captureCb?.Release();
        captureCb = null;
        builtFrame = -1;
    }

    private static void EnsureTargets(int captureW, int captureH, int w, int h) {
        if (capture == null || BlurPipelineMath.NeedsRealloc(capture.width, capture.height, captureW, captureH)
            || ping == null || BlurPipelineMath.NeedsRealloc(ping.width, ping.height, w, h)) {
            ReleaseTargets();
            capture = NewRt(captureW, captureH, "LW_BlurCapture");
            ping = NewRt(w, h, "LW_BlurPing");
            pong = NewRt(w, h, "LW_BlurPong");
        }
        captureCb ??= new CommandBuffer { name = "LW_Blur" };
    }

    private static RenderTexture NewRt(int w, int h, string name) {
        RenderTexture rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        rt.Create();
        return rt;
    }

    private static void ReleaseTargets() {
        capture?.Release();
        ping?.Release();
        pong?.Release();
        capture = null;
        ping = null;
        pong = null;
        lastResult = null;
    }

    private static void Swap(ref RenderTexture a, ref RenderTexture b) {
        RenderTexture t = a;
        a = b;
        b = t;
    }
}
