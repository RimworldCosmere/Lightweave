using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Theme;

public sealed record ThemeDescriptor(string Id, string LabelKey, int Order, Type SourceType, string? SubLabelKey = null);

public static class ThemeRegistry {
    public const string DefaultId = "default";

    private static List<ThemeDescriptor>? cachedDescriptors;
    private static readonly Dictionary<string, Theme> cachedThemes = new Dictionary<string, Theme>(StringComparer.Ordinal);
    private static readonly Dictionary<string, MethodInfo> cachedBuilders = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);

    private static int styleEpoch;
    private static string? lastActiveId;

    public static int StyleEpoch => styleEpoch;

    public static void BumpStyleEpoch() {
        unchecked { styleEpoch++; }
    }

    public static IReadOnlyList<ThemeDescriptor> All => GetDescriptors();

    public static Theme Get(string id) {
        if (cachedThemes.TryGetValue(id, out Theme cached)) {
            return cached;
        }
        IReadOnlyList<ThemeDescriptor> all = GetDescriptors();
        ThemeDescriptor? desc = all.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.Ordinal));
        if (desc == null) {
            if (string.Equals(id, DefaultId, StringComparison.Ordinal)) {
                throw new InvalidOperationException(
                    "ThemeRegistry has no theme registered with id 'default'. The lightweave assembly must contribute at least one [LightweaveTheme] attribute."
                );
            }
            return Get(DefaultId);
        }
        if (!cachedBuilders.TryGetValue(desc.Id, out MethodInfo builder)) {
            throw new InvalidOperationException($"ThemeRegistry lost the cached builder for theme id '{desc.Id}'. This is a bug in ThemeRegistry initialization.");
        }
        FontSet fonts = RequireFonts();
        Theme built = (Theme)builder.Invoke(null, new object[] { fonts.Body, fonts.BodyBold, fonts.Heading, fonts.Display, fonts.Mono })!;
        cachedThemes[desc.Id] = built;
        return built;
    }

    public static Theme Active {
        get {
            string id = LightweaveMod.Settings?.SelectedThemeId ?? DefaultId;
            string resolved = string.IsNullOrEmpty(id) ? DefaultId : id;
            if (!string.Equals(resolved, lastActiveId, StringComparison.Ordinal)) {
                lastActiveId = resolved;
                BumpStyleEpoch();
            }
            return Get(resolved);
        }
    }

    public static Theme Default => Get(DefaultId);
    public static Theme Cosmere => Get("cosmere");
    public static Theme Scadrial => Get("scadrial");
    public static Theme Roshar => Get("roshar");

    private static IReadOnlyList<ThemeDescriptor> GetDescriptors() {
        if (cachedDescriptors != null) {
            return cachedDescriptors;
        }

        List<ThemeDescriptor> list = new List<ThemeDescriptor>();
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
            Type[] types;
            try {
                types = asm.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
                types = ex.Types.Where(t => t != null).ToArray()!;
            } catch (Exception ex) {
                LightweaveLog.Warning($"ThemeRegistry skipping assembly '{asm.FullName}': {ex.Message}");
                continue;
            }

            foreach (Type type in types) {
                if (type == null) {
                    continue;
                }
                LightweaveThemeAttribute? attr;
                try {
                    attr = type.GetCustomAttribute<LightweaveThemeAttribute>();
                } catch (Exception ex) {
                    LightweaveLog.Warning($"ThemeRegistry could not read attributes on '{type.FullName}': {ex.Message}");
                    continue;
                }
                if (attr == null) {
                    continue;
                }
                MethodInfo? buildMethod = type.GetMethod(
                    "Build",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(Font), typeof(Font), typeof(Font), typeof(Font), typeof(Font) },
                    modifiers: null
                );
                if (buildMethod == null || !typeof(Theme).IsAssignableFrom(buildMethod.ReturnType)) {
                    LightweaveLog.Warning(
                        $"Theme type '{type.FullName}' is marked [LightweaveTheme] but is missing a 'public static Theme Build(Font, Font, Font, Font, Font)' method. Skipping."
                    );
                    continue;
                }
                if (!seenIds.Add(attr.Id)) {
                    LightweaveLog.Warning(
                        $"Duplicate theme id '{attr.Id}' on type '{type.FullName}'. First registration wins; this one is ignored."
                    );
                    continue;
                }
                list.Add(new ThemeDescriptor(attr.Id, attr.LabelKey, attr.Order, type, attr.SubLabelKey));
                cachedBuilders[attr.Id] = buildMethod;
            }
        }

        list.Sort((a, b) => {
            int cmp = a.Order.CompareTo(b.Order);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.Id, b.Id);
        });

        cachedDescriptors = list;
        return list;
    }

    private static FontSet RequireFonts() {
        Font? body = LightweaveFonts.CarlitoRegular ?? LightweaveFonts.ArimoRegular;
        Font? bodyBold = LightweaveFonts.CarlitoBold ?? LightweaveFonts.ArimoBold;
        Font? heading = LightweaveFonts.CarlitoBold ?? LightweaveFonts.ArimoBold;
        Font? display = LightweaveFonts.IMFellEnglishSC ?? LightweaveFonts.Cinzel ?? LightweaveFonts.CarlitoBold;
        Font? mono = LightweaveFonts.JetBrainsMono;
        if (body == null || bodyBold == null || heading == null || display == null || mono == null) {
            throw new InvalidOperationException(
                "ThemeRegistry accessed before LightweaveFonts loaded. Ensure FontLoader.cctor has run."
            );
        }

        return new FontSet(body, bodyBold, heading, display, mono);
    }

    private readonly record struct FontSet(Font Body, Font BodyBold, Font Heading, Font Display, Font Mono);
}
