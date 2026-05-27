using System;
using System.Collections.Generic;
using System.Reflection;
using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Icons;
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
using Cosmere.Lightweave.Redesign.ModsConfig;
using Heading = Cosmere.Lightweave.Typography.Typography.Heading;
using Caption = Cosmere.Lightweave.Typography.Typography.Caption;
using Text = Cosmere.Lightweave.Typography.Typography.Text;
using Display = Cosmere.Lightweave.Typography.Display;

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
            h.Add(BuildMasterPane(filtered, selectedKey, query), new Rem(22.5f).ToPixels());
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

            if (isActive) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.ActiveTint), null, null);
            }
            else if (state.Hovered) {
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.HoverTint), null, null);
            }

            if (isActive) {
                float stripeW = Spacing.StripeWidth.ToPixels();
                Rect stripe = new Rect(rect.x, rect.y, stripeW, rect.height);
                PaintBox.Draw(stripe, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
            }

            float padX = SpacingScale.Md.ToPixels();
            Rect labelRect = new Rect(rect.x + padX, rect.y, rect.width - padX * 2f, rect.height);
            TextDraw.Draw(
                labelRect,
                label,
                FontRole.Body,
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

        return BuildDetailBody(selected);
    }

    private static LightweaveNode BuildDetailBody(Mod mod) {
        ModMetaData? meta = mod.Content?.ModMetaData;
        bool isFramework = IsOwnFrameworkMod(mod);
        bool isRedesign = IsOwnRedesignMod(mod);

        LightweaveNode body;
        if (isFramework || isRedesign) {
            LightweaveNode form = isFramework
                ? LightweaveSettingsForm.Build()
                : LightweaveRedesignSettingsForm.Build();

            body = ScrollArea.Create(
                content: form,
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }
            );
        }
        else {
            body = Box.Create(
                children: c => c.Add(ScrollArea.Create(
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
                )),
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

        return Stack.Create(
            gap: SpacingScale.None,
            children: s => {
                s.Add(BuildModHeader(mod, meta));
                s.AddFlex(body);
            },
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        );
    }


    private static LightweaveNode BuildModHeader(Mod? mod, ModMetaData? meta) {
        Font? fellRegular = LightweaveFonts.IMFellEnglishRegular;
        FontRef titleFont = fellRegular != null
            ? new FontRef.Literal(fellRegular)
            : new FontRef.Role(FontRole.Display);

        float actionsColPx = new Rem(14f).ToPixels();

        return Box.Create(
            children: c => c.Add(HStack.Create(SpacingScale.Md, h => {
                h.AddFlex(HStack.Create(SpacingScale.Md, left => {
                    if (meta != null) {
                        left.Add(BuildModHeaderThumbnail(meta), new Rem(5f).ToPixels());
                    }
                    left.AddFlex(Stack.Create(SpacingScale.Xs, s => {
                        string title = meta?.Name ?? meta?.PackageId ?? string.Empty;
                        s.Add(Display.Create(
                            title,
                            level: 2,
                            wrap: true,
                            style: new Style {
                                TextAlign = TextAlign.Start,
                                FontFamily = titleFont,
                            }
                        ));
                        if (meta != null) {
                            s.Add(BuildBylineRow(meta));
                            s.Add(BuildHeaderDescription(meta));
                        }
                    }));
                }));
                h.Add(BuildHeaderActionsColumn(mod, meta), actionsColPx);
            })),
            style: new Style {
                Width = Length.Stretch,
                Padding = new EdgeInsets(
                    Top: SpacingScale.Lg,
                    Right: SpacingScale.Lg,
                    Bottom: SpacingScale.Md,
                    Left: SpacingScale.Lg
                ),
                Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );
    }

    private static LightweaveNode BuildModHeaderThumbnail(ModMetaData meta) {
        LightweaveNode node = NodeBuilder.New("ModSettingsHeaderThumb:" + meta.PackageId);
        float size = new Rem(5f).ToPixels();
        node.PreferredHeight = size;
        node.MeasureWidth = () => size;
        node.Paint = (rect, _) => {
            float sq = Mathf.Min(rect.width, rect.height);
            Rect r = new Rect(rect.x, rect.y, sq, sq);
            Texture2D? preview = meta.PreviewImage;
            if (preview != null) {
                PaintBox.DrawTexture(r, preview, Color.white, ScaleMode.ScaleToFit);
            }
            else {
                PaintBox.Draw(
                    r,
                    BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                    BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
                    null
                );
            }
        };
        return node;
    }

    private static LightweaveNode BuildBylineRow(ModMetaData meta) {
        List<string> parts = new List<string>();
        string author = meta.AuthorsString ?? string.Empty;
        if (!string.IsNullOrEmpty(author)) {
            parts.Add((string)"CL_ModSettings_Header_AuthorPrefix".Translate(author.Named("AUTHOR")));
        }
        if (!string.IsNullOrEmpty(meta.ModVersion)) {
            parts.Add("v" + meta.ModVersion);
        }
        string textLine = string.Join("  ·  ", parts);
        bool isWorkshop = meta.Source == ContentSource.SteamWorkshop;

        return HStack.Create(
            gap: SpacingScale.Sm,
            children: h => {
                if (!string.IsNullOrEmpty(textLine)) {
                    h.AddHug(Text.Create(
                        textLine,
                        style: new Style {
                            FontFamily = FontRole.Mono,
                            FontSize = new Rem(0.875f),
                            TextColor = ThemeSlot.TextMuted,
                        }
                    ));
                }
                if (isWorkshop) {
                    h.AddHug(IconButton.Create(
                        icon: Glyph.Create(Phosphor.SteamLogo, style: new Style {
                            FontSize = new Rem(1f),
                            TextColor = ThemeSlot.TextSecondary,
                        }),
                        onClick: () => OpenWorkshop(meta),
                        variant: Variant.Ghost,
                        iconSize: new Rem(1f),
                        tooltipKey: "CL_ModSettings_Header_Workshop_Tip"
                    ));
                }
                h.AddHug(IconButton.Create(
                    icon: Glyph.Create(Phosphor.FolderOpen, style: new Style {
                        FontSize = new Rem(1f),
                        TextColor = ThemeSlot.TextSecondary,
                    }),
                    onClick: () => OpenModFolder(meta),
                    variant: Variant.Ghost,
                    iconSize: new Rem(1f),
                    tooltipKey: "CL_ModSettings_Header_Folder_Tip"
                ));
                h.AddFlex(Spacer.Flex());
            }
        );
    }

    private static LightweaveNode BuildHeaderDescription(ModMetaData meta) {
        string description = meta.Description ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description)) {
            description = (string)"CL_ModSettings_Description_None".Translate();
        }
        return Text.Create(
            description,
            wrap: true,
            style: new Style {
                FontSize = new Rem(0.9375f),
                TextColor = ThemeSlot.TextSecondary,
            }
        );
    }

    private static LightweaveNode BuildHeaderActionsColumn(Mod? mod, ModMetaData? meta) {
        bool active = meta != null && Verse.ModsConfig.IsActive(meta);
        bool toggleDisabled = meta == null || meta.IsCoreMod;
        bool isLightweaveNative = mod != null && (IsOwnFrameworkMod(mod) || IsOwnRedesignMod(mod));
        bool resetDisabled = mod == null;

        Style buttonStyle = new Style {
            Width = Length.Stretch,
            Height = new Rem(2f),
            FontSize = new Rem(0.8125f),
        };

        return Stack.Create(
            gap: SpacingScale.Xs,
            children: s => {
                s.Add(Button.Create(
                    label: (string)"CL_ModSettings_Action_Reset".Translate(),
                    onClick: () => { if (mod != null) ConfirmResetModSettings(mod); },
                    variant: Variant.Secondary,
                    disabled: resetDisabled,
                    style: buttonStyle
                ));
                s.Add(Button.Create(
                    label: (string)(active
                        ? "CL_ModSettings_Action_Disable"
                        : "CL_ModSettings_Action_Enable").Translate(),
                    onClick: () => ToggleModEnabled(meta),
                    variant: active ? Variant.Danger : Variant.Primary,
                    disabled: toggleDisabled,
                    style: buttonStyle
                ));
                if (mod != null && !isLightweaveNative) {
                    s.Add(Button.Create(
                        label: (string)"CL_ModSettings_PopOut".Translate(),
                        onClick: () => OpenVanillaModSettings(mod),
                        variant: Variant.Ghost,
                        style: buttonStyle
                    ));
                }
            },
            style: new Style { Width = Length.Stretch }
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


    

    private static void ConfirmResetModSettings(Mod mod) {
        string displayName = mod.Content?.ModMetaData?.Name ?? mod.SettingsCategory() ?? KeyFor(mod);
        string body = "CL_ModSettings_Confirm_Reset_Body"
            .Translate(displayName.Named("MOD"))
            .Resolve();
        Verse.Dialog_MessageBox dialog = Verse.Dialog_MessageBox.CreateConfirmation(
            body,
            () => ResetModSettings(mod),
            destructive: true,
            title: (string)"CL_ModSettings_Confirm_Reset_Title".Translate()
        );
        Find.WindowStack.Add(dialog);
    }

    private static void ResetModSettings(Mod mod) {
        try {
            FieldInfo? settingsField = typeof(Verse.Mod).GetField(
                "modSettings",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            if (settingsField == null) {
                LightweaveLog.Error("Could not locate Verse.Mod.modSettings field via reflection.");
                return;
            }
            ModSettings? current = settingsField.GetValue(mod) as ModSettings;
            if (current == null) {
                LightweaveLog.Warning($"Mod '{KeyFor(mod)}' has no settings instance to reset.");
                return;
            }
            Type settingsType = current.GetType();
            ModSettings? fresh = Activator.CreateInstance(settingsType) as ModSettings;
            if (fresh == null) {
                LightweaveLog.Error($"Failed to instantiate fresh settings of type {settingsType.FullName}.");
                return;
            }
            settingsField.SetValue(mod, fresh);
            string identifier = mod.Content?.PackageId ?? mod.GetType().FullName ?? string.Empty;
            LoadedModManager.WriteModSettings(identifier, mod.GetType().Name, fresh);
        }
        catch (Exception ex) {
            LightweaveLog.Error($"Failed to reset mod settings for '{KeyFor(mod)}': {ex}");
        }
    }

    private static void ToggleModEnabled(ModMetaData? meta) {
        if (meta == null) {
            return;
        }
        bool active = Verse.ModsConfig.IsActive(meta);
        Verse.ModsConfig.SetActive(meta, !active);
        Verse.ModsConfig.Save();
        Find.WindowStack.Add(new Dialog_ModsConfigRestart());
    }

    private static void OpenWorkshop(ModMetaData meta) {
        try {
            SteamUtility.OpenWorkshopPage(meta.GetPublishedFileId());
        }
        catch (Exception ex) {
            LightweaveLog.Error($"Failed to open workshop page for '{meta.PackageId}': {ex}");
        }
    }

    private static void OpenModFolder(ModMetaData meta) {
        try {
            System.IO.DirectoryInfo? root = meta.RootDir;
            if (root == null || !root.Exists) {
                LightweaveLog.Warning($"Mod '{meta.PackageId}' has no root directory on disk.");
                return;
            }
            Application.OpenURL(root.FullName);
        }
        catch (Exception ex) {
            LightweaveLog.Error($"Failed to open mod folder for '{meta.PackageId}': {ex}");
        }
    }
}
