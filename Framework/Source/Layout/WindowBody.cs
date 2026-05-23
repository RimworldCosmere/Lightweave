using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;

namespace Cosmere.Lightweave.Layout;

public static class WindowBody {
    public static LightweaveNode Create(
        Action<List<LightweaveNode>>? children = null,
        bool scrollable = false,
        bool transparent = false,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        List<LightweaveNode> kids = new List<LightweaveNode>();
        children?.Invoke(kids);

        Style baseStyle = new Style {
            Padding = EdgeInsets.All(SpacingScale.Md),
            Background = transparent ? null : BackgroundSpec.Of(ThemeSlot.SurfacePrimary),
        };
        Style merged = style.HasValue ? Style.Merge(baseStyle, style.Value) : baseStyle;

        if (!scrollable) {
            return Box.Create(
                c => c.AddRange(kids),
                style: merged,
                classes: StyleExtensions.PrependClass("window-body", classes),
                id: id,
                line: line,
                file: file
            );
        }

        LightweaveNode inner = kids.Count == 1
            ? kids[0]
            : Stack.Create(
                children: s => {
                    for (int i = 0; i < kids.Count; i++) {
                        s.Add(kids[i]);
                    }
                },
                line: line,
                file: file
            );

        return Box.Create(
            c => c.Add(ScrollArea.Create(inner, line: line, file: file)),
            style: merged,
            classes: StyleExtensions.PrependClass("window-body", classes),
            id: id,
            line: line,
            file: file
        );
    }

    
}
