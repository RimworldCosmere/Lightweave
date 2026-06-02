using System;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Slider = Cosmere.Lightweave.Input.Slider;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class NewColonyControls {
    public static LightweaveNode LabeledSlider(
        string label,
        Func<float, string> format,
        float current,
        Action<float> onChange,
        float min,
        float max,
        float step,
        string key
    ) {
        // The value readout is drawn by the Slider itself in its header row (label left, value
        // right) above the track. The Slider reads the live drag draft every frame in Paint, so the
        // header value tracks the thumb without depending on a tree rebuild - committing onChange is
        // throttled (default 10 frames) purely to limit world-state churn, and no longer drives the
        // displayed value. Earlier this header was an external Text baked at build time, which is why
        // the label looked frozen/jumpy mid-drag.
        return Slider.Create(
            value: current,
            onChange: onChange,
            min: min,
            max: max,
            step: step,
            format: format,
            label: label,
            live: true,
            style: new Style { Width = Length.Stretch },
            // Every LabeledSlider routes through this one Slider.Create call site, so without a
            // per-instance discriminator two sliders built from this helper would share hook
            // identity ((line, file) is the hook key) and thus the same drag/draft state. Fold
            // the caller's key into `file` so each slider gets its own hook slots.
            file: "NewColonyControls.LabeledSlider#" + key
        );
    }

    public static LightweaveNode SectionLabel(string text, ColorRef? accent = null, bool trailingRule = true) {
        return Eyebrow.Create(
            text,
            accent: accent ?? (ColorRef)ThemeSlot.SurfaceAccent,
            trailingRule: trailingRule,
            style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(11f / 16f),
                LetterSpacing = Tracking.Of(0.28f),
            }
        );
    }
}
