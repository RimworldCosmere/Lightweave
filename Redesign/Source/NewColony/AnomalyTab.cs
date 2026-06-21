using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
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
                Percent,
                anomaly.Value.ThreatsInactive,
                v => {
                    AnomalyParams next = anomaly.Value;
                    next.ThreatsInactive = v;
                    anomaly.Set(next);
                },
                0f, 1f, 0.01f,
                key: "anomaly-threats-inactive",
                tooltip: "Difficulty_AnomalyThreatsInactive_Info".Translate()
            ));
            s.Add(NewColonyControls.LabeledSlider(
                "CL_NewColony_Anomaly_ThreatsActive".Translate() + " · " + NewColonyFormat.AnomalyIntensityLabel(anomaly.Value.ThreatsActive),
                Percent,
                anomaly.Value.ThreatsActive,
                v => {
                    AnomalyParams next = anomaly.Value;
                    next.ThreatsActive = v;
                    anomaly.Set(next);
                },
                0f, 1f, 0.01f,
                key: "anomaly-threats-active",
                tooltip: "Difficulty_AnomalyThreatsActive_Info".Translate(
                    Mathf.Clamp01(anomaly.Value.ThreatsActive).ToStringPercent(),
                    Mathf.Clamp01(anomaly.Value.ThreatsActive * 1.5f).ToStringPercent())
            ));
            s.Add(NewColonyControls.LabeledSlider(
                "CL_NewColony_Anomaly_StudyEfficiency".Translate(),
                Percent,
                anomaly.Value.StudyEfficiency,
                v => {
                    AnomalyParams next = anomaly.Value;
                    next.StudyEfficiency = v;
                    anomaly.Set(next);
                },
                0.5f, 2f, 0.05f,
                key: "anomaly-study-efficiency",
                tooltip: "Difficulty_StudyEfficiency_Info".Translate()
            ));
            s.Add(BuildStandardPlaystylePicker(anomaly));
        });
    }


    private static LightweaveNode BuildStandardPlaystylePicker(
        Hooks.Hooks.StateHandle<AnomalyParams> anomaly,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Hooks.Hooks.StateHandle<bool> open = Hooks.Hooks.UseState(false, line, file);
        Hooks.Hooks.StateHandle<Rect> anchor = Hooks.Hooks.UseState(Rect.zero, line + 1, file);

        LightweaveNode node = NodeBuilder.New("AnomalyPlaystylePicker", line, file);
        node.ApplyStyling("anomaly-playstyle-picker", null, null, null);

        LightweaveNode trigger = Button.Create(
            "CL_NewColony_Anomaly_SetStandard".Translate(),
            () => open.Set(!open.Value),
            Variant.Secondary,
            trailing: Glyph.Create(
                open.Value ? Icons.Phosphor.CaretUp : Icons.Phosphor.CaretDown,
                style: new Style {
                    FontSize = new Rem(0.6875f),
                    TextColor = ThemeSlot.TextSecondary,
                }));

        LightweaveNode menu = Menu.Create(
            isOpen: open.Value,
            anchorRect: anchor.Value,
            items: BuildStandardPlaystyleItems(anomaly, () => open.Set(false)),
            onDismiss: () => open.Set(false),
            anchor: MenuAnchor.Left,
            direction: MenuDirection.Up,
            instanceKey: "anomaly-standard-playstyle-menu",
            size: new Vector2(anchor.Value.width, -1f));

        node.Children.Add(trigger);
        node.Children.Add(menu);

        node.MeasureWidth = () => trigger.MeasureWidth?.Invoke() ?? 0f;
        node.Measure = availableWidth => trigger.Measure?.Invoke(availableWidth) ?? trigger.PreferredHeight ?? 0f;

        node.Paint = (rect, _) => {
            if (anchor.Value != rect) {
                anchor.Set(rect);
            }
            trigger.MeasuredRect = rect;
            menu.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(trigger, rect);
            LightweaveRoot.PaintSubtree(menu, rect);
        };

        return node;
    }

    private static IReadOnlyList<MenuEntry> BuildStandardPlaystyleItems(
        Hooks.Hooks.StateHandle<AnomalyParams> anomaly,
        Action onDismiss
    ) {
        string standardName = AnomalyPlaystyleDefOf.Standard.defName;
        List<MenuEntry> items = [];
        foreach (DifficultyDef def in DefDatabase<DifficultyDef>.AllDefs) {
            if (def.isCustom) {
                continue;
            }
            DifficultyDef captured = def;
            items.Add(MenuEntry.Of(
                def.LabelCap,
                () => {
                    AnomalyParams next = anomaly.Value;
                    next.PlaystyleDefName = standardName;
                    next.ThreatsInactive = captured.anomalyThreatsInactiveFraction;
                    next.ThreatsActive = captured.anomalyThreatsActiveFraction;
                    next.StudyEfficiency = Mathf.Clamp(captured.studyEfficiencyFactor, 0.5f, 2f);
                    anomaly.Set(next);
                    onDismiss();
                }));
        }
        return items;
    }

    private static string Percent(float v) {
        return Mathf.RoundToInt(v * 100f) + "%";
    }
}
