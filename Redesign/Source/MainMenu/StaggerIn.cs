using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using UnityEngine;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class StaggerIn {
    private const float DefaultDuration = 0.42f;
    private const float TranslatePx = 12f;

    public static LightweaveNode Wrap(
        LightweaveNode child,
        float delaySeconds,
        float duration = DefaultDuration,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        if (LightweaveMod.Settings == null || LightweaveMod.Settings.ReduceMotion) {
            return child;
        }

        LightweaveNode node = NodeBuilder.New("StaggerIn", line, file);
        node.ApplyStyling("stagger-in", style, classes, id);
        node.Children.Add(child);
        node.Measure = available => child.Measure?.Invoke(available) ?? child.PreferredHeight ?? 0f;
        node.MeasureWidth = () => child.MeasureWidth?.Invoke() ?? 0f;

        node.Paint = (rect, paintChildren) => {
            Hooks.Hooks.RefHandle<float> startTime = Hooks.Hooks.UseRef(-1f, line, file);
            if (startTime.Current < 0f) {
                startTime.Current = Time.unscaledTime;
            }

            float elapsed = Time.unscaledTime - startTime.Current - delaySeconds;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = EaseOutCubic(t);

            if (eased < 1f) {
                AnimationClock.RegisterActive(RenderContext.Current.RootId);
            }

            float opacity = eased;
            float translate = (1f - eased) * TranslatePx;

            using (TintScope.Opacity(opacity)) {
                child.MeasuredRect = new Rect(rect.x, rect.y + translate, rect.width, rect.height);
                paintChildren();
            }
        };
        return node;
    }

    private static float EaseOutCubic(float t) {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }
}
