using HarmonyLib;
using Story;
using SubnauticaObjectives.Facts;

namespace SubnauticaObjectives.Patches;

// Patches StoryGoalManager to detect story-driven fact changes at runtime.
// Hooks into the method called whenever the game records a completed story goal,
// covering events like the Aurora explosion, Sunbeam result, QEP entry, etc.
[HarmonyPatch(typeof(StoryGoalManager))]
internal static class StoryGoalPatches
{
    // OnGoalComplete fires for every completed story goal regardless of source.
    // The key parameter matches the IDs used in FactMapper.StoryGoalToFact.
    [HarmonyPostfix]
    [HarmonyPatch(nameof(StoryGoalManager.OnGoalComplete))]
    private static void OnGoalComplete_Postfix(string key)
    {
        var fact = FactMapper.StoryGoalFact(key);
        if (fact is null)
        {
            Plugin.Log?.LogDebug("[StoryGoalPatches] Unmapped goal completed: " + key);
            return;
        }

        Plugin.Log?.LogInfo("[StoryGoalPatches] Mapped goal: " + key + " -> " + fact);
        Plugin.Registry?.Add(fact);
    }
}
