using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Navigation;

[Doc(
    Id = "sidenav",
    Summary = "Vertical navigation rail of grouped rows, each with an optional icon and trailing count.",
    WhenToUse = "Drive a content pane from a flat, always-visible list of sections grouped under eyebrow labels.",
    SourcePath = "Lightweave/Navigation/SideNav.cs"
)]
public static class SideNav {
    private static readonly Dictionary<int, string> CountStringCache = new Dictionary<int, string>();

    private const float RowHeightRem = 2.375f;
    private const float EyebrowHeightRem = 1.625f;
    private const float GroupGapRem = 0.5f;
    private const float SeparatorRem = 1f;
    private const float RowPadXRem = 1.125f;
    private const float IconBoxRem = 1.125f;
    private const float IconGapRem = 0.6875f;

    private const float LabelSizeRem = 0.8125f;
    private const float CountSizeRem = 0.625f;
    private const float EyebrowSizeRem = 0.59375f;
    private const float IconSizeRem = 0.875f;

    public static LightweaveNode Create(
        [DocParam("Groups in display order. Each group renders an optional eyebrow then its rows; a divider separates adjacent groups.")]
        IReadOnlyList<SideNavGroup> groups,
        [DocParam("Id of the currently active row.")]
        string activeId,
        [DocParam("Invoked with the row id when the user picks a different row.")]
        System.Action<string> onSelect,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("SideNav", line, file);
        node.ApplyStyling("sidenav", style, classes, id);

        int rowCount = 0;
        for (int g = 0; g < groups.Count; g++) {
            rowCount += groups[g].Items.Count;
        }

        Rect[]? rowRectsBuf = null;
        string[]? rowIdsBuf = null;
        bool[]? rowDisabledBuf = null;

        node.MeasureWidth = () => {
            float padX = Px(RowPadXRem);
            float iconAdvance = Px(IconBoxRem) + Px(IconGapRem);
            float widest = 0f;
            for (int g = 0; g < groups.Count; g++) {
                SideNavGroup group = groups[g];
                for (int i = 0; i < group.Items.Count; i++) {
                    SideNavItem item = group.Items[i];
                    float labelW = TextDraw.Measure(item.Label, FontRole.Body, new Rem(LabelSizeRem)).x;
                    float w = padX * 2f + iconAdvance + labelW;
                    if (item.Count.HasValue) {
                        w += SpacingScale.Md.ToPixels() + TextDraw.Measure(CountString(item.Count.Value), FontRole.Mono, new Rem(CountSizeRem)).x;
                    }
                    if (w > widest) {
                        widest = w;
                    }
                }
            }

            return Mathf.Ceil(widest);
        };

        node.Measure = _ => {
            float total = 0f;
            for (int g = 0; g < groups.Count; g++) {
                if (g > 0) {
                    total += Px(SeparatorRem) + Px(GroupGapRem) * 2f;
                }
                if (groups[g].Eyebrow != null) {
                    total += Px(EyebrowHeightRem);
                }
                total += groups[g].Items.Count * Px(RowHeightRem);
            }

            return total;
        };

        node.Layout = rect => {
            if (rowRectsBuf == null || rowRectsBuf.Length != rowCount) {
                rowRectsBuf = new Rect[rowCount];
                rowIdsBuf = new string[rowCount];
                rowDisabledBuf = new bool[rowCount];
            }

            float rowH = Px(RowHeightRem);
            float eyebrowH = Px(EyebrowHeightRem);
            float sepH = Px(SeparatorRem);
            float groupGap = Px(GroupGapRem);
            float cursorY = rect.y;
            int r = 0;

            for (int g = 0; g < groups.Count; g++) {
                if (g > 0) {
                    cursorY += groupGap + sepH + groupGap;
                }

                SideNavGroup group = groups[g];
                if (group.Eyebrow != null) {
                    cursorY += eyebrowH;
                }

                for (int i = 0; i < group.Items.Count; i++) {
                    SideNavItem item = group.Items[i];
                    rowRectsBuf[r] = new Rect(rect.x, cursorY, rect.width, rowH);
                    rowIdsBuf![r] = item.Id;
                    rowDisabledBuf![r] = item.Disabled;
                    cursorY += rowH;
                    r++;
                }
            }

            // Input phase: clicks must process on non-Repaint events, so hit testing lives in Layout.
            for (int i = 0; i < rowCount; i++) {
                if (rowDisabledBuf![i]) {
                    continue;
                }
                if (Widgets.ButtonInvisible(rowRectsBuf[i], doMouseoverSound: true)) {
                    if (rowIdsBuf![i] != activeId) {
                        onSelect(rowIdsBuf[i]);
                    }
                }
            }
        };

        node.Draw = rect => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Vector2 mouse = Event.current.mousePosition;

            float rowH = Px(RowHeightRem);
            float eyebrowH = Px(EyebrowHeightRem);
            float sepH = Px(SeparatorRem);
            float groupGap = Px(GroupGapRem);
            float padX = Px(RowPadXRem);
            float stripe = Spacing.StripeWidth.ToPixels();
            float iconBox = Px(IconBoxRem);
            float iconGap = Px(IconGapRem);

            float cursorY = rect.y;
            for (int g = 0; g < groups.Count; g++) {
                if (g > 0) {
                    cursorY += groupGap;
                    Rect sepRect = new Rect(rect.x + padX, cursorY + sepH * 0.5f, rect.width - padX * 2f, 1f);
                    PaintBox.Fill(sepRect, theme.GetColor(ThemeSlot.BorderFaint));
                    cursorY += sepH + groupGap;
                }

                SideNavGroup group = groups[g];
                if (group.Eyebrow != null) {
                    Rect ebRect = new Rect(rect.x + padX, cursorY, rect.width - padX * 2f, eyebrowH);
                    TextDraw.Draw(ebRect, group.Eyebrow, FontRole.Mono, new Rem(EyebrowSizeRem), TextAnchor.LowerLeft, ThemeSlot.TextMuted);
                    cursorY += eyebrowH;
                }

                for (int i = 0; i < group.Items.Count; i++) {
                    SideNavItem item = group.Items[i];
                    Rect rowRect = new Rect(rect.x, cursorY, rect.width, rowH);
                    bool active = item.Id == activeId;
                    bool hovering = !item.Disabled && rowRect.Contains(mouse);

                    if (active) {
                        PaintBox.Fill(rowRect, theme.GetColor(ThemeSlot.ActiveTint));
                        Rect bar = new Rect(rowRect.x, rowRect.y, stripe, rowRect.height);
                        PaintBox.Fill(bar, theme.GetColor(ThemeSlot.BorderFocus));
                    }
                    else if (hovering) {
                        PaintBox.DrawHighlight(rowRect, RadiusSpec.All(RadiusScale.Sm), true);
                    }

                    ThemeSlot textSlot = item.Disabled
                        ? ThemeSlot.TextMuted
                        : active ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary;
                    FontRole role = active ? FontRole.BodyBold : FontRole.Body;

                    float contentX = rowRect.x + padX;
                    if (item.Icon.HasValue) {
                        IconRef icon = item.Icon.Value;
                        Rect iconRect = new Rect(contentX, rowRect.y + (rowRect.height - iconBox) * 0.5f, iconBox, iconBox);
                        TextDraw.Draw(iconRect, icon.Glyph, FontRole.Body, new Rem(IconSizeRem), TextAnchor.MiddleCenter, theme.GetColor(textSlot), fontOverride: icon.ResolveFont());
                    }

                    float labelX = contentX + iconBox + iconGap;
                    float countW = 0f;
                    if (item.Count.HasValue) {
                        string countStr = CountString(item.Count.Value);
                        countW = TextDraw.Measure(countStr, FontRole.Mono, new Rem(CountSizeRem)).x + padX;
                        Rect countRect = new Rect(rowRect.xMax - countW, rowRect.y, countW - padX * 0.25f, rowRect.height);
                        ThemeSlot countSlot = active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted;
                        TextDraw.Draw(countRect, countStr, FontRole.Mono, new Rem(CountSizeRem), TextAnchor.MiddleRight, countSlot);
                    }

                    Rect labelRect = new Rect(labelX, rowRect.y, rowRect.xMax - labelX - countW - padX * 0.5f, rowRect.height);
                    TextDraw.Draw(labelRect, item.Label, role, new Rem(LabelSizeRem), TextAnchor.MiddleLeft, textSlot, clipping: TextClipping.Clip);

                    cursorY += rowH;
                }
            }
        };

        return node;
    }

    private static float Px(float rem) {
        return new Rem(rem).ToPixels();
    }

    private static string CountString(int value) {
        if (!CountStringCache.TryGetValue(value, out string cached)) {
            cached = value.ToString();
            CountStringCache[value] = cached;
        }

        return cached;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => {
            Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("rituals");
            return Container.Create(
                Box.Create(
                    c => c.Add(SideNav.Create(DemoGroups(), selected.Value, selected.Set)),
                    style: new Style {
                        Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                        Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderFaint),
                    }),
                align: ContainerAlign.Start,
                style: new Style { MaxWidth = new Rem(15f) });
        });
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(
            helpers: new[] { nameof(DemoGroups) },
            build: () => {
                Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("rituals");
                return Container.Create(
                    Box.Create(
                        c => c.Add(SideNav.Create(DemoGroups(), selected.Value, selected.Set)),
                        style: new Style {
                            Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderFaint),
                        }),
                    align: ContainerAlign.Start,
                    style: new Style { MaxWidth = new Rem(15f) });
            });
    }

    private static IReadOnlyList<SideNavGroup> DemoGroups() {
        return new List<SideNavGroup> {
            new SideNavGroup(
                new List<SideNavItem> {
                    new SideNavItem("memes", "Memes", Icons.Phosphor.Asterisk, count: 2),
                    new SideNavItem("precepts", "Precepts", Icons.Phosphor.ListChecks, count: 7),
                    new SideNavItem("rituals", "Rituals", Icons.Phosphor.Sparkle, count: 3),
                    new SideNavItem("roles", "Roles", Icons.Phosphor.UserCircle, count: 2),
                },
                eyebrow: "BELIEF"),
            new SideNavGroup(
                new List<SideNavItem> {
                    new SideNavItem("xenotypes", "Xenotypes", Icons.Phosphor.Dna, count: 2),
                    new SideNavItem("apparel", "Apparel", Icons.Phosphor.TShirt, count: 3),
                },
                eyebrow: "PREFERENCES"),
        };
    }
}
