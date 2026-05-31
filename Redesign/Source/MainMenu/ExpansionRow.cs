using System.Collections.Generic;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;
using static Cosmere.Lightweave.Typography.Typography;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Data;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class ExpansionRow {
    public static LightweaveNode? Create() {
        List<ExpansionDef> all = ModLister.AllExpansions;
        List<ExpansionDef> visible = new List<ExpansionDef>();
        for (int i = 0; i < all.Count; i++) {
            if (!all[i].isCore) {
                visible.Add(all[i]);
            }
        }
        if (visible.Count == 0) {
            return null;
        }

        LightweaveNode rowBox = Box.Create(
            children: c => c.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddHug(Eyebrow.Create(
                    "CL_MainMenu_ExpansionsActive".Translate(),
                    style: new Style {
                        TextColor = ThemeSlot.SurfaceAccent,
                        TextAlign = TextAlign.Left,
                        LetterSpacing = Tracking.Widest,
                        FontSize = new Rem(0.625f),
                        FontFamily = FontRole.Mono,
                    }
                ));
                h.AddHug(Divider.Vertical());
                for (int i = 0; i < visible.Count; i++) {
                    h.AddHug(BuildItem(visible[i]));
                }
            })),
            style: new Style {
                Padding = new EdgeInsets(SpacingScale.Xs, SpacingScale.Md, SpacingScale.Xs, SpacingScale.Md),
                Background = BackgroundSpec.Blur(ThemeSlot.ExpansionBarSurface, 8f),
                Border = BorderSpec.All(new Rem(0.0625f), ThemeSlot.AccentGlow),
                Radius = RadiusSpec.All(RadiusScale.Pill),
            }
        );

        return HStack.Create(
            SpacingScale.None,
            h => {
                h.AddFlex(Spacer.Flex());
                h.AddHug(rowBox);
                h.AddFlex(Spacer.Flex());
            }
        );
    }

    private static LightweaveNode BuildItem(ExpansionDef expansion) {
        ExpansionStatus status = expansion.Status;
        bool active = status == ExpansionStatus.Active;

        Theme.Theme theme = RenderContext.Current.Theme;
        Color ink = theme.GetColor(ThemeSlot.TextPrimary);
        Color labelColor = active ? ink : new Color(ink.r, ink.g, ink.b, 0.55f);
        Color accent = theme.GetColor(ThemeSlot.SurfaceAccent);
        Color pillBorder = new Color(accent.r, accent.g, accent.b, 0.55f);

        Texture2D? icon = expansion.IconFromStatus;
        string label = expansion.LabelCap.ToString().ToUpperInvariant();
        string storeUrl = ResolveStoreUrl(expansion);
        string tooltip = (active
            ? "CL_MainMenu_Expansion_VisitStore_Active"
            : "CL_MainMenu_Expansion_VisitStore_Buy").Translate(expansion.LabelCap.Named("LABEL")).Resolve();

        LightweaveNode inner = Box.Create(
            children: c => c.Add(HStack.Create(SpacingScale.Xs, h => {
                if (icon != null) {
                    h.AddHug(Icon.Create(icon, size: new Rem(1.25f)));
                }
                h.AddHug(Eyebrow.Create(label, style: new Style {
                    TextColor = (ColorRef)labelColor,
                    TextAlign = TextAlign.Left,
                    LetterSpacing = Tracking.Widest,
                    FontSize = new Rem(0.75f),
                    FontFamily = FontRole.Mono,
                }));
            })),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Xxs, Right: SpacingScale.Sm, Bottom: SpacingScale.Xxs, Left: SpacingScale.Sm),
                Background = BackgroundSpec.Of(ThemeSlot.ExpansionPillSurface),
                Border = BorderSpec.All(new Rem(0.0625f), (ColorRef)pillBorder),
                Radius = RadiusSpec.All(RadiusScale.Pill),
            }
        );

        if (string.IsNullOrEmpty(storeUrl)) {
            return inner;
        }

        LightweaveNode wrapper = NodeBuilder.New("ExpansionLink");
        wrapper.Children.Add(inner);
        wrapper.Measure = w => inner.Measure?.Invoke(w) ?? inner.PreferredHeight ?? 0f;
        wrapper.MeasureWidth = () => inner.MeasureWidth?.Invoke() ?? 0f;
        wrapper.Paint = (rect, paintChildren) => {
            inner.MeasuredRect = rect;
            paintChildren();

            InteractionState state = InteractionState.Resolve(rect, null, false);
            if (state.Hovered || state.Pressed) {
                Theme.Theme theme = RenderContext.Current.Theme;
                float overlayAlpha = state.Pressed ? 0.18f : 0.10f;
                Color overlay = InteractionFeedback.OverlayColor(theme, state, overlayAlpha);
                PaintBox.Draw(rect, BackgroundSpec.Of(overlay), null, RadiusSpec.All(RadiusScale.Pill));
                Color borderColor = theme.GetColor(ThemeSlot.SurfaceAccent);
                PaintBox.Draw(rect, null, BorderSpec.All(new Rem(0.0625f), borderColor), RadiusSpec.All(RadiusScale.Pill));
            }

            TooltipHandler.TipRegion(rect, tooltip);
            InteractionFeedback.Apply(rect, true, true);

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                Application.OpenURL(storeUrl);
                e.Use();
            }
        };
        return wrapper;
    }

    private static string ResolveStoreUrl(ExpansionDef expansion) {
        (string appId, string slug)? data = expansion.defName switch {
            "Royalty" => ("1149640", "royalty"),
            "Ideology" => ("1392840", "ideology"),
            "Biotech" => ("1826140", "biotech"),
            "Anomaly" => ("2380740", "anomaly"),
            "Odyssey" => ("3022790", "odyssey"),
            _ => null,
        };
        if (data.HasValue) {
            if (SteamManager.Initialized) {
                return $"https://store.steampowered.com/app/{data.Value.appId}/";
            }
            return $"https://rimworldgame.com/{data.Value.slug}/";
        }
        return expansion.StoreURL ?? string.Empty;
    }

    

    
}
