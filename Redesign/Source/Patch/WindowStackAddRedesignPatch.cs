using System.Reflection;
using Concord;
using Cosmere.Lightweave.Redesign.Settings;
using Cosmere.Lightweave.Redesign.LoadColony;
using Cosmere.Lightweave.Redesign.ModOptions;
using Cosmere.Lightweave.Redesign.ModsConfig;
using Cosmere.Lightweave.Redesign.Options;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[Patch]
public abstract class WindowStackAddRedesignPatch : WindowStack {
    private static readonly FieldInfo? ModField =
        typeof(Dialog_ModSettings).GetField("mod", BindingFlags.NonPublic | BindingFlags.Instance);

    [Inject(At.Head, nameof(Add), parameterTypes: [typeof(Window)])]
    public Control Prefix(Window window) {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return Control.Continue;
        }

        if (window is LightweaveWindow) {
            return Control.Continue;
        }

        WindowStack self = this;
        switch (window) {
            case Dialog_SaveFileList_Load loadDialog:
                self.Add(new LoadColonyWindow(loadDialog));
                return Control.Cancel;

            case Dialog_Options options:
                self.Add(new OptionsWindow(options));
                return Control.Cancel;

            case Page_ModsConfig page when page.next == null && page.prev == null:
                self.Add(new ModsConfigWindow(page));
                return Control.Cancel;

            case Dialog_ModSettings modSettings:
                Mod? mod = ModField?.GetValue(modSettings) as Mod;
                if (mod is ILightweaveSettings) {
                    self.Add(new LightweaveModSettingsWindow(mod));
                    return Control.Cancel;
                }
                return Control.Continue;
        }

        return Control.Continue;
    }
}
