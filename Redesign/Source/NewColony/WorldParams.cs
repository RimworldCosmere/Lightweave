namespace Cosmere.Lightweave.Redesign.NewColony;

public struct WorldParams {
    public float Coverage;
    public int Rainfall;
    public int Temperature;
    public int Population;
    public int Landmark;
    public int MapSize;
    public string Seed;
    public float Pollution;

    public static WorldParams Default() {
        return new WorldParams {
            Coverage = 0.3f,
            Rainfall = 3,
            Temperature = 3,
            Population = 3,
            Landmark = 3,
            MapSize = 250,
            Seed = string.Empty,
            Pollution = 0.05f,
        };
    }
}
