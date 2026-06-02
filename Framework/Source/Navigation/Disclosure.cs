using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Navigation;

[Doc(
    Id = "disclosure",
    Summary = "Inline expand/collapse: a toggle row that reveals content in-flow below it.",
    WhenToUse = "An in-place reveal (an add-picker, an advanced-options panel) — distinct from Menu/Popover, which float over the surface.",
    SourcePath = "Lightweave/Navigation/Disclosure.cs"
)]
public static class Disclosure {
    public static LightweaveNode Create(
        [DocParam("Toggle label. Callers translate and case it; the primitive renders it verbatim.")]
        string label,
        [DocParam("Content revealed in-flow below the toggle when open.")]
        LightweaveNode content,
        [DocParam("Optional leading node on the toggle (a '+' glyph, an icon).", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
        LightweaveNode? leading = null,
        [DocParam("Initial open state the first time this disclosure renders.")]
        bool defaultOpen = false,
        [DocParam("Fired with the new open state whenever the toggle flips.", TypeOverride = "Action<bool>?", DefaultOverride = "null")]
        Action<bool>? onToggle = null,
        [DocParam("Toggle-row style while closed (surface bg/border/radius + label font).", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? toggleStyle = null,
        [DocParam("Toggle-row style while open. Falls back to toggleStyle when null.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? openToggleStyle = null,
        [DocParam("Gap between the toggle and the revealed content when open.", TypeOverride = "Rem", DefaultOverride = "0.3125rem")]
        Rem openGap = default,
        [DocParam("Gap between the toggle's leading/label/trailing. Defaults to OptionRow's SpacingScale.Sm.", TypeOverride = "Rem?", DefaultOverride = "null")]
        Rem? toggleGap = null,
        [DocParam("Vertical nudge for the toggle label only, to line an all-caps label up with a leading glyph.", TypeOverride = "Rem?", DefaultOverride = "null")]
        Rem? labelOffsetY = null,
        [DocParam("Caret glyph color. Defaults to the accent slot.", TypeOverride = "ThemeSlot?", DefaultOverride = "null")]
        ThemeSlot? caretColor = null,
        [DocParam("Style for the outer wrapper.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'disclosure' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Disclosure:" + label, line, file);
        node.ApplyStyling("disclosure", style, classes, id);

        StateHandle<bool> openState = UseState(defaultOpen);
        bool open = openState.Value;

        void Toggle() {
            bool next = !open;
            openState.Set(next);
            onToggle?.Invoke(next);
        }

        LightweaveNode caret = Glyph.Create(
            open ? Icons.Phosphor.CaretUp : Icons.Phosphor.CaretDown,
            style: new Style {
                FontSize = new Rem(0.6875f),
                TextColor = caretColor ?? ThemeSlot.SurfaceAccent,
            });

        Style surf = (open ? (openToggleStyle ?? toggleStyle) : toggleStyle) ?? new Style();

        // The toggle label is the all-caps line-box case OptionRow's labelOffsetY exists for:
        // a leading glyph optically centers on its visible mass while the label centers on its
        // full line box (descender space included), so caps read high. Derive the nudge from the
        // toggle font size so every state gets it without callers passing a magic number; an
        // explicit labelOffsetY still wins.
        Rem? effectiveLabelOffset = labelOffsetY;
        if (effectiveLabelOffset == null && leading != null && surf.FontSize.HasValue) {
            effectiveLabelOffset = new Rem(surf.FontSize.Value.Value * 0.27f);
        }

        LightweaveNode toggle = OptionRow.Create(
            label,
            Toggle,
            leading: leading,
            trailing: caret,
            gap: toggleGap,
            labelOffsetY: effectiveLabelOffset,
            style: surf with { Width = Length.Stretch });

        Rem gap = openGap.ToPixels() > 0f ? openGap : new Rem(0.3125f);

        LightweaveNode inner = Stack.Create(
            gap: open ? gap : default,
            children: b => {
                b.Add(toggle);
                if (open) {
                    b.Add(content);
                }
            },
            style: new Style { Width = Length.Stretch });
        node.Children.Add(inner);

        node.MeasureWidth = () => inner.MeasureWidth?.Invoke() ?? 0f;
        node.Measure = availableWidth => inner.Measure?.Invoke(availableWidth) ?? 0f;

        node.Layout = rect => {
            inner.MeasuredRect = rect;
        };

        return node;
    }

    private static Style ClosedToggleStyle() {
        return new Style {
            Width = Length.Stretch,
            Background = BackgroundSpec.VerticalGradient(new ColorRef.Token(ThemeSlot.Glass1), new ColorRef.Token(ThemeSlot.AccentSoft)),
            Border = BorderSpec.AllDashed(new Rem(1f / 16f), new ColorRef.Token(ThemeSlot.AccentGlow)),
            Padding = new EdgeInsets(new Rem(0.6875f), new Rem(0.875f), new Rem(0.6875f), new Rem(0.875f)),
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.6875f),
            LetterSpacing = Tracking.Of(0.18f),
            TextColor = ThemeSlot.SurfaceAccent,
        };
    }

    private static Style OpenToggleStyle() {
        return ClosedToggleStyle() with {
            Background = BackgroundSpec.Of(ThemeSlot.HoverTint),
            Border = BorderSpec.All(new Rem(1f / 16f), new ColorRef.Token(ThemeSlot.SurfaceAccent)),
        };
    }

    private static LightweaveNode DemoContent() {
        return Box.Create(
            c => c.Add(Stack.Create(
                gap: default,
                children: b => {
                    b.Add(OptionRow.Create("Civil outlander union", () => { }));
                    b.Add(OptionRow.Create("Rough outlander union", () => { }));
                    b.Add(OptionRow.Create("Savage tribe", () => { }, disabled: true));
                })),
            style: new Style {
                Width = Length.Stretch,
                Background = BackgroundSpec.Of(ThemeSlot.Glass2),
                Border = BorderSpec.All(new Rem(1f / 16f), new ColorRef.Token(ThemeSlot.BorderSubtle)),
            });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create(
            "ADD FACTION",
            DemoContent(),
            leading: Glyph.Create(Icons.Phosphor.Plus, style: new Style { FontSize = new Rem(0.75f), TextColor = ThemeSlot.SurfaceAccent }),
            toggleStyle: ClosedToggleStyle(),
            openToggleStyle: OpenToggleStyle()));
    }

    [DocVariant("CL_Playground_Label_Closed")]
    public static DocSample DocsClosed() {
        return new DocSample(() => Create(
            "ADD FACTION",
            DemoContent(),
            leading: Glyph.Create("+", style: new Style { FontFamily = FontRole.Mono, FontSize = new Rem(0.6875f), TextColor = ThemeSlot.SurfaceAccent }),
            defaultOpen: false,
            toggleStyle: ClosedToggleStyle(),
            openToggleStyle: OpenToggleStyle()));
    }

    [DocVariant("CL_Playground_Label_Open")]
    public static DocSample DocsOpen() {
        return new DocSample(() => Create(
            "ADD FACTION",
            DemoContent(),
            leading: Glyph.Create("+", style: new Style { FontFamily = FontRole.Mono, FontSize = new Rem(0.6875f), TextColor = ThemeSlot.SurfaceAccent }),
            defaultOpen: true,
            toggleStyle: ClosedToggleStyle(),
            openToggleStyle: OpenToggleStyle()));
    }
}
