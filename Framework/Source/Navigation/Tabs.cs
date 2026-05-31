using System;
using System.Collections.Generic;
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
using static Cosmere.Lightweave.Typography.Typography;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Navigation;

[Doc(
    Id = "tabs",
    Summary = "Horizontal tab bar that switches the body region, with a numbered dossier variant.",
    WhenToUse = "Move between sibling views inside one window.",
    SourcePath = "Lightweave/Navigation/Tabs.cs",
    ShowRtl = true
)]
public static class Tabs {
    private static readonly Dictionary<int, string> NumberStringCache = new Dictionary<int, string>();

    private static string NumberString(int value) {
        if (!NumberStringCache.TryGetValue(value, out string cached)) {
            cached = value.ToString("D2");
            NumberStringCache[value] = cached;
        }

        return cached;
    }

    public static LightweaveNode Create<T>(
        [DocParam("Currently selected tab value.")]
        T value,
        [DocParam("Tab values in display order.")]
        IReadOnlyList<T> items,
        [DocParam("Maps a tab value to the visible label.")]
        Func<T, string> labelFn,
        [DocParam("Invoked when the user picks a different tab.")]
        Action<T> onChange,
        [DocParam("Builds the body node for the active tab.")]
        Func<T, LightweaveNode> bodyFn,
        [DocParam("Body padding (default: 1rem / ~16px). Pass Rem(0) for full-bleed.")]
        Rem? bodyPadding = null,
        [DocParam("Optional predicate; tabs returning true skip the body padding.")]
        Func<T, bool>? noPaddingFor = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New($"Tabs<{typeof(T).Name}>", line, file);
        node.ApplyStyling("tabs", style, classes, id);
        LightweaveNode bodyNode = bodyFn(value);
        node.Children.Add(bodyNode);

        float barHeight = new Rem(2.5f).ToPixels();
        float dividerThickness = new Rem(1f / 16f).ToPixels();
        float chromeHeight = barHeight + dividerThickness;

        Rem effectivePadding = bodyPadding ?? new Rem(1f);
        bool skipPadding = noPaddingFor?.Invoke(value) ?? false;
        float padPx = skipPadding ? 0f : effectivePadding.ToPixels();
        float leftIndentPx = skipPadding ? 0f : new Rem(2f).ToPixels();

        if (bodyNode.Measure != null) {
            node.Measure = width => chromeHeight + padPx * 2f + bodyNode.Measure(Mathf.Max(0f, width - leftIndentPx - padPx));
        }
        else if (bodyNode.PreferredHeight.HasValue) {
            node.PreferredHeight = chromeHeight + padPx * 2f + bodyNode.PreferredHeight.Value;
        }

        node.MeasureWidth = () => {
            int count = items.Count;
            Theme.Theme theme = RenderContext.Current.Theme;
            Font font = theme.GetFont(FontRole.Mono);
            int pixelSize = Mathf.RoundToInt(new Rem(0.875f).ToFontPx());
            GUIStyle gs = GuiStyleCache.GetOrCreate(font, pixelSize);
            float tabPadding = SpacingScale.Md.ToPixels();
            float tabGap = new Rem(0.25f).ToPixels();
            float barW = leftIndentPx;
            for (int i = 0; i < count; i++) {
                string label = labelFn(items[i]) ?? string.Empty;
                barW += gs.CalcSize(new GUIContent(label)).x + tabPadding * 2f;
                if (i < count - 1) {
                    barW += tabGap;
                }
            }

            float bodyW = bodyNode.MeasureWidth?.Invoke() ?? 0f;
            float bodyOuterW = bodyW + leftIndentPx + padPx;
            return Mathf.Ceil(Mathf.Max(barW, bodyOuterW));
        };

        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;

            float underlineThickness = new Rem(2f / 16f).ToPixels();
            float tabGap = new Rem(0.25f).ToPixels();
            float tabPadding = SpacingScale.Md.ToPixels();

            Rect barRect = new Rect(rect.x, rect.y, rect.width, barHeight);
            Rect dividerRect = new Rect(rect.x, barRect.yMax, rect.width, dividerThickness);
            Rect bodyOuter = new Rect(
                rect.x,
                dividerRect.yMax,
                rect.width,
                Mathf.Max(0f, rect.yMax - dividerRect.yMax)
            );
            Rect bodyRect = new Rect(
                bodyOuter.x + (rtl ? padPx : leftIndentPx),
                bodyOuter.y + padPx,
                Mathf.Max(0f, bodyOuter.width - leftIndentPx - padPx),
                Mathf.Max(0f, bodyOuter.height - padPx * 2f)
            );

            Font font = theme.GetFont(FontRole.Mono);
            int pixelSize = Mathf.RoundToInt(new Rem(0.875f).ToFontPx());
            GUIStyle style = GuiStyleCache.GetOrCreate(font, pixelSize);
            style.alignment = TextAnchor.MiddleCenter;

            int count = items.Count;
            float[] widths = new float[count];
            for (int i = 0; i < count; i++) {
                string label = labelFn(items[i]);
                Vector2 labelSize = style.CalcSize(new GUIContent(label));
                widths[i] = labelSize.x + tabPadding * 2f;
            }

            Event e = Event.current;

            float cursor = rtl ? barRect.xMax - leftIndentPx : barRect.x + leftIndentPx;
            for (int i = 0; i < count; i++) {
                T item = items[i];
                bool active = EqualityComparer<T>.Default.Equals(item, value);
                float tabWidth = widths[i];

                Rect tabRect;
                if (rtl) {
                    tabRect = new Rect(cursor - tabWidth, barRect.y, tabWidth, barRect.height);
                    cursor -= tabWidth + tabGap;
                }
                else {
                    tabRect = new Rect(cursor, barRect.y, tabWidth, barRect.height);
                    cursor += tabWidth + tabGap;
                }

                bool hovering = tabRect.Contains(e.mousePosition);
                if (!active) {
                    PaintBox.DrawHighlightIfMouseover(tabRect, RadiusSpec.Top(RadiusScale.Sm));
                    Cosmere.Lightweave.Input.InteractionFeedback.Apply(tabRect, enabled: true, playSound: true);
                }

                ThemeSlot textSlot = active || hovering ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary;

                TextDraw.DrawWithStyle(tabRect, labelFn(item), style, theme.GetColor(textSlot));

                if (active) {
                    Rect underlineRect = new Rect(
                        tabRect.x,
                        tabRect.yMax - underlineThickness,
                        tabRect.width,
                        underlineThickness
                    );
                    PaintBox.Draw(underlineRect, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
                }

                if (e.type == EventType.MouseUp && e.button == 0 && tabRect.Contains(e.mousePosition)) {
                    onChange?.Invoke(item);
                    e.Use();
                }
            }

            PaintBox.Draw(dividerRect, BackgroundSpec.Of(ThemeSlot.BorderSubtle), null, null);

            bodyNode.MeasuredRect = bodyRect;
            LightweaveRoot.PaintSubtree(bodyNode, bodyRect);
        };

        return node;
    }

    private const float DossierPadVRem = 29f / 16f;
    private const float DossierPadHRem = 24f / 16f;
    private const float DossierRowGapRem = 6f / 16f;
    private const float DossierNumberRowRem = 0.6875f;
    private const float DossierLabelRowRem = 1.25f;
    private const float DossierSubtitleRowRem = 0.75f;
    private const float DossierSubtitleMaxRem = 200f / 16f;
    private const float DossierActiveBorderRem = 2f / 16f;

    private static readonly Rem DossierLabelSize = new Rem(DossierLabelRowRem);
    private static readonly Rem DossierSubtitleSize = new Rem(DossierSubtitleRowRem);
    private static readonly Rem DossierNumberSize = new Rem(DossierNumberRowRem);

    public static LightweaveNode Create(
        [DocParam("Tabs in display order. Each carries a number badge, label and optional subtitle.")]
        IReadOnlyList<TabItem> items,
        [DocParam("Id of the currently active tab.")]
        string activeId,
        [DocParam("Invoked with the tab id when the user picks a different tab.")]
        Action<string> onSelect,
        [DocParam("Visual variant. Only Dossier is supported by this overload.")]
        TabsVariant variant = TabsVariant.Dossier,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("DossierTabs", line, file);
        node.ApplyStyling("tabs-dossier", style, classes, id);

        if (style is not { Width: { } }) {
            node.Style = (style ?? new Style()) with { Width = Length.Stretch };
        }

        node.PreferredHeight = DossierTabHeight();

        node.MeasureWidth = () => {
            Theme.Theme theme = RenderContext.Current.Theme;
            float total = 0f;
            for (int i = 0; i < items.Count; i++) {
                total += MeasureDossierTabWidth(items[i], theme);
            }

            return Mathf.Ceil(total);
        };

        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            DrawDossierTabs(rect, items, activeId, onSelect, theme);
        };

        return node;
    }

    private static float DossierTextBlockHeight(bool hasNumber, bool hasSubtitle) {
        float gap = new Rem(DossierRowGapRem).ToPixels();
        float h = new Rem(DossierLabelRowRem).ToPixels();
        if (hasNumber) {
            h += new Rem(DossierNumberRowRem).ToPixels() + gap;
        }
        if (hasSubtitle) {
            h += new Rem(DossierSubtitleRowRem).ToPixels() + gap;
        }

        return h;
    }

    private static float DossierTabHeight() {
        return new Rem(DossierPadVRem).ToPixels() * 2f + DossierTextBlockHeight(true, true);
    }

    private static float MeasureDossierTabWidth(TabItem item, Theme.Theme theme) {
        float labelWidth = TextDraw.Measure(item.Label, FontRole.Display, DossierLabelSize).x;
        float numberWidth = item.Number.HasValue
            ? TextDraw.Measure(NumberString(item.Number.Value), FontRole.Mono, DossierNumberSize).x
            : 0f;
        float subtitleWidth = item.Subtitle != null
            ? TextDraw.Measure(item.Subtitle, FontRole.Caption, DossierSubtitleSize).x
            : 0f;
        float subtitleMax = new Rem(DossierSubtitleMaxRem).ToPixels();
        float textWidth = Mathf.Max(labelWidth, Mathf.Max(numberWidth, Mathf.Min(subtitleWidth, subtitleMax)));
        return new Rem(DossierPadHRem).ToPixels() * 2f + textWidth;
    }

    private static void DrawDossierTabs(
        Rect rect,
        IReadOnlyList<TabItem> items,
        string activeId,
        Action<string> onSelect,
        Theme.Theme theme
    ) {
        float lineThickness = new Rem(1f / 16f).ToPixels();
        Rect lineRect = new Rect(rect.x, rect.yMax - lineThickness, rect.width, lineThickness);
        PaintBox.Draw(lineRect, BackgroundSpec.Of(ThemeSlot.BorderSubtle), null, null);

        Event e = Event.current;
        int count = items.Count;
        if (count == 0) {
            return;
        }

        float colWidth = rect.width / count;
        for (int i = 0; i < count; i++) {
            TabItem item = items[i];
            float tabX = rect.x + i * colWidth;
            float tabWidth = i == count - 1 ? rect.xMax - tabX : colWidth;
            Rect tabRect = new Rect(tabX, rect.y, tabWidth, rect.height);
            bool isActive = item.Id == activeId;

            DrawSingleDossierTab(tabRect, item, isActive, theme, e, onSelect);
        }
    }

    private static void DrawSingleDossierTab(
        Rect tabRect,
        TabItem item,
        bool isActive,
        Theme.Theme theme,
        Event e,
        Action<string> onSelect
    ) {
        if (!item.Disabled) {
            LightweaveHitTracker.Track(tabRect);
            if (!isActive) {
                PaintBox.DrawHighlightIfMouseover(tabRect, RadiusSpec.Top(RadiusScale.Sm));
                Cosmere.Lightweave.Input.InteractionFeedback.Apply(tabRect, enabled: true, playSound: true);
            }
        }

        float padH = new Rem(DossierPadHRem).ToPixels();
        float rowGap = new Rem(DossierRowGapRem).ToPixels();
        float numberH = new Rem(DossierNumberRowRem).ToPixels();
        float labelH = new Rem(DossierLabelRowRem).ToPixels();
        float subtitleH = new Rem(DossierSubtitleRowRem).ToPixels();

        float contentX = tabRect.x + padH;
        float textWidth = Mathf.Max(0f, tabRect.xMax - padH - contentX);

        string? subtitle = item.Subtitle;
        bool hasNumber = item.Number.HasValue || item.Icon.HasValue;
        float blockH = DossierTextBlockHeight(hasNumber, subtitle != null);
        float rowY = tabRect.y + (tabRect.height - blockH) * 0.5f;

        ThemeSlot labelSlot = item.Disabled
            ? ThemeSlot.TextMuted
            : isActive
                ? ThemeSlot.TextPrimary
                : ThemeSlot.TextSecondary;

        if (hasNumber) {
            ThemeSlot numberSlot = isActive ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted;
            Rect numberRect = new Rect(contentX, rowY, textWidth, numberH);
            if (item.Number.HasValue) {
                TextDraw.Draw(numberRect, NumberString(item.Number.Value), FontRole.Mono, DossierNumberSize, TextAnchor.MiddleLeft, theme.GetColor(numberSlot));
            }
            else if (item.Icon.HasValue) {
                IconRef icon = item.Icon.Value;
                TextDraw.Draw(numberRect, icon.Glyph, FontRole.Body, DossierNumberSize, TextAnchor.MiddleLeft, theme.GetColor(numberSlot), fontOverride: icon.ResolveFont());
            }

            rowY += numberH + rowGap;
        }

        Rect labelRect = new Rect(contentX, rowY, textWidth, labelH);
        TextDraw.Draw(labelRect, item.Label, FontRole.Display, DossierLabelSize, TextAnchor.MiddleLeft, theme.GetColor(labelSlot));
        rowY += labelH + rowGap;

        if (subtitle != null) {
            Rect subtitleRect = new Rect(contentX, rowY, textWidth, subtitleH);
            TextDraw.Draw(subtitleRect, subtitle, FontRole.Caption, DossierSubtitleSize, TextAnchor.MiddleLeft, theme.GetColor(isActive ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted));
        }

        if (isActive) {
            float borderH = new Rem(DossierActiveBorderRem).ToPixels();
            Rect accentRect = new Rect(tabRect.x, tabRect.yMax - borderH, tabRect.width, borderH);
            PaintBox.Draw(accentRect, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
        }

        if (!item.Disabled && e.type == EventType.MouseUp && e.button == 0 && tabRect.Contains(e.mousePosition)) {
            if (LightweaveHitTracker.IsTopmost(tabRect)) {
                onSelect?.Invoke(item.Id);
                e.Use();
            }
        }
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => {
            Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("general");
            string[] tabs = ["general", "combat", "storage"];
            return Tabs.Create(
                selected.Value,
                tabs,
                v => v switch {
                    "combat" => (string)"CL_Playground_Navigation_Tabs_Combat".Translate(),
                    "storage" => (string)"CL_Playground_Navigation_Tabs_Storage".Translate(),
                    _ => (string)"CL_Playground_Navigation_Tabs_General".Translate(),
                },
                v => selected.Set(v),
                v => Caption.Create(
                    v switch {
                        "combat" => (string)"CL_Playground_Navigation_Tabs_Body_Combat".Translate(),
                        "storage" => (string)"CL_Playground_Navigation_Tabs_Body_Storage".Translate(),
                        _ => (string)"CL_Playground_Navigation_Tabs_Body_General".Translate(),
                    }
                )
            );
        });
    }

    [DocVariant("CL_Playground_Tabs_Variant_FullBleed")]
    public static DocSample DocsFullBleed() {
        return new DocSample(() => {
            Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("padded");
            string[] tabs = ["padded", "fullbleed"];
            return Tabs.Create(
                selected.Value,
                tabs,
                v => v == "padded"
                    ? (string)"CL_Playground_Tabs_Tab_Padded".Translate()
                    : (string)"CL_Playground_Tabs_Tab_FullBleed".Translate(),
                v => selected.Set(v),
                v => Caption.Create(
                    v == "padded"
                        ? (string)"CL_Playground_Tabs_Body_Padded".Translate()
                        : (string)"CL_Playground_Tabs_Body_FullBleed".Translate()
                ),
                noPaddingFor: v => v == "fullbleed"
            );
        });
    }

    [DocVariant("CL_Playground_Tabs_Variant_LargePadding")]
    public static DocSample DocsLargePadding() {
        return new DocSample(() => {
            Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("alpha");
            string[] tabs = ["alpha", "beta"];
            return Tabs.Create(
                selected.Value,
                tabs,
                v => v,
                v => selected.Set(v),
                v => Caption.Create((string)"CL_Playground_Tabs_Body_LargePadding".Translate()),
                bodyPadding: new Rem(2f)
            );
        });
    }

    [DocVariant("dossier")]
    public static DocSample DocsDossier() {
        return new DocSample(() => {
            Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("storyteller");
            List<TabItem> items = [
                new TabItem("storyteller", "Storyteller", number: 1, subtitle: "Cassandra · Strive to Survive"),
                new TabItem("scenario", "Scenario", number: 2, subtitle: "Crashlanded"),
                new TabItem("planet", "Planet", number: 3, subtitle: "100% · Temperate"),
                new TabItem("ideology", "Ideology", number: 4, subtitle: "Requires Ideology", disabled: true),
            ];
            return Tabs.Create(items, selected.Value, v => selected.Set(v), TabsVariant.Dossier);
        });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => {
            Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("general");
            string[] tabs = ["general", "combat", "storage"];
            return Tabs.Create(
                selected.Value,
                tabs,
                v => v,
                v => selected.Set(v),
                v => Caption.Create(v)
            );
        });
    }
}
