using Cosmere.Lightweave.Redesign.Publish;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class PublishEligibilityTests {
    [Fact]
    public void Official_mods_are_never_publishable() {
        Assert.False(PublishEligibility.CanPublish(isOfficial: true, isLocalFolder: true, hasPublishedId: false, isOwned: false));
        Assert.False(PublishEligibility.CanPublish(isOfficial: true, isLocalFolder: true, hasPublishedId: true, isOwned: true));
    }

    [Fact]
    public void Non_local_mods_are_never_publishable() {
        Assert.False(PublishEligibility.CanPublish(isOfficial: false, isLocalFolder: false, hasPublishedId: false, isOwned: false));
        Assert.False(PublishEligibility.CanPublish(isOfficial: false, isLocalFolder: false, hasPublishedId: true, isOwned: true));
    }

    [Fact]
    public void New_local_mod_without_published_id_is_publishable() {
        Assert.True(PublishEligibility.CanPublish(isOfficial: false, isLocalFolder: true, hasPublishedId: false, isOwned: false));
    }

    [Fact]
    public void Existing_item_is_updatable_only_when_owned() {
        Assert.True(PublishEligibility.CanPublish(isOfficial: false, isLocalFolder: true, hasPublishedId: true, isOwned: true));
        Assert.False(PublishEligibility.CanPublish(isOfficial: false, isLocalFolder: true, hasPublishedId: true, isOwned: false));
    }

    [Fact]
    public void Ownership_is_ignored_for_a_brand_new_mod() {
        Assert.True(PublishEligibility.CanPublish(isOfficial: false, isLocalFolder: true, hasPublishedId: false, isOwned: false));
        Assert.True(PublishEligibility.CanPublish(isOfficial: false, isLocalFolder: true, hasPublishedId: false, isOwned: true));
    }
}
