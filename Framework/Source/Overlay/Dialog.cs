using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Hooks.Hooks;
using static Cosmere.Lightweave.Typography.Typography;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Overlay;

[Doc(
    Id = "dialog",
    Summary = "A focused card surface to drop inside any container.",
    WhenToUse = "For card-shaped content blocks. For full-screen modal experiences, open a LightweaveWindow with a Dialog as its body.",
    SourcePath = "Lightweave/Overlay/Dialog.cs"
)]
public static class Dialog {
    public static LightweaveNode Create(
        [DocParam("Builds the dialog content node.")]
        Func<LightweaveNode> content,
        [DocParam("Card background. Defaults to BackgroundSpec.Of(ThemeSlot.SurfaceRaised).", TypeOverride = "BackgroundSpec?", DefaultOverride = "null")]
        BackgroundSpec? background = null,
        [DocParam("Card border. Defaults to 1/16rem BorderSubtle on all sides.", TypeOverride = "BorderSpec?", DefaultOverride = "null")]
        BorderSpec? border = null,
        [DocParam("Card padding (between border and content). Defaults to SpacingScale.Md.", TypeOverride = "EdgeInsets?", DefaultOverride = "null")]
        EdgeInsets? padding = null,
        [DocParam("Card corner radius. Defaults to RadiusScale.Md.", TypeOverride = "RadiusSpec?", DefaultOverride = "null")]
        RadiusSpec? radius = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        BackgroundSpec resolvedBg = background ?? BackgroundSpec.Of(ThemeSlot.SurfaceRaised);
        BorderSpec resolvedBorder = border ?? BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle);
        EdgeInsets resolvedPadding = padding ?? EdgeInsets.All(SpacingScale.Md);
        RadiusSpec resolvedRadius = radius ?? RadiusSpec.All(RadiusScale.Md);

        LightweaveNode card = Box.Create(
            children: c => c.Add(content()),
            style: new Style {
                Background = resolvedBg,
                Border = resolvedBorder,
                Padding = resolvedPadding,
                Radius = resolvedRadius,
            }
        );
        card.ApplyStyling("dialog", style, classes, id);
        return card;
    }

    private static LightweaveNode BuildPreview(
        Variant confirmVariant = default
    ) {
        return Create(
            content: () => Card.Create(c => {
                c.Add(Card.Header(h => {
                    h.Add(Card.Title((string)"CL_Playground_Dialog_Heading".Translate()));
                    h.Add(Card.Description((string)"CL_Playground_Dialog_Body".Translate()));
                }));
                c.Add(Card.Footer(f => {
                    f.Add(Button.Create(
                        (string)"CL_Playground_Dialog_Cancel".Translate(),
                        () => { },
                        Variant.Secondary
                    ));
                    f.Add(Button.Create(
                        (string)"CL_Playground_Dialog_Confirm".Translate(),
                        () => { },
                        confirmVariant
                    ));
                }));
            }),
            background: BackgroundSpec.Of(Color.clear),
            border: new BorderSpec(),
            padding: EdgeInsets.Zero
        );
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => BuildPreview(), useFullSource: true);
    }

    [DocVariant("CL_Playground_Dialog_Variant_Danger", Order = 1)]
    public static DocSample DocsDanger() {
        return new DocSample(() => BuildPreview(confirmVariant: Variant.Danger), useFullSource: true);
    }

    [DocVariant("CL_Playground_Dialog_Variant_Sharp", Order = 2)]
    public static DocSample DocsSharp() {
        return new DocSample(() =>
            Create(
                content: () => Stack.Create(SpacingScale.Sm, c => {
                    c.Add(Heading.Create(3, (string)"CL_Playground_Dialog_Heading".Translate()));
                    c.Add(Text.Create(
                        (string)"CL_Playground_Dialog_Body".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextSecondary }
                    ));
                }),
                radius: RadiusSpec.None
            ),
            useFullSource: true
        );
    }

    [DocVariant("CL_Playground_Dialog_Variant_Compact", Order = 3)]
    public static DocSample DocsCompact() {
        return new DocSample(() =>
            Create(
                content: () => Text.Create(
                    (string)"CL_Playground_Dialog_Body".Translate(),
                    wrap: true,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextSecondary }
                ),
                padding: EdgeInsets.All(SpacingScale.Sm),
                radius: RadiusSpec.All(RadiusScale.Sm)
            ),
            useFullSource: true
        );
    }

    [DocVariant("CL_Playground_Dialog_Variant_NoBorder", Order = 4)]
    public static DocSample DocsNoBorder() {
        return new DocSample(() =>
            Create(
                content: () => Stack.Create(SpacingScale.Sm, c => {
                    c.Add(Heading.Create(3, (string)"CL_Playground_Dialog_Heading".Translate()));
                    c.Add(Text.Create(
                        (string)"CL_Playground_Dialog_Body".Translate(),
                        wrap: true,
                        style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextSecondary }
                    ));
                }),
                border: new BorderSpec()
            ),
            useFullSource: true
        );
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => BuildPreview(), useFullSource: true);
    }
}
