using Concord;
using Cosmere.Lightweave.Redesign.Settings;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[Patch]
public abstract class Window_MarginPatch : Window {
    [Inject(At.Tail, "get_Margin")]
    public void Postfix(ControlHandle<float> ch) {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings is not { RedesignMainMenu: true }) return;
        Window self = this;
        if (self is Dialog_Options
            or Page_ModsConfig
            or Dialog_SaveFileList_Load) {
            ch.ReturnValue = 0f;
        }
    }
}
