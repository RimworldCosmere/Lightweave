using System.Collections.Generic;

namespace Cosmere.Lightweave.Data;

public sealed record CheckboxTreeNode(
    string Label,
    string Key,
    IReadOnlyList<CheckboxTreeNode>? Children = null,
    object? Payload = null
);
