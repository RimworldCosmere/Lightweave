using Cosmere.Lightweave.Settings;
using UnityEngine;
using UnityEngine.Profiling;
using Verse;

namespace Cosmere.Lightweave.Patch;

internal static class PerformanceOverlay {
    private const int SampleCapacity = 120;
    private const float TargetFrameMs = 1000f / 60f;
    private const float FpsEmaAlpha = 0.12f;
    private const float MsEmaAlpha = 0.18f;
    private const float GpuEmaAlpha = 0.18f;

    private static readonly float[] FrameSamples = new float[SampleCapacity];
    private static int sampleHead;
    private static int sampleCount;

    private static float emaFps;
    private static float emaFrameMs;
    private static float emaGpuWaitMs;
    private static float emaRenderMs;
    private static float displayMaxMs;

    private static Recorder? gpuWaitRecorder;
    private static Recorder? presentFrameRecorder;
    private static Recorder? renderRecorder;
    private static bool recordersAvailable;

    private static Texture2D? bgTex;

    public static void Draw() {
        if (LightweaveMod.Settings is not { ShowPerformanceMetrics: true }) {
            return;
        }

        if (Event.current == null || Event.current.type != EventType.Repaint) {
            return;
        }

        Sample();
        DrawBox();
    }

    private static void Sample() {
        EnsureRecorders();

        float dtMs = Time.unscaledDeltaTime * 1000f;
        if (dtMs <= 0f) {
            return;
        }

        FrameSamples[sampleHead] = dtMs;
        sampleHead = (sampleHead + 1) % SampleCapacity;
        if (sampleCount < SampleCapacity) {
            sampleCount++;
        }

        float instantFps = 1000f / dtMs;
        if (emaFps <= 0f) {
            emaFps = instantFps;
            emaFrameMs = dtMs;
        } else {
            emaFps = emaFps + FpsEmaAlpha * (instantFps - emaFps);
            emaFrameMs = emaFrameMs + MsEmaAlpha * (dtMs - emaFrameMs);
        }

        if (recordersAvailable) {
            float gpuWaitMs = 0f;
            if (gpuWaitRecorder != null) {
                gpuWaitMs += gpuWaitRecorder.elapsedNanoseconds / 1_000_000f;
            }
            if (presentFrameRecorder != null) {
                gpuWaitMs += presentFrameRecorder.elapsedNanoseconds / 1_000_000f;
            }
            float renderMs = renderRecorder != null ? renderRecorder.elapsedNanoseconds / 1_000_000f : 0f;
            emaGpuWaitMs = emaGpuWaitMs + GpuEmaAlpha * (gpuWaitMs - emaGpuWaitMs);
            emaRenderMs = emaRenderMs + GpuEmaAlpha * (renderMs - emaRenderMs);
        }

        float maxMs = 0f;
        for (int i = 0; i < sampleCount; i++) {
            float v = FrameSamples[i];
            if (v > maxMs) {
                maxMs = v;
            }
        }
        displayMaxMs = maxMs;
    }

    private static void DrawBox() {
        EnsureTextures();

        const float pad = 8f;
        const float lineH = 16f;
        const float rowGap = 2f;
        const float labelW = 38f;
        const float valueW = 70f;
        const float boxW = pad + labelW + 6f + valueW + pad;
        const float boxH = pad + lineH * 4f + rowGap * 3f + pad;

        float screenW = Verse.UI.screenWidth;
        Rect box = new Rect(screenW - boxW - 8f, 8f, boxW, boxH);

        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        TextAnchor prevAnchor = Text.Anchor;
        GameFont prevFont = Text.Font;
        bool prevWrap = Text.WordWrap;

        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(box, bgTex);
        GUI.color = new Color(1f, 1f, 1f, 0.18f);
        DrawRectOutline(box);
        GUI.color = Color.white;

        Text.Font = GameFont.Tiny;
        Text.WordWrap = false;

        Rect cursor = new Rect(box.x + pad, box.y + pad, labelW + 6f + valueW, lineH);
        DrawRow(cursor, "FPS", FormatFps(emaFps), FpsColor(emaFps), labelW);
        cursor.y += lineH + rowGap;
        DrawRow(cursor, "AVG", FormatMs(emaFrameMs), MsColor(emaFrameMs), labelW);
        cursor.y += lineH + rowGap;
        DrawRow(cursor, "MAX", FormatMs(displayMaxMs), MsColor(displayMaxMs), labelW);
        cursor.y += lineH + rowGap;
        if (recordersAvailable) {
            DrawRow(cursor, "GPU", FormatMs(emaGpuWaitMs), GpuColor(emaGpuWaitMs, emaFrameMs), labelW);
        } else {
            DrawRow(cursor, "GPU", "n/a", new Color(0.55f, 0.55f, 0.58f, 1f), labelW);
        }

        Text.Font = prevFont;
        Text.Anchor = prevAnchor;
        Text.WordWrap = prevWrap;
        GUI.color = prevColor;
        GUI.contentColor = prevContentColor;
    }

    private static void DrawRow(Rect row, string label, string value, Color valueColor, float labelW) {
        Rect labelRect = new Rect(row.x, row.y, labelW, row.height);
        Rect valueRect = new Rect(row.x + labelW + 6f, row.y, row.width - labelW - 6f, row.height);

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.contentColor = new Color(0.78f, 0.78f, 0.82f, 1f);
        Widgets.Label(labelRect, label);

        Text.Anchor = TextAnchor.MiddleRight;
        GUI.contentColor = valueColor;
        Widgets.Label(valueRect, value);
    }

    private static void DrawRectOutline(Rect r) {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), bgTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), bgTex);
        GUI.DrawTexture(new Rect(r.x, r.y, 1f, r.height), bgTex);
        GUI.DrawTexture(new Rect(r.xMax - 1f, r.y, 1f, r.height), bgTex);
    }

    private static void EnsureTextures() {
        if (bgTex != null) {
            return;
        }
        bgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false, false) {
            name = "LightweavePerfOverlayBg",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
        };
        bgTex.SetPixel(0, 0, Color.white);
        bgTex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
    }

    private static string FormatFps(float fps) {
        if (fps <= 0f) {
            return "--";
        }
        return fps.ToString("0.0");
    }

    private static string FormatMs(float ms) {
        if (ms <= 0f) {
            return "--";
        }
        return ms.ToString("0.0") + " ms";
    }

    private static Color FpsColor(float fps) {
        if (fps >= 55f) {
            return new Color(0.62f, 0.85f, 0.55f, 1f);
        }
        if (fps >= 30f) {
            return new Color(0.95f, 0.78f, 0.36f, 1f);
        }
        return new Color(0.92f, 0.45f, 0.42f, 1f);
    }

    private static Color MsColor(float ms) {
        if (ms <= TargetFrameMs * 1.1f) {
            return new Color(0.62f, 0.85f, 0.55f, 1f);
        }
        if (ms <= TargetFrameMs * 2f) {
            return new Color(0.95f, 0.78f, 0.36f, 1f);
        }
        return new Color(0.92f, 0.45f, 0.42f, 1f);
    }

    private static Color GpuColor(float gpuMs, float frameMs) {
        if (frameMs <= 0f) {
            return new Color(0.78f, 0.78f, 0.82f, 1f);
        }
        float ratio = gpuMs / frameMs;
        if (ratio >= 0.55f) {
            return new Color(0.92f, 0.45f, 0.42f, 1f);
        }
        if (ratio >= 0.30f) {
            return new Color(0.95f, 0.78f, 0.36f, 1f);
        }
        return new Color(0.62f, 0.85f, 0.55f, 1f);
    }

    private static void EnsureRecorders() {
        if (recordersAvailable || gpuWaitRecorder != null) {
            return;
        }
        try {
            gpuWaitRecorder = Recorder.Get("Gfx.WaitForPresentOnGfxThread");
            presentFrameRecorder = Recorder.Get("Gfx.PresentFrame");
            renderRecorder = Recorder.Get("Camera.Render");
            if (gpuWaitRecorder != null) {
                gpuWaitRecorder.enabled = true;
            }
            if (presentFrameRecorder != null) {
                presentFrameRecorder.enabled = true;
            }
            if (renderRecorder != null) {
                renderRecorder.enabled = true;
            }
            recordersAvailable = gpuWaitRecorder != null || presentFrameRecorder != null;
        } catch {
            recordersAvailable = false;
        }
    }
}
