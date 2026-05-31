using Cosmere.Lightweave.Icons;

namespace Cosmere.Lightweave.Navigation;

public readonly struct TabItem {
    public TabItem(
        string id,
        string label,
        IconRef? icon = null,
        string? badge = null,
        bool disabled = false,
        int? number = null,
        string? subtitle = null
    ) {
        Id = id;
        Label = label;
        Icon = icon;
        Badge = badge;
        Disabled = disabled;
        Number = number;
        Subtitle = subtitle;
    }

    public string Id { get; }
    public string Label { get; }
    public IconRef? Icon { get; }
    public string? Badge { get; }
    public bool Disabled { get; }
    public int? Number { get; }
    public string? Subtitle { get; }
}
