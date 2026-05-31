namespace Cosmere.Lightweave.Redesign.NewColony;

public struct AnomalyParams {
    public string PlaystyleDefName;
    public float ThreatsInactive;
    public float ThreatsActive;
    public float StudyEfficiency;

    public static AnomalyParams Default() {
        return new AnomalyParams {
            PlaystyleDefName = "Standard",
            ThreatsInactive = 0.08f,
            ThreatsActive = 0.3f,
            StudyEfficiency = 1f,
        };
    }
}
