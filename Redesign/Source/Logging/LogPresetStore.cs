using System.Collections.Generic;
using Cosmere.Lightweave.Settings;

namespace Cosmere.Lightweave.Redesign.Logging;

internal static class LogPresetStore {
    public static IReadOnlyList<string> Names => LightweaveMod.Settings.LogPresetNames;

    public static bool TryGetExpression(string name, out string expression) {
        List<string> names = LightweaveMod.Settings.LogPresetNames;
        List<string> exprs = LightweaveMod.Settings.LogPresetExpressions;
        int index = names.IndexOf(name);
        if (index < 0 || index >= exprs.Count) {
            expression = "";
            return false;
        }
        expression = exprs[index];
        return true;
    }

    public static void Save(string name, string expression) {
        List<string> names = LightweaveMod.Settings.LogPresetNames;
        List<string> exprs = LightweaveMod.Settings.LogPresetExpressions;
        int index = names.IndexOf(name);
        if (index >= 0 && index < exprs.Count) {
            exprs[index] = expression;
        }
        else {
            names.Add(name);
            exprs.Add(expression);
        }
        LightweaveMod.Save();
    }

    public static bool Remove(string name) {
        List<string> names = LightweaveMod.Settings.LogPresetNames;
        List<string> exprs = LightweaveMod.Settings.LogPresetExpressions;
        int index = names.IndexOf(name);
        if (index < 0) {
            return false;
        }
        names.RemoveAt(index);
        if (index < exprs.Count) {
            exprs.RemoveAt(index);
        }
        LightweaveMod.Save();
        return true;
    }
}
