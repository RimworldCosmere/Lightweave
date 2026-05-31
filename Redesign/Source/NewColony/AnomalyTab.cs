using System.Collections.Generic;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Button = Cosmere.Lightweave.Input.Button;
using Glyph = Cosmere.Lightweave.Typography.Glyph;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class AnomalyTab {
    public static LightweaveNode Build(Hooks.Hooks.StateHandle<AnomalyParams> anomaly) {
        List<AnomalyPlaystyleDef> playstyles = NewColonyData.AnomalyPlaystyles();

        return ScrollArea.Create(HStack.Create(SpacingScale.Xl, h => {
            h.AddFlex(BuildIntegration(playstyles, anomaly));
            h.AddFlex(BuildThreats(anomaly));
        }, style: new Style { Width = Length.Stretch }), style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildIntegration(
        List<AnomalyPlaystyleDef> playstyles,
        Hooks.Hooks.StateHandle<AnomalyParams> anomaly
    ) {
        return Stack.Create(SpacingScale.Md, s => {
            s.Add(NewColonyControls.SectionLabel("CL_NewColony_Anomaly_Integration".Translate()));
            foreach (AnomalyPlaystyleDef def in playstyles) {
                s.Add(BuildModeRow(def, anomaly));
            }
        });
    }

    private static LightweaveNode BuildModeRow(AnomalyPlaystyleDef def, Hooks.Hooks.StateHandle<AnomalyParams> anomaly) {
        string name = def.defName;
        bool isSelected = name == anomaly.Value.PlaystyleDefName;

        LightweaveNode indicator = Glyph.Create(
            isSelected ? Icons.Phosphor.RadioButton : Icons.Phosphor.Circle,
            style: new Style {
                FontSize = new Rem(1.25f),
                TextColor = isSelected ? ThemeSlot.AnomalyAccent : ThemeSlot.TextMuted,
            });

        LightweaveNode textCol = Stack.Create(SpacingScale.Xxs, c => {
            c.Add(Text.Create(def.LabelCap.ToString(), style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.2f),
                LetterSpacing = Tracking.Of(0.03f),
                TextColor = isSelected ? ThemeSlot.AnomalyAccentText : ThemeSlot.TextPrimary,
            }));
            if (!def.description.NullOrEmpty()) {
                c.Add(Text.Create(def.description, wrap: true, style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.7f),
                    LetterSpacing = Tracking.Of(0.04f),
                    TextColor = ThemeSlot.TextMuted,
                }));
            }
        });

        LightweaveNode child = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(indicator);
            h.AddFlex(textCol);
        }, style: new Style { Width = Length.Stretch });

        return SelectableSurface.Create(
            child: child,
            selected: isSelected,
            variant: SelectableSurfaceVariant.ListRow,
            accent: ThemeSlot.AnomalyAccent,
            trailingCaret: false,
            onSelect: () => {
                AnomalyParams next = anomaly.Value;
                next.PlaystyleDefName = name;
                anomaly.Set(next);
            },
            style: new Style { Width = Length.Stretch }
        );
    }

    private static LightweaveNode BuildThreats(Hooks.Hooks.StateHandle<AnomalyParams> anomaly) {
        return Stack.Create(SpacingScale.Lg, s => {
            s.Add(NewColonyControls.SectionLabel("CL_NewColony_Anomaly_ThreatsHeading".Translate()));
            s.Add(Text.Create("CL_NewColony_Anomaly_ThreatsCaption".Translate(), wrap: true, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.72f),
                LetterSpacing = Tracking.Of(0.05f),
                TextColor = ThemeSlot.TextMuted,
            }));
            s.Add(NewColonyControls.LabeledSlider(
                "CL_NewColony_Anomaly_ThreatsInactive".Translate() + " · " + NewColonyFormat.AnomalyIntensityLabel(anomaly.Value.ThreatsInactive),
                Percent(anomaly.Value.ThreatsInactive),
                anomaly.Value.ThreatsInactive,
                v => {
                    AnomalyParams next = anomaly.Value;
                    next.ThreatsInactive = v;
                    anomaly.Set(next);
                },
                0f, 1f, 0.01f
            ));
            s.Add(NewColonyControls.LabeledSlider(
                "CL_NewColony_Anomaly_ThreatsActive".Translate() + " · " + NewColonyFormat.AnomalyIntensityLabel(anomaly.Value.ThreatsActive),
                Percent(anomaly.Value.ThreatsActive),
                anomaly.Value.ThreatsActive,
                v => {
                    AnomalyParams next = anomaly.Value;
                    next.ThreatsActive = v;
                    anomaly.Set(next);
                },
                0f, 1f, 0.01f
            ));
            s.Add(NewColonyControls.LabeledSlider(
                "CL_NewColony_Anomaly_StudyEfficiency".Translate(),
                Percent(anomaly.Value.StudyEfficiency),
                anomaly.Value.StudyEfficiency,
                v => {
                    AnomalyParams next = anomaly.Value;
                    next.StudyEfficiency = v;
                    anomaly.Set(next);
                },
                0.5f, 2f, 0.05f
            ));
            s.Add(Button.Create(
                "CL_NewColony_Anomaly_Reset".Translate(),
                () => anomaly.Set(AnomalyParams.Default()),
                Variant.Secondary,
                leading: Glyph.Create(Icons.Phosphor.ArrowCounterClockwise, style: new Style {
                    TextColor = ThemeSlot.TextSecondary,
                })
            ));
        });
    }

    private static string Percent(float v) {
        return Mathf.RoundToInt(v * 100f) + "%";
    }
}
