using Cosmere.Lightweave.Runtime;
using Xunit;

namespace Cosmere.Lightweave.Tests;

/// <summary>
/// Guards the hook-slot disambiguation that lets sibling ScrollAreas keep independent
/// scroll state.
///
/// The IMGUI surface (ScrollArea.Create -> Hooks.UseRef -> RenderContext) is verified by
/// in-game smoke test because it needs UnityEngine. What is unit-testable, and what the
/// regression actually lives in, is the HookStore slot identity: hooks are addressed by a
/// HookKey whose CallSiteId is derived from (file, line). When several ScrollAreas are
/// created at the SAME call site (one per storyteller carousel slide), they share a
/// ParentPathHash at create-scope, so keying on (line, file) alone collides and they all
/// acquire one LightweaveScrollStatus. A slide that fits its viewport then clamps the
/// shared scroll offset back to zero every frame, freezing the overflowing slide.
///
/// ScrollArea.Create fixes this by folding an explicit id into the hook key (file + "#id:"
/// + id). These tests reproduce the collision and prove the id suffix isolates the slots.
/// The CallSiteId formula mirrors Hooks.Key (private, UnityEngine-bound), so it is
/// replicated here; if Hooks.Key changes, update this helper to match.
/// </summary>
public class ScrollAreaHookIsolationTests {
    private static HookKey KeyFor(int parentPathHash, int line, string file) {
        int callSiteId = unchecked(file.GetHashCode() * 31 + line);
        return new HookKey(parentPathHash, callSiteId, null);
    }

    private static string HookFile(string file, string? id) {
        return id != null ? file + "#id:" + id : file;
    }

    [Fact]
    public void SameCallSite_NoId_SharesOneSlot() {
        HookStore store = new HookStore();
        const int parent = 12345;
        const int line = 76;
        const string file = "StorytellerTab.cs";

        HookSlot randy = store.Acquire(KeyFor(parent, line, HookFile(file, null)));
        HookSlot cassandra = store.Acquire(KeyFor(parent, line, HookFile(file, null)));

        Assert.Same(randy, cassandra);
    }

    [Fact]
    public void SameCallSite_DistinctIds_GetIsolatedSlots() {
        HookStore store = new HookStore();
        const int parent = 12345;
        const int line = 76;
        const string file = "StorytellerTab.cs";

        HookSlot randy = store.Acquire(KeyFor(parent, line, HookFile(file, "teller-meta-Randy")));
        HookSlot cassandra = store.Acquire(KeyFor(parent, line, HookFile(file, "teller-meta-Cassandra")));

        Assert.NotSame(randy, cassandra);
    }

    [Fact]
    public void SameId_AcrossFrames_ReturnsStableSlot() {
        HookStore store = new HookStore();
        const int parent = 12345;
        const int line = 76;
        const string file = "StorytellerTab.cs";

        HookSlot first = store.Acquire(KeyFor(parent, line, HookFile(file, "teller-meta-Randy")));
        first.Value = "scroll-state";
        store.RetireUntouched();

        HookSlot second = store.Acquire(KeyFor(parent, line, HookFile(file, "teller-meta-Randy")));

        Assert.Same(first, second);
        Assert.Equal("scroll-state", second.Value);
    }
}
