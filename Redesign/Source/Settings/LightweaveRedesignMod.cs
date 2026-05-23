using HarmonyLib;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.Settings;

public class LightweaveRedesignMod : Verse.Mod {
    private static LightweaveRedesignMod? instance;

    public static LightweaveRedesignSettings Settings { get; private set; } = null!;

    public static bool BootRedesignMainMenu { get; private set; }

    public LightweaveRedesignMod(ModContentPack content) : base(content) {
        instance = this;
        Settings = GetSettings<LightweaveRedesignSettings>();
        BootRedesignMainMenu = Settings.RedesignMainMenu;
        Harmony harmony = new Harmony("cosmere.lightweave.redesign");
        harmony.PatchAll(typeof(LightweaveRedesignMod).Assembly);
    }

    public static void Save() {
        instance?.WriteSettings();
    }

    public override string SettingsCategory() {
        return "Lightweave Redesign";
    }

    public override void DoSettingsWindowContents(Rect inRect) {
        LightweaveRedesignSettingsForm.Render(inRect);
    }
}
