using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using UnityEngine;
using Cosmere.Lightweave.Surfaces;

namespace Cosmere.Lightweave.Types;

[Doc(
    Id = "shadows",
    Summary = "Drop and inset shadow specs resolved through theme slots.",
    WhenToUse = "Set Style.Shadow = ShadowSpec.Of(ThemeSlot.Shadow*) to anchor a surface in space. Drop shadows render behind the box, inset highlights render on top of the background.",
    SourcePath = "Lightweave/Types/ShadowSpec.cs",
    Category = "Foundation",
    PreferredVariantHeight = 300f,
    HideUsage = true,
    HideSource = true
)]
public static class ShadowsDoc {
    [DocVariant("CL_Playground_shadows_Modal", HideCode = true)]
    public static DocSample DocsModal() {
        return new DocSample(() => SampleBox(ThemeSlot.ShadowModal));
    }

    [DocVariant("CL_Playground_shadows_Card", HideCode = true)]
    public static DocSample DocsCard() {
        return new DocSample(() => SampleBox(ThemeSlot.ShadowCard));
    }

    [DocVariant("CL_Playground_shadows_Popover", HideCode = true)]
    public static DocSample DocsPopover() {
        return new DocSample(() => SampleBox(ThemeSlot.ShadowPopover));
    }

    [DocVariant("CL_Playground_shadows_Tooltip", HideCode = true)]
    public static DocSample DocsTooltip() {
        return new DocSample(() => SampleBox(ThemeSlot.ShadowTooltip));
    }

    [DocVariant("CL_Playground_shadows_Toast", HideCode = true)]
    public static DocSample DocsToast() {
        return new DocSample(() => SampleBox(ThemeSlot.ShadowToast));
    }

    [DocVariant("CL_Playground_shadows_Rim", HideCode = true)]
    public static DocSample DocsRim() {
        return new DocSample(() => SampleBox(ThemeSlot.ShadowRim));
    }

    [DocVariant("CL_Playground_shadows_InsetTop", HideCode = true)]
    public static DocSample DocsInsetTop() {
        return new DocSample(() => SampleBox(ThemeSlot.ShadowInsetTop));
    }

    [DocVariant("CL_Playground_shadows_InsetTopStrong", HideCode = true)]
    public static DocSample DocsInsetTopStrong() {
        return new DocSample(() => SampleBox(ThemeSlot.ShadowInsetTopStrong));
    }

    private static LightweaveNode SampleBox(ThemeSlot shadowSlot) {
        LightweaveNode chip = Box.Create(
            style: new Style {
                Width = new Rem(7f),
                Height = new Rem(4f),
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
                Radius = RadiusSpec.All(RadiusScale.Md),
                Shadow = ShadowSpec.Of(shadowSlot),
            }
        );

        LightweaveNode draggable = Draggable.Create(
            chip,
            initialOffset: new Vector2(120f, 24f),
            style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
            }
        );

        return Box.Create(
            c => c.Add(draggable),
            style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
                Background = BackgroundSpec.Of(new Color(0.94f, 0.91f, 0.83f, 1f)),
                Padding = new EdgeInsets(
                    Top: new Rem(2f),
                    Right: new Rem(1.5f),
                    Bottom: new Rem(5f),
                    Left: new Rem(1.5f)
                ),
                Radius = RadiusSpec.All(RadiusScale.Md),
            }
        );
    }
}
