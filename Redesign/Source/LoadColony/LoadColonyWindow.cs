using System;
using System.Reflection;
using Cosmere.Lightweave.Runtime;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.LoadColony;

internal sealed class LoadColonyWindow : LightweaveWindow {

    private readonly Dialog_SaveFileList_Load inner;
    private static readonly FieldInfo? FilesField =
        typeof(Dialog_SaveFileList_Load).GetField("files", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo? ReloadFilesMethod =
        typeof(Dialog_SaveFileList_Load).GetMethod("ReloadFiles", BindingFlags.NonPublic | BindingFlags.Instance);

    public LoadColonyWindow() : this(new Dialog_SaveFileList_Load()) { }

    public LoadColonyWindow(Dialog_SaveFileList_Load existing) {
        inner = existing;
    }

    protected override LightweaveNode Body() {
        List<SaveFileInfo> files = (FilesField?.GetValue(inner) as List<SaveFileInfo>) ?? new List<SaveFileInfo>();
        return LoadColonyRoot.Build(
            files,
            () => Close(),
            () => ReloadFilesMethod?.Invoke(inner, null)
        );
    }
}
