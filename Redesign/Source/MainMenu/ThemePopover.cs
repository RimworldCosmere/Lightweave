using System;
using System.Collections.Generic;
using System.Linq;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Data;
using TypoText = Cosmere.Lightweave.Typography.Typography.Text;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class ThemePopover {
    private static readonly Rem RowHeight = new Rem(3.5f);

    private static readonly ThemeSlot[] SwatchSlots = {
        ThemeSlot.SurfacePrimary,
        ThemeSlot.SurfaceAccent,
        ThemeSlot.StatusSuccess,
        ThemeSlot.BorderFocus,
    };

    public static LightweaveNode Create(Action onDismiss) {
        IReadOnlyList<Theme.ThemeDescriptor> all = Theme.ThemeRegistry.All;
        LightweaveSettings settings = LightweaveMod.Settings;
        string activeId = settings?.SelectedThemeId ?? Theme.ThemeRegistry.DefaultId;

        float maxHeightPx = new Rem(22f).ToPixels();

        LightweaveNode header = WrapHorizontalPad(BuildHeader(all.Count));
        LightweaveNode divider = Divider.Horizontal();
        LightweaveNode listOrEmpty;
        if (all.Count == 0) {
            listOrEmpty = BuildEmpty();
        }
        else {
            listOrEmpty = ScrollArea.Create(
                BuildList(all, activeId, onDismiss),
                variant: ScrollAreaVariant.Auto
            );
        }

        LightweaveNode stack = Stack.Create(SpacingScale.None, s => {
            s.Add(header);
            s.Add(BuildSpacer(SpacingScale.Sm));
            s.Add(divider);
            s.AddFlex(listOrEmpty);
        });

        LightweaveNode box = Box.Create(
            children: c => c.Add(stack),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Md, Bottom: SpacingScale.Md, Left: SpacingScale.None, Right: SpacingScale.None),
                Background = BackgroundSpec.Of(ThemeSlot.Glass3),
                Border = BorderSpec.All(new Rem(0.0625f), ThemeSlot.BorderSubtle),
                Radius = RadiusSpec.All(RadiusScale.Lg),
            }
        );

        float fixedHeight = SpacingScale.Md.ToPixels() * 2f
            + (header.Measure?.Invoke(0f) ?? header.PreferredHeight ?? new Rem(1.95f).ToPixels())
            + SpacingScale.Sm.ToPixels()
            + (divider.PreferredHeight ?? new Rem(1f / 16f).ToPixels());

        float rowH = RowHeight.ToPixels();
        float listNatural = all.Count == 0
            ? new Rem(3f).ToPixels()
            : all.Count * rowH;

        float total = fixedHeight + listNatural;
        float capped = Mathf.Min(total, maxHeightPx);

        box.PreferredHeight = capped;
        box.Measure = _ => capped;

        return box;
    }

    private static LightweaveNode BuildList(IReadOnlyList<Theme.ThemeDescriptor> themes, string activeId, Action onDismiss) {
        return Stack.Create(SpacingScale.None, s => {
            for (int i = 0; i < themes.Count; i++) {
                Theme.ThemeDescriptor desc = themes[i];
                bool isSelected = string.Equals(desc.Id, activeId, StringComparison.Ordinal);
                s.Add(BuildThemeRow(desc, isSelected, () => {
                    LightweaveSettings settings = LightweaveMod.Settings;
                    if (settings != null && !string.Equals(settings.SelectedThemeId, desc.Id, StringComparison.Ordinal)) {
                        settings.SelectedThemeId = desc.Id;
                        LightweaveMod.Save();
                    }
                    onDismiss?.Invoke();
                }));
            }
        });
    }

    private static LightweaveNode BuildThemeRow(Theme.ThemeDescriptor desc, bool isSelected, Action onClick) {
        string label = (string)desc.LabelKey.Translate();
        string? subtitle = !string.IsNullOrEmpty(desc.SubLabelKey)
            ? (string)desc.SubLabelKey!.Translate()
            : null;

        Theme.Theme previewTheme = Theme.ThemeRegistry.Get(desc.Id);
        Color[] swatches = new Color[SwatchSlots.Length];
        for (int i = 0; i < SwatchSlots.Length; i++) {
            swatches[i] = previewTheme.GetColor(SwatchSlots[i]);
        }

        LightweaveNode node = NodeBuilder.New("ThemeRow:" + desc.Id);
        node.PreferredHeight = RowHeight.ToPixels();
        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;

            InteractionState state = InteractionState.Resolve(rect, null, false);
            bool hovered = state.Hovered;

            if (isSelected) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.AccentSoft), null, null);
            }
            else if (hovered) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.HoverTint), null, null);
            }

            float padX = new Rem(1f).ToPixels();
            float checkW = new Rem(1.5f).ToPixels();
            float gap = new Rem(0.625f).ToPixels();
            float swatchSize = new Rem(0.75f).ToPixels();
            float swatchGap = new Rem(0.125f).ToPixels();
            float swatchStripW = swatchSize * SwatchSlots.Length + swatchGap * (SwatchSlots.Length - 1);

            Rect swatchRect;
            Rect checkRect;
            Rect labelBlock;
            if (rtl) {
                checkRect = new Rect(rect.x + padX, rect.y, checkW, rect.height);
                swatchRect = new Rect(rect.xMax - padX - swatchStripW, rect.y, swatchStripW, rect.height);
                float labelLeft = checkRect.xMax + gap;
                float labelRight = swatchRect.x - gap;
                labelBlock = new Rect(labelLeft, rect.y, Mathf.Max(0f, labelRight - labelLeft), rect.height);
            }
            else {
                swatchRect = new Rect(rect.x + padX, rect.y, swatchStripW, rect.height);
                checkRect = new Rect(rect.xMax - padX - checkW, rect.y, checkW, rect.height);
                float labelLeft = swatchRect.xMax + gap;
                float labelRight = checkRect.x - gap;
                labelBlock = new Rect(labelLeft, rect.y, Mathf.Max(0f, labelRight - labelLeft), rect.height);
            }

            float swatchY = swatchRect.y + (swatchRect.height - swatchSize) * 0.5f;
            for (int i = 0; i < SwatchSlots.Length; i++) {
                float sx = swatchRect.x + i * (swatchSize + swatchGap);
                Rect sRect = new Rect(sx, swatchY, swatchSize, swatchSize);
                PaintBox.Draw(sRect, BackgroundSpec.Of(swatches[i]), null, RadiusSpec.All(RadiusScale.Xs));
            }

            Rem labelSize = new Rem(0.8125f);
            Rem subSize = new Rem(0.625f);
            float labelLineH = labelSize.ToPixels() * 1.2f;
            float subLineH = subSize.ToPixels() * 1.4f;
            float blockH = subtitle != null ? (labelLineH + subLineH) : labelLineH;
            float blockTop = labelBlock.y + (labelBlock.height - blockH) * 0.5f;

            Rect labelRect = new Rect(labelBlock.x, blockTop, labelBlock.width, labelLineH);
            ThemeSlot labelColor = isSelected
                ? ThemeSlot.SurfaceAccent
                : (hovered ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary);
            TextDraw.Draw(
                labelRect,
                label,
                FontRole.Body,
                labelSize,
                rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                labelColor,
                FontStyle.Normal,
                TextClipping.Clip
            );

            if (subtitle != null) {
                Rect subRect = new Rect(labelBlock.x, labelRect.yMax, labelBlock.width, subLineH);
                TextDraw.Draw(
                    subRect,
                    subtitle,
                    FontRole.Mono,
                    subSize,
                    rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                    ThemeSlot.TextMuted,
                    FontStyle.Normal,
                    TextClipping.Clip
                );
            }

            if (isSelected) {
                TextDraw.Draw(
                    checkRect,
                    "✓",
                    FontRole.Body,
                    new Rem(1.05f),
                    TextAnchor.MiddleCenter,
                    ThemeSlot.SurfaceAccent
                );
            }

            if (!isSelected) {
                MouseoverSounds.DoRegion(rect);
            }
            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                onClick?.Invoke();
                e.Use();
            }
        };
        return node;
    }

    private static LightweaveNode BuildHeader(int total) {
        Style eyebrowStyle = new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.8125f),
            LetterSpacing = Tracking.Of(0.18f),
            TextColor = ThemeSlot.TextMuted,
        };
        Style countStyle = new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.8125f),
            LetterSpacing = Tracking.Of(0.18f),
            TextColor = ThemeSlot.TextMuted,
            TextAlign = TextAlign.End,
        };
        return HStack.Create(SpacingScale.Sm, h => {
            h.AddFlex(Eyebrow.Create("CL_MainMenu_Theme_HeaderTitle".Translate(), style: eyebrowStyle));
            h.Add(Eyebrow.Create("CL_MainMenu_Theme_HeaderCount".Translate(total.Named("COUNT")), style: countStyle), new Rem(8f).ToPixels());
        });
    }

    private static LightweaveNode BuildEmpty() {
        return Box.Create(
            children: c => c.Add(TypoText.Create(
                "CL_MainMenu_Theme_None".Translate(),
                style: new Style { TextColor = ThemeSlot.TextMuted, TextAlign = TextAlign.Center }
            )),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Md, Bottom: SpacingScale.Md, Left: SpacingScale.Md, Right: SpacingScale.Md),
            }
        );
    }

    private static LightweaveNode BuildSpacer(Rem size) {
        return Spacer.Fixed(size);
    }

    private static LightweaveNode WrapHorizontalPad(LightweaveNode child) {
        return Box.Create(
            children: c => c.Add(child),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.None, Bottom: SpacingScale.None, Left: SpacingScale.Md, Right: SpacingScale.Md),
            }
        );
    }
}
