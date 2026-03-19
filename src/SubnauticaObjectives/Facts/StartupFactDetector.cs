using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using Story;
using UnityEngine;

namespace SubnauticaObjectives.Facts;

// Reads the vanilla game's progression state at startup and populates the FactRegistry.
// Must be called after the game has finished loading a save (attach to a MonoBehaviour that
// runs in Start() or after Player.main is valid).
public static class StartupFactDetector
{
    private static readonly FieldInfo? EncyclopediaEntriesField =
        typeof(PDAEncyclopedia).GetField("entries", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly MethodInfo? KnownTechContainsMethod =
        typeof(KnownTech).GetMethod("Contains", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(TechType) }, null);

    // Call this once after the game save is loaded and Player.main is valid.
    public static void Detect(FactRegistry registry, ManualLogSource log)
    {
        var detected = new List<string>();

        DetectStoryGoals(detected, log);
        DetectKnownTech(detected, log);
        DetectBaseState(detected, log);
        DetectDatabankDerivedFacts(detected, log);

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
        foreach (string techName in Enum.GetNames(typeof(TechType)))
        {
            if (!IsKnownTech(techName))
                continue;

            var mapped = FactMapper.TechTypeFact(techName);
            if (mapped is not null)
                detected.Add(mapped);

            foreach (string extra in FactMapper.AdditionalTechTypeFacts(techName))
                detected.Add(extra);
        }

        // Multipurpose Room unlock is a strong marker for either Floating Island
        // or Jellyshroom progression on existing saves where story goal keys are absent.
        if (IsKnownTech("BaseRoom") || IsKnownTech("BaseRoomBlueprint"))
        {
            detected.Add("floating_island_visited");
            detected.Add("degasi_jellyshroom_base_visited");
        }

        // Vehicle "built" facts are inferred from scene presence (see DetectBaseState).
        DetectBuiltVehicles(detected, log);
    }

    // ── Databank unlocks (existing-save inference) ──────────────────────────

    private static void DetectDatabankDerivedFacts(List<string> detected, ManualLogSource log)
    {
        var unlocked = GetUnlockedDatabankKeys();
        if (unlocked.Count == 0)
            return;

        bool HasEntry(params string[] tokens) => unlocked.Any(k => tokens.All(t => k.Contains(t)));

        if (HasEntry("lifepod19") || HasEntry("lifepod", "19"))
            detected.Add("lifepod_19_investigated");

        if (HasEntry("floating", "island"))
        {
            detected.Add("floating_island_lead_received");
            detected.Add("floating_island_visited");
        }

        if (HasEntry("degasi", "island") || HasEntry("degasi", "floater"))
            detected.Add("degasi_island_base_visited");

        if (HasEntry("degasi", "jelly"))
            detected.Add("degasi_jellyshroom_base_visited");

        if (HasEntry("degasi", "grand", "reef") || HasEntry("degasi", "deep", "grand", "reef"))
            detected.Add("degasi_deep_grand_reef_base_visited");

        if (HasEntry("disease", "research", "facility") || HasEntry("drf", "location"))
            detected.Add("proposed_drf_location_received");

        log.LogDebug("[StartupFactDetector] Databank unlocks scanned: " + unlocked.Count + " keys.");
    }

    private static void DetectBuiltVehicles(List<string> detected, ManualLogSource log)
    {
        // Check for Seamoth, Cyclops, and PRAWN presence in the world as the most reliable
        // proxy for "has ever been built" at startup.
        if (UnityEngine.Object.FindObjectOfType<SeaMoth>() != null)
            detected.Add("seamoth_built");

        foreach (var sub in UnityEngine.Object.FindObjectsOfType<SubRoot>())
        {
            if (sub.isCyclops)
            {
                detected.Add("cyclops_built");
                break;
            }
        }

        if (UnityEngine.Object.FindObjectOfType<Exosuit>() != null)
            detected.Add("prawn_built");
    }

    // ── Base state (seabase construction) ───────────────────────────────────

    private static void DetectBaseState(List<string> detected, ManualLogSource log)
    {
        bool hasEnterable = false;
        bool hasPower = false;

        foreach (var sub in UnityEngine.Object.FindObjectsOfType<SubRoot>())
        {
            if (sub.isCyclops || !sub.isBase)
                continue;

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

        if (UnityEngine.Object.FindObjectOfType<VehicleDockingBay>() != null)
            detected.Add("moonpool_built");

        if (UnityEngine.Object.FindObjectOfType<BaseUpgradeConsole>() != null)
            detected.Add("vehicle_upgrade_console_available");
    }

    private static HashSet<string> GetUnlockedDatabankKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (EncyclopediaEntriesField?.GetValue(null) is not IDictionary entries)
            return keys;

        foreach (DictionaryEntry entry in entries)
        {
            if (entry.Key is not string key)
                continue;

            // Ignore this mod's generated objective entries.
            if (key.StartsWith("obj_", StringComparison.OrdinalIgnoreCase))
                continue;

            keys.Add(key.ToLowerInvariant());
        }

        return keys;
    }

    private static bool IsKnownTech(string techName)
    {
        if (KnownTechContainsMethod is null)
            return false;

        if (!Enum.TryParse(techName, out TechType techType))
            return false;

        try
        {
            return KnownTechContainsMethod.Invoke(null, new object[] { techType }) is bool b && b;
        }
        catch
        {
            return false;
        }
    }
}
