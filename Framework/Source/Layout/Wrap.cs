using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using static Cosmere.Lightweave.Doc.DocChips;

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

        node.MeasureWidth = () => {
            int flowCount = FlowCount();
            if (flowCount == 0) {
                return 0f;
            }
            float gapPx = gap.ToPixels();
            float minW = Mathf.Max(minChildWidth.ToPixels(), 1f);
            return flowCount * minW + Math.Max(0, flowCount - 1) * gapPx;
        };

        node.Measure = availableWidth => {
            int flowCount = FlowCount();
            if (flowCount == 0) {
                return 0f;
            }

            float gapPx = gap.ToPixels();
            float minW = Mathf.Max(minChildWidth.ToPixels(), 1f);
            float rowH = lineHeight.HasValue ? lineHeight.Value.ToPixels() : minW * 0.6f;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((availableWidth + gapPx) / (minW + gapPx)));
            int rows = (flowCount + perRow - 1) / perRow;
            return rows * rowH + Mathf.Max(0, rows - 1) * gapPx;
        };

        node.Paint = (rect, paintChildren) => {
            float gapPx = gap.ToPixels();
            float minW = Mathf.Max(minChildWidth.ToPixels(), 1f);
            float rowH = lineHeight.HasValue ? lineHeight.Value.ToPixels() : minW * 0.6f;
            float x = rect.x;
            float y = rect.y;
            foreach (LightweaveNode child in kids) {
                if (!child.IsInFlow()) {
                    continue;
                }
                if (x + minW > rect.xMax) {
                    x = rect.x;
                    y += rowH + gapPx;
                }

                child.MeasuredRect = new Rect(x, y, minW, rowH);
                x += minW + gapPx;
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
                    k.Add(Input.Chip.Create("Windrunner", true, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Skybreaker", false, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Dustbringer", true, tone: ChipTone.Error, showDot: false));
                    k.Add(Input.Chip.Create("Edgedancer", true, tone: ChipTone.Info, showDot: false));
                    k.Add(Input.Chip.Create("Truthwatcher", true, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Lightweaver", true, tone: ChipTone.Warn, showDot: false));
                    k.Add(Input.Chip.Create("Elsecaller", false, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Willshaper", false, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Stoneward", true, tone: ChipTone.Info, showDot: false));
                    k.Add(Input.Chip.Create("Bondsmith", true, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Mistborn", true, tone: ChipTone.Warn, showDot: false));
                    k.Add(Input.Chip.Create("Twinborn", false, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Coinshot", true, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Lurcher", false, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Smoker", false, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Seeker", false, tone: ChipTone.None, showDot: false));
                    k.Add(Input.Chip.Create("Soother", true, tone: ChipTone.Info, showDot: false));
                    k.Add(Input.Chip.Create("Rioter", true, tone: ChipTone.Error, showDot: false));
                },
                lineHeight: new Rem(1.75f)
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
