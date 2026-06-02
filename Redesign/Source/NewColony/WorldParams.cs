using System.Collections.Generic;
using RimWorld;

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
    public Season StartingSeason;
    public List<FactionDef> Factions;

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
            StartingSeason = Season.Undefined,
            Factions = DefaultFactionConfig(),
        };
    }

    // Mirrors Page_CreateWorldParams.ResetFactionCounts: seed the configurable factions by their
    // startingCountAtWorldCreation, then drop any faction that a configurable faction replaces.
    public static List<FactionDef> DefaultFactionConfig() {
        List<FactionDef> factions = [];
        foreach (FactionDef def in FactionGenerator.ConfigurableFactions) {
            if (def.startingCountAtWorldCreation <= 0) {
                continue;
            }
            for (int i = 0; i < def.startingCountAtWorldCreation; i++) {
                factions.Add(def);
            }
        }
        foreach (FactionDef def in FactionGenerator.ConfigurableFactions) {
            if (def.replacesFaction != null) {
                factions.RemoveAll(x => x == def.replacesFaction);
            }
        }
        return factions;
    }
}
