namespace Cosmere.Lightweave.Runtime;

public static class ActiveDragRegistry {
    private static int activeCount;

    public static bool IsActive => activeCount > 0;

    public static void Acquire() {
        activeCount++;
    }

    public static void Release() {
        if (activeCount > 0) {
            activeCount--;
        }
    }

    public static void Reset() {
        activeCount = 0;
    }
}
