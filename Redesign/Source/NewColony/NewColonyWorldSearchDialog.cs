using RimWorld;
using RimWorld.Planet;

namespace Cosmere.Lightweave.Redesign.NewColony;

// Vanilla Dialog_WorldSearch.ShouldClose is `WorldRendererUtility.DrawingMap`, which is
// `CurrentWorldRenderMode != WorldRenderMode.Planet`. The New Colony WorldPreview renders the
// globe without entering Planet render mode (that path double-rendered the screen and stranded
// the player on the world map), so the vanilla dialog would self-close on its first frame here.
// Overriding ShouldClose keeps it open over the preview; the player dismisses it manually.
public class NewColonyWorldSearchDialog : Dialog_WorldSearch {
    protected override bool ShouldClose => false;
}
