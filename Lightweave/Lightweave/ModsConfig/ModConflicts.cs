using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Cosmere.Lightweave.ModsConfig;

internal static class ModConflicts {
    public static int CountFor(ModMetaData mod) {
        if (mod == null || !mod.Active) {
            return 0;
        }
        return ConflictSnapshot.Current.CountFor(mod);
    }

    public static bool HasConflict(ModMetaData mod) {
        return CountFor(mod) > 0;
    }
}

internal sealed class ConflictSnapshot {
    private static ConflictSnapshot? current;

    private readonly Dictionary<string, int> counts;

    private ConflictSnapshot(Dictionary<string, int> counts) {
        this.counts = counts;
    }

    public static ConflictSnapshot Current => current ??= Build();

    public static void Invalidate() {
        current = null;
    }

    public int CountFor(ModMetaData? mod) {
        string? pid = mod?.PackageId;
        if (pid == null) {
            return 0;
        }
        return counts.TryGetValue(pid, out int c) ? c : 0;
    }

    private static ConflictSnapshot Build() {
        List<ModMetaData> active = Verse.ModsConfig.ActiveModsInLoadOrder.ToList();
        Dictionary<string, int> indexByPid = new Dictionary<string, int>(active.Count, System.StringComparer.CurrentCultureIgnoreCase);
        for (int i = 0; i < active.Count; i++) {
            string? pid = active[i]?.PackageId;
            if (pid != null && !indexByPid.ContainsKey(pid)) {
                indexByPid[pid] = i;
            }
        }

        Dictionary<string, int> counts = new Dictionary<string, int>(active.Count, System.StringComparer.CurrentCultureIgnoreCase);
        for (int i = 0; i < active.Count; i++) {
            ModMetaData? mod = active[i];
            if (mod == null) {
                continue;
            }
            string? myPid = mod.PackageId;
            if (myPid == null) {
                continue;
            }
            int violations = 0;
            if (mod.Dependencies != null) {
                foreach (ModRequirement req in mod.Dependencies) {
                    if (!req.IsSatisfied) {
                        violations++;
                    }
                }
            }
            violations += CountSide(mod.LoadBefore, indexByPid, before: true, myIdx: i);
            violations += CountSide(mod.ForceLoadBefore, indexByPid, before: true, myIdx: i);
            violations += CountSide(mod.LoadAfter, indexByPid, before: false, myIdx: i);
            violations += CountSide(mod.ForceLoadAfter, indexByPid, before: false, myIdx: i);
            counts[myPid] = violations;
        }
        return new ConflictSnapshot(counts);
    }

    private static int CountSide(List<string>? pids, Dictionary<string, int> indexByPid, bool before, int myIdx) {
        if (pids == null) {
            return 0;
        }
        int n = 0;
        foreach (string pid in pids) {
            if (pid == null) {
                continue;
            }
            if (indexByPid.TryGetValue(pid, out int otherIdx)) {
                if (before ? otherIdx < myIdx : otherIdx > myIdx) {
                    n++;
                }
            }
        }
        return n;
    }
}
