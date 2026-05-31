using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class NewColonyLauncher {
    private static int provisionSig;
    private static int worldGenToken;
    private static int worldGenSig;
    private static bool generating;
    private static WorldParams? pendingGen;
    private static int genFrame;
    private static bool committed;
    private static string? pendingScenario;
    private static string? pendingTeller;
    private static string? pendingDiff;
    private static AnomalyParams pendingAnomaly;

    public static bool WorldReady => Current.Game?.World != null;
    public static int WorldGenToken => worldGenToken;
    public static bool Generating => generating;
    public static bool Committed => committed;
    public static bool OwnsWorld { get; private set; }
    public static string LastSeed { get; private set; } = string.Empty;

    public static bool NeedsRegen(WorldParams world) {
        return !WorldReady || worldGenSig != WorldGenSignature(world);
    }

    public static bool Provision(
        string? scenarioName,
        string? tellerDefName,
        string? diffDefName,
        WorldParams world,
        AnomalyParams anomalyParams
    ) {
        Scenario? scen = NewColonyData.FindScenario(scenarioName);
        StorytellerDef? teller = NewColonyData.FindStoryteller(tellerDefName);
        DifficultyDef? diff = NewColonyData.FindDifficulty(diffDefName);
        if (scen == null || teller == null || diff == null) {
            Messages.Message("CL_NewColony_Error_Incomplete".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        // The window now owns the shared world: the ShouldDoMainMenu patch keeps
        // Current.Game alive (so the generated world survives) without forcing the global
        // renderer into Planet mode, which is what used to render the globe full-screen
        // behind the window.
        OwnsWorld = true;

        int sig = ProvisionSignature(scenarioName, tellerDefName, diffDefName, world.MapSize, anomalyParams);
        if (Current.Game != null && provisionSig == sig && !committed) {
            return true;
        }

        Game.ClearCaches();
        Current.Game = new Game();
        Current.Game.InitData = new GameInitData();
        Current.Game.Scenario = scen;
        Current.Game.Scenario.PreConfigure();
        Find.GameInitData.startedFromEntry = true;
        Current.Game.InitData.mapSize = world.MapSize;
        Current.Game.storyteller = new Storyteller(teller, diff, BuildDifficulty(diff, anomalyParams));

        provisionSig = sig;
        worldGenToken = 0;
        worldGenSig = 0;
        committed = false;
        return true;
    }

    public static void PrepareProvision(
        string? scenarioName,
        string? tellerDefName,
        string? diffDefName,
        AnomalyParams anomalyParams
    ) {
        pendingScenario = scenarioName;
        pendingTeller = tellerDefName;
        pendingDiff = diffDefName;
        pendingAnomaly = anomalyParams;
    }

    public static void GenerateWorld(WorldParams world) {
        pendingGen = world;
        genFrame = Time.frameCount;
        generating = true;
    }

    public static void PumpPendingGen() {
        if (!generating || pendingGen == null) {
            return;
        }
        if (Time.frameCount - genFrame < 2) {
            return;
        }
        WorldParams world = pendingGen.Value;
        pendingGen = null;
        if (!Provision(pendingScenario, pendingTeller, pendingDiff, world, pendingAnomaly)) {
            generating = false;
            return;
        }
        RunWorldGen(world);
        generating = false;
    }

    public static void Commit(WorldParams world, PlanetTile startingTile, Action onClose) {
        if (Current.Game == null) {
            Messages.Message("CL_NewColony_Error_Incomplete".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }

        committed = true;
        generating = false;
        pendingGen = null;
        ReleaseWorld();

        bool regen = NeedsRegen(world);
        if (regen) {
            RunWorldGen(world);
        }

        onClose();

        Scenario scen = Current.Game.Scenario;
        LongEventHandler.QueueLongEvent(() => {
            PlanetTile tile = !regen && startingTile.Valid ? startingTile : TileFinder.RandomStartingTile();
            Find.GameInitData.startingTile = tile;

            Page? entry = FindEntryPage(scen, skipStartingSite: true);
            if (entry == null) {
                PageUtility.InitGameStart();
                return;
            }
            Find.WindowStack.Add(entry);
        }, string.Empty, false, null);
    }

    public static void Teardown() {
        provisionSig = 0;
        worldGenToken = 0;
        worldGenSig = 0;
        generating = false;
        pendingGen = null;
        committed = false;
        ReleaseWorld();
        if (Current.Game == null) {
            return;
        }

        Current.Game.Dispose();
        MemoryUtility.ClearAllMapsAndWorld();
        Current.Game = null;
        MemoryUtility.UnloadUnusedUnityAssets();
    }

    // Release the window's ownership of the shared world. The ShouldDoMainMenu patch then
    // stops protecting Current.Game so vanilla's menu loop resumes, and the WorldCamera is
    // governed normally by wantedMode (None at the menu) — no full-screen globe.
    private static void ReleaseWorld() {
        OwnsWorld = false;
    }

    private static void RunWorldGen(WorldParams world) {
        if (Current.Game == null) {
            return;
        }

        string seedString = world.Seed.NullOrEmpty() ? GenText.RandomSeedString() : world.Seed;
        float coverage = world.Coverage;
        OverallRainfall rainfall = NewColonyFormat.Rainfall(world.Rainfall);
        OverallTemperature temperature = NewColonyFormat.Temperature(world.Temperature);
        OverallPopulation population = NewColonyFormat.Population(world.Population);
        LandmarkDensity landmarkDensity = NewColonyFormat.Landmark(world.Landmark);
        float pollution = world.Pollution;

        LastSeed = seedString;
        worldGenSig = WorldGenSignature(world);

        Find.GameInitData.ResetWorldRelatedMapInitData();
        World built = WorldGenerator.GenerateWorld(
            coverage, seedString, rainfall, temperature, population, landmarkDensity, null, pollution);
        Current.Game.World = built;
        Find.World.renderer.RegenerateAllLayersNow();
        MemoryUtility.UnloadUnusedUnityAssets();
        worldGenToken++;
    }

    private static int WorldGenSignature(WorldParams world) {
        int hash = 17;
        hash = hash * 31 + (world.Seed?.GetHashCode() ?? 0);
        hash = hash * 31 + world.Coverage.GetHashCode();
        hash = hash * 31 + world.Rainfall;
        hash = hash * 31 + world.Temperature;
        hash = hash * 31 + world.Population;
        hash = hash * 31 + world.Landmark;
        hash = hash * 31 + world.Pollution.GetHashCode();
        return hash;
    }

    private static int ProvisionSignature(
        string? scenarioName,
        string? tellerDefName,
        string? diffDefName,
        int mapSize,
        AnomalyParams anomalyParams
    ) {
        int hash = 17;
        hash = hash * 31 + (scenarioName?.GetHashCode() ?? 0);
        hash = hash * 31 + (tellerDefName?.GetHashCode() ?? 0);
        hash = hash * 31 + (diffDefName?.GetHashCode() ?? 0);
        hash = hash * 31 + mapSize;
        hash = hash * 31 + anomalyParams.GetHashCode();
        return hash;
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

    private static Page? FindEntryPage(Scenario scen, bool skipStartingSite) {
        Page page = scen.GetFirstConfigPage();
        Page? cursor = page;
        while (cursor != null) {
            bool skip = cursor is Page_SelectStoryteller
                || cursor is Page_CreateWorldParams
                || (skipStartingSite && cursor is Page_SelectStartingSite);
            if (!skip) {
                cursor.prev = null;
                return cursor;
            }
            cursor = cursor.next;
        }
        return null;
    }
}
