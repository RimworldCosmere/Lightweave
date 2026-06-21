using System;
using Cosmere.Lightweave.Runtime;
using Cryptiklemur.RimObs.Api;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Fonts;

[StaticConstructorOnStartup]
public static class FontLoader {
    private const string LightweavePackageId = "cosmere.lightweave";

    static FontLoader() {
        (string assetName, Action<Font?> setter)[] fonts = [
            ("OpenSans-Regular", f => LightweaveFonts.OpenSansRegular = f),
            ("OpenSans-Bold", f => LightweaveFonts.OpenSansBold = f),
            ("Cinzel", f => LightweaveFonts.Cinzel = f),
            ("IMFellEnglish-Regular", f => LightweaveFonts.IMFellEnglishRegular = f),
            ("IMFellEnglishSC", f => LightweaveFonts.IMFellEnglishSC = f),
            ("JetBrainsMono", f => LightweaveFonts.JetBrainsMono = f),
            ("JetBrainsMono-Bold", f => LightweaveFonts.JetBrainsMonoBold = f),
            ("Phosphor-Bold", f => LightweaveFonts.PhosphorBold = f),
            ("RpgAwesome", f => LightweaveFonts.RpgAwesome = f),
        ];

        Dictionary<string, Font> bundleFonts = LoadFromBundles();

        int loaded = 0;
        int fromBundle = 0;
        List<string> osFallbackNames = new List<string>();
        foreach ((string assetName, Action<Font?> setter) entry in fonts) {
            Font? font = null;
            string source = "missing";
            if (bundleFonts.TryGetValue(entry.assetName, out Font bundled)) {
                font = bundled;
                fromBundle++;
                source = "bundle";
            }
            else {
                font = TryLoadFromOs(entry.assetName);
                if (font != null) {
                    osFallbackNames.Add(entry.assetName);
                    source = "os";
                }
            }

            entry.setter(font);
            if (font != null) {
                loaded++;
            }
            else {
                LightweaveLog.Warning($"font {entry.assetName}: not in asset bundle and no dynamic OS match.");
            }

            Obs.Metrics.Set(
                LightweaveTelemetry.FontsLoaded,
                font != null ? 1L : 0L,
                ("name", entry.assetName),
                ("source", source)
            );
        }

        string osDetail = osFallbackNames.Count == 0 ? string.Empty : " [" + string.Join(", ", osFallbackNames) + "]";
        LightweaveLog.Message(
            $"fonts loaded: {loaded}/{fonts.Length} ({fromBundle} from bundle, {osFallbackNames.Count} from OS fallback){osDetail}."
        );

        GameFontOverride.Apply();
    }

    private static Dictionary<string, Font> LoadFromBundles() {
        Dictionary<string, Font> result = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);
        ModContentPack? mod = FindLightweaveMod();
        if (mod == null) {
            LightweaveLog.Warning("Lightweave mod content pack not found; cannot load font asset bundle.");
            return result;
        }

        ModAssetBundlesHandler? handler = mod.assetBundles;
        List<AssetBundle>? bundles = handler?.loadedAssetBundles;
        if (bundles == null || bundles.Count == 0) {
            LightweaveLog.Warning(
                "Lightweave has no loaded asset bundles; run `./make.ps1 generatables` to build them."
            );
            return result;
        }

        foreach (AssetBundle bundle in bundles) {
            if (bundle == null) {
                continue;
            }

            Font[]? fonts;
            try {
                fonts = bundle.LoadAllAssets<Font>();
            }
            catch (Exception ex) {
                LightweaveLog.Warning($"failed to enumerate fonts in bundle '{bundle.name}': {ex.Message}");
                continue;
            }

            if (fonts == null) {
                continue;
            }

            foreach (Font font in fonts) {
                if (font == null || string.IsNullOrEmpty(font.name)) {
                    continue;
                }

                result[font.name] = font;
            }
        }

        return result;
    }

    private static ModContentPack? FindLightweaveMod() {
        List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
        for (int i = 0; i < mods.Count; i++) {
            ModContentPack mod = mods[i];
            if (mod == null) {
                continue;
            }

            if (string.Equals(mod.PackageIdPlayerFacing, LightweavePackageId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(mod.PackageId, LightweavePackageId, StringComparison.OrdinalIgnoreCase)) {
                return mod;
            }
        }

        return null;
    }

    private static Font? TryLoadFromOs(string assetName) {
        string[] osNames = BuildOsNameCandidates(assetName);
        Font? osFont = Font.CreateDynamicFontFromOSFont(osNames, 16);
        if (osFont != null && osFont.dynamic) {
            return osFont;
        }

        return null;
    }

    private static string[] BuildOsNameCandidates(string filename) {
        List<string> names = new List<string>();
        string spacedDash = filename.Replace('-', ' ');
        names.Add(spacedDash);

        int dashIdx = filename.IndexOf('-');
        string family = dashIdx > 0 ? filename.Substring(0, dashIdx) : filename;
        string suffix = dashIdx > 0 ? filename.Substring(dashIdx + 1) : string.Empty;
        if (family != filename) {
            names.Add(family);
        }

        string camelSpaced = SplitCamelCase(family);
        if (camelSpaced != family) {
            if (dashIdx > 0) {
                names.Add(camelSpaced + " " + suffix);
            }

            names.Add(camelSpaced);
        }

        if (family.Equals("JetBrainsMono", StringComparison.OrdinalIgnoreCase)) {
            names.Add("JetBrainsMono Nerd Font");
            names.Add("JetBrains Mono Nerd Font");
            names.Add("JetBrainsMono NF");
        }

        if (family.StartsWith("IMFellEnglish", StringComparison.OrdinalIgnoreCase)) {
            bool smallCaps = family.EndsWith("SC", StringComparison.OrdinalIgnoreCase);
            string display = smallCaps ? "IM Fell English SC" : "IM Fell English";
            string suffixPart = string.IsNullOrEmpty(suffix) ? string.Empty : " " + suffix;
            names.Add(display + suffixPart);
            names.Add(display);
            if (smallCaps) {
                names.Add("IM FELL DW Pica SC");
            }
        }

        return names.ToArray();
    }

    private static string SplitCamelCase(string input) {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(input.Length + 4);
        for (int i = 0; i < input.Length; i++) {
            char c = input[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(input[i - 1])) {
                sb.Append(' ');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
