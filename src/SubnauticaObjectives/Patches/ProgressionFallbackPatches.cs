using HarmonyLib;

namespace SubnauticaObjectives.Patches;

// Fallback hooks for major progression events where story-goal keys can vary.
[HarmonyPatch(typeof(Radio))]
internal static class RadioProgressionPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("OnRepair")]
    private static void OnRepair_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("radio_repaired", "[RadioProgressionPatches] Radio repaired observed.");
    }
}

[HarmonyPatch(typeof(LeakingRadiation))]
internal static class AuroraDriveCorePatches
{
    [HarmonyPostfix]
    [HarmonyPatch("NotifyLeaksFixed")]
    private static void NotifyLeaksFixed_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("drive_core_repaired", "[AuroraDriveCorePatches] All Aurora leaks fixed observed.");
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_fixleaks")]
    private static void FixLeaksConsole_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("drive_core_repaired", "[AuroraDriveCorePatches] fixleaks console command observed.");
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_decontaminate")]
    private static void DecontaminateConsole_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("drive_core_repaired", "[AuroraDriveCorePatches] decontaminate console command observed.");
    }
}

[HarmonyPatch(typeof(CrashedShipExploder))]
internal static class AuroraConsoleStatePatches
{
    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_countdownship")]
    private static void CountdownShip_Postfix()
    {
        Plugin.Log?.LogInfo("[AuroraConsoleStatePatches] countdownship command observed.");
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_restoreship")]
    private static void RestoreShip_Postfix()
    {
        Plugin.Log?.LogInfo("[AuroraConsoleStatePatches] restoreship command observed. Fact rollback is not supported in monotonic campaign mode.");
    }
}

[HarmonyPatch(typeof(StoryGoalCustomEventHandler))]
internal static class StoryGoalConsoleCommandPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_startsunbeamstoryevent")]
    private static void StartSunbeamStoryEvent_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("sunbeam_timer_started", "[StoryGoalConsoleCommandPatches] startsunbeamstoryevent observed.");
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_sunbeamcountdownstart")]
    private static void SunbeamCountdownStart_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("sunbeam_timer_started", "[StoryGoalConsoleCommandPatches] sunbeamcountdownstart observed.");
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_precursorgunaim")]
    private static void PrecursorGunAim_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("sunbeam_event_seen", "[StoryGoalConsoleCommandPatches] precursorgunaim observed.");
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_infectionreveal")]
    private static void InfectionReveal_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("infection_revealed", "[StoryGoalConsoleCommandPatches] infectionreveal observed.");
    }
}

[HarmonyPatch(typeof(VFXSunbeam))]
internal static class SunbeamFxConsolePatches
{
    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_playsunbeamfx")]
    private static void PlaySunbeamFx_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("sunbeam_event_seen", "[SunbeamFxConsolePatches] playsunbeamfx observed.");
    }
}

[HarmonyPatch(typeof(LaunchRocket))]
internal static class NeptuneConsolePatches
{
    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_forcerocketready")]
    private static void ForceRocketReady_Postfix()
    {
        ProgressionFactPatchHelpers.AddFactOnce("rocket_ready", "[NeptuneConsolePatches] forcerocketready observed.");
    }
}

internal static class ProgressionFactPatchHelpers
{
    internal static void AddFactOnce(string fact, string logMessage)
    {
        bool added = Plugin.Registry?.Add(fact) ?? false;
        if (added)
            Plugin.Log?.LogInfo(logMessage);
    }
}

