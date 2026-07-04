using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Settings;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
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

    private static readonly string[] RowLabels = ["FPS", "AVG", "MAX", "GPU", "RAM", "VRAM"];
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
        rowGapPx = Mathf.Round(1f * scale);
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
        float boxH = padPx + rowHeightPx * RowLabels.Length + rowGapPx * (RowLabels.Length - 1) + padPx;

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

        // Frosted Glass3 background matching the continue bar / dock buttons / EXPANSIONS bar.
        // box is window-local; BackdropBlur maps its UV via GUIToScreenPoint so the frost samples
        // the correct screen region even though this draws inside an above-Super ImmediateWindow.
        float radiusPx = Mathf.Round(6f * (LightweaveMod.Settings?.FontScale ?? 1f));
        BackdropBlur.Draw(box, 6f, cornerRadiusPx: radiusPx);
        Vector4 radVec = new Vector4(radiusPx, radiusPx, radiusPx, radiusPx);
        GUI.color = Color.white;
        GUI.DrawTexture(box, bgTex, ScaleMode.StretchToFill, true, 0f, ThemeRegistry.Active.GetColor(ThemeSlot.Glass3), Vector4.zero, radVec);
        GUI.color = Color.white;

        Theme.Theme theme = ThemeRegistry.Active;
        Color labelColor = theme.GetColor(ThemeSlot.TextSecondary);
        Color neutralValue = theme.GetColor(ThemeSlot.TextPrimary);

        Rect cursor = new Rect(box.x + padPx, box.y + padPx, labelWidthPx + colGapPx + valueWidthPx, rowHeightPx);
        DrawRow(cursor, "FPS", FormatFps(emaFps), labelColor, FpsColor(theme, emaFps), labelWidthPx, colGapPx);
        cursor.y += rowHeightPx + rowGapPx;
        DrawRow(cursor, "AVG", FormatMs(emaFrameMs), labelColor, MsColor(theme, emaFrameMs), labelWidthPx, colGapPx);
        cursor.y += rowHeightPx + rowGapPx;
        DrawRow(cursor, "MAX", FormatMs(displayMaxMs), labelColor, MsColor(theme, displayMaxMs), labelWidthPx, colGapPx);
        cursor.y += rowHeightPx + rowGapPx;
        if (recordersAvailable) {
            DrawRow(cursor, "GPU", FormatMs(emaGpuWaitMs), labelColor, GpuColor(theme, emaGpuWaitMs, emaFrameMs), labelWidthPx, colGapPx);
        } else {
            DrawRow(cursor, "GPU", "n/a", labelColor, theme.GetColor(ThemeSlot.TextMuted), labelWidthPx, colGapPx);
        }
        cursor.y += rowHeightPx + rowGapPx;
        DrawRow(cursor, "RAM", FormatMb(Profiler.GetTotalAllocatedMemoryLong()), labelColor, neutralValue, labelWidthPx, colGapPx);
        cursor.y += rowHeightPx + rowGapPx;
        DrawRow(cursor, "VRAM", FormatMb((long)Texture.currentTextureMemory), labelColor, neutralValue, labelWidthPx, colGapPx);

        Text.Font = prevFont;
        Text.Anchor = prevAnchor;
        Text.WordWrap = prevWrap;
        GUI.color = prevColor;
        GUI.contentColor = prevContentColor;
    }

    private static void DrawRow(Rect row, string label, string value, Color labelColor, Color valueColor, float labelW, float colGap) {
        Rect labelRect = new Rect(row.x, row.y, labelW, row.height);
        Rect valueRect = new Rect(row.x + labelW + colGap, row.y, row.width - labelW - colGap, row.height);

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.contentColor = labelColor;
        Widgets.Label(labelRect, label);

        Text.Anchor = TextAnchor.MiddleRight;
        GUI.contentColor = valueColor;
        Widgets.Label(valueRect, value);
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

    private static string FormatMb(long bytes) {
        if (bytes <= 0L) {
            return "--";
        }
        return (bytes / (1024L * 1024L)).ToString() + " MB";
    }

    private static Color FpsColor(Theme.Theme theme, float fps) {
        if (fps >= 55f) {
            return theme.GetColor(ThemeSlot.StatusSuccess);
        }
        if (fps >= 30f) {
            return theme.GetColor(ThemeSlot.StatusWarning);
        }
        return theme.GetColor(ThemeSlot.StatusDanger);
    }

    private static Color MsColor(Theme.Theme theme, float ms) {
        if (ms <= TargetFrameMs * 1.1f) {
            return theme.GetColor(ThemeSlot.StatusSuccess);
        }
        if (ms <= TargetFrameMs * 2f) {
            return theme.GetColor(ThemeSlot.StatusWarning);
        }
        return theme.GetColor(ThemeSlot.StatusDanger);
    }

    private static Color GpuColor(Theme.Theme theme, float gpuMs, float frameMs) {
        if (frameMs <= 0f) {
            return theme.GetColor(ThemeSlot.TextMuted);
        }
        float ratio = gpuMs / frameMs;
        if (ratio >= 0.55f) {
            return theme.GetColor(ThemeSlot.StatusDanger);
        }
        if (ratio >= 0.30f) {
            return theme.GetColor(ThemeSlot.StatusWarning);
        }
        return theme.GetColor(ThemeSlot.StatusSuccess);
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
