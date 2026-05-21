using System;

namespace Cosmere.Lightweave.Layout;

public readonly struct WindowHeaderTab {
    public string Label { get; }
    public bool IsActive { get; }
    public Action OnClick { get; }

    public WindowHeaderTab(string label, bool isActive, Action onClick) {
        Label = label;
        IsActive = isActive;
        OnClick = onClick;
    }
}
