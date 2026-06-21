using Cosmere.Lightweave.Runtime;

namespace Cosmere.Lightweave.Settings;

public interface ILightweaveSettings {
    string LightweaveSettingsTitle { get; }

    string? LightweaveSettingsSubtitle { get; }

    LightweaveNode BuildLightweaveSettingsBody();

    void ResetLightweaveSettings();
}
