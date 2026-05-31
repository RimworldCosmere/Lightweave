using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;

namespace Cosmere.Lightweave.Redesign.NewColony;

internal sealed class NewColonyWindow : LightweaveWindow {
    protected override float WidthFraction => 1f;
    protected override float HeightFraction => 1f;
    protected override float MaxCardWidth => 99999f;
    protected override float MaxCardHeight => 99999f;
    protected override bool EdgeResizable => false;
    protected override RadiusSpec? CardRadius => RadiusSpec.All(RadiusScale.None);
    protected override EdgeInsets? CardPadding => EdgeInsets.All(new Rem(0f));
    protected override bool DrawAccentGradient => false;
    protected override BackgroundSpec? CardBackground => BackgroundSpec.Of(ThemeSlot.WindowGlass);

    protected override LightweaveNode Body() {
        return NewColonyRoot.Build(() => Close());
    }
}
