using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using static Cosmere.Lightweave.Doc.DocChips;

namespace Cosmere.Lightweave.Layout;

[Doc(
    Id = "scrollarea",
    Summary = "Scrollable viewport that clips overflow content.",
    WhenToUse = "Wrap content that may exceed the available height.",
    SourcePath = "Lightweave/Layout/ScrollArea.cs",
    PreferredVariantHeight = 160f
)]
public static class ScrollArea {
    public static LightweaveNode Create(
        [DocParam("Content to scroll within the viewport.")]
        LightweaveNode content,
        [DocParam("Optional reset key. When changed, scroll position resets.", TypeOverride = "object?", DefaultOverride = "null")]
        object? resetKey = null,
        [DocParam("Visual variant. Slim (default): always-visible accent rail. Auto: rail fades in on hover/scroll. Bare: no rail, top/bottom mask fades. Ticks: rail with clickable section markers. Progress: single growing accent bar.")]
        ScrollAreaVariant variant = ScrollAreaVariant.Slim,
        [DocParam("Tone of the rail/thumb. Accent (default) uses the surface accent color; Neutral uses a muted text color.")]
        ScrollAreaTone tone = ScrollAreaTone.Accent,
        [DocParam("Draw a 14px gradient at the top/bottom edges of the viewport when content overflows in that direction. Subtle hint of scrollability.")]
        bool edge = false,
        [DocParam("When true, content shorter than the viewport is stretched to fill the viewport. Useful for input surfaces (TextArea) that should visually fill the available space and only scroll when content exceeds it.")]
        bool stretchContent = false,
        [DocParam("Sections used by the Ticks variant. Each section is rendered as a clickable tick marker at its Pct position (0..1).", TypeOverride = "IReadOnlyList<ScrollAreaSection>?", DefaultOverride = "null")]
        IReadOnlyList<ScrollAreaSection>? sections = null,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'scroll-area' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Hooks.Hooks.RefHandle<LightweaveScrollStatus> statusRef =
            Hooks.Hooks.UseRef(new LightweaveScrollStatus(), line, file);
        Hooks.Hooks.RefHandle<object?> lastResetKey = Hooks.Hooks.UseRef<object?>(null, line, file + "#resetKey");

        if (!Equals(lastResetKey.Current, resetKey)) {
            statusRef.Current.Position = Vector2.zero;
            lastResetKey.Current = resetKey;
        }

        LightweaveNode node = BuildScrollArea(content, statusRef.Current, variant, tone, edge, stretchContent, sections, line, file);
        node.ApplyStyling("scroll-area", style, classes, id);
        return node;
    }

    public static LightweaveNode External(
        LightweaveNode content,
        LightweaveScrollStatus status,
        ScrollAreaVariant variant = ScrollAreaVariant.Slim,
        ScrollAreaTone tone = ScrollAreaTone.Accent,
        bool edge = false,
        bool stretchContent = false,
        IReadOnlyList<ScrollAreaSection>? sections = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = BuildScrollArea(content, status, variant, tone, edge, stretchContent, sections, line, file);
        node.ApplyStyling("scroll-area", style, classes, id);
        return node;
    }

    private static LightweaveNode BuildScrollArea(
        LightweaveNode content,
        LightweaveScrollStatus status,
        ScrollAreaVariant variant,
        ScrollAreaTone tone,
        bool edge,
        bool stretchContent,
        IReadOnlyList<ScrollAreaSection>? sections,
        int line,
        string file
    ) {
        LightweaveNode node = NodeBuilder.New("ScrollArea", line, file);
        node.Children.Add(content);
        node.Paint = (rect, paintChildren) => {
            float scrollbarGutter = LightweaveScrollView.GutterPixels(status.VerticalVisible, variant);
            float innerWidth = rect.width - scrollbarGutter;
            float naturalHeight = content.IsInFlow()
                ? content.Measure?.Invoke(innerWidth) ?? content.PreferredHeight ?? rect.height
                : rect.height;
            float contentHeight = stretchContent ? Mathf.Max(naturalHeight, rect.height) : naturalHeight;
            status.Height = contentHeight;
            using (new LightweaveScrollView(rect, status, variant, tone, edge, sections, content)) {
                if (content.IsInFlow()) {
                    content.MeasuredRect = new Rect(0f, 0f, innerWidth, contentHeight);
                }
                paintChildren();
            }
        };
        return node;
    }

    private static LightweaveNode DocsRows(int count) {
        return Stack.Create(
            SpacingScale.Xxs,
            s => {
                for (int i = 0; i < count; i++) {
                    s.Add(SampleChip("row " + (i + 1)), new Rem(1.75f).ToPixels());
                }
            }
        );
    }

    [DocVariant("CL_Playground_ScrollArea_Slim")]
    public static DocSample DocsSlim() {
        return new DocSample(() =>
            ScrollArea.Create(DocsRows(20))
        );
    }

    [DocVariant("CL_Playground_ScrollArea_Auto")]
    public static DocSample DocsAuto() {
        return new DocSample(() =>
            ScrollArea.Create(DocsRows(20), variant: ScrollAreaVariant.Auto)
        );
    }

    [DocVariant("CL_Playground_ScrollArea_Bare")]
    public static DocSample DocsBare() {
        return new DocSample(() =>
            ScrollArea.Create(DocsRows(20), variant: ScrollAreaVariant.Bare)
        );
    }

    [DocVariant("CL_Playground_ScrollArea_Ticks")]
    public static DocSample DocsTicks() {
        return new DocSample(() =>
            ScrollArea.Create(
                DocsRows(20),
                variant: ScrollAreaVariant.Ticks,
                sections: new[] {
                    new ScrollAreaSection("display", "DISPLAY", 0.05f),
                    new ScrollAreaSection("audio", "AUDIO", 0.25f),
                    new ScrollAreaSection("input", "INPUT", 0.5f),
                    new ScrollAreaSection("gameplay", "GAMEPLAY", 0.75f),
                    new ScrollAreaSection("network", "NETWORK", 0.95f),
                }
            )
        );
    }


    [DocVariant("CL_Playground_ScrollArea_TicksFromAnchors")]
    public static DocSample DocsTicksFromAnchors() {
        return new DocSample(() =>
            ScrollArea.Create(
                Stack.Create(SpacingScale.Xxs, c => {
                    c.Add(Section.Anchor("display", "DISPLAY", DocsRows(4)));
                    c.Add(Section.Anchor("audio", "AUDIO", DocsRows(4)));
                    c.Add(Section.Anchor("input", "INPUT", DocsRows(4)));
                    c.Add(Section.Anchor("gameplay", "GAMEPLAY", DocsRows(4)));
                    c.Add(Section.Anchor("network", "NETWORK", DocsRows(4)));
                }),
                variant: ScrollAreaVariant.Ticks
            )
        );
    }

    [DocVariant("CL_Playground_ScrollArea_Progress")]
    public static DocSample DocsProgress() {
        return new DocSample(() =>
            ScrollArea.Create(DocsRows(20), variant: ScrollAreaVariant.Progress)
        );
    }

    [DocVariant("CL_Playground_ScrollArea_Edge")]
    public static DocSample DocsEdge() {
        return new DocSample(() =>
            ScrollArea.Create(DocsRows(20), variant: ScrollAreaVariant.Slim, edge: true)
        );
    }

    [DocVariant("CL_Playground_ScrollArea_Neutral")]
    public static DocSample DocsNeutral() {
        return new DocSample(() =>
            ScrollArea.Create(DocsRows(20), tone: ScrollAreaTone.Neutral)
        );
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() =>
            ScrollArea.Create(DocsRows(8))
        );
    }
}
