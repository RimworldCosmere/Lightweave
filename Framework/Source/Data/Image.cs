using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Data;

[Doc(
    Id = "image",
    Summary = "Draws a Texture2D into the node rect with a fit mode and optional corner radius.",
    WhenToUse = "Render a bitmap such as a world-map background, storyteller portrait, or hero art. Falls back to a sunken placeholder when the texture is null.",
    SourcePath = "Lightweave/Data/Image.cs"
)]
public static class Image {
    public static LightweaveNode Create(
        [DocParam("Bitmap to draw. When null, a sunken placeholder box is drawn instead.", TypeOverride = "Texture2D?", DefaultOverride = "null")]
        Texture2D? texture,
        [DocParam("How the texture maps into the node rect: Cover crops to fill, Contain letterboxes, Fill stretches, None centers at native size.")]
        ImageFit fit = ImageFit.Cover,
        [DocParam("Color multiplier applied to the texture. Defaults to white (no tint).", TypeOverride = "Color?", DefaultOverride = "null")]
        Color? tint = null,
        [DocParam("Corner rounding. None leaves square corners.")]
        RadiusScale radius = RadiusScale.None,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Image", line, file);
        node.ApplyStyling("image", style, classes, id);

        Color resolvedTint = tint ?? Color.white;
        bool rounded = radius != RadiusScale.None;
        ScaleMode scaleMode = ResolveScaleMode(fit);

        node.MeasureWidth = () => {
            Style resolved = node.GetResolvedStyle();
            if (resolved.Width is { Mode: Length.Kind.Rem } fixedWidth) {
                return fixedWidth.ToPixels(0f, texture != null ? texture.width : 0f);
            }
            return texture != null ? texture.width : 0f;
        };
        node.Measure = _ => {
            Style resolved = node.GetResolvedStyle();
            if (resolved.Height is { Mode: Length.Kind.Rem } fixedHeight) {
                return fixedHeight.ToPixels(0f, texture != null ? texture.height : 0f);
            }
            return texture != null ? texture.height : 0f;
        };

        node.Layout = rect => { node.MeasuredRect = rect; };

        node.Draw = rect => {
            if (texture == null) {
                RadiusSpec? placeholderRadius = rounded ? RadiusSpec.All(radius) : null;
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.SurfaceSunken), null, placeholderRadius);
                return;
            }

            if (fit == ImageFit.None) {
                DrawNative(rect, texture, resolvedTint);
                return;
            }

            if (rounded) {
                PaintBox.Draw(
                    rect,
                    new BackgroundSpec.Textured(texture, scaleMode, resolvedTint),
                    null,
                    RadiusSpec.All(radius)
                );
                return;
            }

            PaintBox.DrawTexture(rect, texture, resolvedTint, scaleMode);
        };

        return node;
    }

    private static ScaleMode ResolveScaleMode(ImageFit fit) {
        return fit switch {
            ImageFit.Cover => ScaleMode.ScaleAndCrop,
            ImageFit.Contain => ScaleMode.ScaleToFit,
            ImageFit.Fill => ScaleMode.StretchToFill,
            _ => ScaleMode.ScaleToFit
        };
    }

    private static void DrawNative(Rect rect, Texture2D texture, Color tint) {
        float width = Mathf.Min(texture.width, rect.width);
        float height = Mathf.Min(texture.height, rect.height);
        Rect native = new Rect(
            rect.x + (rect.width - width) * 0.5f,
            rect.y + (rect.height - height) * 0.5f,
            width,
            height
        );
        PaintBox.DrawTexture(native, texture, tint, ScaleMode.ScaleToFit);
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Image.Create(BaseContent.BadTex, ImageFit.Cover, radius: RadiusScale.Md,
            style: new Style { Width = Length.Rem(8f), Height = Length.Rem(5f) }));
    }

    [DocVariant("CL_Playground_Image_Cover")]
    public static DocSample DocsCover() {
        return new DocSample(() => Image.Create(BaseContent.BadTex, ImageFit.Cover,
            style: new Style { Width = Length.Rem(8f), Height = Length.Rem(5f) }));
    }

    [DocVariant("CL_Playground_Image_Contain")]
    public static DocSample DocsContain() {
        return new DocSample(() => Image.Create(BaseContent.BadTex, ImageFit.Contain,
            style: new Style { Width = Length.Rem(8f), Height = Length.Rem(5f) }));
    }

    [DocVariant("CL_Playground_Image_Fill")]
    public static DocSample DocsFill() {
        return new DocSample(() => Image.Create(BaseContent.BadTex, ImageFit.Fill,
            style: new Style { Width = Length.Rem(8f), Height = Length.Rem(5f) }));
    }

    [DocVariant("CL_Playground_Image_None")]
    public static DocSample DocsNone() {
        return new DocSample(() => Image.Create(BaseContent.BadTex, ImageFit.None,
            style: new Style { Width = Length.Rem(8f), Height = Length.Rem(5f) }));
    }

    [DocVariant("CL_Playground_Image_Rounded")]
    public static DocSample DocsRounded() {
        return new DocSample(() => Image.Create(BaseContent.BadTex, ImageFit.Cover, radius: RadiusScale.Lg,
            style: new Style { Width = Length.Rem(8f), Height = Length.Rem(5f) }));
    }

    [DocState("CL_Playground_Image_Placeholder")]
    public static DocSample DocsPlaceholder() {
        return new DocSample(() => Image.Create(null, ImageFit.Cover, radius: RadiusScale.Md,
            style: new Style { Width = Length.Rem(8f), Height = Length.Rem(5f) }));
    }
}
