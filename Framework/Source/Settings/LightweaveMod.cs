using Concord;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Settings;

public class LightweaveMod : Verse.Mod, ILightweaveSettings {
    private static LightweaveMod? instance;

    public static LightweaveSettings Settings { get; private set; } = null!;

    public LightweaveMod(ModContentPack content) : base(content) {
        instance = this;
        Settings = GetSettings<LightweaveSettings>();
        Patcher.Apply(typeof(LightweaveMod).Assembly);
    }

    public static void Save() {
        instance?.WriteSettings();
    }

    public override string SettingsCategory() {
        return "Lightweave";
    }

    public string LightweaveSettingsTitle => (string)"CL_Settings_Title".Translate();

    public string? LightweaveSettingsSubtitle => (string)"CL_Settings_Subtitle".Translate();

    public Runtime.LightweaveNode BuildLightweaveSettingsBody() {
        return LightweaveSettingsForm.Build();
    }

    public void ResetLightweaveSettings() {
        LightweaveSettingsForm.ResetAll(Settings);
        WriteSettings();
    }

    public override void DoSettingsWindowContents(Rect inRect) {
        LightweaveSettingsForm.Render(inRect);
    }
}
