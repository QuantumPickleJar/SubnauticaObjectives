using HarmonyLib;
using SubnauticaObjectives.PDA;

namespace SubnauticaObjectives.Patches;

// After PDAEncyclopedia.Initialize populates the mapping dictionary from PDAData,
// we pre-register all graph-node entries so that save-file restoration (which calls
// Add for previously-unlocked entries) never hits "Entry not found" errors.
[HarmonyPatch(typeof(PDAEncyclopedia))]
internal static class PdaLifecyclePatches
{
    [HarmonyPostfix]
    [HarmonyPatch("Initialize")]
    private static void Initialize_Postfix()
    {
        Plugin.Log?.LogInfo("[PdaLifecyclePatches] PDAEncyclopedia initialized; pre-registering entries.");
        ObjectivesPdaTab.PreRegisterAllEntries();
    }
}