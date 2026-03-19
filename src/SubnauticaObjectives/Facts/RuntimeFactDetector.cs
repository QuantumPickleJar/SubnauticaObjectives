using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace SubnauticaObjectives.Facts;

// Performs low-frequency runtime fact inference for progression signals that
// are not fully covered by deterministic story-goal keys.
public static class RuntimeFactDetector
{
    private static float _nextPollTime;

    public static void Poll(FactRegistry registry, ManualLogSource log)
    {
        if (Time.time < _nextPollTime)
            return;

        _nextPollTime = Time.time + 2f;

        TryDetectRadioRepaired(registry, log);
        TryDetectDriveCoreRepaired(registry, log);
        TryDetectAuroraVisit(registry, log);
        TryDetectKnownTechDerivedFacts(registry, log);
        TryDetectBaseAndVehicleFacts(registry, log);
    }

    private static void TryDetectDriveCoreRepaired(FactRegistry registry, ManualLogSource log)
    {
        if (registry.Contains("drive_core_repaired"))
            return;

        if (!registry.Contains("aurora_exploded"))
            return;

        var leaking = LeakingRadiation.main;
        if (leaking == null)
            return;

        try
        {
            var getNumLeaks = leaking.GetType().GetMethod("GetNumLeaks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (getNumLeaks == null)
                return;

            if (getNumLeaks.Invoke(leaking, null) is int leakCount && leakCount == 0 && registry.Add("drive_core_repaired"))
                log.LogInfo("[RuntimeFactDetector] Inferred drive_core_repaired from LeakingRadiation leak count.");
        }
        catch
        {
            // Keep polling fallback silent if game API behavior changes.
        }
    }

    private static void TryDetectRadioRepaired(FactRegistry registry, ManualLogSource log)
    {
        if (registry.Contains("radio_repaired"))
            return;

        var pod = EscapePod.main;
        if (pod == null)
            return;

        Component? radioComponent = null;
        foreach (var c in pod.GetComponentsInChildren<Component>(true))
        {
            if (c == null)
                continue;

            if (c.GetType().Name == "Radio")
            {
                radioComponent = c;
                break;
            }
        }

        if (radioComponent == null)
            return;

        if (TryReadBoolMember(radioComponent, out bool damaged, "isDamaged", "damaged", "isBroken", "broken"))
        {
            if (!damaged && registry.Add("radio_repaired"))
                log.LogInfo("[RuntimeFactDetector] Inferred radio_repaired from radio damage state.");
            return;
        }

        var mixin = radioComponent.GetComponent<LiveMixin>();
        if (mixin != null && mixin.maxHealth > 0f && mixin.health >= mixin.maxHealth - 0.01f)
        {
            if (registry.Add("radio_repaired"))
                log.LogInfo("[RuntimeFactDetector] Inferred radio_repaired from radio health.");
        }
    }

    private static void TryDetectAuroraVisit(FactRegistry registry, ManualLogSource log)
    {
        if (registry.Contains("player_has_visited_aurora"))
            return;

        var player = Player.main;
        if (player == null)
            return;

        string biome = GetBiomeString(player);
        if (string.IsNullOrEmpty(biome))
            return;

        string lower = biome.ToLowerInvariant();
        if (!lower.Contains("aurora") && !lower.Contains("crashedship"))
            return;

        if (registry.Add("player_has_visited_aurora"))
            log.LogInfo("[RuntimeFactDetector] Inferred player_has_visited_aurora from biome: " + biome);
    }

    private static void TryDetectKnownTechDerivedFacts(FactRegistry registry, ManualLogSource log)
    {
        if (!registry.Contains("habitat_builder_blueprint_unlocked") && IsKnownTech("Builder"))
        {
            if (registry.Add("habitat_builder_blueprint_unlocked"))
                log.LogInfo("[RuntimeFactDetector] Inferred habitat_builder_blueprint_unlocked from KnownTech.");
        }
    }

    private static void TryDetectBaseAndVehicleFacts(FactRegistry registry, ManualLogSource log)
    {
        if (!registry.Contains("seamoth_built") && UnityEngine.Object.FindObjectOfType<SeaMoth>() != null)
        {
            if (registry.Add("seamoth_built"))
                log.LogInfo("[RuntimeFactDetector] Inferred seamoth_built from world vehicle presence.");
        }

        if (!registry.Contains("cyclops_built"))
        {
            foreach (var sub in UnityEngine.Object.FindObjectsOfType<SubRoot>())
            {
                if (!sub.isCyclops)
                    continue;

                if (registry.Add("cyclops_built"))
                    log.LogInfo("[RuntimeFactDetector] Inferred cyclops_built from world vehicle presence.");
                break;
            }
        }

        if (!registry.Contains("prawn_built") && UnityEngine.Object.FindObjectOfType<Exosuit>() != null)
        {
            if (registry.Add("prawn_built"))
                log.LogInfo("[RuntimeFactDetector] Inferred prawn_built from world vehicle presence.");
        }

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

        if (hasEnterable && !registry.Contains("constructed_enterable_base"))
        {
            if (registry.Add("constructed_enterable_base"))
                log.LogInfo("[RuntimeFactDetector] Inferred constructed_enterable_base from SubRoot state.");
        }

        if (hasPower && !registry.Contains("constructed_power_source"))
        {
            if (registry.Add("constructed_power_source"))
                log.LogInfo("[RuntimeFactDetector] Inferred constructed_power_source from SubRoot power state.");
        }

        if (hasEnterable && hasPower && !registry.Contains("constructed_enterable_powered_base"))
        {
            if (registry.Add("constructed_enterable_powered_base"))
                log.LogInfo("[RuntimeFactDetector] Inferred constructed_enterable_powered_base from SubRoot state.");
        }

        if (!registry.Contains("moonpool_built") && UnityEngine.Object.FindObjectOfType<VehicleDockingBay>() != null)
        {
            if (registry.Add("moonpool_built"))
                log.LogInfo("[RuntimeFactDetector] Inferred moonpool_built from docking bay presence.");
        }

        if (!registry.Contains("vehicle_upgrade_console_available") && UnityEngine.Object.FindObjectOfType<BaseUpgradeConsole>() != null)
        {
            if (registry.Add("vehicle_upgrade_console_available"))
                log.LogInfo("[RuntimeFactDetector] Inferred vehicle_upgrade_console_available from console presence.");
        }
    }

    private static bool IsKnownTech(string techName)
    {
        if (!Enum.TryParse(techName, out TechType techType))
            return false;

        var containsMethod = typeof(KnownTech).GetMethod("Contains", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(TechType) }, null);
        if (containsMethod == null)
            return false;

        try
        {
            return containsMethod.Invoke(null, new object[] { techType }) is bool b && b;
        }
        catch
        {
            return false;
        }
    }

    private static string GetBiomeString(Player player)
    {
        var method = player.GetType().GetMethod("GetBiomeString", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (method == null)
            return string.Empty;

        try
        {
            return method.Invoke(player, null) as string ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryReadBoolMember(object target, out bool value, params string[] memberNames)
    {
        var type = target.GetType();
        foreach (string name in memberNames)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.PropertyType == typeof(bool))
            {
                value = (bool)property.GetValue(target, null);
                return true;
            }

            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(bool))
            {
                value = (bool)field.GetValue(target);
                return true;
            }
        }

        value = false;
        return false;
    }
}
