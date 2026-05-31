using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Cosmere.Lightweave.Surfaces;

namespace Cosmere.Lightweave.Layout;

[Doc(
    Id = "imgui-host",
    Summary = "Hosts a raw IMGUI render callback inside a Lightweave layout slot.",
    WhenToUse = "Embed vanilla IMGUI code (e.g. a mod's DoSettingsWindowContents, a Widgets helper, a Listing_Standard block) inline inside a Lightweave layout without forcing it into its own window.",
    SourcePath = "Lightweave/Layout/ImguiHost.cs"
)]
public static class ImguiHost {
    public static LightweaveNode Create(
        [DocParam("Render callback invoked every event with the allocated screen rect.")]
        Action<Rect> render,
        [DocParam("Fixed height for the hosted region. Use Style.Height(Length.Grower) on the returned node to fill instead.", TypeOverride = "Rem?", DefaultOverride = "null")]
        Rem? height = null,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'imgui-host' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        if (render == null) {
            throw new ArgumentNullException(nameof(render));
        }

        LightweaveNode node = NodeBuilder.New("ImguiHost", line, file);
        node.ApplyStyling("imgui-host", style, classes, id);

        if (height.HasValue) {
            node.PreferredHeight = height.Value.ToPixels();
        }

        node.MeasureWidth = () => 0f;

        node.Paint = (rect, _) => {
            try {
                render(rect);
            }
            catch (Exception ex) {
                LightweaveLog.Error($"ImguiHost render threw: {ex}");
            }
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() =>
            Box.Create(
                children: c => c.Add(Create(
                    rect => Verse.Widgets.Label(rect, "raw IMGUI inside Lightweave"),
                    height: new Rem(2f)
                )),
                style: new Style {
                    Padding = EdgeInsets.All(new Rem(0.5f)),
                }
            )
        );
    }
}
