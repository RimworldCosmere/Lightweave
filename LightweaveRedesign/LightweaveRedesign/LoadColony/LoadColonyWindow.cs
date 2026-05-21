using System;
using System.Reflection;
using Cosmere.Lightweave.Runtime;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.LoadColony;

internal sealed class LoadColonyWindow : LightweaveWindow {
    private static readonly FieldInfo? FilesField = AccessTools.Field(typeof(Dialog_FileList), "files");
    private static readonly MethodInfo? ReloadFilesMethod = AccessTools.Method(typeof(Dialog_FileList), "ReloadFiles");

    private readonly Dialog_SaveFileList_Load inner;

    public LoadColonyWindow() : this(new Dialog_SaveFileList_Load()) { }

    public LoadColonyWindow(Dialog_SaveFileList_Load existing) {
        inner = existing;
    }

    protected override LightweaveNode Body() {
        List<SaveFileInfo> files = FilesField?.GetValue(inner) as List<SaveFileInfo>
                                   ?? new List<SaveFileInfo>();
        return LoadColonyRoot.Build(
            files,
            () => Close(),
            () => ReloadFilesMethod?.Invoke(inner, null)
        );
    }
}
