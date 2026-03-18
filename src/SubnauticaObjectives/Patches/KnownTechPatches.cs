using HarmonyLib;
using SubnauticaObjectives.Facts;

namespace SubnauticaObjectives.Patches;

// Patches KnownTech to detect blueprint unlocks and first-craft events.
// Fires whenever the game adds a new TechType to the player's known blueprints,
// which covers both fragment-scan completions and direct first-craft events.
[HarmonyPatch(typeof(KnownTech))]
internal static class KnownTechPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(KnownTech.Add), typeof(TechType), typeof(bool))]
    private static void Add_Postfix(TechType techType)
    {
        string name = techType.ToString();

        var fact = FactMapper.TechTypeFact(name);
        if (fact is not null)
        {
            Plugin.Log?.LogInfo("[KnownTechPatches] Mapped tech: " + name + " -> " + fact);
            Plugin.Registry?.Add(fact);
            return;
        }

        // Also check for fragment-completion facts (Cyclops sub-blueprints, Prawn).
        var cyclops = FactMapper.CyclopsFragmentFact(name);
        if (cyclops is not null)
        {
            Plugin.Log?.LogInfo("[KnownTechPatches] Mapped cyclops fragment: " + name + " -> " + cyclops);
            Plugin.Registry?.Add(cyclops);
            return;
        }

        var prawn = FactMapper.PrawnFragmentFact(name);
        if (prawn is not null)
        {
            Plugin.Log?.LogInfo("[KnownTechPatches] Mapped prawn fragment: " + name + " -> " + prawn);
            Plugin.Registry?.Add(prawn);
            return;
        }

        Plugin.Log?.LogDebug("[KnownTechPatches] Unmapped tech added: " + name);
    }
}
