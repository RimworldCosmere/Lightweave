using System;
using System.Reflection;

namespace Cosmere.Lightweave.Runtime;

internal static class LightweaveTelemetry {
    internal static readonly bool Enabled = ProbeRimObs();

    private static bool ProbeRimObs() {
        try {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++) {
                if (assemblies[i].GetName().Name == "RimObs") {
                    return true;
                }
            }
        }
        catch {
            // RimObs is optional; any failure to detect it means telemetry stays off.
        }

        return false;
    }

    internal static void FontsLoaded(long value, string name, string source) {
        if (Enabled) {
            LightweaveTelemetryBridge.FontsLoaded(value, name, source);
        }
    }

    internal static void RenderCall(string evt) {
        if (Enabled) {
            LightweaveTelemetryBridge.RenderCall(evt);
        }
    }

    internal static void BuildHit() {
        if (Enabled) {
            LightweaveTelemetryBridge.BuildHit();
        }
    }

    internal static void BuildMiss(string reason) {
        if (Enabled) {
            LightweaveTelemetryBridge.BuildMiss(reason);
        }
    }

    internal static void ObserveBuild(long ticks) {
        if (Enabled) {
            LightweaveTelemetryBridge.ObserveBuild(ticks);
        }
    }

    internal static void ObservePaint(long ticks) {
        if (Enabled) {
            LightweaveTelemetryBridge.ObservePaint(ticks);
        }
    }

    internal static void ObserveTotal(long ticks) {
        if (Enabled) {
            LightweaveTelemetryBridge.ObserveTotal(ticks);
        }
    }
}
