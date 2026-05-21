using System;

namespace Cosmere.Lightweave.Runtime;

public static class ActiveDragRegistry {
    private static int activeCount;
    private static Guid ownerRoot;

    public static bool IsActive => activeCount > 0;

    public static Guid OwnerRoot => ownerRoot;

    public static bool IsActiveFromOther(Guid rootId) {
        return activeCount > 0 && ownerRoot != Guid.Empty && ownerRoot != rootId;
    }

    public static void Acquire() {
        activeCount++;
    }

    public static void Acquire(Guid owner) {
        if (activeCount == 0) {
            ownerRoot = owner;
        }
        activeCount++;
    }

    public static void Release() {
        if (activeCount > 0) {
            activeCount--;
            if (activeCount == 0) {
                ownerRoot = Guid.Empty;
            }
        }
    }

    public static void Reset() {
        activeCount = 0;
        ownerRoot = Guid.Empty;
    }
}
