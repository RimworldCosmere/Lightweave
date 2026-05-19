namespace Cosmere.Lightweave.Tokens;

public static class Spacing {
    public const int BaseUnit = 16;
    public const float FontBaseUnitDefault = 16f;

    public static float FontBaseUnit => FontBaseUnitDefault * (Settings.LightweaveMod.Settings?.FontScale ?? 1f);
}
