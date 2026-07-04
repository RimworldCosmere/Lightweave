using System;
using Concord;
using Cosmere.Lightweave.Redesign.Publish;
using Steamworks;
using Verse.Steam;

namespace Cosmere.Lightweave.Redesign.Patch;

/// <summary>
/// Terminal hook for a Lightweave-initiated Workshop upload. Cleans up the temp staging dir and
/// records the outcome on <see cref="PublishSession"/> so the publish dialog can advance to its
/// Result step. No-ops for uploads we did not initiate (no active session).
/// </summary>
[Patch(typeof(Workshop))]
public static class Workshop_OnItemSubmittedPatch {
    [Inject(At.Tail, "OnItemSubmitted")]
    public static void Postfix(ControlHandle ch, SubmitItemUpdateResult_t result, bool IOFailure) {
        string? packageId = PublishSession.ActivePackageId;
        if (packageId == null) {
            return;
        }

        try {
            bool succeeded = !IOFailure && result.m_eResult == EResult.k_EResultOK;
            bool needsLegal = result.m_bUserNeedsToAcceptWorkshopLegalAgreement;
            string? error = succeeded
                ? null
                : IOFailure ? "IO failure" : result.m_eResult.ToString();

            PublishStager.Cleanup(packageId);
            PublishSession.Complete(succeeded, error, needsLegal);
        }
        catch (Exception ex) {
            RedesignLog.Error("Workshop OnItemSubmitted cleanup failed: " + ex);
            PublishSession.Complete(false, ex.Message, false);
        }
    }
}
