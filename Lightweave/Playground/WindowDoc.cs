using System.Collections.Generic;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Playground;

[Doc(
    Id = "window",
    Summary = "Top-level resizable Lightweave window assembled from Header / Body / Footer slots.",
    WhenToUse = "Host a tool, dialog, or panel as its own movable window in the WindowStack.",
    SourcePath = "Lightweave/Runtime/LightweaveWindow.cs",
    Target = typeof(LightweaveWindow),
    Label = "Window"
)]
public static class WindowDoc {
    [DocVariant("CL_Playground_Window_Bordered", Order = 1)]
    public static DocSample DocsBordered() {
        return new DocSample(() =>
            Button.Create(
                (string)"CL_Playground_Window_Open".Translate(),
                () => Find.WindowStack.Add(new BorderedSampleWindow()),
                Variant.Secondary
            ),
            companion: typeof(BorderedSampleWindow)
        );
    }

    [DocVariant("CL_Playground_Window_Borderless", Order = 2)]
    public static DocSample DocsBorderless() {
        return new DocSample(() =>
            Button.Create(
                (string)"CL_Playground_Window_Open".Translate(),
                () => Find.WindowStack.Add(new BorderlessSampleWindow()),
                Variant.Secondary
            ),
            companion: typeof(BorderlessSampleWindow)
        );
    }

    [DocVariant("CL_Playground_Window_FixedSize", Order = 3)]
    public static DocSample DocsFixedSize() {
        return new DocSample(() =>
            Button.Create(
                (string)"CL_Playground_Window_Open".Translate(),
                () => Find.WindowStack.Add(new FixedSizeSampleWindow()),
                Variant.Secondary
            ),
            companion: typeof(FixedSizeSampleWindow)
        );
    }

    [DocVariant("CL_Playground_Window_Large", Order = 4)]
    public static DocSample DocsLarge() {
        return new DocSample(() =>
            Button.Create(
                (string)"CL_Playground_Window_Open".Translate(),
                () => Find.WindowStack.Add(new LargeSampleWindow()),
                Variant.Secondary
            ),
            companion: typeof(LargeSampleWindow)
        );
    }

    [DocVariant("CL_Playground_Window_WithFooter", Order = 5)]
    public static DocSample DocsWithFooter() {
        return new DocSample(() =>
            Button.Create(
                (string)"CL_Playground_Window_Open".Translate(),
                () => Find.WindowStack.Add(new DialogSampleWindow()),
                Variant.Secondary
            ),
            companion: typeof(DialogSampleWindow)
        );
    }

    [DocVariant("CL_Playground_Window_StatusBar", Order = 6)]
    public static DocSample DocsStatusBar() {
        return new DocSample(() =>
            Button.Create(
                (string)"CL_Playground_Window_Open".Translate(),
                () => Find.WindowStack.Add(new StatusBarSampleWindow()),
                Variant.Secondary
            ),
            companion: typeof(StatusBarSampleWindow)
        );
    }

    [DocVariant("CL_Playground_Window_Header_TitleOnly", Order = 10)]
    public static DocSample DocsHeaderTitleOnly() {
        return new DocSample(() => WindowHeader.Create(
            title: "Options",
            draggable: false
        ));
    }

    [DocVariant("CL_Playground_Window_Header_Subtitle", Order = 11)]
    public static DocSample DocsHeaderSubtitle() {
        return new DocSample(() => WindowHeader.Create(
            title: "New Colony",
            subtitle: "A few quiet questions before the dropship opens.",
            draggable: false
        ));
    }

    [DocVariant("CL_Playground_Window_Header_Crumb", Order = 12)]
    public static DocSample DocsHeaderCrumb() {
        return new DocSample(() => WindowHeader.Create(
            crumb: "MAIN / LOAD COLONY",
            title: "Load Colony",
            onClose: () => { },
            actions: HStack.Create(SpacingScale.Xxs, h => {
                h.AddHug(Button.Create("Import…", null, Variant.Ghost));
                h.AddHug(Button.Create("Resume", null, Variant.Primary));
            }),
            draggable: false
        ));
    }

    [DocVariant("CL_Playground_Window_Header_Full", Order = 13)]
    public static DocSample DocsHeaderFull() {
        return new DocSample(() => WindowHeader.Create(
            crumb: "MAIN / MODS",
            title: "Mods",
            subtitle: "Order matters. Drag to reorder; conflicts highlight in red.",
            onClose: () => { },
            actions: HStack.Create(SpacingScale.Xxs, h => {
                h.AddHug(Button.Create("Reset", null, Variant.Ghost));
                h.AddHug(Button.Create("Save", null, Variant.Primary));
            }),
            headerContent: HStack.Create(SpacingScale.Sm, h => {
                h.AddHug(Text.Create("14 ACTIVE · 1 CONFLICT", style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.7f),
                    LetterSpacing = Tracking.Of(0.08f),
                    TextColor = ThemeSlot.TextMuted,
                }));
                h.AddFlex(Spacer.Flex());
                h.AddHug(Button.Create("Auto-resolve", null, Variant.Ghost));
            }),
            secondaryActions: new List<WindowHeaderTab> {
                new WindowHeaderTab("Load Order", true, () => { }),
                new WindowHeaderTab("Installed", false, () => { }),
                new WindowHeaderTab("Browse Workshop", false, () => { }),
            },
            draggable: false
        ));
    }

    [DocVariant("CL_Playground_Window_Header_Tabs", Order = 14)]
    public static DocSample DocsHeaderTabs() {
        return new DocSample(() => WindowHeader.Create(
            title: "Mods",
            onClose: () => { },
            secondaryActions: new List<WindowHeaderTab> {
                new WindowHeaderTab("Installed", true, () => { }),
                new WindowHeaderTab("Load Order", false, () => { }),
            },
            draggable: false
        ));
    }

    [DocVariant("CL_Playground_Window_Header_Status", Order = 15)]
    public static DocSample DocsHeaderStatus() {
        return new DocSample(() => WindowHeader.Create(
            title: "Mods",
            onClose: () => { },
            headerContent: HStack.Create(SpacingScale.Xs, h => {
                h.AddHug(Text.Create("12 ACTIVE · 14 INSTALLED", style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.7f),
                    LetterSpacing = Tracking.Of(0.08f),
                    TextColor = ThemeSlot.TextMuted,
                }));
                h.AddHug(Text.Create("· 1 CONFLICT", style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.7f),
                    LetterSpacing = Tracking.Of(0.08f),
                    TextColor = ThemeSlot.StatusWarning,
                }));
            }),
            draggable: false
        ));
    }

    [Doc(Slot = true, Label = "WindowHeader.Create()")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Reflected for docs only.")]
    public static void HeaderApi(
        [DocParam("Eyebrow breadcrumb rendered above the title row in monospaced caps. Null hides the crumb row unless a close button is present.", TypeOverride = "string?", DefaultOverride = "null")]
        string? crumb = null,
        [DocParam("Display title rendered in uppercase. Pair with subtitle for a richer header block.", TypeOverride = "string?", DefaultOverride = "null")]
        string? title = null,
        [DocParam("Italic supporting copy rendered below the title. Wraps automatically.", TypeOverride = "string?", DefaultOverride = "null")]
        string? subtitle = null,
        [DocParam("Trailing slot in the title row. Typically a HStack of Buttons for primary actions.", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
        LightweaveNode? actions = null,
        [DocParam("Extra row rendered below the title block. Use for status banners, conflict notices, etc.", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
        LightweaveNode? headerContent = null,
        [DocParam("Optional secondary navigation tabs rendered in a row beneath the header content.", TypeOverride = "IReadOnlyList<WindowHeaderTab>?", DefaultOverride = "null")]
        IReadOnlyList<WindowHeaderTab>? secondaryActions = null,
        [DocParam("Callback invoked when the close icon is clicked. Null hides the close icon and may hide the crumb row entirely.", TypeOverride = "Action?", DefaultOverride = "null")]
        System.Action? onClose = null,
        [DocParam("Whether the header rect publishes itself as the window drag hotspot.")]
        bool draggable = true,
        [DocParam("Whether a hairline divider is painted at the bottom of the header.")]
        bool drawDivider = true,
        Style? style = null,
        string[]? classes = null,
        string? id = null
    ) { }

    [Doc(Slot = true, Label = "WindowBody.Create()")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Reflected for docs only.")]
    public static void BodyApi(
        [DocParam("Builder for the body's stacked children. Compose primitives directly (Stack, Card, Text, etc.).", TypeOverride = "Action<List<LightweaveNode>>?", DefaultOverride = "null")]
        System.Action<System.Collections.Generic.List<LightweaveNode>>? children = null,
        [DocParam("If true, the body wraps its children in a ScrollArea.")]
        bool scrollable = false,
        Style? style = null,
        string[]? classes = null,
        string? id = null
    ) { }

    [Doc(Slot = true, Label = "WindowFooter.Create()")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Reflected for docs only.")]
    public static void FooterApi(
        [DocParam("Leading slot stretched to fill the remaining width. Use for status copy or a meter.", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
        LightweaveNode? content = null,
        [DocParam("Trailing slot hugged to the end of the row. Typically a HStack of Buttons (Cancel / Confirm).", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
        LightweaveNode? actions = null,
        [DocParam("Whether a hairline divider is painted at the top of the footer.")]
        bool drawDivider = true,
        [DocParam("Whether a triangular resize grip is painted in the trailing-bottom corner.")]
        bool showResizeGrip = false,
        Style? style = null,
        string[]? classes = null,
        string? id = null
    ) { }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() =>
            Button.Create(
                (string)"CL_Playground_Window_Open".Translate(),
                () => Find.WindowStack.Add(new BorderedSampleWindow()),
                Variant.Secondary
            ),
            companion: typeof(BorderedSampleWindow)
        );
    }

    private sealed class BorderedSampleWindow : LightweaveWindow {
        public override Vector2 InitialSize => new Vector2(480f, 320f);
        protected override Vector2 MinWindowSize => new Vector2(320f, 200f);

        protected override LightweaveNode Header() {
            return WindowHeader.Create(
                title: (string)"CL_Playground_Window_Sample_Title".Translate(),
                onClose: () => Close()
            );
        }

        protected override LightweaveNode Body() {
            return WindowBody.Create(
                transparent: true,
                style: new Style { Padding = EdgeInsets.All(SpacingScale.Md) },
                children: c => c.Add(Text.Create(
                    (string)"CL_Playground_Window_Sample_Bordered_Body".Translate(),
                    wrap: true,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextSecondary }
                ))
            );
        }
    }

    private sealed class BorderlessSampleWindow : LightweaveWindow {
        public override Vector2 InitialSize => new Vector2(480f, 320f);
        protected override Vector2 MinWindowSize => new Vector2(320f, 200f);
        protected override float WidthFraction => 1f;
        protected override float HeightFraction => 1f;
        protected internal override bool DrawScrim => false;
        protected internal override bool DrawVignette => false;
        protected override bool DrawAccentGradient => false;
        protected override BorderSpec? CardBorder => BorderSpec.All(new Rem(0f), ThemeSlot.BorderDefault);

        protected override LightweaveNode Header() {
            return WindowHeader.Create(
                title: (string)"CL_Playground_Window_Sample_Title".Translate(),
                onClose: () => Close()
            );
        }

        protected override LightweaveNode Body() {
            return WindowBody.Create(
                transparent: true,
                style: new Style { Padding = EdgeInsets.All(SpacingScale.Md), Background = BackgroundSpec.Of(ThemeSlot.SurfacePrimary) },
                children: c => c.Add(Text.Create(
                    (string)"CL_Playground_Window_Sample_Borderless_Body".Translate(),
                    wrap: true,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextSecondary }
                ))
            );
        }
    }

    private sealed class FixedSizeSampleWindow : LightweaveWindow {
        public override Vector2 InitialSize => new Vector2(420f, 280f);
        protected override bool EdgeResizable => false;
        protected override Vector2 MinWindowSize => new Vector2(420f, 280f);

        protected override LightweaveNode Header() {
            return WindowHeader.Create(
                title: (string)"CL_Playground_Window_Sample_Title".Translate(),
                onClose: () => Close()
            );
        }

        protected override LightweaveNode Body() {
            return WindowBody.Create(
                transparent: true,
                style: new Style { Padding = EdgeInsets.All(SpacingScale.Md) },
                children: c => c.Add(Text.Create(
                    (string)"CL_Playground_Window_Sample_Fixed_Body".Translate(),
                    wrap: true,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextSecondary }
                ))
            );
        }
    }

    private sealed class LargeSampleWindow : LightweaveWindow {
        public override Vector2 InitialSize => new Vector2(720f, 520f);
        protected override Vector2 MinWindowSize => new Vector2(520f, 360f);

        protected override LightweaveNode Header() {
            return WindowHeader.Create(
                title: (string)"CL_Playground_Window_Sample_Title".Translate(),
                onClose: () => Close()
            );
        }

        protected override LightweaveNode Body() {
            return WindowBody.Create(
                transparent: true,
                style: new Style { Padding = EdgeInsets.All(SpacingScale.Md) },
                children: c => c.Add(Text.Create(
                    (string)"CL_Playground_Window_Sample_Large_Body".Translate(),
                    wrap: true,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextSecondary }
                ))
            );
        }
    }

    private sealed class DialogSampleWindow : LightweaveWindow {
        public override Vector2 InitialSize => new Vector2(540f, 320f);
        protected override Vector2 MinWindowSize => new Vector2(420f, 240f);

        protected override LightweaveNode Header() {
            return WindowHeader.Create(
                title: (string)"CL_Playground_Window_Sample_Dialog_Title".Translate(),
                subtitle: (string)"CL_Playground_Window_Sample_Dialog_Subtitle".Translate(),
                onClose: () => Close()
            );
        }

        protected override LightweaveNode Body() {
            return WindowBody.Create(
                transparent: true,
                style: new Style { Padding = EdgeInsets.All(SpacingScale.Md) },
                children: c => c.Add(Text.Create(
                    (string)"CL_Playground_Window_Sample_Dialog_Body".Translate(),
                    wrap: true,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextSecondary }
                ))
            );
        }

        protected override LightweaveNode Footer() {
            return WindowFooter.Create(
                actions: HStack.Create(SpacingScale.Xxs, r => {
                    r.AddHug(Button.Create(
                        (string)"CL_Playground_Window_Cancel".Translate(),
                        () => Close(),
                        Variant.Ghost
                    ));
                    r.AddHug(Button.Create(
                        (string)"CL_Playground_Window_Confirm".Translate(),
                        () => Close(),
                        Variant.Primary
                    ));
                })
            );
        }
    }

    private sealed class StatusBarSampleWindow : LightweaveWindow {
        public override Vector2 InitialSize => new Vector2(560f, 360f);
        protected override Vector2 MinWindowSize => new Vector2(360f, 220f);

        protected override LightweaveNode Header() {
            return WindowHeader.Create(
                title: (string)"CL_Playground_Window_Sample_Status_Title".Translate(),
                onClose: () => Close()
            );
        }

        protected override LightweaveNode Body() {
            return WindowBody.Create(
                transparent: true,
                style: new Style { Padding = EdgeInsets.All(SpacingScale.Md) },
                children: c => c.Add(Text.Create(
                    (string)"CL_Playground_Window_Sample_Status_Body".Translate(),
                    wrap: true,
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextSecondary }
                ))
            );
        }

        protected override LightweaveNode Footer() {
            return WindowFooter.Create(
                showResizeGrip: true,
                content: Text.Create(
                    (string)"CL_Playground_Window_Sample_Status_Indicator".Translate(),
                    style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.7f),
                        LetterSpacing = Tracking.Of(0.08f),
                        TextColor = ThemeSlot.TextMuted,
                    }
                )
            );
        }
    }
}
