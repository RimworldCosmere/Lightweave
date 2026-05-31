using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Redesign.Options.Tabs;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using RimWorld;
using UnityEngine;
using Verse;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Data;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;

namespace Cosmere.Lightweave.Redesign.Options;

public static class OptionsRoot {
    public static LightweaveNode Build(Dialog_Options dialog, Action onClose) {
        Hooks.Hooks.StateHandle<OptionsTab> tab = Hooks.Hooks.UseState(OptionsTab.General);

        return Stack.Create(SpacingScale.None, root => {
            root.Add(WindowHeader.Create(
                title: "CL_Options_Title".Translate(),
                actions: Button.Create(
                    (string)"CL_Options_ResetCategory".Translate(),
                    null,
                    Variant.Ghost
                ),
                onClose: () => {
                    Prefs.Save();
                    onClose?.Invoke();
                },
                drawDivider: true
            ));
            root.AddFlex(HStack.Create(SpacingScale.None, h => {
                h.Add(BuildSidebar(tab), new Rem(15f).ToPixels());
                h.AddFlex(BuildContent(tab.Value, dialog));
            }));
        });
    }

    private static LightweaveNode BuildSidebar(Hooks.Hooks.StateHandle<OptionsTab> tab) {
        return Box.Create(
            children: c => c.Add(Stack.Create(SpacingScale.None, s => {
                AppendTab(s, tab, OptionsTab.General, "CL_Options_Tab_General");
                AppendTab(s, tab, OptionsTab.Gameplay, "CL_Options_Tab_Gameplay");
                AppendTab(s, tab, OptionsTab.Graphics, "CL_Options_Tab_Graphics");
                AppendTab(s, tab, OptionsTab.Audio, "CL_Options_Tab_Audio");
                AppendTab(s, tab, OptionsTab.Controls, "CL_Options_Tab_Controls");
                AppendTab(s, tab, OptionsTab.Interface, "CL_Options_Tab_Interface");
                if (Prefs.DevMode) {
                    AppendTab(s, tab, OptionsTab.Developer, "CL_Options_Tab_Developer");
                }
                s.Add(Box.Create(
                    children: c => c.Add(Divider.Horizontal()),
                    style: new Style {
                        Padding = new EdgeInsets(Top: SpacingScale.Sm, Bottom: SpacingScale.Sm, Left: SpacingScale.Md, Right: SpacingScale.Md),
                    }
                ));
                AppendTab(s, tab, OptionsTab.ModSettings, "CL_Options_Tab_ModSettings");
            })),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.None, Bottom: SpacingScale.Md, Left: SpacingScale.None, Right: SpacingScale.None),
                Border = new BorderSpec(Right: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static void AppendTab(
        StackBuilder s,
        Hooks.Hooks.StateHandle<OptionsTab> selection,
        OptionsTab tab,
        string labelKey
    ) {
        bool isActive = selection.Value == tab;
        string label = labelKey.Translate();
        LightweaveNode node = NodeBuilder.New("OptionsTab:" + tab);
        node.PreferredHeight = new Rem(2.8f).ToPixels();
        node.Paint = (rect, _) => {
            InteractionState state = InteractionState.Resolve(rect, null, false);

            if (isActive) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.ActiveTint), null, null);
            }
            else if (state.Hovered) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.HoverTint), null, null);
            }

            if (isActive) {
                float stripeW = Spacing.StripeWidth.ToPixels();
                Rect stripe = new Rect(rect.x, rect.y, stripeW, rect.height);
                PaintBox.Draw(stripe, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
            }

            float padX = SpacingScale.Md.ToPixels();
            Rect labelRect = new Rect(rect.x + padX, rect.y, rect.width - padX * 2f, rect.height);
            TextDraw.Draw(
                labelRect,
                label,
                FontRole.Body,
                new Rem(0.95f),
                TextAnchor.MiddleLeft,
                isActive ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary
            );

            InteractionFeedback.Apply(rect, true, true);

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                selection.Set(tab);
                e.Use();
            }
        };
        s.Add(node);
    }

    private static LightweaveNode BuildContent(OptionsTab tab, Dialog_Options dialog) {
        if (tab == OptionsTab.ModSettings) {
            return ModSettingsTab.Build();
        }

        LightweaveNode body = tab switch {
            OptionsTab.General => GeneralTab.Build(),
            OptionsTab.Graphics => GraphicsTab.Build(),
            OptionsTab.Audio => AudioTab.Build(),
            OptionsTab.Gameplay => GameplayTab.Build(),
            OptionsTab.Interface => InterfaceTab.Build(),
            OptionsTab.Controls => ControlsTab.Build(),
            OptionsTab.Developer => DeveloperTab.Build(),
            _ => GeneralTab.Build(),
        };

        LightweaveNode paddedBody = Box.Create(
            children: c => c.Add(body),
            style: new Style {
                Padding = new EdgeInsets(Right: new Rem(0.75f)),
            }
        );

        return Box.Create(
            children: c => c.Add(ScrollArea.Create(content: paddedBody)),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Lg),
            }
        );
    }
}
