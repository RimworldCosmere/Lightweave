namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Pure decision for whether the in-game publish/update button is offered, factored out
/// of <see cref="PublishGate"/> so it can be unit-tested without Steam or Verse.
/// A brand-new local mod (no PublishedFileId) is always publishable - the local user
/// becomes the owner on create. An existing Workshop item is only offered when the local
/// user owns it; Steam ownership is resolved asynchronously by <see cref="PublishOwnership"/>.
/// </summary>
public static class PublishEligibility {
    public static bool CanPublish(bool isOfficial, bool isLocalFolder, bool hasPublishedId, bool isOwned) {
        if (isOfficial || !isLocalFolder) {
            return false;
        }

        if (!hasPublishedId) {
            return true;
        }

        return isOwned;
    }
}
