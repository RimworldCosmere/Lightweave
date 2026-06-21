using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.Data;

[Doc(
    Id = "avatar",
    Summary = "Fixed-size rounded portrait tile with centered initials or an icon glyph.",
    WhenToUse = "Pawn pickers, profile chips, member rows, anywhere a person/entity needs a small visual anchor. Reports its size to the parent layout so it does not overlap siblings.",
    SourcePath = "Lightweave/Blocks/Avatar.cs"
)]
public static class Avatar {
    public static LightweaveNode Create(
        [DocParam("Short initials or single glyph string shown centered (\"V\", \"K\", \"Sz\").")]
        string initials,
        [DocParam("Square edge length. Width and height share this value.")]
        Rem? size = null,
        [DocParam("Accent slot used for the border and text color. When null, uses BorderDefault + TextSecondary.", TypeOverride = "ThemeSlot?", DefaultOverride = "null")]
        ThemeSlot? accent = null,
        [DocParam("Background slot. Defaults to SurfaceSunken.", TypeOverride = "ThemeSlot?", DefaultOverride = "null")]
        ThemeSlot? background = null,
        [DocParam("Border color slot. Defaults to the accent (so the frame matches the icon tone). Pass a subtle slot (e.g. BorderSubtle) to de-emphasize the frame independently of the icon color.", TypeOverride = "ThemeSlot?", DefaultOverride = "null")]
        ThemeSlot? border = null,
        [DocParam("Optional Phosphor icon shown instead of initials.", TypeOverride = "IconRef?", DefaultOverride = "null")]
        IconRef? icon = null,
        [DocParam("Optional bitmap (e.g. a def icon) drawn centered and tinted with the accent color. Takes priority over icon and initials.", TypeOverride = "Texture2D?", DefaultOverride = "null")]
        Texture2D? texture = null,
        [DocParam("Icon/texture size as a fraction of the badge edge. Default 0.5; raise to make the icon (glyph or bitmap) read larger inside the badge.")]
        float iconScale = 0.5f,
        [DocParam("When false, a bitmap texture is drawn untinted (its own colors) instead of being tinted with the accent. Use false to preserve the identity of detailed/colored source icons. No effect on glyph/initials.")]
        bool tintTexture = true,
        [DocParam("Optional literal color to tint a bitmap texture with, in place of the accent slot color. Only applies when tintTexture is true and a texture is set. Use for source-colored symbols like an ideoligion icon (Ideo.Color).", TypeOverride = "Color?", DefaultOverride = "null")]
        Color? tintColor = null,
        [DocParam("When false, the badge frame (background + border) is not painted; only the symbol (glyph/texture/initials) is drawn. Use when an outer container (e.g. a SelectableSurface tile) already supplies the box, so the icon doesn't read as a box-in-a-box.")]
        bool chrome = true,
        [DocParam("Corner radius scale for the badge frame. Defaults to Md; pass Full for a circular badge.", TypeOverride = "RadiusScale?", DefaultOverride = "null")]
        RadiusScale? radius = null,
        [DocParam("Font role used to render initials/digits. Defaults to Display (serif). Pass Mono for digits that must sit dead-center: serif numerals carry asymmetric side-bearings and read off-center in a small badge, where tabular mono figures center cleanly.")]
        FontRole initialsFont = FontRole.Display,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Rem sizeRem = size ?? new Rem(3f);
        ThemeSlot fgSlot = accent ?? ThemeSlot.TextSecondary;
        ThemeSlot borderSlot = border ?? accent ?? ThemeSlot.BorderDefault;
        ThemeSlot bgSlot = background ?? ThemeSlot.SurfaceSunken;

        LightweaveNode node = NodeBuilder.New("Avatar", line, file);
        node.ApplyStyling("avatar", style, classes, id);

        float sizePx = sizeRem.ToPixels();
        node.MeasureWidth = () => sizePx;
        node.Measure = _ => sizePx;

        node.Paint = (rect, _) => {
            Rect square = new Rect(rect.x, rect.y, sizePx, sizePx);
            if (chrome) {
                PaintBox.Draw(
                    square,
                    BackgroundSpec.Of(bgSlot),
                    BorderSpec.All(new Rem(1f / 16f), borderSlot),
                    RadiusSpec.All(radius ?? RadiusScale.Md)
                );
            }

            Theme.Theme theme = RenderContext.Current.Theme;
            Color fg = theme.GetColor(fgSlot);
            if (texture != null) {
                float inset = sizePx * (1f - iconScale) * 0.5f;
                Rect glyphRect = new Rect(square.x + inset, square.y + inset, sizePx - inset * 2f, sizePx - inset * 2f);
                Color texTint = tintTexture ? (tintColor ?? fg) : Color.white;
                PaintBox.DrawTexture(glyphRect, texture, texTint, ScaleMode.ScaleToFit);
            }
            else if (icon.HasValue) {
                IconRef ir = icon.Value;
                TextDraw.Draw(
                    square,
                    ir.Glyph,
                    FontRole.Body,
                    sizeRem * iconScale,
                    TextAnchor.MiddleCenter,
                    fg,
                    fontOverride: ir.ResolveFont()
                );
            }
            else {
                // Optically center on the glyph ink (not the line box) so cap-height digits/initials sit
                // dead-center in the circle. TextDraw.DrawCentered resolves the first glyph's
                // CharacterInfo for the vertical center - robust across font roles, no per-font fudge.
                TextDraw.DrawCentered(
                    square,
                    initials,
                    initialsFont,
                    sizeRem * 0.45f,
                    fg
                );
            }
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Avatar.Create("V", size: new Rem(3f)));
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => Avatar.Create("K", size: new Rem(3f)));
    }

    [DocVariant("CL_Playground_Label_Large")]
    public static DocSample DocsLarge() {
        return new DocSample(() => Avatar.Create("Sz", size: new Rem(4.5f)));
    }

    [DocVariant("CL_Playground_Label_Accent")]
    public static DocSample DocsAccent() {
        return new DocSample(() => Avatar.Create("D", size: new Rem(3.5f), accent: ThemeSlot.SurfaceAccent));
    }

    [DocVariant("CL_Playground_Label_Icon")]
    public static DocSample DocsIcon() {
        return new DocSample(() => Avatar.Create("", size: new Rem(3f), icon: Icons.Phosphor.User));
    }

    [DocVariant("CL_Playground_Avatar_Chromeless")]
    public static DocSample DocsChromeless() {
        return new DocSample(() => Avatar.Create("", size: new Rem(3f), icon: Icons.Phosphor.User, accent: ThemeSlot.SurfaceAccent, chrome: false));
    }
}
