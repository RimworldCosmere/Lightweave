using System;
using Cosmere.Lightweave.Runtime;

namespace Cosmere.Lightweave.Navigation;

public sealed record MenuEntry(
    string Label,
    Action? OnInvoke = null,
    LightweaveNode? Icon = null,
    bool Disabled = false,
    IReadOnlyList<MenuEntry>? Children = null,
    bool IsDivider = false,
    bool Danger = false,
    string? Subtitle = null,
    bool Active = false,
    string? Hotkey = null
) {
    public static MenuEntry Of(
        string label,
        Action onInvoke,
        LightweaveNode? icon = null,
        bool disabled = false,
        string? subtitle = null,
        bool active = false,
        string? hotkey = null,
        bool danger = false
    ) {
        return new MenuEntry(label, onInvoke, icon, disabled, null, false, danger, subtitle, active, hotkey);
    }

    public static MenuEntry Submenu(
        string label,
        IReadOnlyList<MenuEntry> children,
        LightweaveNode? icon = null,
        bool disabled = false,
        string? subtitle = null
    ) {
        return new MenuEntry(label, null, icon, disabled, children, false, false, subtitle);
    }

    public static MenuEntry Divider() {
        return new MenuEntry(string.Empty, null, null, false, null, true);
    }
}