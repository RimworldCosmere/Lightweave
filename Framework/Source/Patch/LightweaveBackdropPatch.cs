using System.Collections.Generic;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Patch;

public static class LightweaveBackdropRegistry {
    private static readonly List<LightweaveWindow> active = new List<LightweaveWindow>();
    private static readonly Dictionary<LightweaveWindow, float> startTime = new Dictionary<LightweaveWindow, float>();

    public static IReadOnlyList<LightweaveWindow> Active => active;

    public static void Register(LightweaveWindow window) {
        if (!active.Contains(window)) {
            active.Add(window);
            startTime[window] = Time.unscaledTime;
        }
    }

    public static void Unregister(LightweaveWindow window) {
        active.Remove(window);
        startTime.Remove(window);
    }

    public static float ScrimAlphaFor(LightweaveWindow window, float targetAlpha) {
        if (LightweaveMod.Settings?.ReduceMotion ?? false) {
            return targetAlpha;
        }
        if (!startTime.TryGetValue(window, out float t0)) {
            return targetAlpha;
        }
        float elapsed = Time.unscaledTime - t0;
        float progress = elapsed >= 0.16f ? 1f : Mathf.Clamp01(elapsed / 0.16f);
        return targetAlpha * progress;
    }

    public static void PruneOrphans() {
        if (active.Count == 0) {
            return;
        }

        WindowStack? stack = Find.WindowStack;
        if (stack == null) {
            active.Clear();
            return;
        }

        for (int i = active.Count - 1; i >= 0; i--) {
            LightweaveWindow window = active[i];
            if (window == null || !stack.Windows.Contains(window)) {
                active.RemoveAt(i);
                if (window != null) {
                    startTime.Remove(window);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.WindowStackOnGUI))]
public static class WindowStackOnGUI_BackdropPatch {
    public static void Prefix() {
        if (Event.current == null || Event.current.type != EventType.Repaint) {
            return;
        }

        LightweaveBackdropRegistry.PruneOrphans();

        IReadOnlyList<LightweaveWindow> windows = LightweaveBackdropRegistry.Active;
        if (windows.Count == 0) {
            return;
        }

        Rect screen = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
        for (int i = 0; i < windows.Count; i++) {
            LightweaveWindow window = windows[i];
            Theme.Theme theme = window.ThemeOverride ?? ThemeRegistry.Active;

            if (window.DrawScrim) {
                Color scrimBase = theme.GetColor(ThemeSlot.ScrimDefault);
                Color scrim = window.ScrimColor ?? new Color(scrimBase.r, scrimBase.g, scrimBase.b, 0.55f);
                float fadeProgress = scrim.a > 0f
                    ? LightweaveBackdropRegistry.ScrimAlphaFor(window, scrim.a) / scrim.a
                    : 1f;
                BackdropBlur.Draw(screen, 6f * fadeProgress);
                scrim.a *= fadeProgress;
                Widgets.DrawBoxSolid(screen, scrim);
            }

            if (window.DrawVignette) {
                Color tint = ResolveVignetteColor(window.VignetteColor, theme);
                tint.a *= Mathf.Clamp01(window.VignetteIntensity);
                float s = Mathf.Max(0.1f, window.VignetteScale);
                Texture2D tex = window.VignetteShape switch {
                    VignetteShape.Frame => VignetteTextureCache.Frame(innerSize: Mathf.Clamp01(0.62f / s), softness: 0.18f),
                    VignetteShape.Linear => VignetteTextureCache.Linear(VignetteEdge.Bottom, falloff: 1.2f / s),
                    _ => VignetteTextureCache.Radial(falloff: 1.6f / s),
                };
                PaintBox.DrawTexture(screen, tex, tint);
            }
        }
    }

    private static Color ResolveVignetteColor(ColorRef? cr, Theme.Theme theme) {
        return cr switch {
            ColorRef.Literal lit => lit.Value,
            ColorRef.Token tok => theme.GetColor(tok.Slot),
            _ => theme.GetColor(ThemeSlot.OverlayDim),
        };
    }
}
