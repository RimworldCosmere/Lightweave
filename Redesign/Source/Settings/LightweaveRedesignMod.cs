using Cosmere.Lightweave.Settings;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.Settings;

public class LightweaveRedesignMod : Verse.Mod, ILightweaveSettings {
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

    public string LightweaveSettingsTitle => (string)"CL_RedesignSettings_Title".Translate();

    public string? LightweaveSettingsSubtitle => (string)"CL_RedesignSettings_Subtitle".Translate();

    public Cosmere.Lightweave.Runtime.LightweaveNode BuildLightweaveSettingsBody() {
        return LightweaveRedesignSettingsForm.Build();
    }

    public void ResetLightweaveSettings() {
        LightweaveRedesignSettingsForm.ResetAll(Settings);
        WriteSettings();
    }

    public override void DoSettingsWindowContents(Rect inRect) {
        LightweaveRedesignSettingsForm.Render(inRect);
    }
}
