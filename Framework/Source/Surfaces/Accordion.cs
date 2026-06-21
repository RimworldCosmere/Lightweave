using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Cosmere.Lightweave.Typography.Typography;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Surfaces;

public sealed record AccordionItem(
    int Id,
    string Title,
    string? Subtitle = null,
    bool Disabled = false,
    LightweaveNode? Content = null,
    Action? OnOpen = null,
    Action? OnClose = null
);

[Doc(
    Id = "accordion",
    Summary = "Stacked collapsible panels in single or multi-expand mode.",
    WhenToUse = "Reveal long-form content in sections without leaving the surface.",
    SourcePath = "Lightweave/Navigation/Accordion.cs",
    PreferredVariantHeight = 260f
)]
public static class Accordion {
    private const float HeaderHeight = 40f;
    private const float ExpandDurationSeconds = 0.18f;
    private static readonly Func<float, float> EaseOutCubic = t => 1f - Mathf.Pow(1f - t, 3f);

    public static LightweaveNode Create(
        [DocParam("Section definitions. HashSet enforces unique ids; iteration order drives display order.")]
        HashSet<AccordionItem> items,
        [DocParam("Set of section ids currently expanded.")]
        HashSet<int> expandedIds,
        [DocParam("Invoked with the section id when toggled. Optional.")]
        Action<int>? onToggle = null,
        [DocParam("Fallback content builder when AccordionItem.Content is null.")]
        Func<AccordionItem, LightweaveNode>? bodyBuilder = null,
        [DocParam("Single-open or multi-open behavior.")]
        AccordionMode mode = AccordionMode.Single,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New($"Accordion:{mode}", line, file);
        node.ApplyStyling("accordion", style, classes, id);

        List<AccordionItem> orderedItems = new List<AccordionItem>(items.Count);
        List<LightweaveNode> contentNodes = new List<LightweaveNode>(items.Count);
        foreach (AccordionItem item in items) {
            orderedItems.Add(item);
            LightweaveNode? content = item.Content ?? bodyBuilder?.Invoke(item);
            contentNodes.Add(content ?? Layout.Spacer.Fixed(SpacingScale.None));
        }

        for (int i = 0; i < contentNodes.Count; i++) {
            node.Children.Add(contentNodes[i]);
        }

        float headerPadXPx = SpacingScale.Lg.ToPixels();
        float headerPadYPx = SpacingScale.Md.ToPixels();
        float headerGapPx = SpacingScale.Sm.ToPixels();
        float chevronSizePx = new Rem(1f).ToPixels();
        float ordinalColumnPx = new Rem(2f).ToPixels();
        float bodyLeftInsetPx = headerPadXPx + ordinalColumnPx + headerGapPx;
        float bodyRightInsetPx = headerPadXPx;
        float bodyTopPadPx = new Rem(0.5f).ToPixels();
        float bodyBottomPadPx = new Rem(1.25f).ToPixels();
        float headerHeightPx = new Rem(3.25f).ToPixels();

        float ResolveContentHeight(int idx, float width) {
            LightweaveNode content = contentNodes[idx];
            if (content.Measure != null) {
                return content.Measure(width);
            }

            return content.PreferredHeight ?? 0f;
        }

        node.Measure = availableWidth => {
            float innerWidth = Mathf.Max(0f, availableWidth - bodyLeftInsetPx - bodyRightInsetPx);
            float total = 0f;
            for (int i = 0; i < orderedItems.Count; i++) {
                total += headerHeightPx;
                if (expandedIds.Contains(orderedItems[i].Id)) {
                    total += ResolveContentHeight(i, innerWidth) + bodyTopPadPx + bodyBottomPadPx;
                }
            }

            return total;
        };

        node.MeasureWidth = () => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Font titleFont = theme.GetFont(FontRole.Heading);
            int titleFontSize = Mathf.RoundToInt(new Rem(1.0625f).ToFontPx());
            GUIStyle titleStyle = GuiStyleCache.GetOrCreate(titleFont, titleFontSize);
            Font metaFont = theme.GetFont(FontRole.Caption);
            int metaFontSize = Mathf.RoundToInt(new Rem(0.6875f).ToFontPx());
            GUIStyle metaStyle = GuiStyleCache.GetOrCreate(metaFont, metaFontSize);

            float maxW = 0f;
            for (int i = 0; i < orderedItems.Count; i++) {
                AccordionItem item = orderedItems[i];
                float titleW = string.IsNullOrEmpty(item.Title) ? 0f : titleStyle.CalcSize(new GUIContent(item.Title)).x;
                float metaW = string.IsNullOrEmpty(item.Subtitle) ? 0f : metaStyle.CalcSize(new GUIContent(item.Subtitle!.ToUpperInvariant())).x;
                float headerW = headerPadXPx + ordinalColumnPx + headerGapPx + titleW + headerGapPx + metaW + headerGapPx + chevronSizePx + headerPadXPx;
                if (headerW > maxW) {
                    maxW = headerW;
                }
                float contentW = contentNodes[i].MeasureWidth?.Invoke() ?? 0f;
                float panelW = bodyLeftInsetPx + contentW + bodyRightInsetPx;
                if (panelW > maxW) {
                    maxW = panelW;
                }
            }
            return Mathf.Ceil(maxW);
        };

        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;

            float innerContentWidth = Mathf.Max(0f, rect.width - bodyLeftInsetPx - bodyRightInsetPx);

            Font titleFont = theme.GetFont(FontRole.Heading);
            int titleFontSize = Mathf.RoundToInt(new Rem(1.0625f).ToFontPx());
            GUIStyle titleStyle = GuiStyleCache.GetOrCreate(titleFont, titleFontSize);
            titleStyle.alignment = rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            titleStyle.wordWrap = false;

            Font metaFont = theme.GetFont(FontRole.Caption);
            int metaFontSize = Mathf.RoundToInt(new Rem(0.6875f).ToFontPx());
            GUIStyle metaStyle = GuiStyleCache.GetOrCreate(metaFont, metaFontSize);
            metaStyle.alignment = rtl ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            metaStyle.wordWrap = false;

            Font ordinalFont = theme.GetFont(FontRole.Caption);
            int ordinalFontSize = Mathf.RoundToInt(new Rem(0.6875f).ToFontPx());
            GUIStyle ordinalStyle = GuiStyleCache.GetOrCreate(ordinalFont, ordinalFontSize);
            ordinalStyle.alignment = TextAnchor.MiddleCenter;
            ordinalStyle.wordWrap = false;

            Color borderColor = theme.GetColor(ThemeSlot.BorderDefault);
            Color outerBg = theme.GetColor(ThemeSlot.SurfaceSunken);
            Color titleNormal = theme.GetColor(ThemeSlot.TextPrimary);
            Color titleAccent = theme.GetColor(ThemeSlot.SurfaceAccent);
            Color metaColor = theme.GetColor(ThemeSlot.TextMuted);
            Color chevronColor = theme.GetColor(ThemeSlot.TextSecondary);
            Color chevronAccent = theme.GetColor(ThemeSlot.SurfaceAccent);
            Color openHeaderBg = theme.GetColor(ThemeSlot.SurfaceRaised);
            openHeaderBg.a *= 0.55f;
            Color hoverBg = theme.GetColor(ThemeSlot.SurfaceRaised);
            hoverBg.a *= 0.4f;

            BackgroundSpec outerBgSpec = BackgroundSpec.Of(ThemeSlot.SurfaceSunken);
            BorderSpec outerBorderSpec = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault);
            PaintBox.Draw(rect, outerBgSpec, outerBorderSpec, null);

            Event e = Event.current;
            float cursorY = rect.y;

            for (int i = 0; i < orderedItems.Count; i++) {
                AccordionItem item = orderedItems[i];
                LightweaveNode content = contentNodes[i];
                bool expanded = expandedIds.Contains(item.Id);

                float contentNaturalHeight = ResolveContentHeight(i, innerContentWidth);
                float panelHeightWhenOpen = contentNaturalHeight + bodyTopPadPx + bodyBottomPadPx;

                float progress = UseAnim.Animate(
                    expanded ? 1f : 0f,
                    ExpandDurationSeconds,
                    EaseOutCubic,
                    i,
                    file + ":" + line + "#acc:" + item.Id
                );
                float revealHeight = panelHeightWhenOpen * progress;

                bool isFirst = i == 0;

                Rect headerRect = new Rect(rect.x, cursorY, rect.width, headerHeightPx);

                bool hovered = !item.Disabled && headerRect.Contains(e.mousePosition);

                if (expanded || progress > 0.001f) {
                    Color bgTint = openHeaderBg;
                    bgTint.a *= Mathf.Lerp(0f, 1f, progress);
                    PaintBox.Draw(headerRect, BackgroundSpec.Of(bgTint), null, null);
                }
                if (hovered) {
                    PaintBox.Draw(headerRect, BackgroundSpec.Of(hoverBg), null, null);
                    MouseoverSounds.DoRegion(headerRect);
                }

                if (!isFirst) {
                    Rect divider = new Rect(rect.x, headerRect.y, rect.width, 1f);
                    PaintBox.Draw(divider, BackgroundSpec.Of(borderColor), null, null);
                }

                float ordinalX = rtl
                    ? headerRect.xMax - headerPadXPx - ordinalColumnPx
                    : headerRect.x + headerPadXPx;
                Rect ordinalRect = new Rect(ordinalX, headerRect.y, ordinalColumnPx, headerRect.height);

                float chevronX = rtl
                    ? headerRect.x + headerPadXPx
                    : headerRect.xMax - headerPadXPx - chevronSizePx;
                Rect chevronRect = new Rect(
                    chevronX,
                    headerRect.y + (headerRect.height - chevronSizePx) / 2f,
                    chevronSizePx,
                    chevronSizePx
                );

                string metaText = string.IsNullOrEmpty(item.Subtitle) ? string.Empty : item.Subtitle!.ToUpperInvariant();
                float metaW = string.IsNullOrEmpty(metaText) ? 0f : metaStyle.CalcSize(new GUIContent(metaText)).x;
                float metaX = rtl
                    ? ordinalRect.xMax + headerGapPx
                    : chevronRect.x - headerGapPx - metaW;
                Rect metaRect = new Rect(metaX, headerRect.y, metaW, headerRect.height);

                Rect titleRect;
                if (rtl) {
                    float titleLeft = metaRect.xMax + headerGapPx;
                    float titleRight = chevronRect.x - headerGapPx;
                    titleRect = new Rect(titleLeft, headerRect.y, Mathf.Max(0f, titleRight - titleLeft), headerRect.height);
                }
                else {
                    float titleLeft = ordinalRect.xMax + headerGapPx;
                    float titleRight = (metaW > 0f ? metaRect.x : chevronRect.x) - headerGapPx;
                    titleRect = new Rect(titleLeft, headerRect.y, Mathf.Max(0f, titleRight - titleLeft), headerRect.height);
                }

                string ordinalText = (i + 1).ToString("D2");
                Color ordinalColor = item.Disabled ? theme.GetColor(ThemeSlot.TextMuted) : theme.GetColor(ThemeSlot.TextMuted);
                TextDraw.DrawWithStyle(ordinalRect, ordinalText, ordinalStyle, ordinalColor);

                Color titleColor = item.Disabled
                    ? theme.GetColor(ThemeSlot.TextMuted)
                    : (expanded || hovered ? titleAccent : titleNormal);
                TextDraw.DrawWithStyle(titleRect, item.Title, titleStyle, titleColor);

                if (!string.IsNullOrEmpty(metaText)) {
                    TextDraw.DrawWithStyle(metaRect, metaText, metaStyle, metaColor);
                }

                Color chevColor = item.Disabled
                    ? theme.GetColor(ThemeSlot.TextMuted)
                    : ((expanded || hovered) ? chevronAccent : chevronColor);
                DrawChevron(chevronRect, chevColor, progress, rtl, theme);

                cursorY = headerRect.yMax;

                if (revealHeight > 0.5f) {
                    Rect panelRect = new Rect(rect.x, cursorY, rect.width, revealHeight);

                    Color panelBg = outerBg;
                    panelBg.a *= 0.5f;
                    PaintBox.Draw(panelRect, BackgroundSpec.Of(panelBg), null, null);

                    Rect innerRect = new Rect(
                        panelRect.x + bodyLeftInsetPx,
                        panelRect.y + bodyTopPadPx,
                        Mathf.Max(0f, panelRect.width - bodyLeftInsetPx - bodyRightInsetPx),
                        Mathf.Max(0f, panelRect.height - bodyTopPadPx - bodyBottomPadPx)
                    );

                    using (ClipScope.Begin(panelRect)) {
                        Rect clippedInner = new Rect(
                            bodyLeftInsetPx,
                            bodyTopPadPx - (panelHeightWhenOpen - revealHeight),
                            innerRect.width,
                            contentNaturalHeight
                        );
                        content.MeasuredRect = clippedInner;
                        LightweaveRoot.PaintSubtree(content, clippedInner);
                    }

                    cursorY = panelRect.yMax;
                }

                LightweaveHitTracker.Track(headerRect);
                if (!item.Disabled && e.type == EventType.MouseUp && e.button == 0 && headerRect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(headerRect)) {
                    bool wasExpanded = expanded;
                    if (wasExpanded) {
                        item.OnClose?.Invoke();
                    }
                    else {
                        item.OnOpen?.Invoke();
                    }
                    onToggle?.Invoke(item.Id);
                    e.Use();
                }
            }
        };

        return node;
    }

    public static float MeasureHeight(IEnumerable<AccordionItem> items, HashSet<int> expandedIds, float contentHeightFallback = 56f) {
        float total = 0f;
        foreach (AccordionItem item in items) {
            total += HeaderHeight;
            if (expandedIds.Contains(item.Id)) {
                total += contentHeightFallback;
            }
        }

        return total;
    }

    private static void DrawChevron(Rect rect, Color color, float progress, bool rtl, Theme.Theme theme) {
        IconRef caret = rtl ? Icons.Phosphor.CaretLeft : Icons.Phosphor.CaretRight;
        Vector2 pivot = new Vector2(rect.x + rect.width / 2f, rect.y + rect.height / 2f);
        float angle = Mathf.Lerp(0f, rtl ? -90f : 90f, progress);
        using (RotateScope.Around(angle, pivot)) {
            TextDraw.Draw(
                rect,
                caret.Glyph,
                FontRole.Body,
                new Rem(1f),
                TextAnchor.MiddleCenter,
                color,
                fontOverride: caret.ResolveFont()
            );
        }
    }

    

    [DocVariant("CL_Playground_accordion_Mode_Single")]
    public static DocSample DocsSingle() {
        return new DocSample(() => {
            HashSet<AccordionItem> items = new HashSet<AccordionItem> {
                new AccordionItem(
                    Id: 1,
                    Title: (string)"CL_Playground_accordion_Header_Overview".Translate(),
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_Overview".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
                new AccordionItem(
                    Id: 2,
                    Title: (string)"CL_Playground_accordion_Header_Stormlight".Translate(),
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_Stormlight".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
                new AccordionItem(
                    Id: 3,
                    Title: (string)"CL_Playground_accordion_Header_Spren".Translate(),
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_Spren".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
            };
            Hooks.Hooks.StateHandle<HashSet<int>> open =
                Hooks.Hooks.UseState<HashSet<int>>(new HashSet<int> { 1 });
            return Accordion.Create(
                items,
                open.Value,
                onToggle: id => {
                    HashSet<int> next = open.Value.Contains(id)
                        ? new HashSet<int>()
                        : new HashSet<int> { id };
                    open.Set(next);
                }
            );
        });
    }

    [DocVariant("CL_Playground_accordion_Mode_Multi")]
    public static DocSample DocsMulti() {
        return new DocSample(() => {
            HashSet<AccordionItem> items = new HashSet<AccordionItem> {
                new AccordionItem(
                    Id: 1,
                    Title: (string)"CL_Playground_accordion_Header_Overview".Translate(),
                    Subtitle: (string)"CL_Playground_accordion_Subtitle_Overview".Translate(),
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_Overview".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
                new AccordionItem(
                    Id: 2,
                    Title: (string)"CL_Playground_accordion_Header_Stormlight".Translate(),
                    Subtitle: (string)"CL_Playground_accordion_Subtitle_Stormlight".Translate(),
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_Stormlight".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
                new AccordionItem(
                    Id: 3,
                    Title: (string)"CL_Playground_accordion_Header_Spren".Translate(),
                    Subtitle: (string)"CL_Playground_accordion_Subtitle_Spren".Translate(),
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_Spren".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
            };
            Hooks.Hooks.StateHandle<HashSet<int>> open =
                Hooks.Hooks.UseState<HashSet<int>>(new HashSet<int> { 2, 3 });
            return Accordion.Create(
                items,
                open.Value,
                onToggle: id => {
                    HashSet<int> next = new HashSet<int>(open.Value);
                    if (!next.Add(id)) {
                        next.Remove(id);
                    }

                    open.Set(next);
                },
                mode: AccordionMode.Multi
            );
        });
    }


    [DocVariant("CL_Playground_accordion_Mode_Disabled")]
    public static DocSample DocsDisabled() {
        return new DocSample(() => {
            HashSet<AccordionItem> items = new HashSet<AccordionItem> {
                new AccordionItem(
                    Id: 1,
                    Title: (string)"CL_Playground_accordion_Header_Overview".Translate(),
                    Subtitle: (string)"CL_Playground_accordion_Subtitle_Overview".Translate(),
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_Overview".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
                new AccordionItem(
                    Id: 2,
                    Title: (string)"CL_Playground_accordion_Header_OldMagic".Translate(),
                    Subtitle: (string)"CL_Playground_accordion_Subtitle_OldMagic".Translate(),
                    Disabled: true,
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_OldMagic".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
                new AccordionItem(
                    Id: 3,
                    Title: (string)"CL_Playground_accordion_Header_Spren".Translate(),
                    Subtitle: (string)"CL_Playground_accordion_Subtitle_Spren".Translate(),
                    Content: Text.Create(
                        (string)"CL_Playground_accordion_Body_Spren".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextPrimary }
                    )
                ),
            };
            Hooks.Hooks.StateHandle<HashSet<int>> open =
                Hooks.Hooks.UseState<HashSet<int>>(new HashSet<int> { 1 });
            return Accordion.Create(
                items,
                open.Value,
                onToggle: id => {
                    HashSet<int> next = new HashSet<int>(open.Value);
                    if (!next.Add(id)) {
                        next.Remove(id);
                    }

                    open.Set(next);
                },
                mode: AccordionMode.Multi
            );
        });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => {
            HashSet<AccordionItem> items = new HashSet<AccordionItem> {
                new AccordionItem(
                    Id: 1,
                    Title: "Overview",
                    Subtitle: "Stormlight + Bondsmiths",
                    Content: Text.Create("Stormlight is the most common form of Investiture on Roshar.", wrap: true)
                ),
                new AccordionItem(
                    Id: 2,
                    Title: "Spren",
                    Disabled: true,
                    Content: Text.Create("Sentient nature-spirits born of Investiture and human attention.", wrap: true)
                ),
            };

            Hooks.Hooks.RefHandle<HashSet<int>> expanded =
                Hooks.Hooks.UseRef(new HashSet<int> { 1 });
            return Accordion.Create(items, expanded.Current);
        });
    }
}