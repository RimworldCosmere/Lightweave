using Cosmere.Lightweave.Rendering;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.MainMenu;

[StaticConstructorOnStartup]
public static class BlurredBackground {
    private const float TopScrimAlpha = 0.18f;
    private const float BottomScrimAlpha = 0.55f;
    private const float CenterDarkenAlpha = 0.32f;

    private static readonly Texture2D Scrim = SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0f, 0f, 1f));
    private static readonly Texture2D ScrimGradient = SolidColorMaterials.NewSolidColorTexture(new Color(0.04f, 0.05f, 0.07f, 1f));

    public static void Draw(Rect screen) {
        if (Event.current.type != EventType.Repaint) {
            return;
        }

        PaintBox.DrawTexture(screen, Scrim, new Color(1f, 1f, 1f, CenterDarkenAlpha));

        Rect topBand = new Rect(screen.x, screen.y, screen.width, screen.height * 0.18f);
        Rect bottomBand = new Rect(screen.x, screen.yMax - screen.height * 0.32f, screen.width, screen.height * 0.32f);

        PaintBox.DrawTexture(topBand, ScrimGradient, new Color(1f, 1f, 1f, TopScrimAlpha));
        PaintBox.DrawTexture(bottomBand, ScrimGradient, new Color(1f, 1f, 1f, BottomScrimAlpha));
    }
}
