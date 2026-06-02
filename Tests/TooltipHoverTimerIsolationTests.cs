using Cosmere.Lightweave.Runtime;
using Xunit;

namespace Cosmere.Lightweave.Tests;

/// <summary>
/// Guards the hook-slot disambiguation that lets sibling Tooltips keep independent hover timers.
///
/// The IMGUI surface (Tooltip.CreateInternal -> Hooks.UseRef -> RenderContext) is verified by
/// in-game smoke test because it needs UnityEngine. What is unit-testable, and where the
/// regression actually lives, is the HookStore slot identity: a tooltip's hoverTimer is addressed
/// by a HookKey whose CallSiteId derives from (file, line). Several IconButtons routed through one
/// helper (WorldTab.OverlayToggle -> IconButton.Create at a single source line) all forward the
/// SAME (line, file) into Tooltip, and as siblings built in one builder lambda they also share a
/// ParentPathHash. Keying on (line, file) alone therefore collides: all of them acquire one
/// hoverTimer slot. The non-hovered siblings run Tooltip's `if (!hovered) timer = 0` branch every
/// frame, resetting the shared timer, so it never reaches delayDuration and no tooltip ever shows.
///
/// Tooltip.CreateInternal fixes this by folding the explicit id into the hook key
/// (file + "#id:" + id), and IconButton forwards its unique tooltipKey as that id. These tests
/// reproduce the collision and prove the id suffix isolates the slots. The CallSiteId formula
/// mirrors Hooks.Key (private, UnityEngine-bound), so it is replicated here; if Hooks.Key changes,
/// update this helper to match.
/// </summary>
public class TooltipHoverTimerIsolationTests {
    private static HookKey KeyFor(int parentPathHash, int line, string file) {
        int callSiteId = unchecked(file.GetHashCode() * 31 + line);
        return new HookKey(parentPathHash, callSiteId, null);
    }

    private static string HookFile(string file, string? id) {
        return string.IsNullOrEmpty(id) ? file : file + "#id:" + id;
    }

    [Fact]
    public void SameCallSite_NoId_SharesOneTimerSlot() {
        HookStore store = new HookStore();
        const int parent = 9182;
        const int line = 107;
        const string file = "IconButton.cs";

        HookSlot first = store.Acquire(KeyFor(parent, line, HookFile(file, null)));
        HookSlot second = store.Acquire(KeyFor(parent, line, HookFile(file, null)));

        Assert.Same(first, second);
    }

    [Fact]
    public void SameCallSite_DistinctTooltipKeys_GetIsolatedTimerSlots() {
        HookStore store = new HookStore();
        const int parent = 9182;
        const int line = 107;
        const string file = "IconButton.cs";

        HookSlot icons = store.Acquire(KeyFor(parent, line, HookFile(file, "ShowImportantExpandingIconsToggleButton")));
        HookSlot bases = store.Acquire(KeyFor(parent, line, HookFile(file, "ShowBasesExpandingIconsToggleButton")));

        Assert.NotSame(icons, bases);
    }

    [Fact]
    public void SameTooltipKey_AcrossFrames_ReturnsStableTimerSlot() {
        HookStore store = new HookStore();
        const int parent = 9182;
        const int line = 107;
        const string file = "IconButton.cs";

        HookSlot first = store.Acquire(KeyFor(parent, line, HookFile(file, "UsePlanetDayNightSystemToggleButton")));
        first.Value = 0.31f;
        store.RetireUntouched();

        HookSlot second = store.Acquire(KeyFor(parent, line, HookFile(file, "UsePlanetDayNightSystemToggleButton")));

        Assert.Same(first, second);
        Assert.Equal(0.31f, second.Value);
    }
}
