using Cosmere.Lightweave.Runtime;
using Xunit;

namespace Cosmere.Lightweave.Tests;

/// <summary>
/// Guards the hook-slot disambiguation that lets sibling LabeledSliders keep independent
/// drag/draft state.
///
/// The IMGUI surface (NewColonyControls.LabeledSlider -> Slider.Create -> Hooks.UseRef ->
/// RenderContext) is verified by in-game smoke test because it needs UnityEngine. What is
/// unit-testable, and what the regression actually lives in, is the HookStore slot identity:
/// hooks are addressed by a HookKey whose CallSiteId is derived from (file, line). Every
/// LabeledSlider routes through ONE Slider.Create call site, so the World tab's Rainfall and
/// Temperature sliders (and the Anomaly tab's three threat sliders) shared a ParentPathHash and
/// the same (line, file) - they all acquired one set of dragging/draftValue slots. Dragging
/// Temperature then drove Rainfall, and Temperature could not be set at all.
///
/// LabeledSlider fixes this by folding the caller's key into the Slider's hook file
/// ("NewColonyControls.LabeledSlider#" + key). These tests reproduce the collision and prove the
/// key suffix isolates the slots. The CallSiteId formula mirrors Hooks.Key (private,
/// UnityEngine-bound), so it is replicated here; if Hooks.Key changes, update this helper.
/// </summary>
public class LabeledSliderHookIsolationTests {
    private static HookKey KeyFor(int parentPathHash, int line, string file) {
        int callSiteId = unchecked(file.GetHashCode() * 31 + line);
        return new HookKey(parentPathHash, callSiteId, null);
    }

    private static string HookFile(string key) {
        return "NewColonyControls.LabeledSlider#" + key;
    }

    [Fact]
    public void SameKey_SharesOneSlot() {
        HookStore store = new HookStore();
        const int parent = 4242;
        const int line = 28;

        HookSlot a = store.Acquire(KeyFor(parent, line, HookFile("rainfall")));
        HookSlot b = store.Acquire(KeyFor(parent, line, HookFile("rainfall")));

        Assert.Same(a, b);
    }

    [Fact]
    public void DistinctKeys_GetIsolatedSlots() {
        HookStore store = new HookStore();
        const int parent = 4242;
        const int line = 28;

        HookSlot rainfall = store.Acquire(KeyFor(parent, line, HookFile("rainfall")));
        HookSlot temperature = store.Acquire(KeyFor(parent, line, HookFile("temperature")));

        Assert.NotSame(rainfall, temperature);
    }

    [Fact]
    public void AnomalyThreatSliders_AllIsolated() {
        HookStore store = new HookStore();
        const int parent = 4242;
        const int line = 28;

        HookSlot inactive = store.Acquire(KeyFor(parent, line, HookFile("anomaly-threats-inactive")));
        HookSlot active = store.Acquire(KeyFor(parent, line, HookFile("anomaly-threats-active")));
        HookSlot study = store.Acquire(KeyFor(parent, line, HookFile("anomaly-study-efficiency")));

        Assert.NotSame(inactive, active);
        Assert.NotSame(active, study);
        Assert.NotSame(inactive, study);
    }

    [Fact]
    public void SameKey_AcrossFrames_ReturnsStableSlot() {
        HookStore store = new HookStore();
        const int parent = 4242;
        const int line = 28;

        HookSlot first = store.Acquire(KeyFor(parent, line, HookFile("temperature")));
        first.Value = "draft-state";
        store.RetireUntouched();

        HookSlot second = store.Acquire(KeyFor(parent, line, HookFile("temperature")));

        Assert.Same(first, second);
        Assert.Equal("draft-state", second.Value);
    }
}
