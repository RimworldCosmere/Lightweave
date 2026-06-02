namespace Cosmere.Lightweave.Types;

// Stroke style for a BorderSpec. Dashed is honored only for square (radius 0)
// borders via PaintBox's rect-stroke path; a dashed border with a corner radius
// falls back to a solid ring.
public enum BorderStyleKind {
    Solid,
    Dashed,
}
