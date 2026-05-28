using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Input;

public sealed record ButtonGroupItem(
    string Label,
    Action OnClick,
    bool Disabled = false
);

[Doc(
    Id = "buttongroup",
    Summary = "Connected row of buttons sharing a single bordered frame.",
    WhenToUse = "Group a small set of related actions or filter choices together.",
    SourcePath = "Lightweave/Input/ButtonGroup.cs",
    ShowRtl = true
)]
public static class ButtonGroup {
    public static LightweaveNode Create(
        [DocParam("Items rendered as segments left-to-right.")]
        IReadOnlyList<ButtonGroupItem> items,
        [DocParam("Visual variant shared by every segment.")]
        Variant variant = default,
        [DocParam("Outer shape. Pill draws a bordered, rounded container; Flat omits the outer frame and lets segments fill the rect flush with the parent.")]
        ButtonGroupShape shape = ButtonGroupShape.Pill,
        [DocParam("When true (default) labels render in BodyBold for legibility at small pill sizes. Pass false to use the regular Body weight.")]
        bool bold = true,
        [DocParam("Override hover sound. Null = component default (true).")]
        bool? playHoverSound = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        if (variant.Id == null) {
            variant = Variant.Secondary;
        }

        LightweaveNode node = NodeBuilder.New($"ButtonGroup:{variant}:{shape}", line, file);
        node.ApplyStyling("button-group", style, classes, id);
        node.PreferredHeight = new Rem(1.75f).ToPixels();

        node.MeasureWidth = () => {
            if (items == null || items.Count == 0) {
                return 0f;
            }
            Theme.Theme theme = RenderContext.Current.Theme;
            Font font = theme.GetFont(bold ? FontRole.BodyBold : FontRole.Body);
            int pixelSize = Mathf.RoundToInt(new Rem(0.875f).ToFontPx());
            GUIStyle gs = GuiStyleCache.GetOrCreate(font, pixelSize);
            float padPx = SpacingScale.Md.ToPixels();
            float total = 0f;
            for (int i = 0; i < items.Count; i++) {
                float lblW = string.IsNullOrEmpty(items[i].Label) ? 0f : gs.CalcSize(new GUIContent(items[i].Label)).x;
                total += lblW + padPx * 2f;
            }
            return Mathf.Ceil(total);
        };

        node.Paint = (rect, _) => {
            if (items == null || items.Count == 0) {
                return;
            }

            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;
            bool soundEnabled = playHoverSound ?? true;
            bool flat = shape == ButtonGroupShape.Flat;

            if (!flat && variant == Variant.Secondary) {
                rect = new Rect(rect.x + 1f, rect.y, Mathf.Max(0f, rect.width - 1f), rect.height);
            }

            int count = items.Count;

            Font font = theme.GetFont(bold ? FontRole.BodyBold : FontRole.Body);
            int pixelSize = Mathf.RoundToInt(new Rem(0.875f).ToFontPx());
            GUIStyle style = GuiStyleCache.GetOrCreate(font, pixelSize);
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = false;

            float borderPx = new Rem(1f / 16f).ToPixels();
            Rem outerCornerRem = RadiusSpec.ResolveRem(RadiusScale.Sm);

            InteractionState groupState = new InteractionState(false, false, false, false);
            ThemeSlot? outerBorderSlot = flat ? null : VariantPalette.Border(variant, groupState);

            Rect contentRect;
            Rem innerCornerRem;
            if (outerBorderSlot.HasValue) {
                PaintBox.Draw(
                    rect,
                    BackgroundSpec.Of(outerBorderSlot.Value),
                    null,
                    RadiusSpec.All(outerCornerRem)
                );
                contentRect = new Rect(
                    rect.x + borderPx,
                    rect.y + borderPx,
                    Mathf.Max(0f, rect.width - borderPx * 2f),
                    Mathf.Max(0f, rect.height - borderPx * 2f)
                );
                innerCornerRem = new Rem(Mathf.Max(0f, outerCornerRem.Value - 1f / 16f));
            }
            else {
                contentRect = rect;
                innerCornerRem = flat ? new Rem(0f) : outerCornerRem;
            }

            float segmentWidth = contentRect.width / count;

            Event e = Event.current;

            for (int i = 0; i < count; i++) {
                int logicalIndex = rtl ? count - 1 - i : i;
                ButtonGroupItem item = items[logicalIndex];

                Rect segRect = new Rect(
                    contentRect.x + i * segmentWidth,
                    contentRect.y,
                    segmentWidth,
                    contentRect.height
                );

                bool isFirst = logicalIndex == 0;
                bool isLast = logicalIndex == count - 1;

                InteractionState state = InteractionState.Resolve(segRect, null, item.Disabled);
                ThemeSlot? bgSlot = VariantPalette.Background(variant, state);

                Rem leadingCorner = isFirst ? innerCornerRem : new Rem(0f);
                Rem trailingCorner = isLast ? innerCornerRem : new Rem(0f);
                RadiusSpec segRadius = rtl
                    ? new RadiusSpec(
                        TopStart: trailingCorner,
                        TopEnd: leadingCorner,
                        BottomStart: trailingCorner,
                        BottomEnd: leadingCorner
                    )
                    : new RadiusSpec(
                        TopStart: leadingCorner,
                        TopEnd: trailingCorner,
                        BottomStart: leadingCorner,
                        BottomEnd: trailingCorner
                    );

                if (bgSlot.HasValue) {
                    PaintBox.Draw(segRect, BackgroundSpec.Of(bgSlot.Value), null, segRadius);
                }

                float overlay = VariantPalette.OverlayAlpha(state);
                if (overlay > 0f) {
                    Color overlayColor = InteractionFeedback.OverlayColor(theme, state, overlay);
                    PaintBox.Draw(segRect, BackgroundSpec.Of(overlayColor), null, segRadius);
                }

                InteractionFeedback.Apply(segRect, !item.Disabled, soundEnabled);

                ThemeSlot fgSlot = VariantPalette.Foreground(variant, state);
                TextDraw.DrawWithStyle(segRect, item.Label, style, theme.GetColor(fgSlot));

                if (!item.Disabled &&
                    e.type == EventType.MouseUp &&
                    e.button == 0 &&
                    segRect.Contains(e.mousePosition)) {
                    item.OnClick?.Invoke();
                    e.Use();
                }
            }

            const float dividerPx = 2f;
            ThemeSlot dividerSlot = outerBorderSlot ?? ThemeSlot.BorderDefault;
            Color dividerColor = theme.GetColor(dividerSlot);
            for (int i = 1; i < count; i++) {
                float sepX = Mathf.Round(contentRect.x + i * segmentWidth - dividerPx * 0.5f);
                Rect sepRect = new Rect(sepX, contentRect.y, dividerPx, contentRect.height);
                PaintBox.Draw(sepRect, BackgroundSpec.Of(dividerColor), null, null);
            }
        };

        return node;
    }

    [DocVariant("CL_Playground_Label_Primary")]
    public static DocSample DocsPrimary() {
        bool forced = RenderContext.Current.ForceDisabled;
        StateHandle<string> lastPick = UseState<string>(
            (string)"CL_Playground_buttongroup_None".Translate()
        );

        return new DocSample(() => Create(
            new List<ButtonGroupItem> {
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Day".Translate(),
                    () => lastPick.Set("Day"),
                    forced
                ),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Week".Translate(),
                    () => lastPick.Set("Week"),
                    forced
                ),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Month".Translate(),
                    () => lastPick.Set("Month"),
                    forced
                ),
            },
            Variant.Primary
        ));
    }

    [DocVariant("CL_Playground_Label_Secondary")]
    public static DocSample DocsSecondary() {
        bool forced = RenderContext.Current.ForceDisabled;
        StateHandle<string> lastPick = UseState<string>(
            (string)"CL_Playground_buttongroup_None".Translate()
        );

        return new DocSample(() => Create(
            new List<ButtonGroupItem> {
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Refresh".Translate(),
                    () => lastPick.Set("Refresh"),
                    forced
                ),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Export".Translate(),
                    () => lastPick.Set("Export"),
                    forced
                ),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Delete".Translate(),
                    () => lastPick.Set("Delete"),
                    forced
                ),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_More".Translate(),
                    () => lastPick.Set("More"),
                    true
                ),
            }
        ));
    }


    [DocVariant("CL_Playground_Label_Flat")]
    public static DocSample DocsFlat() {
        bool forced = RenderContext.Current.ForceDisabled;
        StateHandle<string> active = UseState<string>("All");

        return new DocSample(() => Create(
            new List<ButtonGroupItem> {
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Flat_All".Translate(),
                    () => active.Set("All"),
                    forced
                ),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Flat_Manual".Translate(),
                    () => active.Set("Manual"),
                    forced
                ),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Flat_Autosaves".Translate(),
                    () => active.Set("Autosaves"),
                    forced
                ),
            },
            shape: ButtonGroupShape.Flat
        ));
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        StateHandle<string> lastPick = UseState<string>(
            (string)"CL_Playground_buttongroup_None".Translate()
        );

        return new DocSample(() => Create(
            new List<ButtonGroupItem> {
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Day".Translate(),
                    () => lastPick.Set("Day")),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Week".Translate(),
                    () => lastPick.Set("Week")),
                new ButtonGroupItem(
                    (string)"CL_Playground_buttongroup_Month".Translate(),
                    () => lastPick.Set("Month")),
            }
        ));
    }
}
