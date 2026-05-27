using System;
using System.Linq;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Redesign.ModsConfig;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using Verse;

namespace Cosmere.Lightweave.Redesign.ModOptions;

public sealed class LightweaveModSettingsWindow : LightweaveWindow {
    private readonly Mod mod;
    private readonly ILightweaveSettings settings;
    private readonly ModMetaData? metadata;

    public LightweaveModSettingsWindow(Mod mod) {
        this.mod = mod;
        if (mod is not ILightweaveSettings ls) {
            throw new ArgumentException($"Mod {mod.GetType().FullName} must implement ILightweaveSettings.", nameof(mod));
        }
        settings = ls;
        metadata = ResolveMetadata(mod);
        absorbInputAroundWindow = true;
        forcePause = true;
        closeOnCancel = true;
        closeOnAccept = false;
        drawOwnCloseX = true;
    }

    protected override float WidthFraction => 0.55f;

    protected override float HeightFraction => 0.7f;

    protected override float MaxCardWidth => 1100f;

    protected override float MaxCardHeight => 900f;

    protected override LightweaveNode Header() {
        if (metadata == null) {
            return Stack.Create(SpacingScale.None, _ => { });
        }
        return ModDetailPane.BuildHeader(metadata, trailing: BuildActions);
    }

    protected override LightweaveNode Body() {
        return settings.BuildLightweaveSettingsBody();
    }

    public override void PostClose() {
        base.PostClose();
        mod.WriteSettings();
    }


    private void BuildActions(StackBuilder column) {
        string resetLabel = ((string)"CL_ModSettings_Action_Reset".Translate()).ToUpperInvariant();
        column.Add(Button.Create(
            label: resetLabel,
            onClick: () => settings.ResetLightweaveSettings(),
            variant: Variant.Secondary
        ));
        if (metadata != null) {
            bool active = metadata.Active;
            string toggleLabel = active
                ? ((string)"CL_ModsConfig_Action_Disable".Translate()).ToUpperInvariant()
                : ((string)"CL_ModsConfig_Action_Enable".Translate()).ToUpperInvariant();
            column.Add(Button.Create(
                label: toggleLabel,
                onClick: () => Verse.ModsConfig.SetActive(metadata, !active),
                variant: Variant.Secondary,
                style: new Style { TextColor = ThemeSlot.StatusDanger }
            ));
        }
    }

    private static ModMetaData? ResolveMetadata(Mod mod) {
        string? packageId = mod.Content?.PackageId;
        if (string.IsNullOrEmpty(packageId)) {
            return null;
        }
        return ModLister.AllInstalledMods.FirstOrDefault(
            m => string.Equals(m.PackageId, packageId, StringComparison.OrdinalIgnoreCase)
        );
    }
}
