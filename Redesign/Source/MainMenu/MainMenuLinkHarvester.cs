using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Patch;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class MainMenuLinkHarvester {
    internal static bool Capturing;
    internal static List<ListableOption>? Captured;

    private static List<HarvestedLink>? cache;

    public static IReadOnlyList<HarvestedLink> GetLinks() {
        if (cache != null) {
            return cache;
        }

        List<HarvestedLink> result = new List<HarvestedLink>();

        GameFont savedFont = Text.Font;
        TextAnchor savedAnchor = Text.Anchor;
        Color savedColor = GUI.color;

        Capturing = true;
        Captured = null;
        try {
            Rect offscreen = new Rect(0f, UI.screenHeight + 200f, 400f, 600f);
            MainMenuDrawer.DoMainMenuControls(offscreen, false);
        }
        catch (Exception ex) {
            RedesignLog.Error("Main menu link harvest failed: " + ex);
        }
        finally {
            Capturing = false;
            Text.Font = savedFont;
            Text.Anchor = savedAnchor;
            GUI.color = savedColor;
        }

        if (Captured != null) {
            for (int i = 0; i < Captured.Count; i++) {
                ListableOption opt = Captured[i];
                if (opt == null || opt is PlaygroundMenuOption) {
                    continue;
                }

                HarvestedLink? link = ToLink(opt);
                if (link != null) {
                    result.Add(link);
                }
            }
        }

        Captured = null;
        cache = result;
        RedesignLog.Message($"Harvested {result.Count} main-menu links.");
        return cache;
    }

    private static HarvestedLink? ToLink(ListableOption opt) {
        string label = opt.label;
        if (string.IsNullOrEmpty(label)) {
            return null;
        }

        Texture2D? icon = null;
        Action? onClick = opt.action;

        if (opt is ListableOption_WebLink webLink) {
            if (webLink.image is Texture2D tex) {
                icon = tex;
            }

            if (onClick == null && webLink.url is string url && !string.IsNullOrEmpty(url)) {
                onClick = () => Application.OpenURL(url);
            }
        }

        if (onClick == null) {
            return null;
        }

        return new HarvestedLink(label, icon, onClick);
    }
}
