using System;

namespace Cosmere.Lightweave.Runtime;

/// <summary>
///     Tracks which roots have in-flight animations for the current frame.
///     LightweaveWindow hosts repaint automatically every frame, so no forced
///     repaint is needed there. One-shot Render callers are responsible for
///     calling Render again if HasActiveForFrame returns true.
/// </summary>
public static class AnimationClock {
    private static readonly HashSet<Guid> activeThisFrame = new HashSet<Guid>();
    private static readonly HashSet<Guid> activeLastFrame = new HashSet<Guid>();
    private static int lastSeenUnityFrame = -1;

    public static void RegisterActive(Guid rootId) {
        activeThisFrame.Add(rootId);
    }

    public static bool HasActiveForFrame(Guid rootId) {
        return activeThisFrame.Contains(rootId);
    }

    public static bool WasActiveLastFrame(Guid rootId) {
        return activeLastFrame.Contains(rootId);
    }

    public static void ClearFrame() {
        int frame = UnityEngine.Time.frameCount;
        if (frame != lastSeenUnityFrame) {
            activeLastFrame.Clear();
            foreach (Guid id in activeThisFrame) {
                activeLastFrame.Add(id);
            }
            activeThisFrame.Clear();
            lastSeenUnityFrame = frame;
        }
    }
}