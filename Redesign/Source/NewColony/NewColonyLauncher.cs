using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class NewColonyLauncher {
    public static void Launch(
        string? scenarioName,
        string? tellerDefName,
        string? diffDefName,
        WorldParams world,
        AnomalyParams anomalyParams,
        Action onClose
    ) {
        Scenario? scen = NewColonyData.FindScenario(scenarioName);
        StorytellerDef? teller = NewColonyData.FindStoryteller(tellerDefName);
        DifficultyDef? diff = NewColonyData.FindDifficulty(diffDefName);
        if (scen == null || teller == null || diff == null) {
            Messages.Message("CL_NewColony_Error_Incomplete".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }

        onClose();

        Game.ClearCaches();
        Current.Game = new Game();
        Current.Game.InitData = new GameInitData();
        Current.Game.Scenario = scen;
        Current.Game.Scenario.PreConfigure();
        Find.GameInitData.startedFromEntry = true;
        Current.Game.InitData.mapSize = world.MapSize;
        Current.Game.storyteller = new Storyteller(teller, diff, BuildDifficulty(diff, anomalyParams));

        string seedString = world.Seed.NullOrEmpty() ? GenText.RandomSeedString() : world.Seed;
        float coverage = world.Coverage;
        OverallRainfall rainfall = NewColonyFormat.Rainfall(world.Rainfall);
        OverallTemperature temperature = NewColonyFormat.Temperature(world.Temperature);
        OverallPopulation population = NewColonyFormat.Population(world.Population);
        LandmarkDensity landmarkDensity = NewColonyFormat.Landmark(world.Landmark);
        float pollution = world.Pollution;

        LongEventHandler.QueueLongEvent(() => {
            Find.GameInitData.ResetWorldRelatedMapInitData();
            Current.Game.World = WorldGenerator.GenerateWorld(
                coverage, seedString, rainfall, temperature, population, landmarkDensity, null, pollution);
        }, "GeneratingWorld", true, null);

        LongEventHandler.QueueLongEvent(() => {
            Page? entry = FindEntryPage(scen);
            if (entry == null) {
                PageUtility.InitGameStart();
                return;
            }
            Find.WindowStack.Add(entry);
        }, string.Empty, false, null);
    }

    private static Difficulty BuildDifficulty(DifficultyDef def, AnomalyParams anomalyParams) {
        Difficulty difficulty = new Difficulty(def);
        if (!Verse.ModsConfig.AnomalyActive) {
            return difficulty;
        }

        difficulty.anomalyThreatsInactiveFraction = anomalyParams.ThreatsInactive;
        difficulty.anomalyThreatsActiveFraction = anomalyParams.ThreatsActive;
        difficulty.studyEfficiencyFactor = anomalyParams.StudyEfficiency;

        AnomalyPlaystyleDef? playstyle = DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail(anomalyParams.PlaystyleDefName);
        if (playstyle != null) {
            Traverse.Create(difficulty).Field("anomalyPlaystyleDef").SetValue(playstyle);
        }
        return difficulty;
    }

    private static Page? FindEntryPage(Scenario scen) {
        Page page = scen.GetFirstConfigPage();
        Page? cursor = page;
        while (cursor != null) {
            if (cursor is not Page_SelectStoryteller && cursor is not Page_CreateWorldParams) {
                cursor.prev = null;
                return cursor;
            }
            cursor = cursor.next;
        }
        return null;
    }
}
