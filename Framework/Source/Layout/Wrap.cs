using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using static Cosmere.Lightweave.Doc.DocChips;
using Cosmere.Lightweave.Data;

namespace Cosmere.Lightweave.Layout;

[Doc(
    Id = "wrap",
    Summary = "Wrapping flow that re-flows children onto new rows when the line fills.",
    WhenToUse = "Variable count of equally-sized chips that should wrap to width.",
    SourcePath = "Lightweave/Layout/Wrap.cs",
    PreferredVariantHeight = 120f,
    ShowRtl = true
)]
public static class Wrap {
    public static LightweaveNode Create(
        [DocParam("Gap between cells.", TypeOverride = "Rem", DefaultOverride = "0")]
        Rem gap = default,
        [DocParam("Minimum width per cell. Drives how many fit per row.", TypeOverride = "Rem", DefaultOverride = "0")]
        Rem minChildWidth = default,
        [DocParam("Builder callback to populate children.")]
        Action<List<LightweaveNode>>? children = null,
        [DocParam("Optional explicit row height. Falls back to minChildWidth * 0.6 when unset.", TypeOverride = "Rem?", DefaultOverride = "null")]
        Rem? lineHeight = null,
        [DocParam("Flow cells at each child's natural width instead of a uniform grid. Use for content-sized tags/chips that should wrap like text.", TypeOverride = "bool", DefaultOverride = "false")]
        bool flow = false,
        [DocParam("In uniform (non-flow) mode, stretch cells to fill the row: column count is derived from minChildWidth, then each cell expands to an equal share so the right edge is flush, and row height follows each row's tallest child. This is the responsive auto-fit grid behaviour (CSS repeat(auto-fit, minmax(min, 1fr))). Ignored when flow is true.", TypeOverride = "bool", DefaultOverride = "false")]
        bool stretch = false,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'wrap' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        List<LightweaveNode> kids = new List<LightweaveNode>();
        children?.Invoke(kids);
        LightweaveNode node = NodeBuilder.New("Wrap", line, file);
        node.ApplyStyling("wrap", style, classes, id);
        node.Children.AddRange(kids);

        int FlowCount() {
            int c = 0;
            for (int i = 0; i < kids.Count; i++) {
                if (kids[i].IsInFlow()) {
                    c++;
                }
            }
            return c;
        }

        System.Collections.Generic.List<LightweaveNode> CollectFlow() {
            System.Collections.Generic.List<LightweaveNode> flow = new System.Collections.Generic.List<LightweaveNode>(kids.Count);
            for (int i = 0; i < kids.Count; i++) {
                if (kids[i].IsInFlow()) {
                    flow.Add(kids[i]);
                }
            }
            return flow;
        }

        int PerRow(float availableWidth, float gapPx, float minW) {
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + gapPx) / (minW + gapPx)));
        }

        float CellWidth(LightweaveNode child, float minW) {
            if (!flow) {
                return minW;
            }
            float natural = child.MeasureWidth?.Invoke() ?? minW;
            return Mathf.Max(natural, 1f);
        }

        float RowHeight(float minW) {
            if (lineHeight.HasValue) {
                return lineHeight.Value.ToPixels();
            }
            if (flow) {
                float maxChildHeight = 0f;
                for (int i = 0; i < kids.Count; i++) {
                    if (!kids[i].IsInFlow()) {
                        continue;
                    }
                    // Prefer an explicit PreferredHeight, but fall back to Measure() at the child's own
                    // cell width. Without the Measure fallback, content-sized chips (a Box wrapping an
                    // HStack of avatar + text + buttons) report no PreferredHeight, so the row collapses
                    // to the 1.75rem default and clips the chip's taller content.
                    float childHeight = kids[i].PreferredHeight
                        ?? (kids[i].Measure?.Invoke(CellWidth(kids[i], minW)) ?? 0f);
                    if (childHeight > maxChildHeight) {
                        maxChildHeight = childHeight;
                    }
                }
                return maxChildHeight > 0f ? maxChildHeight : new Rem(1.75f).ToPixels();
            }
            return minW * 0.6f;
        }

        (float left, float top, float right, float bottom) ResolvePaddingPixels() {
            Style s = node.GetResolvedStyle();
            EdgeInsets pad = s.Padding ?? EdgeInsets.Zero;
            return pad.Resolve(RenderContext.Current.Direction);
        }

        node.MeasureWidth = () => {
            int flowCount = FlowCount();
            if (flowCount == 0) {
                return 0f;
            }
            (float pl, _, float pr, _) = ResolvePaddingPixels();
            float gapPx = gap.ToPixels();
            float minW = Mathf.Max(minChildWidth.ToPixels(), 1f);
            if (flow) {
                float total = 0f;
                bool first = true;
                foreach (LightweaveNode child in kids) {
                    if (!child.IsInFlow()) {
                        continue;
                    }
                    if (!first) {
                        total += gapPx;
                    }
                    total += CellWidth(child, minW);
                    first = false;
                }
                return total + pl + pr;
            }
            return flowCount * minW + Math.Max(0, flowCount - 1) * gapPx + pl + pr;
        };

        node.Measure = availableWidth => {
            int flowCount = FlowCount();
            if (flowCount == 0) {
                return 0f;
            }

            // Account for our own Style.Padding: the paint pipeline shrinks our rect by it before
            // children lay out, so the column math (and returned height) must use the same shrunk
            // width - otherwise a padded Wrap measures a different column count than it paints,
            // and a wrapping ScrollArea over it under-measures and refuses to scroll.
            (float padL, float padT, float padR, float padB) = ResolvePaddingPixels();
            float vPad = padT + padB;
            availableWidth = Mathf.Max(0f, availableWidth - padL - padR);

            float gapPx = gap.ToPixels();
            float minW = Mathf.Max(minChildWidth.ToPixels(), 1f);
            float rowH = RowHeight(minW);

            if (flow) {
                int rows = 1;
                float x = 0f;
                foreach (LightweaveNode child in kids) {
                    if (!child.IsInFlow()) {
                        continue;
                    }
                    float cw = CellWidth(child, minW);
                    if (x > 0f && x + gapPx + cw > availableWidth) {
                        rows++;
                        x = cw;
                    }
                    else {
                        x += (x > 0f ? gapPx : 0f) + cw;
                    }
                }
                return rows * rowH + Mathf.Max(0, rows - 1) * gapPx + vPad;
            }

            int perRow = PerRow(availableWidth, gapPx, minW);

            if (stretch) {
                float cellW = (availableWidth - (perRow - 1) * gapPx) / perRow;
                System.Collections.Generic.List<LightweaveNode> flow = CollectFlow();
                int rows = (flow.Count + perRow - 1) / perRow;
                float total = 0f;
                for (int r = 0; r < rows; r++) {
                    float rowMax = 0f;
                    for (int c = 0; c < perRow; c++) {
                        int idx = r * perRow + c;
                        if (idx >= flow.Count) {
                            break;
                        }
                        float h = flow[idx].Measure?.Invoke(cellW) ?? flow[idx].PreferredHeight ?? 0f;
                        if (h > rowMax) {
                            rowMax = h;
                        }
                    }
                    total += rowMax;
                }
                return total + gapPx * Mathf.Max(0, rows - 1) + vPad;
            }

            int gridRows = (flowCount + perRow - 1) / perRow;
            return gridRows * rowH + Mathf.Max(0, gridRows - 1) * gapPx + vPad;
        };

        node.Paint = (rect, paintChildren) => {
            float gapPx = gap.ToPixels();
            float minW = Mathf.Max(minChildWidth.ToPixels(), 1f);

            if (stretch && !flow) {
                int perRow = PerRow(rect.width, gapPx, minW);
                float cellW = (rect.width - (perRow - 1) * gapPx) / perRow;
                System.Collections.Generic.List<LightweaveNode> flowKids = CollectFlow();
                float yy = rect.y;
                for (int i = 0; i < flowKids.Count;) {
                    float rowMax = 0f;
                    for (int c = 0; c < perRow && i + c < flowKids.Count; c++) {
                        float h = flowKids[i + c].Measure?.Invoke(cellW) ?? flowKids[i + c].PreferredHeight ?? 0f;
                        if (h > rowMax) {
                            rowMax = h;
                        }
                    }
                    float xx = rect.x;
                    for (int c = 0; c < perRow && i < flowKids.Count; c++, i++) {
                        flowKids[i].MeasuredRect = new Rect(xx, yy, cellW, rowMax);
                        xx += cellW + gapPx;
                    }
                    yy += rowMax + gapPx;
                }
                paintChildren();
                return;
            }

            float rowH = RowHeight(minW);
            float x = rect.x;
            float y = rect.y;
            foreach (LightweaveNode child in kids) {
                if (!child.IsInFlow()) {
                    continue;
                }
                float cw = CellWidth(child, minW);
                if (x > rect.x && x + cw > rect.xMax) {
                    x = rect.x;
                    y += rowH + gapPx;
                }
                else if (!flow && x + minW > rect.xMax) {
                    x = rect.x;
                    y += rowH + gapPx;
                }

                child.MeasuredRect = new Rect(x, y, cw, rowH);
                x += cw + gapPx;
            }

            paintChildren();
        };
        return node;
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => 
            Wrap.Create(
                SpacingScale.Xs,
                new Rem(3f),
                k => {
                    k.Add(SampleChip("one"));
                    k.Add(SampleChip("two"));
                    k.Add(SampleChip("three"));
                    k.Add(SampleChip("four"));
                }
            )
        );
    }

[DocVariant("CL_Playground_Layout_Wrap_Badges", Order = 1)]
    public static DocSample DocsBadges() {
        return new DocSample(() =>
            Wrap.Create(
                SpacingScale.Xs,
                new Rem(7.5f),
                k => {
                    k.Add(Data.Chip.Create("Windrunner", state: true, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Skybreaker", state: false, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Dustbringer", state: true, variant: ChipVariant.Error, showDot: false));
                    k.Add(Data.Chip.Create("Edgedancer", state: true, variant: ChipVariant.Info, showDot: false));
                    k.Add(Data.Chip.Create("Truthwatcher", state: true, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Lightweaver", state: true, variant: ChipVariant.Warn, showDot: false));
                    k.Add(Data.Chip.Create("Elsecaller", state: false, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Willshaper", state: false, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Stoneward", state: true, variant: ChipVariant.Info, showDot: false));
                    k.Add(Data.Chip.Create("Bondsmith", state: true, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Mistborn", state: true, variant: ChipVariant.Warn, showDot: false));
                    k.Add(Data.Chip.Create("Twinborn", state: false, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Coinshot", state: true, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Lurcher", state: false, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Smoker", state: false, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Seeker", state: false, variant: ChipVariant.None, showDot: false));
                    k.Add(Data.Chip.Create("Soother", state: true, variant: ChipVariant.Info, showDot: false));
                    k.Add(Data.Chip.Create("Rioter", state: true, variant: ChipVariant.Error, showDot: false));
                },
                lineHeight: new Rem(1.75f)
            )
        );
    }

    [DocVariant("CL_Playground_Layout_Wrap_Flow", Order = 2)]
    public static DocSample DocsFlow() {
        return new DocSample(() =>
            Wrap.Create(
                SpacingScale.Xs,
                children: k => {
                    k.Add(Data.Chip.Create("Silver x800", upper: false, bold: false));
                    k.Add(Data.Chip.Create("Packaged survival meals x10", upper: false, bold: false));
                    k.Add(Data.Chip.Create("Medicine x30", upper: false, bold: false));
                    k.Add(Data.Chip.Create("Bolt-action rifle", upper: false, bold: false));
                    k.Add(Data.Chip.Create("Knife", upper: false, bold: false));
                    k.Add(Data.Chip.Create("Flak vest", upper: false, bold: false));
                },
                flow: true
            )
        );
    }

    [DocVariant("CL_Playground_Layout_Wrap_Stretch", Order = 3)]
    public static DocSample DocsStretch() {
        return new DocSample(() =>
            Wrap.Create(
                SpacingScale.Xs,
                new Rem(8f),
                k => {
                    k.Add(SampleChip("alpha"));
                    k.Add(SampleChip("beta"));
                    k.Add(SampleChip("gamma"));
                    k.Add(SampleChip("delta"));
                    k.Add(SampleChip("epsilon"));
                },
                stretch: true
            )
        );
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => 
            Wrap.Create(
                SpacingScale.Xs,
                new Rem(3f),
                k => {
                    k.Add(SampleChip("alpha"));
                    k.Add(SampleChip("beta"));
                    k.Add(SampleChip("gamma"));
                }
            )
        );
    }
}
