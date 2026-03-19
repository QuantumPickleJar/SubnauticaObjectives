using HarmonyLib;
using SubnauticaObjectives.Facts;
using UnityEngine;

namespace SubnauticaObjectives.Patches;

// Captures successful crafting completion events so "built" facts are based on
// actual crafted items rather than blueprint unlock side effects.
[HarmonyPatch(typeof(CrafterLogic))]
internal static class CraftingPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CrafterLogic.NotifyCraftEnd), typeof(GameObject), typeof(TechType))]
    private static void NotifyCraftEnd_Postfix(TechType techType)
    {
        string techName = techType.ToString();
        var fact = FactMapper.CraftedTechFact(techName);
        if (fact is null)
            return;

        bool added = Plugin.Registry?.Add(fact) ?? false;
        if (added)
            Plugin.Log?.LogInfo("[CraftingPatches] Crafted tech mapped: " + techName + " -> " + fact);
    }
}
