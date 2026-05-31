using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "slider",
    Summary = "Continuous or stepped value selector along a horizontal track.",
    WhenToUse = "Pick a numeric value within a known range.",
    SourcePath = "Lightweave/Input/Slider.cs"
)]
public static class Slider {
    public static LightweaveNode Create(
        [DocParam("Current slider value.")] float value,
        [DocParam("Invoked with the new value. Fires on release by default; see `live`.")]
        Action<float> onChange,
        [DocParam("Minimum value.")] float min = 0f,
        [DocParam("Maximum value.")] float max = 1f,
        [DocParam("Snap interval. 0 disables stepping.")]
        float step = 0f,
        [DocParam("Optional list of tick marks rendered along the track.")]
        float[]? marks = null,
        [DocParam("Optional formatter used to render the value label.")]
        Func<float, string>? format = null,
        [DocParam("Disables interaction and applies disabled styling.")]
        bool disabled = false,
        [DocParam("When true, fires onChange during drag. When false (default), only on mouse-up.")]
        bool live = false,
        [DocParam("When `live` is true, minimum frames between onChange invocations during drag. Default 10.")]
        int liveThrottleFrames = 10,
        [DocParam("Minimum frames between label string re-formats during drag. Default 3 (~20Hz at 60fps).")]
        int labelThrottleFrames = 3,
        [DocParam("When true, renders a right-aligned mono-font readout column beside the track.")]
        bool readout = true,
        [DocParam("Readout column width. Defaults to 4rem.", TypeOverride = "Rem", DefaultOverride = "4")]
        Rem? readoutWidth = null,
        Variant variant = default,
        [DocParam("Optional accent slot override recoloring the filled track + thumb + glow. Defaults to the variant accent (gold).")]
        ThemeSlot? accent = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Slider", line, file);
        node.ApplyStyling("slider", style, classes, id);
        node.PreferredHeight = new Rem(1.25f).ToPixels();

        float readoutColumnPx = readout ? (readoutWidth ?? new Rem(4f)).ToPixels() : 0f;
        float readoutGapPx = readout ? SpacingScale.Md.ToPixels() : 0f;

        node.MeasureWidth = () => {
            float trackMin = new Rem(8f).ToPixels();
            return Mathf.Ceil(trackMin + readoutGapPx + readoutColumnPx);
        };

        node.Paint = (rect, paintChildren) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;

            RenderContext rc = RenderContext.Current;
            bool effectiveDisabled = disabled || rc.ForceDisabled;
            bool forcedHover = rc.ForceHovered || rc.ForcePressed;
            bool focused = !effectiveDisabled && rc.ForceFocused;

            RefHandle<bool> dragging = UseRef(false, line, file);
            RefHandle<float> draftValue = UseRef(value, line, file + "#draft");
            RefHandle<int> lastFireFrame = UseRef(int.MinValue, line, file + "#lastFire");
            RefHandle<string> cachedLabel = UseRef(string.Empty, line, file + "#labelCache");
            RefHandle<int> lastLabelFrame = UseRef(int.MinValue, line, file + "#labelFrame");
            RefHandle<float> lastLabelValue = UseRef(float.NaN, line, file + "#labelValue");

            float trackBandHeight = new Rem(1.25f).ToPixels();
            float trackThickness = new Rem(0.5f).ToPixels();
            float thumbSize = new Rem(1.0f).ToPixels();
            float tickWidth = new Rem(1f / 16f).ToPixels();
            float tickHeight = new Rem(0.75f).ToPixels();
            float edgePadding = thumbSize / 2f;

            float trackBandY = rect.y + Mathf.Max(0f, (rect.height - trackBandHeight) / 2f);

            Rect readoutRect;
            Rect trackBand;
            if (readout) {
                if (rtl) {
                    readoutRect = new Rect(rect.x, trackBandY, readoutColumnPx, trackBandHeight);
                    float trackX = rect.x + readoutColumnPx + readoutGapPx;
                    trackBand = new Rect(trackX, trackBandY, Mathf.Max(0f, rect.xMax - trackX), trackBandHeight);
                }
                else {
                    float readoutX = rect.xMax - readoutColumnPx;
                    readoutRect = new Rect(readoutX, trackBandY, readoutColumnPx, trackBandHeight);
                    float trackWidthPx = Mathf.Max(0f, readoutX - readoutGapPx - rect.x);
                    trackBand = new Rect(rect.x, trackBandY, trackWidthPx, trackBandHeight);
                }
            }
            else {
                readoutRect = Rect.zero;
                trackBand = new Rect(rect.x, trackBandY, rect.width, trackBandHeight);
            }

            float trackLeft = trackBand.x + edgePadding;
            float trackRight = trackBand.xMax - edgePadding;
            float trackWidth = Mathf.Max(0f, trackRight - trackLeft);
            float trackY = trackBand.y + (trackBand.height - trackThickness) / 2f;
            Rect trackRect = new Rect(trackLeft, trackY, trackWidth, trackThickness);
            LightweaveHitTracker.Track(trackBand);

            if (!effectiveDisabled && dragging.Current && trackWidth > 0f) {
                float computed = ComputeValue(Event.current.mousePosition.x, trackRect, min, max, step, rtl);
                if (!Mathf.Approximately(computed, draftValue.Current)) {
                    draftValue.Current = computed;
                }
            }

            float effectiveValue = dragging.Current ? draftValue.Current : value;
            float range = max - min;
            float clampedValue = Mathf.Clamp(effectiveValue, min, max);
            float logicalFraction = range > 0f ? (clampedValue - min) / range : 0f;
            float physicalFraction = rtl ? 1f - logicalFraction : logicalFraction;
            float thumbCenterX = trackRect.x + physicalFraction * trackRect.width;
            float thumbX = thumbCenterX - thumbSize / 2f;
            float thumbY = trackBand.y + (trackBand.height - thumbSize) / 2f;
            Rect thumbRect = new Rect(thumbX, thumbY, thumbSize, thumbSize);

            bool thumbHovered = !effectiveDisabled && (thumbRect.Contains(Event.current.mousePosition) || forcedHover);

            ThemeSlot accentSlot = InputSurface.ResolveAccentFillSlot(variant);
            if (accent.HasValue) {
                accentSlot = accent.Value;
            }
            ThemeSlot filledSlot = effectiveDisabled ? ThemeSlot.SurfaceDisabled : accentSlot;
            ThemeSlot unfilledSlot = effectiveDisabled ? ThemeSlot.SurfaceDisabled : ThemeSlot.SurfaceInput;
            RadiusSpec trackRadius = RadiusSpec.All(RadiusScale.Pill);
            BorderSpec trackBorder = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle);

            PaintBox.Draw(trackRect, BackgroundSpec.Of(unfilledSlot), trackBorder, trackRadius);

            if (rtl) {
                float fillWidth = Mathf.Max(0f, trackRect.xMax - thumbCenterX);
                if (fillWidth > 0f) {
                    Rect filledRect = new Rect(thumbCenterX, trackRect.y, fillWidth, trackRect.height);
                    PaintBox.Draw(filledRect, BackgroundSpec.Of(filledSlot), null, RadiusSpec.Right(RadiusScale.Pill));
                }
            } else {
                float fillWidth = Mathf.Max(0f, thumbCenterX - trackRect.x);
                if (fillWidth > 0f) {
                    Rect filledRect = new Rect(trackRect.x, trackRect.y, fillWidth, trackRect.height);
                    PaintBox.Draw(filledRect, BackgroundSpec.Of(filledSlot), null, RadiusSpec.Left(RadiusScale.Pill));
                }
            }

            if (marks != null && marks.Length > 0 && range > 0f) {
                ThemeSlot markSlot = effectiveDisabled ? ThemeSlot.BorderSubtle : ThemeSlot.BorderDefault;
                BackgroundSpec markBg = BackgroundSpec.Of(markSlot);
                float markY = trackBand.y + (trackBand.height - tickHeight) / 2f;
                for (int i = 0; i < marks.Length; i++) {
                    float markValue = Mathf.Clamp(marks[i], min, max);
                    float markLogical = (markValue - min) / range;
                    float markPhysical = rtl ? 1f - markLogical : markLogical;
                    float markX = trackRect.x + markPhysical * trackRect.width - tickWidth / 2f;
                    Rect markRect = new Rect(markX, markY, tickWidth, tickHeight);
                    PaintBox.Draw(markRect, markBg, null, null);
                }
            }

            RadiusSpec thumbRadius = RadiusSpec.All(RadiusScale.Pill);

            if ((thumbHovered || dragging.Current || focused) && !effectiveDisabled) {
                Rem glowRem = new Rem(1.0f + 0.625f);
                float glowSize = glowRem.ToPixels();
                Rect glowRect = new Rect(
                    thumbRect.center.x - glowSize / 2f,
                    thumbRect.center.y - glowSize / 2f,
                    glowSize,
                    glowSize
                );
                Color glow = theme.GetColor(accentSlot);
                glow.a *= 0.28f;
                RadiusSpec glowRadius = RadiusSpec.All(glowRem * 0.5f);
                PaintBox.Draw(glowRect, BackgroundSpec.Of(glow), null, glowRadius);
            }

            ThemeSlot thumbFillSlot = effectiveDisabled
                ? ThemeSlot.SurfaceDisabled
                : accentSlot;
            BackgroundSpec thumbBg = BackgroundSpec.Of(thumbFillSlot);
            BorderSpec? thumbBorder = focused
                ? BorderSpec.All(new Rem(2f / 16f), ThemeSlot.BorderFocus)
                : null;
            PaintBox.Draw(thumbRect, thumbBg, thumbBorder, thumbRadius);

            if (readout && Event.current.type == EventType.Repaint) {
                int currentFrame = Time.frameCount;
                int labelThrottle = Mathf.Max(0, labelThrottleFrames);
                bool valueChanged = !Mathf.Approximately(lastLabelValue.Current, clampedValue);
                bool throttleElapsed = currentFrame - lastLabelFrame.Current >= labelThrottle;
                bool stale = string.IsNullOrEmpty(cachedLabel.Current);
                if (stale || valueChanged && (!dragging.Current || throttleElapsed)) {
                    cachedLabel.Current = format != null
                        ? format(clampedValue)
                        : clampedValue.ToString("0.00", CultureInfo.InvariantCulture);
                    lastLabelValue.Current = clampedValue;
                    lastLabelFrame.Current = currentFrame;
                }

                Font readoutFont = theme.GetFont(FontRole.Mono);
                int readoutPixelSize = Mathf.RoundToInt(new Rem(0.78f).ToFontPx());
                GUIStyle readoutStyle = GuiStyleCache.GetOrCreate(readoutFont, readoutPixelSize, FontStyle.Normal);
                readoutStyle.alignment = rtl ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                Color readoutColor = effectiveDisabled
                    ? theme.GetColor(ThemeSlot.TextMuted)
                    : theme.GetColor(ThemeSlot.TextPrimary);
                TextDraw.DrawWithStyle(readoutRect, cachedLabel.Current, readoutStyle, readoutColor);
            }

            paintChildren();

            if (effectiveDisabled) {
                return;
            }

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && trackBand.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(trackBand)) {
                dragging.Current = true;
                float computed = ComputeValue(e.mousePosition.x, trackRect, min, max, step, rtl);
                draftValue.Current = computed;
                if (live && !Mathf.Approximately(computed, value)) {
                    onChange?.Invoke(computed);
                    RenderContext.Current.Hooks.Invalidate();
                    lastFireFrame.Current = Time.frameCount;
                }

                e.Use();
            } else if ((e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && dragging.Current) {
                dragging.Current = false;
                float final = draftValue.Current;
                if (!Mathf.Approximately(final, value)) {
                    onChange?.Invoke(final);
                    RenderContext.Current.Hooks.Invalidate();
                    lastFireFrame.Current = Time.frameCount;
                }

                if (e.type == EventType.MouseUp) {
                    e.Use();
                }
            } else if (e.type == EventType.MouseDrag && dragging.Current) {
                if (live && !Mathf.Approximately(draftValue.Current, value)) {
                    int frame = Time.frameCount;
                    int throttle = Mathf.Max(0, liveThrottleFrames);
                    if (frame - lastFireFrame.Current >= throttle) {
                        onChange?.Invoke(draftValue.Current);
                        RenderContext.Current.Hooks.Invalidate();
                        lastFireFrame.Current = frame;
                    }
                }

                e.Use();
            }
        };

        return node;
    }

    private static float ComputeValue(
        float mouseX,
        Rect trackRect,
        float min,
        float max,
        float step,
        bool rtl
    ) {
        if (trackRect.width <= 0f) {
            return min;
        }

        float localX = mouseX - trackRect.x;
        float fraction = Mathf.Clamp01(localX / trackRect.width);
        if (rtl) {
            fraction = 1f - fraction;
        }

        float newValue = Mathf.Lerp(min, max, fraction);
        if (step > 0f) {
            newValue = min + Mathf.Round((newValue - min) / step) * step;
            newValue = Mathf.Clamp(newValue, min, max);
        }

        return newValue;
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        StateHandle<float> s = UseState(0.4f);
        return new DocSample(() => Create(s.Value, v => s.Set(v)));
    }

    [DocVariant("CL_Playground_Label_Accented")]
    public static DocSample DocsAccented() {
        StateHandle<float> s = UseState(0.4f);
        return new DocSample(() => Create(
                s.Value,
                v => s.Set(v),
                0f,
                1f,
                0.25f,
                new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }
            )
        );
    }

    [DocVariant("CL_Playground_Label_Danger")]
    public static DocSample DocsDanger() {
        StateHandle<float> s = UseState(0.4f);
        return new DocSample(() => Create(s.Value, v => s.Set(v), variant: Variant.Danger));
    }

    [DocVariant("CL_Playground_Label_AnomalyAccent")]
    public static DocSample DocsAnomalyAccent() {
        StateHandle<float> s = UseState(0.4f);
        return new DocSample(() => Create(
                s.Value,
                v => s.Set(v),
                0f,
                1f,
                accent: ThemeSlot.AnomalyAccent
            )
        );
    }

    private static LightweaveNode AllVariantsRow() {
        return Stack.Create(
            SpacingScale.Md,
            col => {
                col.Add(Create(0.4f, _ => { }));
                col.Add(Create(0.6f, _ => { }, 0f, 1f, 0.25f, new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }));
                col.Add(Create(0.5f, _ => { }, variant: Variant.Danger));
            }
        );
    }

    [DocState("CL_Playground_Label_Default", HideCode = true)]
    public static DocSample DocsDefaultState() {
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
        StateHandle<float> s = UseState(0.5f);
        return new DocSample(() => Create(s.Value, v => s.Set(v)));
    }
}