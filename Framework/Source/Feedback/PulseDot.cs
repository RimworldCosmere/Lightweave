using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using static Cosmere.Lightweave.Doc.DocChips;

namespace Cosmere.Lightweave.Feedback;

[Doc(
    Id = "pulsedot",
    Summary = "Filled circular dot whose opacity pulses to signal a continuously-running state.",
    WhenToUse = "Indicate a live state such as a tailing log stream, an open connection, or an active recording.",
    SourcePath = "Lightweave/Feedback/PulseDot.cs"
)]
public static class PulseDot {
    public static LightweaveNode Create(
        [DocParam("Fill color slot. Defaults to StatusSuccess.")]
        ThemeSlot? color = null,
        [DocParam("Diameter of the dot. Defaults to 0.375rem.")]
        Rem size = default,
        [DocParam("Lowest opacity reached at the dim end of the pulse.")]
        float minAlpha = 0.3f,
        [DocParam("Highest opacity reached at the bright end of the pulse.")]
        float maxAlpha = 1f,
        [DocParam("Seconds for one full bright -> dim -> bright cycle.")]
        float periodSeconds = 1.4f,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        ThemeSlot resolvedColor = color ?? ThemeSlot.StatusSuccess;
        Rem resolvedSize = size.Equals(default(Rem)) ? new Rem(0.375f) : size;

        LightweaveNode node = NodeBuilder.New("PulseDot", line, file);
        node.ApplyStyling("pulse-dot", style, classes, id);
        node.PreferredHeight = resolvedSize.ToPixels();
        node.MeasureWidth = () => Mathf.Ceil(resolvedSize.ToPixels());

        node.Paint = (rect, paintChildren) => {
            AnimationClock.RegisterActive(RenderContext.Current.RootId);

            Theme.Theme theme = RenderContext.Current.Theme;
            Color baseColor = theme.GetColor(resolvedColor);

            float period = periodSeconds > 0f ? periodSeconds : 1.4f;
            float wave = Mathf.Sin(Time.unscaledTime / period * (Mathf.PI * 2f)) * 0.5f + 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
            Color dotColor = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);

            float d = resolvedSize.ToPixels();
            Rect square = new Rect(
                rect.x + (rect.width - d) * 0.5f,
                rect.y + (rect.height - d) * 0.5f,
                d,
                d
            );
            PaintBox.Draw(square, BackgroundSpec.Of(dotColor), null, RadiusSpec.All(resolvedSize * 0.5f));
            paintChildren();
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => CenterFixed(PulseDot.Create(), 16f, 16f));
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => CenterFixed(PulseDot.Create(), 16f, 16f));
    }

    [DocVariant("CL_Playground_Label_Large")]
    public static DocSample DocsLarge() {
        return new DocSample(() => CenterFixed(PulseDot.Create(size: new Rem(0.75f)), 24f, 24f));
    }

    [DocVariant("CL_Playground_Label_Danger")]
    public static DocSample DocsDanger() {
        return new DocSample(() => CenterFixed(PulseDot.Create(color: ThemeSlot.StatusDanger), 16f, 16f));
    }
}
