using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class ScenarioInfo {
    private static readonly FieldInfo? ThingDefField =
        typeof(ScenPart_ThingCount).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? CountField =
        typeof(ScenPart_ThingCount).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? AnimalCountField =
        typeof(ScenPart_StartingAnimal).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? ArriveMethodField =
        typeof(ScenPart_PlayerPawnsArriveMethod).GetField("method", BindingFlags.Instance | BindingFlags.NonPublic);

    private static FieldInfo? namedPawnsField;
    private static bool namedPawnsResolved;

    public static int StartingColonists(Scenario scen) {
        foreach (ScenPart part in scen.AllParts) {
            if (part is ScenPart_ConfigPage_ConfigureStartingPawns_Xenotypes xenotypes) {
                int total = 0;
                for (int i = 0; i < xenotypes.xenotypeCounts.Count; i++) {
                    total += xenotypes.xenotypeCounts[i].count;
                }
                return total;
            }
            if (part is ScenPart_ConfigPage_ConfigureStartingPawns_KindDefs kindDefs) {
                int total = 0;
                for (int i = 0; i < kindDefs.kindCounts.Count; i++) {
                    total += kindDefs.kindCounts[i].count;
                }
                return total;
            }
            if (part is ScenPart_ConfigPage_ConfigureStartingPawns pawns) {
                return pawns.pawnCount;
            }
            if (part.GetType().Name == "ScenPart_NamedPawns") {
                int named = NamedPawnsCount(part);
                if (named > 0) {
                    return named;
                }
            }
        }
        return 0;
    }

    private static int NamedPawnsCount(ScenPart part) {
        if (!namedPawnsResolved) {
            namedPawnsField = part.GetType().GetField("pawns", BindingFlags.Instance | BindingFlags.Public);
            namedPawnsResolved = true;
        }
        if (namedPawnsField?.GetValue(part) is System.Collections.ICollection list) {
            return list.Count;
        }
        return 0;
    }

    public static List<(ThingDef def, int count)> LoadoutEntries(Scenario scen) {
        List<(ThingDef def, int count)> entries = [];
        if (ThingDefField == null || CountField == null) {
            return entries;
        }
        foreach (ScenPart part in scen.AllParts) {
            if (part is not ScenPart_StartingThing_Defined thing) {
                continue;
            }
            if (ThingDefField.GetValue(thing) is not ThingDef def) {
                continue;
            }
            int count = (int)CountField.GetValue(thing);
            entries.Add((def, count));
        }
        return entries;
    }

    public static int StartingItemsCount(Scenario scen) {
        int count = 0;
        foreach (ScenPart part in scen.AllParts) {
            if (part is ScenPart_StartingThing_Defined) {
                count++;
            }
        }
        return count;
    }

    public static int StartingAnimalCount(Scenario scen) {
        if (AnimalCountField == null) {
            return 0;
        }
        int total = 0;
        foreach (ScenPart part in scen.AllParts) {
            if (part is ScenPart_StartingAnimal animal) {
                total += (int)AnimalCountField.GetValue(animal);
            }
        }
        return total;
    }

    public static string? ArrivalMethodLabel(Scenario scen) {
        if (ArriveMethodField == null) {
            return null;
        }
        foreach (ScenPart part in scen.AllParts) {
            if (part is ScenPart_PlayerPawnsArriveMethod arrive) {
                PlayerPawnsArriveMethod method = (PlayerPawnsArriveMethod)ArriveMethodField.GetValue(arrive);
                return method.ToStringHuman();
            }
        }
        return null;
    }

    public static string? PlanetLayerLabel(Scenario scen) {
        foreach (ScenPart part in scen.AllParts) {
            if (part is ScenPart_PlanetLayer planet && planet.layer != null) {
                return planet.layer.LabelCap.ToString();
            }
        }
        return null;
    }
}
