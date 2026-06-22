using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Hooks;
using static Cosmere.Lightweave.Hooks.Hooks;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class MoreButton {
    private static readonly Rem TileHeight = new Rem(5.6875f);
    private static readonly Rem MenuWidth = new Rem(18f);

    public static LightweaveNode Create(
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        StateHandle<bool> open = UseState(false, line, file);
        StateHandle<Rect> anchor = UseState(Rect.zero, line + 1, file);

        LightweaveNode node = NodeBuilder.New("MoreButton", line, file);
        node.ApplyStyling("more-button", style, classes, id);
        node.PreferredHeight = TileHeight.ToPixels();

        Action toggle = () => open.Set(!open.Value);

        LightweaveNode tile = DockTile.Create(
            "CL_MainMenu_More".Translate(),
            hotkey: string.Empty,
            toggle,
            disabled: false,
            chevron: true,
            expanded: open.Value
        );

        LightweaveNode menu = Menu.Create(
            isOpen: open.Value,
            anchorRect: anchor.Value,
            items: BuildItems(() => open.Set(false)),
            onDismiss: () => open.Set(false),
            anchor: MenuAnchor.Left,
            direction: MenuDirection.Up,
            instanceKey: "more-menu",
            header: (string)"CL_MainMenu_More_Header".Translate(),
            headerMeta: null,
            searchPlaceholder: null,
            size: new Vector2(MenuWidth.ToPixels(), -1f),
            showShadow: false
        );

        node.Children.Add(tile);
        node.Children.Add(menu);

        node.Paint = (rect, _) => {
            if (anchor.Value != rect) {
                anchor.Set(rect);
            }
            tile.MeasuredRect = rect;
            menu.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(tile, rect);
            LightweaveRoot.PaintSubtree(menu, rect);
        };

        return node;
    }

    public static IReadOnlyList<MenuEntry> BuildItems(Action onDismiss) {
        List<MenuEntry> items = new List<MenuEntry> {
            MenuEntry.Of(
                (string)"CL_MainMenu_Playground".Translate(),
                () => Run(OpenPlayground, onDismiss),
                icon: BuildIcon("✦"),
                subtitle: (string)"CL_MainMenu_More_Sub_Playground".Translate()
            ),
            MenuEntry.Divider(),
            MenuEntry.Of(
                (string)"CL_MainMenu_Credits".Translate(),
                () => Run(MainMenuActions.OpenCredits, onDismiss),
                icon: BuildIcon("★"),
                subtitle: (string)"CL_MainMenu_More_Sub_Credits".Translate()
            ),
        };

        IReadOnlyList<HarvestedLink> links = MainMenuLinkHarvester.GetLinks();
        if (links.Count > 0) {
            items.Add(MenuEntry.Divider());
            for (int i = 0; i < links.Count; i++) {
                HarvestedLink link = links[i];
                items.Add(MenuEntry.Of(
                    link.Label,
                    () => Run(link.OnClick, onDismiss),
                    icon: LinkIcon(link)
                ));
            }
        }

        items.Add(MenuEntry.Divider());
        items.Add(MenuEntry.Of(
            (string)"CL_MainMenu_EulaLicenses".Translate(),
            () => Run(OpenEula, onDismiss),
            icon: BuildIcon("§")
        ));
        items.Add(MenuEntry.Of(
            (string)"CL_MainMenu_PrivacyPolicy".Translate(),
            () => Run(OpenPrivacy, onDismiss),
            icon: BuildIcon("§")
        ));

        if (Prefs.DevMode) {
            items.Add(MenuEntry.Divider());
            items.Add(MenuEntry.Of(
                (string)"CL_MainMenu_DevQuickTest".Translate(),
                () => Run(MainMenuActions.DevQuickTest, onDismiss),
                icon: BuildIcon("⚙")
            ));
            if (LanguageDatabase.activeLanguage == LanguageDatabase.defaultLanguage &&
                LanguageDatabase.activeLanguage.anyError) {
                items.Add(MenuEntry.Of(
                    (string)"CL_MainMenu_TranslationReport".Translate(),
                    () => Run(MainMenuActions.SaveTranslationReport, onDismiss),
                    icon: BuildIcon("⚙")
                ));
            }
        }

        items.Add(MenuEntry.Divider());
        items.Add(MenuEntry.Of(
            (string)"CL_MainMenu_QuitToOS".Translate(),
            () => Run(MainMenuActions.QuitToOS, onDismiss),
            icon: BuildIcon("×"),
            danger: true,
            subtitle: (string)"CL_MainMenu_More_Sub_QuitToOS".Translate()
        ));

        return items;
    }

    private static LightweaveNode BuildIcon(string glyph) {
        return GlyphIcon.Create(glyph);
    }

    private static LightweaveNode LinkIcon(HarvestedLink link) {
        if (link.Icon != null) {
            return Image.Create(
                link.Icon,
                ImageFit.Contain,
                style: new Style { Width = Length.Rem(1f), Height = Length.Rem(1f) }
            );
        }

        return GlyphIcon.Create("⧉");
    }

    private static void OpenPlayground() {
        if (Find.WindowStack.IsOpen<Playground.LightweavePlayground>()) {
            return;
        }
        Find.WindowStack.Add(new Playground.LightweavePlayground());
    }

    private static void Run(Action action, Action onDismiss) {
        action?.Invoke();
        onDismiss?.Invoke();
    }

    private static void OpenEula() {
        Application.OpenURL("https://rimworldgame.com/eula/");
    }

    private static void OpenPrivacy() {
        Application.OpenURL("https://rimworldgame.com/privacy/");
    }
}
