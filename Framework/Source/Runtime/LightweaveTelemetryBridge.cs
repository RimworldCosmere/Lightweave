using Cryptiklemur.RimObs.Api;
using Cryptiklemur.RimObs.Metrics;

namespace Cosmere.Lightweave.Runtime;

internal static class LightweaveTelemetryBridge {
    private static readonly object FontsLoadedGauge = Obs.Metrics.RegisterGauge("fonts_loaded");
    private static readonly object RenderCalls = Obs.Metrics.RegisterCounter("render_calls");
    private static readonly object RenderBuildHits = Obs.Metrics.RegisterCounter("render_build_hits");
    private static readonly object RenderBuildMisses = Obs.Metrics.RegisterCounter("render_build_misses");
    private static readonly object RenderTotalTicks = Obs.Metrics.RegisterHistogram("render_total_ticks", unit: "ticks");
    private static readonly object RenderBuildTicks = Obs.Metrics.RegisterHistogram("render_build_ticks", unit: "ticks");
    private static readonly object RenderPaintTicks = Obs.Metrics.RegisterHistogram("render_paint_ticks", unit: "ticks");

    internal static void FontsLoaded(long value, string name, string source) {
        Obs.Metrics.Set((GaugeHandle)FontsLoadedGauge, value, ("name", name), ("source", source));
    }

    internal static void RenderCall(string evt) {
        Obs.Metrics.Add((CounterHandle)RenderCalls, 1L, "event", evt);
    }

    internal static void BuildHit() {
        Obs.Metrics.Add((CounterHandle)RenderBuildHits, 1L);
    }

    internal static void BuildMiss(string reason) {
        Obs.Metrics.Add((CounterHandle)RenderBuildMisses, 1L, "reason", reason);
    }

    internal static void ObserveBuild(long ticks) {
        Obs.Metrics.Observe((HistogramHandle)RenderBuildTicks, ticks);
    }

    internal static void ObservePaint(long ticks) {
        Obs.Metrics.Observe((HistogramHandle)RenderPaintTicks, ticks);
    }

    internal static void ObserveTotal(long ticks) {
        Obs.Metrics.Observe((HistogramHandle)RenderTotalTicks, ticks);
    }
}
