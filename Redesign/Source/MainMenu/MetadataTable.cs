using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class MetadataTable {
    public static LightweaveNode Create(
        SaveMetadata.LatestSave? latestSave
    ) {
        Style diagText = new Style {
            FontFamily = FontRole.Mono,
            LetterSpacing = Tracking.Of(0.02f),
        };
        return Container.Create(
            KeyValueTable.Create(
                GetList(),
                labelColumnRem: 4f,
                labelStyle: Style.Merge(diagText, new Style { FontSize = new Rem(0.65625f), TextColor = ThemeSlot.TextSecondary }),
                valueStyle: Style.Merge(diagText, new Style { FontSize = new Rem(0.625f), TextColor = ThemeSlot.TextSecondary })
            ),
            align: ContainerAlign.Start,
            style: new Style {
                MaxWidth = new Rem(28f),
                Padding = new EdgeInsets(Left: SpacingScale.Md, Right: SpacingScale.Md, Bottom: SpacingScale.Md),
            }
        );
    }

    public static List<KeyValueRow> GetList() {
        return [
            new KeyValueRow("CL_MainMenu_Meta_Version".Translate(), ResolveVersion()),
            new KeyValueRow("CL_MainMenu_Meta_Build".Translate(), ResolveBuildStamp()),
            new KeyValueRow("CL_MainMenu_Meta_Channel".Translate(), ResolveChannel()),
            new KeyValueRow("CL_MainMenu_Meta_Harmony".Translate(), ResolveHarmonyVersion()),
            new KeyValueRow("CL_MainMenu_Meta_Mods".Translate(), CountModsLine()),
        ];
    }

    private static string ResolveVersion() {
        string version = VersionControl.CurrentVersionString ?? string.Empty;
        string arch = IntPtr.Size == 8 ? "64-bit" : "32-bit";
        return version + " (" + arch + ")";
    }

    private static string ResolveBuildStamp() {
        try {
            string asmPath = typeof(Game).Assembly.Location;
            if (!string.IsNullOrEmpty(asmPath) && File.Exists(asmPath)) {
                DateTime dt = File.GetLastWriteTime(asmPath);
                return "CL_MainMenu_Meta_BuildStamp".Translate(
                    dt.ToString("MMM d yyyy", CultureInfo.InvariantCulture).Named("DATE")
                );
            }
        } catch { }

        return "-";
    }

    private static string ResolveChannel() {
        if (Current.ProgramState != ProgramState.Entry) {
            return "ingame";
        }

        return Prefs.DevMode
            ? "CL_MainMenu_Meta_ChannelDev".Translate()
            : "CL_MainMenu_Meta_ChannelStable".Translate();
    }

    private static string ResolveHarmonyVersion() {
        try {
            Assembly? harmony = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "0Harmony");
            if (harmony != null) {
                Version v = harmony.GetName().Version;
                return "v" + v;
            }
        } catch { }

        return "-";
    }

    private static string CountModsLine() {
        int total = LoadedModManager.RunningModsListForReading.Count;
        int extra = LoadedModManager.RunningModsListForReading.Count(m => !m.IsCoreMod && !m.IsOfficialMod);
        return "CL_MainMenu_Meta_ModsLine".Translate(total.Named("TOTAL"), extra.Named("EXTRA"));
    }
}