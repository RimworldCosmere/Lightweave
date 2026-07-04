using System.Collections.Generic;

namespace Cosmere.Lightweave.Navigation;

public readonly struct SideNavGroup {
    public SideNavGroup(
        IReadOnlyList<SideNavItem> items,
        string? eyebrow = null
    ) {
        Items = items;
        Eyebrow = eyebrow;
    }

    public IReadOnlyList<SideNavItem> Items { get; }
    public string? Eyebrow { get; }
}
