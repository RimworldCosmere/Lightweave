using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Playground;

public sealed class LightweavePlayground : LightweaveWindow {
    public static readonly IReadOnlyList<PlaygroundCategory> Categories = new[] {
        new PlaygroundCategory(
            "foundation",
            "CL_Playground_Category_Foundation",
            "CL_Playground_Category_Foundation_Desc",
            new[] { "colors", "shadows", "fonts", "rem-spacing", "responsive" }
        ),
        new PlaygroundCategory(
            "layout",
            "CL_Playground_Category_Layout",
            "CL_Playground_Category_Layout_Desc",
            new[] {
                "stack", "column", "row", "hstack", "grid", "wrap", "scrollarea", "divider", "spacer",
                "carousel", "container", "card", "box", "vignette", "splitpane",
            }
        ),
        new PlaygroundCategory(
            "typography",
            "CL_Playground_Category_Typography",
            "CL_Playground_Category_Typography_Desc",
            new[] { "heading", "text", "label", "caption", "richtext", "code", "icon" }
        ),
        new PlaygroundCategory(
            "buttons",
            "CL_Playground_Category_Buttons",
            "CL_Playground_Category_Buttons_Desc",
            new[] { "button", "iconbutton", "togglebutton", "buttongroup" }
        ),
        new PlaygroundCategory(
            "inputs",
            "CL_Playground_Category_Inputs",
            "CL_Playground_Category_Inputs_Desc",
            new[] {
                "textfield", "checkbox", "switch", "radio", "slider", "textarea", "numberfield", "searchfield",
                "dropdown", "colorpicker", "keybinding", "chip",
            }
        ),
        new PlaygroundCategory(
            "feedback",
            "CL_Playground_Category_Feedback",
            "CL_Playground_Category_Feedback_Desc",
            new[] { "spinner", "progressbar", "ringgauge", "tooltip", "alert" }
        ),
        new PlaygroundCategory(
            "blocks",
            "CL_Playground_Category_Blocks",
            "CL_Playground_Category_Blocks_Desc",
            new[] { "avatar" }
        ),
        new PlaygroundCategory(
            "navigation",
            "CL_Playground_Category_Navigation",
            "CL_Playground_Category_Navigation_Desc",
            new[] { "tabs", "segmented", "breadcrumbs", "menu", "contextmenu", "accordion", "sidenav", "section" }
        ),
        new PlaygroundCategory(
            "overlay",
            "CL_Playground_Category_Overlay",
            "CL_Playground_Category_Overlay_Desc",
            new[] { "window", "dialog", "popover", "drawer", "toast" }
        ),
        new PlaygroundCategory(
            "data",
            "CL_Playground_Category_Data",
            "CL_Playground_Category_Data_Desc",
            new[] { "list", "table", "tree", "keyvalue", "chart" }
        ),
        new PlaygroundCategory(
            "control",
            "CL_Playground_Category_Control",
            "CL_Playground_Category_Control_Desc",
            new[] { "each", "conditional" }
        ),
        new PlaygroundCategory(
            "hooks",
            "CL_Playground_Category_Hooks",
            "CL_Playground_Category_Hooks_Desc",
            new[] { "usestate", "useanim", "usefocus", "usehotkey" }
        ),
    };

    private static readonly Dictionary<string, string> SourcePaths = new Dictionary<string, string> {
        { "colors", "Lightweave/Tokens/ThemeSlot.cs" },
        { "shadows", "Lightweave/Types/ShadowSpec.cs" },
        { "fonts", "Lightweave/Tokens/FontRole.cs" },
        { "rem-spacing", "Lightweave/Types/Rem.cs" },
        { "responsive", "Lightweave/Tokens/Breakpoint.cs" },
        { "stack", "Lightweave/Layout/Stack.cs" },
        { "column", "Lightweave/Layout/Column.cs" },
        { "row", "Lightweave/Layout/Row.cs" },
        { "hstack", "Lightweave/Layout/HStack.cs" },
        { "grid", "Lightweave/Layout/Grid.cs" },
        { "wrap", "Lightweave/Layout/Wrap.cs" },
        { "scrollarea", "Lightweave/Layout/ScrollArea.cs" },
        { "divider", "Lightweave/Layout/Divider.cs" },
        { "spacer", "Lightweave/Layout/Spacer.cs" },
        { "carousel", "Lightweave/Layout/Carousel.cs" },
        { "container", "Lightweave/Layout/Container.cs" },
        { "splitpane", "Lightweave/Layout/SplitPane.cs" },
        { "card", "Lightweave/Layout/Card.cs" },
        { "box", "Lightweave/Layout/Box.cs" },
        { "vignette", "Lightweave/Layout/Vignette.cs" },
        { "each", "Lightweave/Layout/Each.cs" },
        { "conditional", "Lightweave/Layout/Conditional.cs" },
        { "heading", "Lightweave/Typography/Heading.cs" },
        { "text", "Lightweave/Typography/Text.cs" },
        { "label", "Lightweave/Typography/Label.cs" },
        { "caption", "Lightweave/Typography/Caption.cs" },
        { "richtext", "Lightweave/Typography/RichText.cs" },
        { "code", "Lightweave/Typography/Code.cs" },
        { "icon", "Lightweave/Typography/Icon.cs" },
        { "button", "Lightweave/Input/Button.cs" },
        { "iconbutton", "Lightweave/Input/IconButton.cs" },
        { "togglebutton", "Lightweave/Input/ToggleButton.cs" },
        { "buttongroup", "Lightweave/Input/ButtonGroup.cs" },
        { "textfield", "Lightweave/Input/TextField.cs" },
        { "checkbox", "Lightweave/Input/Checkbox.cs" },
        { "switch", "Lightweave/Input/Switch.cs" },
        { "radio", "Lightweave/Input/Radio.cs" },
        { "slider", "Lightweave/Input/Slider.cs" },
        { "textarea", "Lightweave/Input/TextArea.cs" },
        { "numberfield", "Lightweave/Input/NumberField.cs" },
        { "searchfield", "Lightweave/Input/SearchField.cs" },
        { "dropdown", "Lightweave/Input/Dropdown.cs" },
        { "colorpicker", "Lightweave/Input/ColorPicker.cs" },
        { "keybinding", "Lightweave/Input/KeyBindingField.cs" },
        { "chip", "Lightweave/Input/Chip.cs" },
        { "spinner", "Lightweave/Feedback/Spinner.cs" },
        { "progressbar", "Lightweave/Feedback/ProgressBar.cs" },
        { "ringgauge", "Lightweave/Feedback/RingGauge.cs" },
        { "chart", "Lightweave/Feedback/Chart.cs" },
        { "tooltip", "Lightweave/Feedback/Tooltip.cs" },
        { "alert", "Lightweave/Feedback/Alert.cs" },
        { "avatar", "Lightweave/Blocks/Avatar.cs" },
        { "tabs", "Lightweave/Navigation/Tabs.cs" },
        { "segmented", "Lightweave/Navigation/Segmented.cs" },
        { "breadcrumbs", "Lightweave/Navigation/Breadcrumbs.cs" },
        { "menu", "Lightweave/Navigation/Menu.cs" },
        { "contextmenu", "Lightweave/Navigation/ContextMenu.cs" },
        { "accordion", "Lightweave/Navigation/Accordion.cs" },
        { "section", "Lightweave/Navigation/Section.cs" },
        { "sidenav", "Lightweave/Playground/PlaygroundRail.cs" },
        { "window", "Lightweave/Runtime/LightweaveWindow.cs" },
        { "dialog", "Lightweave/Overlay/Dialog.cs" },
        { "popover", "Lightweave/Overlay/Popover.cs" },
        { "drawer", "Lightweave/Overlay/Drawer.cs" },
        { "toast", "Lightweave/Overlay/Toast.cs" },
        { "list", "Lightweave/Data/List.cs" },
        { "table", "Lightweave/Data/Table.cs" },
        { "tree", "Lightweave/Data/Tree.cs" },
        { "keyvalue", "Lightweave/Data/KeyValue.cs" },
        { "usestate", "Lightweave/Hooks/Hooks.cs" },
        { "useanim", "Lightweave/Hooks/Hooks.cs" },
        { "usefocus", "Lightweave/Hooks/Hooks.cs" },
        { "usehotkey", "Lightweave/Hooks/Hooks.cs" },
    };

    private static readonly Dictionary<string, float> DemoRowHeights = new Dictionary<string, float> {
        { "list", 200f },
        { "table", 220f },
        { "tree", 200f },
        { "keyvalue", 180f },
        { "accordion", 260f },
        { "sidenav", 340f },
        { "carousel", 200f },
        { "container", 96f },
        { "splitpane", 360f },
        { "chip", 120f },
        { "card", 280f },
        { "vignette", 160f },
        { "dialog", 280f },
        { "stack", 160f },
        { "column", 160f },
        { "wrap", 120f },
        { "scrollarea", 160f },
    };

    private PlaygroundTheme currentTheme = ResolveDefaultTheme();

    private static PlaygroundTheme ResolveDefaultTheme() {
        string id = LightweaveMod.Settings?.SelectedThemeId ?? ThemeRegistry.DefaultId;
        return id switch {
            "cosmere" => PlaygroundTheme.Cosmere,
            "scadrial" => PlaygroundTheme.Scadrial,
            "roshar" => PlaygroundTheme.Roshar,
            _ => PlaygroundTheme.Default,
        };
    }

    public static string? OverrideSelectedPrimitive;

    public LightweavePlayground() {
        doCloseX = false;
        drawOwnCloseX = false;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        closeOnCancel = true;
        preventCameraMotion = false;
        absorbInputAroundWindow = false;
    }

    public override Vector2 InitialSize => new Vector2(UI.screenWidth * 0.85f, UI.screenHeight * 0.9f);

    protected override Vector2 MinWindowSize => new Vector2(720f, 480f);

    protected internal override Theme.Theme? ThemeOverride => currentTheme switch {
        PlaygroundTheme.Cosmere => ThemeRegistry.Cosmere,
        PlaygroundTheme.Scadrial => ThemeRegistry.Scadrial,
        PlaygroundTheme.Roshar => ThemeRegistry.Roshar,
        _ => ThemeRegistry.Default,
    };

    protected override Rect? DragRegion(Rect inRect) {
        return base.DragRegion(inRect);
    }

    protected override LightweaveNode Body() {
        Hooks.Hooks.StateHandle<PlaygroundTheme> themeHandle = Hooks.Hooks.UseState(currentTheme);
        Hooks.Hooks.StateHandle<string> selectedPrimitiveHandle = Hooks.Hooks.UseState(DefaultPrimitiveId());

        if (OverrideSelectedPrimitive != null && selectedPrimitiveHandle.Value != OverrideSelectedPrimitive) {
            selectedPrimitiveHandle.Set(OverrideSelectedPrimitive);
        }

        if (PlaygroundTour.IsActive) {
            PlaygroundTour.TryAdvance();
            string? tourPrim = PlaygroundTour.CurrentPrimitiveId();
            if (tourPrim != null && selectedPrimitiveHandle.Value != tourPrim) {
                selectedPrimitiveHandle.Set(tourPrim);
            }
        }

        Hooks.Hooks.RefHandle<DocContext> docCtxRef = Hooks.Hooks.UseRef(new DocContext());
        Hooks.Hooks.RefHandle<string?> lastPrimitiveRef = Hooks.Hooks.UseRef<string?>(null);
        if (lastPrimitiveRef.Current != selectedPrimitiveHandle.Value) {
            docCtxRef.Current.Reset();
            lastPrimitiveRef.Current = selectedPrimitiveHandle.Value;
            if (PlaygroundTour.IsActive) {
                PlaygroundTour.NotifyPrimitiveSwitched();
            }
        }

        if (PlaygroundTour.IsActive) {
            LightweaveScrollStatus scroll = docCtxRef.Current.Scroll;
            PlaygroundTour.NotifyMetrics(scroll.LastContentHeight, scroll.LastViewportHeight);
            float t = PlaygroundTour.ScrollProgress();
            float maxScroll = Mathf.Max(0f, scroll.LastContentHeight - scroll.LastViewportHeight);
            scroll.Position = new Vector2(0f, t * maxScroll);
        }

        currentTheme = themeHandle.Value;

        LightweaveNode header = PlaygroundHeader.Create(themeHandle, onClose: () => Close());
        LightweaveNode rail = Layout.ScrollArea.Create(PlaygroundRail.Create(Categories, selectedPrimitiveHandle));
        LightweaveNode body = BuildBody(selectedPrimitiveHandle.Value, docCtxRef.Current);

        return PlaygroundShell.Create(header, rail, body);
    }

    private static string DefaultPrimitiveId() {
        PlaygroundCategory first = Categories[0];
        List<string> sorted = new List<string>(first.PrimitiveIds);
        sorted.Sort(string.CompareOrdinal);
        return sorted.Count > 0 ? sorted[0] : first.PrimitiveIds[0];
    }

    private LightweaveNode BuildBody(string selectedPrimitiveId, DocContext ctx) {
        PlaygroundCategory? owningCategory = null;
        for (int i = 0; i < Categories.Count; i++) {
            IReadOnlyList<string> ids = Categories[i].PrimitiveIds;
            for (int j = 0; j < ids.Count; j++) {
                if (ids[j] == selectedPrimitiveId) {
                    owningCategory = Categories[i];
                    break;
                }
            }

            if (owningCategory != null) {
                break;
            }
        }

        string titleKey = "CL_Playground_" + selectedPrimitiveId + "_Title";
        string whatKey = "CL_Playground_" + selectedPrimitiveId + "_What";
        string whenKey = "CL_Playground_" + selectedPrimitiveId + "_When";

        (IReadOnlyList<PlaygroundVariant> variants, IReadOnlyList<PlaygroundState> states) demo =
            DocReflection.BuildSamplesById(selectedPrimitiveId, false);

        float? rowOverride = DocReflection.GetPreferredVariantHeight(selectedPrimitiveId);
        if (!rowOverride.HasValue && DemoRowHeights.TryGetValue(selectedPrimitiveId, out float rh)) {
            rowOverride = rh;
        }

        string sourcePath = SourcePaths.TryGetValue(selectedPrimitiveId, out string sp)
            ? sp
            : "Lightweave/" + selectedPrimitiveId + ".cs";

        LightweaveNode? breadcrumb = owningCategory != null
            ? CategoryBreadcrumbNode(owningCategory, selectedPrimitiveId)
            : null;

        PlaygroundDocs? docs = DocReflection.BuildDocsById(selectedPrimitiveId);

        PlaygroundPanelResult panel = PlaygroundPanel.Create(
            titleKey,
            whatKey,
            whenKey,
            demo.variants,
            demo.states,
            sourcePath,
            ctx,
            breadcrumb,
            rowOverride,
            docs
        );

        LightweaveNode docStack = Layout.Stack.Create(
            SpacingScale.None,
            s => {
                s.Add(Layout.Spacer.Fixed(SpacingScale.Lg));
                s.Add(panel.Body);
                s.Add(Layout.Spacer.Fixed(SpacingScale.Xxl));
            }
        );

        LightweaveNode contained = Layout.Container.Create(
            docStack,
            style: new Style {
                MaxWidth = new Rem(64f),
                Padding = new EdgeInsets(Top: SpacingScale.None, Right: SpacingScale.Xl, Bottom: SpacingScale.None, Left: SpacingScale.Xl),
            }
        );
        LightweaveNode mainScroll = Layout.ScrollArea.External(contained, ctx.Scroll);

        LightweaveNode tocNode = BuildTocColumn(panel.TocEntries, ctx);

        LightweaveNode bodyHStack = Layout.HStack.Create(
            SpacingScale.None,
            r => {
                r.AddFlex(mainScroll);
                if (RenderContext.Current.Breakpoint >= Breakpoint.Lg) {
                    r.Add(tocNode, new Rem(15f).ToPixels());
                }
            }
        );

        return bodyHStack;
    }

    private static LightweaveNode BuildTocColumn(IReadOnlyList<TocEntry> entries, DocContext ctx) {
        if (entries.Count == 0) {
            return Layout.Spacer.Fixed(new Rem(0f));
        }

        LightweaveNode toc = Doc.Doc.TableOfContents(
            (string)"CL_Playground_Panel_OnThisPage".Translate(),
            entries,
            ctx
        );

        return Layout.Box.Create(
            c => c.Add(toc),
            style: new Style {
                Padding = new EdgeInsets(Top: SpacingScale.Xl, Right: SpacingScale.Lg, Bottom: SpacingScale.Xl, Left: SpacingScale.Lg),
            }
        );
    }

    private static LightweaveNode CategoryBreadcrumbNode(PlaygroundCategory category, string primitiveId) {
        string categoryLabel = (string)category.LabelKey.Translate();
        string primitiveLabel = (string)("CL_Playground_" + primitiveId + "_Title").Translate();
        return Typography.Typography.Caption.Create(categoryLabel + " / " + primitiveLabel);
    }
}