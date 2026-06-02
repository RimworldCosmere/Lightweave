using Cosmere.Lightweave.Settings;
using UnityEngine;
using UnityEngine.Profiling;
using Verse;

namespace Cosmere.Lightweave.Patch;

[StaticConstructorOnStartup]
internal static class PerformanceOverlay {
    static PerformanceOverlay() {
        EnsureTextures();
    }

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

    private static readonly string[] RowLabels = ["FPS", "AVG", "MAX", "GPU"];
    private const string ValueMeasureSample = "999.9 ms";

    // Stable, nonzero immediate-window id. WindowStack negates it internally.
    private const int WindowId = 6234607;
    // One past WindowLayer.Super. The stack only ever compares layers with <= / >=,
    // so an above-range value sorts last (drawn on top) and is skipped by every
    // window's KeepPinnedOnTop, which only re-pins against same-or-lower layers.
    private const WindowLayer AlwaysOnTopLayer = WindowLayer.Super + 1;
    private static readonly System.Action DrawContentAction = DrawBoxContents;

    private static Rect boxRect;
    private static float padPx;
    private static float rowGapPx;
    private static float colGapPx;
    private static float rowHeightPx;
    private static float labelWidthPx;
    private static float valueWidthPx;

    public static void Draw() {
        if (LightweaveMod.Settings is not { ShowPerformanceMetrics: true }) {
            return;
        }

        if (Event.current == null || Event.current.type != EventType.Repaint) {
            return;
        }

        Sample();
        MeasureBox();
        // Route the draw through an immediate window whose layer sits ABOVE Super so
        // it composites on top of every window's GUI.Window content - including the
        // fullscreen New Colony window. Drawing directly in the UIRoot postfix lands
        // on the background GUI layer (covered by any open window), and a plain Super
        // immediate window loses to LightweaveWindow.KeepPinnedOnTop, which re-pins
        // the New Colony window above any same-or-lower layer. The above-Super layer
        // both sorts the overlay last in the stack and makes that pin logic skip it.
        Find.WindowStack.ImmediateWindow(WindowId, boxRect, AlwaysOnTopLayer, DrawContentAction, doBackground: false);
    }

    public static void HandleToggleHotkey() {
        Event e = Event.current;
        if (e == null || e.type != EventType.KeyDown || e.keyCode != KeyCode.F9) {
            return;
        }
        // Vanilla KeyBindingDef cannot express a modifier chord, so the default toggle
        // (Alt+F9) is matched against the raw event.
        if (!e.alt || e.control || e.command || e.shift) {
            return;
        }
        if (LightweaveMod.Settings is not { } settings) {
            return;
        }
        settings.ShowPerformanceMetrics = !settings.ShowPerformanceMetrics;
        LightweaveMod.Save();
        e.Use();
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

    private static void MeasureBox() {
        TextAnchor prevAnchor = Text.Anchor;
        GameFont prevFont = Text.Font;
        bool prevWrap = Text.WordWrap;

        Text.Font = GameFont.Tiny;
        Text.WordWrap = false;

        // Geometry is measured, not fixed px. The framework's GameFontOverride
        // rewrites the GameFont GUIStyles' fontSize by the user's FontScale, so Tiny
        // text grows - but Text.LineHeight reads a startup-baked array that never
        // tracks that mutation. Text.CalcSize reads the live, scaled GUIStyle, so it
        // is the only width/height source that follows the setting. Measuring a
        // fixed "999.9 ms" sample (rather than the live values) keeps the box width
        // stable as the readouts change digits each frame.
        float scale = LightweaveMod.Settings?.FontScale ?? 1f;
        padPx = Mathf.Round(8f * scale);
        rowGapPx = Mathf.Round(2f * scale);
        colGapPx = Mathf.Round(6f * scale);

        Vector2 valueSize = Text.CalcSize(ValueMeasureSample);
        rowHeightPx = Mathf.Ceil(valueSize.y);
        valueWidthPx = Mathf.Ceil(valueSize.x);

        float labelW = 0f;
        for (int i = 0; i < RowLabels.Length; i++) {
            float w = Text.CalcSize(RowLabels[i]).x;
            if (w > labelW) {
                labelW = w;
            }
        }
        labelWidthPx = Mathf.Ceil(labelW);

        float boxW = padPx + labelWidthPx + colGapPx + valueWidthPx + padPx;
        float boxH = padPx + rowHeightPx * 4f + rowGapPx * 3f + padPx;

        float screenW = Verse.UI.screenWidth;
        boxRect = new Rect(screenW - boxW - 8f, 8f, boxW, boxH);

        Text.Font = prevFont;
        Text.Anchor = prevAnchor;
        Text.WordWrap = prevWrap;
    }

    // Drawn inside a Super-layer ImmediateWindow, so coordinates are window-local
    // (origin 0,0) and the geometry comes from the static fields MeasureBox set this
    // frame. The window rect itself was placed in screen space by MeasureBox.
    private static void DrawBoxContents() {
        EnsureTextures();

        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        TextAnchor prevAnchor = Text.Anchor;
        GameFont prevFont = Text.Font;
        bool prevWrap = Text.WordWrap;

        Text.Font = GameFont.Tiny;
        Text.WordWrap = false;

        Rect box = new Rect(0f, 0f, boxRect.width, boxRect.height);

        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(box, bgTex);
        GUI.color = new Color(1f, 1f, 1f, 0.18f);
        DrawRectOutline(box);
        GUI.color = Color.white;

        Rect cursor = new Rect(box.x + padPx, box.y + padPx, labelWidthPx + colGapPx + valueWidthPx, rowHeightPx);
        DrawRow(cursor, "FPS", FormatFps(emaFps), FpsColor(emaFps), labelWidthPx, colGapPx);
        cursor.y += rowHeightPx + rowGapPx;
        DrawRow(cursor, "AVG", FormatMs(emaFrameMs), MsColor(emaFrameMs), labelWidthPx, colGapPx);
        cursor.y += rowHeightPx + rowGapPx;
        DrawRow(cursor, "MAX", FormatMs(displayMaxMs), MsColor(displayMaxMs), labelWidthPx, colGapPx);
        cursor.y += rowHeightPx + rowGapPx;
        if (recordersAvailable) {
            DrawRow(cursor, "GPU", FormatMs(emaGpuWaitMs), GpuColor(emaGpuWaitMs, emaFrameMs), labelWidthPx, colGapPx);
        } else {
            DrawRow(cursor, "GPU", "n/a", new Color(0.55f, 0.55f, 0.58f, 1f), labelWidthPx, colGapPx);
        }

        Text.Font = prevFont;
        Text.Anchor = prevAnchor;
        Text.WordWrap = prevWrap;
        GUI.color = prevColor;
        GUI.contentColor = prevContentColor;
    }

    private static void DrawRow(Rect row, string label, string value, Color valueColor, float labelW, float colGap) {
        Rect labelRect = new Rect(row.x, row.y, labelW, row.height);
        Rect valueRect = new Rect(row.x + labelW + colGap, row.y, row.width - labelW - colGap, row.height);

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
