using BepInEx.Logging;
using Story;
using UnityEngine;

namespace SubnauticaObjectives.Facts;

// Reads the vanilla game's progression state at startup and populates the FactRegistry.
// Must be called after the game has finished loading a save (attach to a MonoBehaviour that
// runs in Start() or after Player.main is valid).
public static class StartupFactDetector
{
    // Call this once after the game save is loaded and Player.main is valid.
    public static void Detect(FactRegistry registry, ManualLogSource log)
    {
        var detected = new List<string>();

        DetectStoryGoals(detected, log);
        DetectKnownTech(detected, log);
        DetectBaseState(detected, log);

        registry.AddBulk(detected, () =>
            log.LogInfo($"[StartupFactDetector] Loaded {detected.Count} facts from save state."));
    }

    // ── Story goal completion ────────────────────────────────────────────────

    private static void DetectStoryGoals(List<string> detected, ManualLogSource log)
    {
        var mgr = StoryGoalManager.main;
        if (mgr is null)
        {
            log.LogWarning("[StartupFactDetector] StoryGoalManager.main is null — skipping story goals.");
            return;
        }

        foreach (var goalKey in mgr.completedGoals)
        {
            var fact = FactMapper.StoryGoalFact(goalKey);
            if (fact is not null)
                detected.Add(fact);
        }

        // Aurora explosion has its own tracker separate from story goals.
        // TODO: verify the exact API — IsExploded() may be HasExploded() or a property.
        if (CrashedShipExploder.main != null && CrashedShipExploder.main.IsExploded())
            detected.Add("aurora_exploded");
    }

    // ── KnownTech (blueprint unlocks / crafted items) ───────────────────────

    private static void DetectKnownTech(List<string> detected, ManualLogSource log)
    {
        // KnownTech.knownTech is the authoritative list of unlocked blueprints.
        // TODO: verify field name in Living Large interop — may be KnownTech.knownTech or similar.
        foreach (var techType in KnownTech.knownTech)
        {
            string name = techType.ToString();

            var fact = FactMapper.TechTypeFact(name);
            if (fact is not null)
                detected.Add(fact);

            var cyclops = FactMapper.CyclopsFragmentFact(name);
            if (cyclops is not null)
                detected.Add(cyclops);

            var prawn = FactMapper.PrawnFragmentFact(name);
            if (prawn is not null)
                detected.Add(prawn);
        }

        // Vehicle "built" facts are inferred from scene presence (see DetectBaseState).
        DetectBuiltVehicles(detected, log);
    }

    private static void DetectBuiltVehicles(List<string> detected, ManualLogSource log)
    {
        // Check for Seamoth, Cyclops, and PRAWN presence in the world as the most reliable
        // proxy for "has ever been built" at startup.
        if (Object.FindObjectOfType<SeaMoth>() != null)
            detected.Add("seamoth_built");

        foreach (var sub in Object.FindObjectsOfType<SubRoot>())
        {
            if (sub.isCyclops)
            {
                detected.Add("cyclops_built");
                break;
            }
        }

        if (Object.FindObjectOfType<Exosuit>() != null)
            detected.Add("prawn_built");
    }

    // ── Base state (seabase construction) ───────────────────────────────────

    private static void DetectBaseState(List<string> detected, ManualLogSource log)
    {
        bool hasEnterable = false;
        bool hasPower = false;

        foreach (var sub in Object.FindObjectsOfType<SubRoot>())
        {
            if (sub.isCyclops) continue;

            hasEnterable = true;

            if (sub.powerRelay != null && sub.powerRelay.GetPower() > 0f)
                hasPower = true;
        }

        if (hasEnterable)
        {
            detected.Add("constructed_enterable_base");
            if (hasPower)
            {
                detected.Add("constructed_power_source");
                detected.Add("constructed_enterable_powered_base");
            }
        }

        if (Object.FindObjectOfType<VehicleDockingBay>() != null)
            detected.Add("moonpool_built");

        if (Object.FindObjectOfType<BaseUpgradeConsole>() != null)
            detected.Add("vehicle_upgrade_console_available");
    }
}
