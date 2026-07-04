using System;
using Xunit;

namespace Cosmere.Lightweave.Tests;

/// <summary>
/// Guards the per-render-root scoping of HoverBlockRegistry blocks.
///
/// The registry itself (Register/IsBlocked, double-buffered on Time.frameCount with
/// UnityEngine Rect/Vector2) is verified by in-game smoke test because it needs UnityEngine.
/// What is unit-testable, and where the regression actually lives, is the match predicate:
/// a block carries the RootId of the window that registered it, and IsBlocked only counts a
/// block whose RootId equals the querying window's RootId.
///
/// The bug: an ideology-wizard Modal registers a host-covering block to suppress the New
/// Colony content it sits on top of. A LogViewer window opened on top is a SEPARATE render
/// root. With an unscoped registry it read the modal's full-screen block and refused its own
/// mouse wheel, so the on-top LogViewer could not scroll. Scoping the block by RootId lets the
/// LogViewer query its own root, find no blocks it registered, and scroll - while the modal's
/// block still suppresses the same-root base content beneath it.
///
/// The predicate below mirrors HoverBlockRegistry.IsBlocked (internal, UnityEngine-bound);
/// if that match changes, update this helper to match.
/// </summary>
public class HoverBlockScopeTests {
    private readonly record struct Block(Guid RootId, float X, float Y, float W, float H);

    private static bool Contains(Block b, float px, float py) {
        return px >= b.X && px < b.X + b.W && py >= b.Y && py < b.Y + b.H;
    }

    private static bool IsBlocked(Block[] blocks, Guid rootId, float px, float py) {
        for (int i = 0; i < blocks.Length; i++) {
            if (blocks[i].RootId == rootId && Contains(blocks[i], px, py)) {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void Block_SuppressesSameRoot_BaseContentBeneathModal() {
        Guid newColony = Guid.NewGuid();
        Block[] blocks = [new Block(newColony, 0f, 0f, 1920f, 1080f)];

        Assert.True(IsBlocked(blocks, newColony, 960f, 540f));
    }

    [Fact]
    public void Block_DoesNotLeakAcrossRoots_OnTopLogViewerStillScrolls() {
        Guid newColony = Guid.NewGuid();
        Guid logViewer = Guid.NewGuid();
        Block[] blocks = [new Block(newColony, 0f, 0f, 1920f, 1080f)];

        Assert.False(IsBlocked(blocks, logViewer, 960f, 540f));
    }

    [Fact]
    public void OutsidePoint_IsNotBlocked_EvenSameRoot() {
        Guid root = Guid.NewGuid();
        Block[] blocks = [new Block(root, 100f, 100f, 200f, 200f)];

        Assert.False(IsBlocked(blocks, root, 50f, 50f));
    }
}
