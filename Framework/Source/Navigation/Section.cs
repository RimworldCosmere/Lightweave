using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Surfaces;

namespace Cosmere.Lightweave.Navigation;

[Doc(
    Id = "section",
    Summary = "Marks a region of content as a named section so an enclosing ScrollArea (variant: Ticks) can render a clickable tick marker for it.",
    WhenToUse = "Wrap each top-level section inside a Ticks ScrollArea so the rail picks up labels and positions automatically without the caller hand-supplying percentages.",
    SourcePath = "Lightweave/Navigation/Section.cs"
)]
public static class Section {
    public static LightweaveNode Anchor(
        [DocParam("Stable id used to address this section (e.g. for ScrollArea jump-to-section navigation).")]
        string id,
        [DocParam("Short uppercase label shown on the tick marker's hover popover (caller is responsible for .Translate()).")]
        string label,
        [DocParam("Section body; rendered exactly as-is. The wrapper contributes no padding, background, or border.")]
        LightweaveNode content,
        Style? style = null,
        string[]? classes = null,
        string? nodeId = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Section.Anchor", line, file);
        node.ApplyStyling("section-anchor", style, classes, nodeId);
        node.Tag = new SectionAnchorMeta(id, label);
        node.Children.Add(content);

        node.MeasureWidth = () => content.MeasureWidth?.Invoke() ?? 0f;

        node.Measure = availableWidth => {
            if (!content.IsInFlow()) {
                return 0f;
            }
            return content.Measure?.Invoke(availableWidth) ?? content.PreferredHeight ?? 0f;
        };

        node.Layout = rect => {
            if (content.IsInFlow()) {
                content.MeasuredRect = rect;
            }
        };

        return node;
    }

    private static LightweaveNode DocsSectionBody(string label) {
        return Box.Create(
            c => c.Add(Typography.Typography.Text.Create(label)),
            style: new Style {
                Padding = EdgeInsets.All(SpacingScale.Sm),
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
                Radius = RadiusSpec.All(RadiusScale.Sm),
                Height = new Rem(3f),
            }
        );
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() =>
            ScrollArea.Create(
                Stack.Create(SpacingScale.Xs, c => {
                    c.Add(Section.Anchor("intro", "INTRO", DocsSectionBody("Intro section body")));
                    c.Add(Section.Anchor("middle", "MIDDLE", DocsSectionBody("Middle section body")));
                    c.Add(Section.Anchor("end", "END", DocsSectionBody("End section body")));
                }),
                variant: ScrollAreaVariant.Ticks
            )
        );
    }
}
