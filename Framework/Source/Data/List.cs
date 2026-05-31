using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Surfaces;
using Caption = Cosmere.Lightweave.Typography.Typography.Caption;
using Code = Cosmere.Lightweave.Typography.Typography.Code;
using Heading = Cosmere.Lightweave.Typography.Typography.Heading;
using Icon = Cosmere.Lightweave.Typography.Typography.Icon;
using Label = Cosmere.Lightweave.Typography.Typography.Label;
using RichText = Cosmere.Lightweave.Typography.Typography.RichText;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Data;

[Doc(
    Id = "list",
    Summary = "Vertical scrolling list of items rendered through a row builder.",
    WhenToUse = "Show a homogenous, potentially long sequence of rows that benefits from virtualization.",
    SourcePath = "Lightweave/Data/List.cs",
    PreferredVariantHeight = 200f,
    ShowRtl = true
)]
public static class List {
    public static LightweaveNode Create<T>(
        [DocParam("Source items rendered top-to-bottom.")]
        IReadOnlyList<T> items,
        [DocParam("Builds the row node for each item, given the item and its index.")]
        Func<T, int, LightweaveNode> rowBuilder,
        [DocParam("Fixed row height in pixels. Required for virtualization.")]
        float? rowHeight = null,
        [DocParam("Stable key extractor used to preserve row identity across renders.")]
        Func<T, object>? keyFn = null,
        [DocParam("When true, only rows in view are built. Requires rowHeight.")]
        bool virtualize = true,
        [DocParam("Extra rows built above and below the viewport to absorb fast scrolling.")]
        int overscan = 2,
        [DocParam("Draws a debug overlay reporting the painted row range over the total.")]
        bool showHud = false,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Hooks.Hooks.RefHandle<LightweaveScrollStatus> statusRef =
            Hooks.Hooks.UseRef(new LightweaveScrollStatus(), line, file);

        LightweaveNode node = NodeBuilder.New($"List<{typeof(T).Name}>", line, file);
        node.ApplyStyling("list", style, classes, id);

        if (rowHeight.HasValue && items != null) {
            node.PreferredHeight = items.Count * rowHeight.Value;
        }

        node.MeasureWidth = () => {
            if (items == null || items.Count == 0) {
                return 0f;
            }
            float maxW = 0f;
            int sampleCount = Math.Min(items.Count, 8);
            for (int i = 0; i < sampleCount; i++) {
                LightweaveNode row = rowBuilder(items[i], i);
                float w = row.MeasureWidth?.Invoke() ?? 0f;
                if (w > maxW) {
                    maxW = w;
                }
            }
            return Mathf.Ceil(maxW + LightweaveScrollView.GutterPixels(true));
        };

        node.Paint = (rect, paintChildren) => {
            if (items == null) {
                return;
            }

            bool doVirtualize = virtualize && rowHeight.HasValue;
            float totalHeight = rowHeight.HasValue
                ? items.Count * rowHeight.Value
                : items.Count * 36f;
            int paintedStart = 0;
            int paintedEnd = items.Count;

            statusRef.Current.Height = totalHeight;
            using (new LightweaveScrollView(rect, statusRef.Current)) {
                float scrollbarGutter = LightweaveScrollView.GutterPixels(statusRef.Current.VerticalVisible);
                float innerWidth = rect.width - scrollbarGutter;

                node.Children.Clear();

                if (doVirtualize) {
                    float rh = Mathf.Max(1f, rowHeight!.Value);
                    float scrollY = statusRef.Current.Position.y;
                    int pad = Math.Max(0, overscan);

                    paintedStart = Math.Max(0, (int)Math.Floor(scrollY / rh) - pad);
                    paintedEnd = Math.Min(items.Count, (int)Math.Ceiling((scrollY + rect.height) / rh) + pad);

                    for (int i = paintedStart; i < paintedEnd; i++) {
                        LightweaveNode row = rowBuilder(items[i], i);
                        row.ExplicitKey = keyFn?.Invoke(items[i]) ?? i;
                        row.MeasuredRect = new Rect(0f, i * rh, innerWidth, rh);
                        node.Children.Add(row);
                    }
                }
                else {
                    float rh = rowHeight ?? 36f;
                    for (int i = 0; i < items.Count; i++) {
                        LightweaveNode row = rowBuilder(items[i], i);
                        row.ExplicitKey = keyFn?.Invoke(items[i]) ?? i;
                        row.MeasuredRect = new Rect(0f, i * rh, innerWidth, rh);
                        node.Children.Add(row);
                    }
                }

                paintChildren();
            }

            if (showHud) {
                DrawHud(rect, paintedStart, paintedEnd, items.Count);
            }
        };

        return node;
    }

    private static void DrawHud(Rect rect, int start, int end, int total) {
        Theme.Theme theme = RenderContext.Current.Theme;
        int painted = Mathf.Max(0, end - start);
        int last = end > start ? end - 1 : start;
        string text = string.Format(
            CultureInfo.InvariantCulture,
            "{0}–{1} of {2} · {3} painted",
            start,
            last,
            total,
            painted
        );

        Font font = theme.GetFont(FontRole.Mono);
        int px = Mathf.RoundToInt(new Rem(0.68f).ToFontPx());
        GUIStyle textStyle = GuiStyleCache.GetOrCreate(font, px, FontStyle.Normal);
        textStyle.alignment = TextAnchor.MiddleCenter;

        float padX = SpacingScale.Sm.ToPixels();
        float padY = new Rem(0.25f).ToPixels();
        Vector2 textSize = textStyle.CalcSize(new GUIContent(text));
        float w = textSize.x + padX * 2f;
        float h = textSize.y + padY * 2f;
        float offset = SpacingScale.Sm.ToPixels();
        float gutter = LightweaveScrollView.GutterPixels(true);

        Rect badge = new Rect(
            rect.xMax - w - offset - gutter,
            rect.yMax - h - offset,
            w,
            h
        );

        PaintBox.Draw(
            badge,
            BackgroundSpec.Of(ThemeSlot.SurfaceTooltip),
            BorderSpec.All(new Rem(0.0625f), ThemeSlot.BorderTooltip),
            RadiusSpec.All(RadiusScale.Sm)
        );
        TextDraw.DrawWithStyle(badge, text, textStyle, theme.GetColor(ThemeSlot.TextSecondary));
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => {
            string[] items = new[] {
                (string)"CL_Playground_DemoItem_Highstorm".Translate(),
                (string)"CL_Playground_DemoItem_Stormlight".Translate(),
                (string)"CL_Playground_DemoItem_Radiant".Translate(),
                (string)"CL_Playground_DemoItem_Emotion".Translate(),
                (string)"CL_Playground_DemoItem_Alpha".Translate(),
                (string)"CL_Playground_DemoItem_Beta".Translate(),
                (string)"CL_Playground_DemoItem_Gamma".Translate(),
                (string)"CL_Playground_DemoItem_Delta".Translate(),
            };
            return List.Create(
                items,
                (item, _) => Box.Create(
                    k => k.Add(
                        Text.Create(
                            item,
                            style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextPrimary }
                        )
                    ),
                    style: new Style {
                        Padding = new EdgeInsets(SpacingScale.Sm, Bottom: SpacingScale.Sm, Left: SpacingScale.Md, Right: SpacingScale.Md),
                    }
                ),
                rowHeight: new Rem(2.25f).ToPixels()
            );
        });
    }

    [DocVariant("CL_Playground_Label_Virtualized")]
    public static DocSample DocsVirtualized() {
        return new DocSample(() => {
            int[] items = new int[2048];
            for (int i = 0; i < items.Length; i++) {
                items[i] = i;
            }
            return List.Create(
                items,
                (item, _) => Box.Create(
                    k => k.Add(
                        Text.Create(
                            (string)"CL_Playground_List_HudRow".Translate(item.Named("INDEX")),
                            style: new Style { FontFamily = FontRole.Mono, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextSecondary }
                        )
                    ),
                    style: new Style {
                        Padding = new EdgeInsets(SpacingScale.Xs, Bottom: SpacingScale.Xs, Left: SpacingScale.Md, Right: SpacingScale.Md),
                    }
                ),
                rowHeight: new Rem(1.625f).ToPixels(),
                overscan: 6,
                showHud: true,
                style: new Style { Height = Length.Stretch }
            );
        });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => {
            string[] items = new[] {
                "Highstorm",
                "Stormlight",
                "Radiant",
                "Spren bond",
            };
            return List.Create(
                items,
                (item, _) => Box.Create(
                    k => k.Add(
                        Text.Create(
                            item,
                            style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextPrimary }
                        )
                    ),
                    style: new Style {
                        Padding = new EdgeInsets(SpacingScale.Sm, Bottom: SpacingScale.Sm, Left: SpacingScale.Md, Right: SpacingScale.Md),
                    }
                ),
                rowHeight: new Rem(2.25f).ToPixels()
            );
        });
    }
}