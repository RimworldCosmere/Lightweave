using Cryptiklemur.RimObs.Api;
using Cryptiklemur.RimObs.Metrics;

namespace Cosmere.Lightweave.Runtime;

internal static class LightweaveTelemetry {
    public static readonly GaugeHandle FontsLoaded = Obs.Metrics.RegisterGauge("fonts_loaded");

    public static readonly CounterHandle RenderCalls = Obs.Metrics.RegisterCounter("render_calls");
    public static readonly CounterHandle RenderBuildHits = Obs.Metrics.RegisterCounter("render_build_hits");
    public static readonly CounterHandle RenderBuildMisses = Obs.Metrics.RegisterCounter("render_build_misses");
    public static readonly HistogramHandle RenderTotalTicks = Obs.Metrics.RegisterHistogram("render_total_ticks", unit: "ticks");
    public static readonly HistogramHandle RenderBuildTicks = Obs.Metrics.RegisterHistogram("render_build_ticks", unit: "ticks");
    public static readonly HistogramHandle RenderPaintTicks = Obs.Metrics.RegisterHistogram("render_paint_ticks", unit: "ticks");
}
