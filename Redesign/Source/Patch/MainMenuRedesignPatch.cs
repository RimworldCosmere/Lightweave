using Concord;
using Cosmere.Lightweave.Redesign.Settings;
using System;
using Cosmere.Lightweave.Redesign.MainMenu;
using Cosmere.Lightweave.Runtime;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[Patch(typeof(MainMenuDrawer))]
public abstract class MainMenuRedesignPatch {
    private static readonly Guid RootId = Guid.NewGuid();

    [InjectField("anyMapFiles")]
    private static bool anyMapFiles;

    [Inject(At.Head, nameof(MainMenuDrawer.MainMenuOnGUI))]
    public static Control Prefix() {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return Control.Continue;
        }

        if (Current.ProgramState != ProgramState.Entry) {
            return Control.Continue;
        }

        EventType et = Event.current?.type ?? EventType.Used;
        bool isHotEvent = et == EventType.Layout
            || et == EventType.MouseDrag
            || et == EventType.Used;
        if (Runtime.ActiveDragRegistry.IsActiveFromOther(RootId) && isHotEvent) {
            return Control.Cancel;
        }

        WindowStack? stack = Find.WindowStack;
        if (stack != null && Event.current != null) {
            bool mouseEvent = Event.current.isMouse || Event.current.type == EventType.ScrollWheel;
            if (mouseEvent && (stack.AnyWindowAbsorbingAllInput || stack.GetWindowAt(UI.MousePositionOnUIInverted) != null)) {
                Event.current.Use();
            }
        }

        try {
            Rect screen = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            bool mapFiles = anyMapFiles;
            LightweaveRoot.Render(screen, RootId, () => MainMenuRoot.Build(mapFiles));
        }
        catch (Exception ex) {
            RedesignLog.Error("Main menu redesign failed: " + ex);
            return Control.Continue;
        }

        return Control.Cancel;
    }
}
