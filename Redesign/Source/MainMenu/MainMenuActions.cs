using System;
using System.Reflection;
using Cosmere.Lightweave.Redesign.NewColony;
using Cosmere.Lightweave.Runtime;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Profile;
using Verse.Sound;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class MainMenuActions {
    private static readonly MethodInfo? InitLearnToPlayMethod =
        typeof(RimWorld.MainMenuDrawer).GetMethod("InitLearnToPlay", BindingFlags.NonPublic | BindingFlags.Static);

    public static void NewColony() {
        Find.WindowStack.Add(new NewColonyWindow());
    }

    public static void OpenLoadDialog() {
        Find.WindowStack.Add(new Dialog_SaveFileList_Load());
    }

    public static void Tutorial() {
        InitLearnToPlayMethod?.Invoke(null, null);
    }

    public static void OpenOptions() {
        Find.WindowStack.Add(new Dialog_Options());
    }

    public static void OpenMods() {
        Find.WindowStack.Add(new Page_ModsConfig());
    }

    public static void OpenCredits() {
        Find.WindowStack.Add(new Screen_Credits());
    }

    public static void QuitToOS() {
        Root.Shutdown();
    }

    public static void DevQuickTest() {
        LongEventHandler.QueueLongEvent(() => {
            Root_Play.SetupForQuickTestPlay();
            PageUtility.InitGameStart();
        }, "GeneratingMap", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
    }

    public static void SaveTranslationReport() {
        LanguageReportGenerator.SaveTranslationReport();
    }

    public static void ContinueLatestSave(string? fileName) {
        if (string.IsNullOrEmpty(fileName)) {
            OpenLoadDialog();
            return;
        }

        SoundDefOf.Click.PlayOneShotOnCamera();
        try {
            GameDataSaveLoader.LoadGame(fileName);
        }
        catch (Exception ex) {
            RedesignLog.Error("continue-load failed for " + fileName + ": " + ex);
            OpenLoadDialog();
        }
    }


}
