using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Data;

public sealed record TreeNode(
    string Label,
    IReadOnlyList<TreeNode>? Children = null,
    object? Payload = null
);

[Doc(
    Id = "tree",
    Summary = "Hierarchical, expandable list of nodes with chevron toggles.",
    WhenToUse = "Browse nested data such as locations, categories, or org structure.",
    SourcePath = "Lightweave/Data/Tree.cs",
    PreferredVariantHeight = 200f,
    ShowRtl = true
)]
public static class Tree {
    private const string ChevronCollapsedLtr = "▸";
    private const string ChevronCollapsedRtl = "◂";
    private const string ChevronExpanded = "▾";
    private static readonly Rem RowHeight = new Rem(1.75f);
    private static readonly Rem IndentPerLevel = new Rem(1.5f);
    private static readonly Rem ChevronWidth = new Rem(1.25f);
    private static readonly Rem LabelSize = new Rem(0.875f);

    public static LightweaveNode Create(
        [DocParam("Top-level nodes of the tree.")]
        IReadOnlyList<TreeNode> roots,
        [DocParam("Invoked when a leaf or label is clicked.")]
        Action<TreeNode>? onSelect = null,
        [DocParam("Override hover sound on rows/chevrons. Null = component default (false).")]
        bool? playHoverSound = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Hooks.Hooks.StateHandle<HashSet<TreeNode>> expandedState =
            Hooks.Hooks.UseState<HashSet<TreeNode>>(
                new HashSet<TreeNode>(ReferenceComparer.Instance),
                line,
                file
            );
        Hooks.Hooks.RefHandle<LightweaveScrollStatus> statusRef =
            Hooks.Hooks.UseRef(new LightweaveScrollStatus(), line, file + "#scroll");

        LightweaveNode node = NodeBuilder.New("Tree", line, file);
        node.ApplyStyling("tree", style, classes, id);

        List<(TreeNode Node, int Depth)> visibleScratch = new List<(TreeNode, int)>(64);

        node.Measure = _ => {
            if (roots == null || roots.Count == 0) {
                return 0f;
            }

            HashSet<TreeNode> expanded = expandedState.Value;
            int visibleCount = 0;
            for (int i = 0; i < roots.Count; i++) {
                visibleCount += CountVisible(roots[i], expanded);
            }

            return visibleCount * RowHeight.ToPixels();
        };

        node.MeasureWidth = () => {
            if (roots == null || roots.Count == 0) {
                return 0f;
            }
            HashSet<TreeNode> expanded = expandedState.Value;
            List<(TreeNode Node, int Depth)> visible = visibleScratch;
            visible.Clear();
            for (int i = 0; i < roots.Count; i++) {
                Flatten(roots[i], 0, expanded, visible);
            }
            Theme.Theme theme = RenderContext.Current.Theme;
            Font labelFont = theme.GetFont(FontRole.Body);
            int labelPx = Mathf.RoundToInt(new Rem(0.875f).ToFontPx());
            GUIStyle gs = GuiStyleCache.GetOrCreate(labelFont, labelPx);
            float indent = IndentPerLevel.ToPixels();
            float chevron = ChevronWidth.ToPixels();
            float pad = SpacingScale.Xs.ToPixels();
            float maxW = 0f;
            for (int i = 0; i < visible.Count; i++) {
                string label = visible[i].Node.Label ?? string.Empty;
                float labelW = gs.CalcSize(new GUIContent(label)).x;
                float w = visible[i].Depth * indent + chevron + pad + labelW + pad;
                if (w > maxW) {
                    maxW = w;
                }
            }
            return Mathf.Ceil(maxW + LightweaveScrollView.GutterPixels(true));
        };

        node.Paint = (rect, _) => {
            if (roots == null || roots.Count == 0) {
                return;
            }

            HashSet<TreeNode> expanded = expandedState.Value;
            List<(TreeNode Node, int Depth)> visible = visibleScratch;
            visible.Clear();
            for (int i = 0; i < roots.Count; i++) {
                Flatten(roots[i], 0, expanded, visible);
            }

            float rh = RowHeight.ToPixels();
            statusRef.Current.Height = visible.Count * rh;

            using (new LightweaveScrollView(rect, statusRef.Current)) {
                float scrollbarGutter = LightweaveScrollView.GutterPixels(statusRef.Current.VerticalVisible);
                float innerWidth = rect.width - scrollbarGutter;

                bool soundEnabled = playHoverSound ?? false;
                for (int i = 0; i < visible.Count; i++) {
                    Rect rowRect = new Rect(0f, i * rh, innerWidth, rh);
                    PaintRow(rowRect, visible[i].Node, visible[i].Depth, expanded, expandedState, onSelect, soundEnabled);
                }
            }
        };

        return node;
    }

    private static int CountVisible(TreeNode current, HashSet<TreeNode> expanded) {
        int count = 1;
        if (current.Children == null || current.Children.Count == 0) {
            return count;
        }

        if (!expanded.Contains(current)) {
            return count;
        }

        for (int i = 0; i < current.Children.Count; i++) {
            count += CountVisible(current.Children[i], expanded);
        }

        return count;
    }

    private static void Flatten(
        TreeNode current,
        int depth,
        HashSet<TreeNode> expanded,
        List<(TreeNode, int)> output
    ) {
        output.Add((current, depth));
        if (current.Children == null || current.Children.Count == 0) {
            return;
        }

        if (!expanded.Contains(current)) {
            return;
        }

        for (int i = 0; i < current.Children.Count; i++) {
            Flatten(current.Children[i], depth + 1, expanded, output);
        }
    }

    private static void PaintRow(
        Rect rowRect,
        TreeNode treeNode,
        int depth,
        HashSet<TreeNode> expanded,
        Hooks.Hooks.StateHandle<HashSet<TreeNode>> expandedState,
        Action<TreeNode>? onSelect,
        bool soundEnabled
    ) {
        Theme.Theme theme = RenderContext.Current.Theme;
        Direction dir = RenderContext.Current.Direction;
        bool rtl = dir == Direction.Rtl;
        Event e = Event.current;

        if (rowRect.Contains(e.mousePosition)) {
            PaintBox.DrawHighlight(rowRect, RadiusSpec.All(RadiusScale.Sm), true);
        }

        float indentPx = depth * IndentPerLevel.ToPixels();
        float chevronPx = ChevronWidth.ToPixels();
        float padPx = SpacingScale.Xs.ToPixels();
        bool hasChildren = treeNode.Children != null && treeNode.Children.Count > 0;
        bool isExpanded = hasChildren && expanded.Contains(treeNode);

        Rect chevronRect;
        Rect labelRect;
        if (rtl) {
            float chevronX = rowRect.xMax - indentPx - chevronPx;
            chevronRect = new Rect(chevronX, rowRect.y, chevronPx, rowRect.height);
            float labelEndX = chevronX - padPx;
            labelRect = new Rect(rowRect.x, rowRect.y, Mathf.Max(0f, labelEndX - rowRect.x), rowRect.height);
        }
        else {
            float chevronX = rowRect.x + indentPx;
            chevronRect = new Rect(chevronX, rowRect.y, chevronPx, rowRect.height);
            float labelStartX = chevronX + chevronPx + padPx;
            labelRect = new Rect(labelStartX, rowRect.y, Mathf.Max(0f, rowRect.xMax - labelStartX), rowRect.height);
        }

        if (hasChildren) {
            Font chevronFont = theme.GetFont(FontRole.Body);
            int chevronPixelSize = Mathf.RoundToInt(new Rem(1f).ToFontPx());
            GUIStyle chevronStyle = GuiStyleCache.GetOrCreate(chevronFont, chevronPixelSize);
            chevronStyle.alignment = TextAnchor.MiddleCenter;

            string glyph = isExpanded
                ? ChevronExpanded
                : rtl
                    ? ChevronCollapsedRtl
                    : ChevronCollapsedLtr;

            TextDraw.DrawWithStyle(chevronRect, glyph, chevronStyle, theme.GetColor(ThemeSlot.TextMuted));
        }

        Font labelFont = theme.GetFont(FontRole.Body);
        int labelPixelSize = Mathf.RoundToInt(LabelSize.ToFontPx());
        GUIStyle labelStyle = GuiStyleCache.GetOrCreate(labelFont, labelPixelSize);
        labelStyle.alignment = rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

        TextDraw.DrawWithStyle(labelRect, treeNode.Label ?? string.Empty, labelStyle, theme.GetColor(ThemeSlot.TextPrimary));

        if (hasChildren || onSelect != null) {
            Cosmere.Lightweave.Input.InteractionFeedback.Apply(rowRect, true, soundEnabled);
        }

        if (e.type == EventType.MouseUp && e.button == 0 && rowRect.Contains(e.mousePosition)) {
            if (hasChildren) {
                HashSet<TreeNode> next = new HashSet<TreeNode>(expanded, ReferenceComparer.Instance);
                if (!next.Add(treeNode)) {
                    next.Remove(treeNode);
                }

                expandedState.Set(next);
            }
            onSelect?.Invoke(treeNode);
            e.Use();
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<TreeNode> {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();

        public bool Equals(TreeNode? x, TreeNode? y) {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(TreeNode obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    private static LightweaveNode BuildSampleTree() {
        TreeNode[] roots = Hooks.Hooks.UseMemo(
            () => {
                TreeNode shatteredPlains = new TreeNode(
                    (string)"CL_Playground_Breadcrumbs_Crumb_ShatteredPlains".Translate()
                );
                TreeNode luthadel = new TreeNode(
                    (string)"CL_Playground_Breadcrumbs_Crumb_Luthadel".Translate(),
                    new[] {
                        new TreeNode((string)"CL_Playground_Breadcrumbs_Crumb_CentralDistrict".Translate()),
                        new TreeNode((string)"CL_Playground_Breadcrumbs_Crumb_VentureKeep".Translate()),
                    }
                );
                TreeNode roshar = new TreeNode(
                    (string)"CL_Playground_Breadcrumbs_Crumb_Roshar".Translate(),
                    new[] { shatteredPlains }
                );
                TreeNode scadrial = new TreeNode(
                    (string)"CL_Playground_Breadcrumbs_Crumb_Scadrial".Translate(),
                    new[] { luthadel }
                );
                return new[] { roshar, scadrial };
            },
            Array.Empty<object>()
        );

        return Tree.Create(roots);
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => BuildSampleTree(), useFullSource: true);
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => BuildSampleTree(), useFullSource: true);
    }
}