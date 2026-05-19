using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Overlay;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.LoadColony;

public static class LoadColonyRoot {
    public static LightweaveNode Build(List<SaveFileInfo> files, Action onClose, Action onReloadFiles) {
        Hooks.Hooks.StateHandle<string> filter = Hooks.Hooks.UseState<string>("all");
        Hooks.Hooks.StateHandle<string?> selected = Hooks.Hooks.UseState<string?>(InitialSelection(files));

        List<SaveFileInfo> filteredFiles = ApplyFilter(files, filter.Value);
        SaveFileInfo? activeFile = ResolveActive(filteredFiles, selected.Value) ?? ResolveActive(files, selected.Value);
        SaveStatusInspector.SaveStatus? activeStatus = activeFile != null
            ? SaveStatusInspector.Inspect(activeFile)
            : null;

        Action onAfterMutate = () => {
            string? activePath = activeFile?.FileInfo.FullName;
            if (activePath is { Length: > 0 }) {
                SaveStatusInspector.Invalidate(activePath);
            }
            onReloadFiles?.Invoke();
        };

        return Stack.Create(SpacingScale.None, root => {
            root.Add(WindowHeader.Create(
                title: "CL_LoadColony_Title".Translate(),
                onClose: onClose,
                drawDivider: true
            ));
            root.AddFlex(HStack.Create(SpacingScale.None, h => {
                h.Add(SaveListPane.Create(
                    filteredFiles,
                    selected.Value,
                    name => selected.Set(name),
                    filter.Value,
                    f => filter.Set(f)
                ), new Rem(30f).ToPixels());
                h.AddFlex(SaveDetailPane.Create(
                    activeFile,
                    activeStatus,
                    onClose,
                    onAfterMutate
                ));
            }));
        });
    }

    private static List<SaveFileInfo> ApplyFilter(List<SaveFileInfo> files, string filter) {
        if (files == null) return new List<SaveFileInfo>();
        if (filter == "all") return files;
        bool wantAuto = filter == "auto";
        List<SaveFileInfo> result = new List<SaveFileInfo>();
        for (int i = 0; i < files.Count; i++) {
            string name = Path.GetFileNameWithoutExtension(files[i].FileName);
            bool isAuto = name.StartsWith("Autosave", StringComparison.OrdinalIgnoreCase)
                          || name.StartsWith("autosave", StringComparison.OrdinalIgnoreCase);
            if (isAuto == wantAuto) result.Add(files[i]);
        }
        return result;
    }

    private static string? InitialSelection(List<SaveFileInfo> files) {
        if (files == null || files.Count == 0) {
            return null;
        }
        SaveFileInfo? newest = null;
        foreach (SaveFileInfo file in files) {
            if (newest == null || file.LastWriteTime > newest.LastWriteTime) {
                newest = file;
            }
        }
        return newest != null ? Path.GetFileNameWithoutExtension(newest.FileName) : null;
    }

    private static SaveFileInfo? ResolveActive(List<SaveFileInfo> files, string? selected) {
        if (files == null || string.IsNullOrEmpty(selected)) {
            return null;
        }
        foreach (SaveFileInfo file in files) {
            if (string.Equals(Path.GetFileNameWithoutExtension(file.FileName), selected, StringComparison.OrdinalIgnoreCase)) {
                return file;
            }
        }
        return null;
    }
}
