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
        [DocParam("Optional Phosphor icon shown instead of initials.", TypeOverride = "IconRef?", DefaultOverride = "null")]
        IconRef? icon = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Rem sizeRem = size ?? new Rem(3f);
        ThemeSlot fgSlot = accent ?? ThemeSlot.TextSecondary;
        ThemeSlot borderSlot = accent ?? ThemeSlot.BorderDefault;
        ThemeSlot bgSlot = background ?? ThemeSlot.SurfaceSunken;

        LightweaveNode node = NodeBuilder.New("Avatar", line, file);
        node.ApplyStyling("avatar", style, classes, id);

        float sizePx = sizeRem.ToPixels();
        node.MeasureWidth = () => sizePx;
        node.Measure = _ => sizePx;

        node.Paint = (rect, _) => {
            Rect square = new Rect(rect.x, rect.y, sizePx, sizePx);
            PaintBox.Draw(
                square,
                BackgroundSpec.Of(bgSlot),
                BorderSpec.All(new Rem(1f / 16f), borderSlot),
                RadiusSpec.All(RadiusScale.Md)
            );

            Theme.Theme theme = RenderContext.Current.Theme;
            Color fg = theme.GetColor(fgSlot);
            if (icon.HasValue) {
                IconRef ir = icon.Value;
                TextDraw.Draw(
                    square,
                    ir.Glyph,
                    FontRole.Body,
                    sizeRem * 0.5f,
                    TextAnchor.MiddleCenter,
                    fg,
                    fontOverride: ir.ResolveFont()
                );
            }
            else {
                TextDraw.Draw(
                    square,
                    initials,
                    FontRole.Display,
                    sizeRem * 0.45f,
                    TextAnchor.MiddleCenter,
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
}
