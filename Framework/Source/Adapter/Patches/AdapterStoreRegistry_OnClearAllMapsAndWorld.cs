using Concord;
using Verse.Profile;

namespace Cosmere.Lightweave.Adapter.Patches;

[Patch(typeof(MemoryUtility))]
public static class AdapterStoreRegistry_OnClearAllMapsAndWorld {
    [Inject(At.Head, nameof(MemoryUtility.ClearAllMapsAndWorld))]
    public static void Prefix(ControlHandle ch) {
        AdapterStoreRegistry.ClearAll();
    }
}
