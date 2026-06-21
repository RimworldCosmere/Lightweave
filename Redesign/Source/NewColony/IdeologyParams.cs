namespace Cosmere.Lightweave.Redesign.NewColony;

public enum IdeoMode {
    Inactive,
    Preset,
    CustomFluid,
    CustomFixed,
}

public struct IdeologyParams {
    public IdeoMode Mode;
    public string? PresetDefName;

    public static IdeologyParams Default() {
        return new IdeologyParams {
            Mode = IdeoMode.Inactive,
            PresetDefName = null,
        };
    }
}
