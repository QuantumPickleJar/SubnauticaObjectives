using HarmonyLib;

namespace SubnauticaObjectives.Patches;

// Ensures Aurora explosion state is observed even when StoryGoal key mapping differs
// across game versions or console commands.
[HarmonyPatch(typeof(CrashedShipExploder))]
internal static class AuroraExplosionPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("OnConsoleCommand_explodeship")]
    private static void ExplodeShipCommand_Postfix()
    {
        Plugin.Log?.LogInfo("[AuroraExplosionPatches] explodeship command observed.");
        Plugin.Registry?.Add("aurora_exploded");
    }

    [HarmonyPostfix]
    [HarmonyPatch("PlayExplosionFX")]
    private static void PlayExplosionFX_Postfix()
    {
        Plugin.Log?.LogInfo("[AuroraExplosionPatches] Aurora explosion FX observed.");
        Plugin.Registry?.Add("aurora_exploded");
    }
}