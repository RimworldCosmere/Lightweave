using System;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Slider = Cosmere.Lightweave.Input.Slider;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class NewColonyControls {
    public static LightweaveNode LabeledSlider(
        string label,
        string value,
        float current,
        Action<float> onChange,
        float min,
        float max,
        float step
    ) {
        return Stack.Create(SpacingScale.Xs, s => {
            s.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddFlex(Eyebrow.Create(label));
                h.AddHug(Text.Create(value, style: new Style {
                    FontFamily = FontRole.Mono,
                    TextColor = ThemeSlot.TextPrimary,
                }));
            }, style: new Style { Width = Length.Stretch }));
            s.Add(Slider.Create(
                value: current,
                onChange: onChange,
                min: min,
                max: max,
                step: step,
                readout: false,
                live: true,
                style: new Style { Width = Length.Stretch }
            ));
        });
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
