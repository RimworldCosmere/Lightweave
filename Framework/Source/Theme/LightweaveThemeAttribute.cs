using System;

namespace Cosmere.Lightweave.Theme;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LightweaveThemeAttribute : Attribute {
    public string Id { get; }
    public string LabelKey { get; }
    public int Order { get; init; }
    public string? SubLabelKey { get; init; }

    public LightweaveThemeAttribute(string id, string labelKey) {
        Id = id;
        LabelKey = labelKey;
    }
}
