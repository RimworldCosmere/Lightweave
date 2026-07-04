using Concord;
using Verse;

namespace Cosmere.Lightweave.Adapter.Patches;

[Patch]
public abstract class AdapterStoreRegistry_OnThingDestroyed : Thing {
    [Inject(At.Tail, nameof(Destroy))]
    public void Postfix() {
        AdapterStoreRegistry.ReleaseAllFor(thingIDNumber);
    }
}
