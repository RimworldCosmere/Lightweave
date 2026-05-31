using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "switch",
    Summary = "Animated on/off toggle with adjacent label.",
    WhenToUse = "Toggle a setting that takes effect immediately.",
    SourcePath = "Lightweave/Input/Switch.cs"
)]
public static class Switch {
    private const float AnimationDurationSec = 0.12f;

    public static LightweaveNode Create(
        [DocParam("Text rendered next to the track.")]
        string label,
        [DocParam("Current on/off state.")]
        bool value,
        [DocParam("Invoked with the new value when toggled.")]
        Action<bool> onChange,
        [DocParam("Disables interaction and applies disabled styling.")]
        bool disabled = false,
        [DocParam("Optional key disambiguating multiple instances declared on the same line.")]
        object? instanceKey = null,
        Variant variant = default,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        string keySuffix = instanceKey == null ? string.Empty : "#" + instanceKey;
        string progressKey = file + "#sw_progress" + keySuffix;
        string lastTimeKey = file + "#sw_lastTime" + keySuffix;

        LightweaveNode node = NodeBuilder.New($"Switch:{label}", line, file);
        node.ApplyStyling("switch", style, classes, id);
        node.PreferredHeight = new Rem(1.75f).ToPixels();
        node.MeasureWidth = () => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Font labelFont = theme.GetFont(FontRole.Body);
            int labelPixelSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
            GUIStyle labelStyle = GuiStyleCache.GetOrCreate(labelFont, labelPixelSize);
            float labelWidth = labelStyle.CalcSize(new GUIContent(label)).x;
            float trackWidth = new Rem(2.25f).ToPixels();
            float gapPx = new Rem(0.5f).ToPixels();
            return trackWidth + gapPx + labelWidth;
        };

        node.Paint = (rect, paintChildren) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;

            float trackWidth = new Rem(2.25f).ToPixels();
            float trackHeight = new Rem(1.125f).ToPixels();
            float thumbInset = new Rem(0.1875f).ToPixels();
            float thumbHeight = trackHeight - thumbInset * 2f;
            float thumbWidth = new Rem(0.75f).ToPixels();
            float rowHeight = new Rem(1.75f).ToPixels();
            float gapPx = new Rem(0.5f).ToPixels();

            float rowY = rect.y;
            float trackY = rowY + (rowHeight - trackHeight) / 2f;

            float trackX = rtl ? rect.xMax - trackWidth : rect.x;
            Rect trackRect = new Rect(trackX, trackY, trackWidth, trackHeight);

            float labelX = rtl ? rect.x : trackX + trackWidth + gapPx;
            float labelWidth = rtl
                ? trackX - gapPx - rect.x
                : rect.xMax - labelX;
            Rect labelRect = new Rect(labelX, rowY, Mathf.Max(0f, labelWidth), rowHeight);

            Rect hitRect = new Rect(rect.x, rowY, rect.width, rowHeight);
            LightweaveHitTracker.Track(hitRect);

            Hooks.Hooks.RefHandle<float> progress = Hooks.Hooks.UseRef(value ? 1f : 0f, line, progressKey);
            Hooks.Hooks.RefHandle<float> lastTime = Hooks.Hooks.UseRef(
                Time.realtimeSinceStartup,
                line,
                lastTimeKey
            );

            float now = Time.realtimeSinceStartup;
            float dt = Mathf.Max(0f, now - lastTime.Current);
            lastTime.Current = now;

            float target = value ? 1f : 0f;
            if (!Mathf.Approximately(progress.Current, target)) {
                float delta = dt / AnimationDurationSec;
                if (target > progress.Current) {
                    progress.Current = Mathf.Min(target, progress.Current + delta);
                }
                else {
                    progress.Current = Mathf.Max(target, progress.Current - delta);
                }
            }

            float animFraction = Mathf.Clamp01(progress.Current);

            RenderContext rc = RenderContext.Current;
            bool effectiveDisabled = disabled || rc.ForceDisabled;
            bool mouseOver = Mouse.IsOver(hitRect);
            bool hovered = !effectiveDisabled && (mouseOver || rc.ForceHovered || rc.ForcePressed);
            bool focused = !effectiveDisabled && rc.ForceFocused;
            if (!effectiveDisabled) {
                MouseoverSounds.DoRegion(hitRect);
            }
            else if (mouseOver) {
                CursorOverrides.MarkDisabledHover();
            }

            ThemeSlot? trackBorderSlot;
            if (effectiveDisabled) {
                trackBorderSlot = ThemeSlot.BorderOff;
            }
            else if (focused) {
                trackBorderSlot = ThemeSlot.BorderFocus;
            }
            else if (value) {
                trackBorderSlot = null;
            }
            else if (hovered) {
                trackBorderSlot = ThemeSlot.BorderHover;
            }
            else {
                trackBorderSlot = ThemeSlot.BorderSubtle;
            }

            ThemeSlot trackFill = effectiveDisabled
                ? ThemeSlot.SurfaceDisabled
                : value
                    ? InputSurface.ResolveAccentFillSlot(variant)
                    : ThemeSlot.SurfaceInput;
            BackgroundSpec trackBg = BackgroundSpec.Of(trackFill);
            BorderSpec? trackBorder = trackBorderSlot.HasValue
                ? BorderSpec.All(new Rem(1f / 16f), trackBorderSlot.Value)
                : null;
            RadiusSpec trackRadius = RadiusSpec.All(RadiusScale.Pill);
            PaintBox.Draw(trackRect, trackBg, trackBorder, trackRadius);

            float leftX = trackRect.x + thumbInset;
            float rightX = trackRect.xMax - thumbInset - thumbWidth;
            float thumbX = rtl
                ? Mathf.Lerp(rightX, leftX, animFraction)
                : Mathf.Lerp(leftX, rightX, animFraction);
            float thumbY = trackRect.y + (trackHeight - thumbHeight) / 2f;
            Rect thumbRect = new Rect(thumbX, thumbY, thumbWidth, thumbHeight);

            ThemeSlot thumbSlot = effectiveDisabled
                ? ThemeSlot.BorderOff
                : value
                    ? InputSurface.ResolveOnAccentSlot(variant)
                    : ThemeSlot.TextMuted;
            BackgroundSpec thumbBg = BackgroundSpec.Of(thumbSlot);
            RadiusSpec thumbRadius = RadiusSpec.All(RadiusScale.Pill);
            PaintBox.Draw(thumbRect, thumbBg, null, thumbRadius);

            Font labelFont = theme.GetFont(FontRole.Body);
            int labelPixelSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
            GUIStyle labelStyle = GuiStyleCache.GetOrCreate(labelFont, labelPixelSize);
            labelStyle.alignment = rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            Color labelColor = effectiveDisabled
                ? theme.GetColor(ThemeSlot.TextMuted)
                : theme.GetColor(ThemeSlot.TextPrimary);
            TextDraw.DrawWithStyle(labelRect, label, labelStyle, labelColor);

            paintChildren();

            Event e = Event.current;
            if (!effectiveDisabled && e.type == EventType.MouseUp && e.button == 0 && hitRect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(hitRect)) {
                onChange?.Invoke(!value);
                RenderContext.Current.Hooks.Invalidate();
                e.Use();
            }
        };

        return node;
    }

    [DocVariant("CL_Playground_Label_On")]
    public static DocSample DocsOn() {
        StateHandle<bool> s = UseState(true);
        return new DocSample(() => Create(
            (string)"CL_Playground_Controls_Switch_Label".Translate(),
            s.Value,
            v => s.Set(v)
        ));
    }

    [DocVariant("CL_Playground_Label_Off")]
    public static DocSample DocsOff() {
        StateHandle<bool> s = UseState(false);
        return new DocSample(() => Create("Off", s.Value, v => s.Set(v)));
    }

    [DocVariant("CL_Playground_Label_Danger")]
    public static DocSample DocsDanger() {
        StateHandle<bool> s = UseState(true);
        return new DocSample(() => Create("Danger", s.Value, v => s.Set(v), variant: Variant.Danger));
    }

    private static LightweaveNode AllVariantsRow() {
        return HStack.Create(
            SpacingScale.Lg,
            row => {
                row.AddHug(Create("On", true, _ => { }, instanceKey: "sw_v_on"));
                row.AddHug(Create("Off", false, _ => { }, instanceKey: "sw_v_off"));
                row.AddHug(Create("Danger", true, _ => { }, instanceKey: "sw_v_danger", variant: Variant.Danger));
            }
        );
    }

    [DocState("CL_Playground_Label_Default", HideCode = true)]
    public static DocSample DocsDefault() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Hover", HideCode = true)]
    public static DocSample DocsHover() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Active", HideCode = true)]
    public static DocSample DocsActive() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Focus", HideCode = true)]
    public static DocSample DocsFocus() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Disabled", HideCode = true)]
    public static DocSample DocsDisabled() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        StateHandle<bool> s = UseState(true);
        return new DocSample(() => Create("Enabled", s.Value, v => s.Set(v)));
    }
}