using System;
using Concord;
using Cosmere.Lightweave.Redesign.LoadColony;
using Cosmere.Lightweave.Runtime;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[Patch(typeof(GameDataSaveLoader))]
public static class SaveGameMetadataPatch {
    [Inject(At.Tail, nameof(GameDataSaveLoader.SaveGame))]
    public static void Postfix(ControlHandle ch, string fileName) {
        try {
            string saveFilePath = GenFilePaths.FilePathForSavedGame(fileName);
            SaveSidecarData data = SaveSidecar.CaptureFromCurrentGame();
            SaveSidecar.Write(saveFilePath, data);
            ColonyScreenshotCapture.ScheduleForSave(saveFilePath);
        }
        catch (Exception ex) {
            RedesignLog.Warning("SaveGameMetadataPatch failed: " + ex);
        }
    }
}
