using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Cosmere.Lightweave.Typography;
using Cosmere.Lightweave.Redesign.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using Heading = Cosmere.Lightweave.Typography.Typography.Heading;
using Caption = Cosmere.Lightweave.Typography.Typography.Caption;

namespace Cosmere.Lightweave.Redesign.Options.Tabs;

public static class ModSettingsTab {
    public static LightweaveNode Build() {
        List<Mod> mods = CollectModsWithSettings();

        string initialKey = mods.Count > 0 ? KeyFor(mods[0]) : string.Empty;
        Hooks.Hooks.StateHandle<string> selectedKey = Hooks.Hooks.UseState(initialKey);
        Hooks.Hooks.StateHandle<string> query = Hooks.Hooks.UseState(string.Empty);

        string raw = query.Value ?? string.Empty;
        string q = raw.Trim().ToLowerInvariant();
        List<Mod> filtered = new List<Mod>(mods.Count);
        for (int i = 0; i < mods.Count; i++) {
            Mod m = mods[i];
            if (q.Length == 0 || MatchesQuery(m, q)) {
                filtered.Add(m);
            }
        }

        Mod? selected = FindByKey(mods, selectedKey.Value);
        if (selected == null && filtered.Count > 0) {
            selected = filtered[0];
        }

        return HStack.Create(SpacingScale.None, h => {
            h.Add(BuildMasterPane(filtered, selectedKey, query), new Rem(16f).ToPixels());
            h.AddFlex(BuildDetailPane(selected));
        });
    }

    private static List<Mod> CollectModsWithSettings() {
        List<Mod> result = new List<Mod>();
        foreach (Mod handle in LoadedModManager.ModHandles) {
            if (handle == null) {
                continue;
            }

            string category = handle.SettingsCategory();
            if (!string.IsNullOrEmpty(category)) {
                result.Add(handle);
            }
        }

        result.Sort((a, b) => string.Compare(a.SettingsCategory(), b.SettingsCategory(), StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static string KeyFor(Mod mod) {
        if (mod.Content != null && !string.IsNullOrEmpty(mod.Content.PackageId)) {
            return mod.Content.PackageId;
        }

        return mod.SettingsCategory() ?? mod.GetType().FullName ?? string.Empty;
    }

    private static Mod? FindByKey(List<Mod> mods, string key) {
        if (string.IsNullOrEmpty(key)) {
            return null;
        }

        for (int i = 0; i < mods.Count; i++) {
            if (string.Equals(KeyFor(mods[i]), key, StringComparison.OrdinalIgnoreCase)) {
                return mods[i];
            }
        }

        return null;
    }

    private static bool MatchesQuery(Mod mod, string lowerQuery) {
        string name = mod.SettingsCategory() ?? string.Empty;
        return name.ToLowerInvariant().Contains(lowerQuery);
    }

    private static LightweaveNode BuildMasterPane(
        List<Mod> filtered,
        Hooks.Hooks.StateHandle<string> selectedKey,
        Hooks.Hooks.StateHandle<string> query
    ) {
        LightweaveNode stack = Stack.Create(
            gap: SpacingScale.Sm,
            children: s => {
                s.Add(SearchField.Create(
                    value: query.Value ?? string.Empty,
                    onChange: v => query.Set(v ?? string.Empty),
                    placeholder: "CL_ModSettings_SearchPlaceholder".Translate()
                ));
                s.AddFlex(ScrollArea.Create(
                    content: BuildMasterList(filtered, selectedKey)
                ));
            },
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        );

        return Box.Create(
            children: c => c.Add(stack),
            style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
                Border = new BorderSpec(Right: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static LightweaveNode BuildMasterList(
        List<Mod> filtered,
        Hooks.Hooks.StateHandle<string> selectedKey
    ) {
        if (filtered.Count == 0) {
            return Box.Create(
                children: c => c.Add(Caption.Create("CL_ModSettings_NoMatches".Translate())),
                style: new Style {
                    Padding = EdgeInsets.All(SpacingScale.Md),
                }
            );
        }

        return Stack.Create(SpacingScale.None, s => {
            for (int i = 0; i < filtered.Count; i++) {
                Mod captured = filtered[i];
                string key = KeyFor(captured);
                s.Add(BuildMasterRow(captured, key, selectedKey));
            }
        });
    }

    private static LightweaveNode BuildMasterRow(
        Mod mod,
        string key,
        Hooks.Hooks.StateHandle<string> selectedKey
    ) {
        bool isActive = string.Equals(selectedKey.Value, key, StringComparison.OrdinalIgnoreCase);
        string label = mod.SettingsCategory() ?? string.Empty;

        LightweaveNode node = NodeBuilder.New("ModSettingsRow:" + key);
        node.ApplyStyling("mod-settings-row", null, null, null);
        node.PreferredHeight = new Rem(2.5f).ToPixels();
        node.Paint = (rect, _) => {
            InteractionState state = InteractionState.Resolve(rect, null, false);
            Theme.Theme theme = RenderContext.Current.Theme;

            if (isActive) {
                Color baseSlot = theme.GetColor(ThemeSlot.SurfaceGhostHover);
                Color warm = new Color(baseSlot.r, baseSlot.g, baseSlot.b, 0.4f);
                PaintBox.Draw(rect, BackgroundSpec.Of(warm), null, null);
            }
            else if (state.Hovered) {
                Color baseSlot = theme.GetColor(ThemeSlot.SurfaceGhostHover);
                Color hover = new Color(baseSlot.r, baseSlot.g, baseSlot.b, 0.2f);
                PaintBox.Draw(rect, BackgroundSpec.Of(hover), null, null);
            }

            if (isActive) {
                float stripeW = new Rem(0.1875f).ToPixels();
                Rect stripe = new Rect(rect.x, rect.y, stripeW, rect.height);
                PaintBox.Draw(stripe, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
            }

            float padX = SpacingScale.Md.ToPixels();
            Rect labelRect = new Rect(rect.x + padX, rect.y, rect.width - padX * 2f, rect.height);
            TextDraw.Draw(
                labelRect,
                label,
                isActive ? FontRole.BodyBold : FontRole.Body,
                new Rem(0.9f),
                TextAnchor.MiddleLeft,
                isActive ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary
            );

            InteractionFeedback.Apply(rect, true, true);

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                selectedKey.Set(key);
                e.Use();
            }
        };
        return node;
    }

    private static LightweaveNode BuildDetailPane(Mod? selected) {
        if (selected == null) {
            return Box.Create(
                children: c => c.Add(Caption.Create("CL_ModSettings_NoSelection".Translate())),
                style: new Style {
                    Width = Length.Stretch,
                    Height = Length.Stretch,
                    Padding = EdgeInsets.All(SpacingScale.Xl),
                }
            );
        }

        return Box.Create(
            children: c => c.Add(BuildDetailBody(selected)),
            style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
                Padding = new EdgeInsets(
                    Left: SpacingScale.Lg,
                    Top: SpacingScale.Lg,
                    Right: SpacingScale.Lg,
                    Bottom: SpacingScale.Md
                ),
            }
        );
    }

    private static LightweaveNode BuildDetailBody(Mod mod) {
        if (IsOwnFrameworkMod(mod)) {
            return ScrollArea.Create(
                content: LightweaveSettingsForm.Build(),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }
            );
        }

        if (IsOwnRedesignMod(mod)) {
            return ScrollArea.Create(
                content: LightweaveRedesignSettingsForm.Build(),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }
            );
        }

        return BuildThirdPartyBody(mod);
    }

    private static LightweaveNode BuildThirdPartyBody(Mod mod) {
        string title = mod.SettingsCategory() ?? string.Empty;
        return Stack.Create(
            gap: SpacingScale.Sm,
            children: s => {
                s.Add(HStack.Create(SpacingScale.Sm, h => {
                    h.AddFlex(Heading.Create(3, title));
                    h.AddHug(Button.Create(
                        label: (string)"CL_ModSettings_PopOut".Translate(),
                        onClick: () => OpenVanillaModSettings(mod),
                        variant: Variant.Ghost
                    ));
                }));
                s.AddFlex(ScrollArea.Create(
                    content: ImguiHost.Create(
                        render: rect => {
                            try {
                                mod.DoSettingsWindowContents(rect);
                            }
                            catch (Exception ex) {
                                LightweaveLog.Error($"Mod '{KeyFor(mod)}' DoSettingsWindowContents threw: {ex}");
                            }
                        },
                        height: new Rem(40f)
                    ),
                    style: new Style { Width = Length.Stretch, Height = Length.Stretch }
                ));
            },
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        );
    }

    private static bool IsOwnFrameworkMod(Mod mod) {
        if (mod.Content == null) {
            return false;
        }

        return string.Equals(mod.Content.PackageId, "cosmere.lightweave", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOwnRedesignMod(Mod mod) {
        if (mod.Content == null) {
            return false;
        }

        return string.Equals(mod.Content.PackageId, "cosmere.lightweave.redesign", StringComparison.OrdinalIgnoreCase);
    }

    private static void OpenVanillaModSettings(Mod mod) {
        Dialog_ModSettings dialog = new Dialog_ModSettings(mod) {
            layer = WindowLayer.Super,
        };
        Find.WindowStack.Add(dialog);
    }
}
