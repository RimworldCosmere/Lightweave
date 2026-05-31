using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using Verse;
using static Cosmere.Lightweave.Hooks.Hooks;
using static Cosmere.Lightweave.Typography.Typography;

namespace Cosmere.Lightweave.Data;

[Doc(
    Id = "checkbox-tree",
    Summary = "Hierarchical checkbox list with tri-state folder rows and chevron expansion.",
    WhenToUse = "Let the user include/exclude items in a nested structure such as a file tree. Controlled: the caller owns checked state, the tree owns expansion.",
    SourcePath = "Lightweave/Data/CheckboxTree.cs",
    PreferredVariantHeight = 220f
)]
public static class CheckboxTree {
    private static readonly Rem IndentPerLevel = new Rem(1.25f);
    private static readonly Rem ChevronIconSize = new Rem(0.875f);

    public static LightweaveNode Create(
        [DocParam("Top-level nodes of the tree.")]
        IReadOnlyList<CheckboxTreeNode> roots,
        [DocParam("Returns whether the node with this key is checked. Used when stateOf is not supplied.")]
        Func<string, bool> isChecked,
        [DocParam("Invoked with the toggled node and its new value. Caller owns propagation to children.")]
        Action<CheckboxTreeNode, bool> onToggle,
        [DocParam("Optional resolver for a node's tri-state. Supply to render Mixed folders; falls back to isChecked.", TypeOverride = "Func<CheckboxTreeNode, TriState>?", DefaultOverride = "null")]
        Func<CheckboxTreeNode, TriState>? stateOf = null,
        [DocParam("Disables every row and chevron.")]
        bool disabled = false,
        [DocParam("Override hover sound on chevrons. Null = component default.", TypeOverride = "bool?", DefaultOverride = "null")]
        bool? playHoverSound = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        StateHandle<HashSet<string>> expandedState = UseState(new HashSet<string>(), line, file);
        HashSet<string> expanded = expandedState.Value;

        float chevronColPx = ChevronIconSize.ToPixels() + SpacingScale.Xs.ToPixels() * 2f;

        TriState ResolveState(CheckboxTreeNode node) {
            if (stateOf != null) {
                return stateOf(node);
            }

            return isChecked(node.Key) ? TriState.Checked : TriState.Unchecked;
        }

        void ToggleExpanded(string key) {
            HashSet<string> next = new HashSet<string>(expanded);
            if (!next.Add(key)) {
                next.Remove(key);
            }

            expandedState.Set(next);
        }

        void AppendRow(StackBuilder builder, CheckboxTreeNode node, int depth) {
            bool hasChildren = node.Children != null && node.Children.Count > 0;
            bool isExpanded = hasChildren && expanded.Contains(node.Key);
            TriState state = ResolveState(node);

            LightweaveNode row = HStack.Create(
                gap: SpacingScale.Xs,
                children: cols => {
                    if (hasChildren) {
                        LightweaveNode chevron = Glyph.Create(
                            isExpanded ? Icons.Phosphor.CaretDown : Icons.Phosphor.CaretRight,
                            style: new Style { FontSize = ChevronIconSize, TextColor = ThemeSlot.TextMuted }
                        );
                        cols.Add(
                            IconButton.Create(
                                icon: chevron,
                                onClick: () => ToggleExpanded(node.Key),
                                variant: Variant.Ghost,
                                iconSize: ChevronIconSize,
                                disabled: disabled,
                                playHoverSound: playHoverSound
                            ),
                            chevronColPx
                        );
                    }
                    else {
                        cols.Add(Box.Create(), chevronColPx);
                    }

                    cols.AddHug(Checkbox.Create(
                        label: node.Label,
                        value: state == TriState.Checked,
                        onChange: v => onToggle(node, v),
                        disabled: disabled,
                        indeterminate: state == TriState.Mixed
                    ));
                },
                style: new Style { Padding = new EdgeInsets(Left: IndentPerLevel * depth) }
            );

            builder.Add(row);

            if (!isExpanded) {
                return;
            }

            for (int i = 0; i < node.Children!.Count; i++) {
                AppendRow(builder, node.Children[i], depth + 1);
            }
        }

        string[]? mergedClasses = StyleExtensions.PrependClass("checkbox-tree", classes);

        return Stack.Create(
            gap: SpacingScale.Xxs,
            children: builder => {
                for (int i = 0; i < roots.Count; i++) {
                    AppendRow(builder, roots[i], 0);
                }
            },
            style: style,
            classes: mergedClasses,
            id: id,
            line: line,
            file: file
        );
    }

    private static CheckboxTreeNode[] SampleRoots() {
        return UseMemo(
            () => new[] {
                new CheckboxTreeNode((string)"CL_Playground_CheckboxTree_About".Translate(), "About", new[] {
                    new CheckboxTreeNode("About.xml", "About/About.xml"),
                    new CheckboxTreeNode("Preview.png", "About/Preview.png"),
                    new CheckboxTreeNode("PublishedFileId.txt", "About/PublishedFileId.txt"),
                }),
                new CheckboxTreeNode((string)"CL_Playground_CheckboxTree_Source".Translate(), "Source", new[] {
                    new CheckboxTreeNode((string)"CL_Playground_CheckboxTree_Assemblies".Translate(), "Source/Assemblies", new[] {
                        new CheckboxTreeNode("Lightweave.dll", "Source/Assemblies/Lightweave.dll"),
                    }),
                    new CheckboxTreeNode(".gitignore", "Source/.gitignore"),
                }),
                new CheckboxTreeNode((string)"CL_Playground_CheckboxTree_Defs".Translate(), "Defs", new[] {
                    new CheckboxTreeNode("ThingDefs.xml", "Defs/ThingDefs.xml"),
                }),
            },
            Array.Empty<object>()
        );
    }

    private static void CollectLeafKeys(CheckboxTreeNode node, List<string> output) {
        if (node.Children == null || node.Children.Count == 0) {
            output.Add(node.Key);
            return;
        }

        for (int i = 0; i < node.Children.Count; i++) {
            CollectLeafKeys(node.Children[i], output);
        }
    }

    private static TriState SampleState(CheckboxTreeNode node, HashSet<string> checkedKeys) {
        List<string> leaves = new List<string>();
        CollectLeafKeys(node, leaves);
        int on = 0;
        for (int i = 0; i < leaves.Count; i++) {
            if (checkedKeys.Contains(leaves[i])) {
                on++;
            }
        }

        if (on == 0) {
            return TriState.Unchecked;
        }

        return on == leaves.Count ? TriState.Checked : TriState.Mixed;
    }

    private static void SampleToggle(CheckboxTreeNode node, bool on, HashSet<string> set) {
        List<string> leaves = new List<string>();
        CollectLeafKeys(node, leaves);
        for (int i = 0; i < leaves.Count; i++) {
            if (on) {
                set.Add(leaves[i]);
            }
            else {
                set.Remove(leaves[i]);
            }
        }
    }

    private static LightweaveNode BuildSample(IReadOnlyCollection<string> initialChecked, bool disabled = false) {
        CheckboxTreeNode[] roots = SampleRoots();
        StateHandle<HashSet<string>> checkedState = UseState(new HashSet<string>(initialChecked));
        HashSet<string> checkedKeys = checkedState.Value;

        return Create(
            roots: roots,
            isChecked: checkedKeys.Contains,
            onToggle: (node, on) => {
                HashSet<string> next = new HashSet<string>(checkedKeys);
                SampleToggle(node, on, next);
                checkedState.Set(next);
            },
            stateOf: node => SampleState(node, checkedKeys),
            disabled: disabled
        );
    }

    [DocVariant("CL_Playground_CheckboxTree_AllIncluded")]
    public static DocSample DocsAllIncluded() {
        return new DocSample(() => BuildSample([
            "About/About.xml", "About/Preview.png", "About/PublishedFileId.txt",
            "Source/Assemblies/Lightweave.dll", "Source/.gitignore",
            "Defs/ThingDefs.xml",
        ]), useFullSource: true);
    }

    [DocVariant("CL_Playground_CheckboxTree_PartialFolder", Order = 1)]
    public static DocSample DocsPartialFolder() {
        return new DocSample(() => BuildSample([
            "About/About.xml", "About/Preview.png",
            "Defs/ThingDefs.xml",
        ]), useFullSource: true);
    }

    [DocVariant("CL_Playground_CheckboxTree_DeepNesting", Order = 2)]
    public static DocSample DocsDeepNesting() {
        return new DocSample(() => BuildSample([
            "Source/Assemblies/Lightweave.dll",
        ]), useFullSource: true);
    }

    [DocState("CL_Playground_Label_Disabled")]
    public static DocSample DocsDisabled() {
        return new DocSample(() => BuildSample([
            "About/About.xml", "Defs/ThingDefs.xml",
        ], disabled: true));
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => BuildSample([
            "About/About.xml", "About/Preview.png", "About/PublishedFileId.txt",
            "Defs/ThingDefs.xml",
        ]), useFullSource: true);
    }
}
