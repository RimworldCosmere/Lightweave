namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Cross-cutting bus carrying the in-flight upload's identity and final outcome between the
/// publish dialog and the parameterless-fired Steam callbacks (<c>Workshop.OnItemSubmitted</c>).
/// The dialog polls <see cref="Succeeded"/> each frame to advance to its Result step.
/// </summary>
public static class PublishSession {
    public static string? ActivePackageId { get; private set; }

    public static bool? Succeeded { get; private set; }

    public static string? ErrorDetail { get; private set; }

    public static bool NeedsLegalAgreement { get; private set; }

    public static bool InFlight => ActivePackageId != null && Succeeded == null;

    public static void Begin(string packageId) {
        ActivePackageId = packageId;
        Succeeded = null;
        ErrorDetail = null;
        NeedsLegalAgreement = false;
    }

    public static void Complete(bool succeeded, string? errorDetail, bool needsLegalAgreement) {
        Succeeded = succeeded;
        ErrorDetail = errorDetail;
        NeedsLegalAgreement = needsLegalAgreement;
    }

    public static void Reset() {
        ActivePackageId = null;
        Succeeded = null;
        ErrorDetail = null;
        NeedsLegalAgreement = false;
    }
}
