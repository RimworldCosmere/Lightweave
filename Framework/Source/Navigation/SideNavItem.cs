using Cosmere.Lightweave.Icons;

namespace Cosmere.Lightweave.Navigation;

public readonly struct SideNavItem {
    public SideNavItem(
        string id,
        string label,
        IconRef? icon = null,
        int? count = null,
        bool disabled = false
    ) {
        Id = id;
        Label = label;
        Icon = icon;
        Count = count;
        Disabled = disabled;
    }

    public string Id { get; }
    public string Label { get; }
    public IconRef? Icon { get; }
    public int? Count { get; }
    public bool Disabled { get; }
}
