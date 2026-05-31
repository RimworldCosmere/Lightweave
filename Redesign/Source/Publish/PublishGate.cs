using Steamworks;
using Verse;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Single source of truth for whether the in-game publish flow is offered for a mod.
/// A non-official mod from the local Mods folder is eligible; a brand-new mod (no
/// PublishedFileId) shows "Publish", while an existing Workshop item shows "Update" only
/// when the local user owns it (resolved via <see cref="PublishOwnership"/>). The pure
/// decision lives in <see cref="PublishEligibility"/>.
/// </summary>
public static class PublishGate {
    public static bool CanPublish(ModMetaData mod) {
        if (mod == null) {
            return false;
        }

        PublishedFileId_t id = mod.GetPublishedFileId();
        bool hasPublishedId = id != PublishedFileId_t.Invalid;
        bool isOwned = hasPublishedId && PublishOwnership.IsOwned(id);
        return PublishEligibility.CanPublish(
            mod.Official,
            mod.Source == ContentSource.ModsFolder,
            hasPublishedId,
            isOwned
        );
    }
}
