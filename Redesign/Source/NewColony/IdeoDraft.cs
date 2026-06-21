using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.NewColony;

// Owns the player's draft ideoligion as a live Ideo in Find.IdeoManager during the Ideology step,
// mirroring vanilla Page_ConfigureIdeo. The editor reads/mutates this live Ideo instead of a params
// model. Every entry point is idempotent and self-healing: if a Game rebuild (scenario/teller/diff
// change) wiped the IdeoManager, the next EnsureForMode recreates the draft rather than crashing.
public static class IdeoDraft {
    private static Ideo? draftIdeo;

    // The player's current primary ideoligion, or null before the world (and player faction) exist.
    public static Ideo? Active() {
        if (Current.Game == null || Find.World == null) {
            return null;
        }
        return Faction.OfPlayer?.ideos?.PrimaryIdeo;
    }

    // The edited custom draft if it still lives in the manager (survives a world swap that detaches
    // it from the player faction), else null. Lets the commit path re-attach it without losing edits.
    public static Ideo? SurvivingDraft() {
        if (draftIdeo == null || Current.Game == null || Find.IdeoManager == null) {
            return null;
        }
        return Find.IdeoManager.IdeosListForReading.Contains(draftIdeo) ? draftIdeo : null;
    }

    // Ensures the right live ideo exists for the chosen mode. Custom modes seed a MINIMAL editable
    // ideo (a generated base with its non-structure memes stripped) so the progressive wizard builds
    // it up step by step; Preset stays def-driven and is generated at commit; Inactive flips classic mode.
    public static void EnsureForMode(IdeoMode mode, string? presetDefName) {
        if (!Verse.ModsConfig.IdeologyActive || !NewColonyLauncher.WorldReady) {
            return;
        }

        Faction? player = Faction.OfPlayer;
        if (player == null || Find.IdeoManager == null) {
            return;
        }

        if (mode == IdeoMode.Inactive) {
            Find.IdeoManager.classicMode = true;
            return;
        }

        if (mode == IdeoMode.Preset) {
            // The gallery/summary read the IdeoPresetDef directly; the ideo is generated at commit.
            Find.IdeoManager.classicMode = false;
            return;
        }

        bool wantFluid = mode == IdeoMode.CustomFluid;
        Ideo? current = player.ideos?.PrimaryIdeo;
        if (draftIdeo != null
            && current == draftIdeo
            && Find.IdeoManager.IdeosListForReading.Contains(draftIdeo)
            && draftIdeo.Fluid == wantFluid) {
            return;
        }

        // Generate a valid base (name + icon + a cached, valid description), then strip ALL memes so the
        // wizard starts with nothing selected: the player must pick a structure on step 1 and a starting
        // meme on step 2. ClearNormalMemes recaches while the structure is still present (keeping the
        // ideo valid); we then drop the structure from the list without a recache-on-empty so the
        // structure grid shows no selection. The player's SwapStructure pick re-inserts + recaches.
        Ideo ideo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(player.def));
        ideo.Fluid = wantFluid;
        IdeoDraftMutations.ClearNormalMemes(ideo);
        ideo.memes.RemoveAll(m => m.category == MemeCategory.Structure);
        AssignDraft(ideo);
        draftIdeo = ideo;
    }

    // Canonical idempotent assigner. Mirrors Page_ChooseIdeoPreset.DoCustomize -> SelectOrMakeNewIdeo:
    // detach prior starting ideos, prune the orphans, then add + set the new primary.
    public static void AssignDraft(Ideo ideo) {
        Faction player = Faction.OfPlayer;
        if (player?.ideos == null || Find.IdeoManager == null) {
            return;
        }

        foreach (Ideo existing in Find.IdeoManager.IdeosListForReading) {
            existing.initialPlayerIdeo = false;
        }
        player.ideos.RemoveAll();
        Find.IdeoManager.RemoveUnusedStartingIdeos();

        if (!Find.IdeoManager.IdeosListForReading.Contains(ideo)) {
            Find.IdeoManager.Add(ideo);
        }
        player.ideos.SetPrimary(ideo);
        ideo.initialPlayerIdeo = true;
        Find.IdeoManager.classicMode = false;
        draftIdeo = ideo;
    }

    // Drops the unreferenced draft on window close/cancel. Safe/idempotent.
    public static void Discard() {
        draftIdeo = null;
        if (Current.Game != null && Find.IdeoManager != null) {
            Find.IdeoManager.RemoveUnusedStartingIdeos();
        }
    }
}
