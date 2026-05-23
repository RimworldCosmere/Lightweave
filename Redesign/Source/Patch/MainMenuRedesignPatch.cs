using Cosmere.Lightweave.Redesign.Settings;
using System;
using System.Reflection;
using Cosmere.Lightweave.Redesign.MainMenu;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI))]
public static class MainMenuRedesignPatch {
    private static readonly Guid RootId = Guid.NewGuid();

    private static readonly FieldInfo? AnyMapFilesField = AccessTools.Field(typeof(MainMenuDrawer), "anyMapFiles");

    public static bool Prefix() {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return true;
        }

        if (Current.ProgramState != ProgramState.Entry) {
            return true;
        }

        EventType et = Event.current?.type ?? EventType.Used;
        bool isHotEvent = et == EventType.Layout
            || et == EventType.MouseDrag
            || et == EventType.Used;
        if (Runtime.ActiveDragRegistry.IsActiveFromOther(RootId) && isHotEvent) {
            return false;
        }

        try {
            Rect screen = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            bool anyMapFiles = AnyMapFilesField?.GetValue(null) is bool b && b;
            LightweaveRoot.Render(screen, RootId, () => MainMenuRoot.Build(anyMapFiles));
        }
        catch (Exception ex) {
            RedesignLog.Error("Main menu redesign failed: " + ex);
            return true;
        }

        return false;
    }
}
