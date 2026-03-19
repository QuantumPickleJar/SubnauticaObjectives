using HarmonyLib;
using SubnauticaObjectives.Facts;

namespace SubnauticaObjectives.Patches;

// Patches KnownTech to detect blueprint and unlock progression events.
// Built-item facts are handled by craft-completion hooks in CraftingPatches.
[HarmonyPatch(typeof(KnownTech))]
internal static class KnownTechPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(KnownTech.Add), typeof(TechType), typeof(bool))]
    private static void Add_Postfix(TechType techType)
    {
        string name = techType.ToString();
        bool mappedAny = false;
        var registry = Plugin.Registry;

        var fact = FactMapper.TechTypeFact(name);
        if (fact is not null)
        {
            bool allow = ShouldApplyKnownTechFact(fact, registry);
            if (allow)
            {
                Plugin.Log?.LogInfo("[KnownTechPatches] Mapped tech: " + name + " -> " + fact);
                registry?.Add(fact);
                mappedAny = true;
            }
            else
            {
                Plugin.Log?.LogDebug("[KnownTechPatches] Deferred mapped tech: " + name + " -> " + fact);
            }
        }

        foreach (string extraFact in FactMapper.AdditionalTechTypeFacts(name))
        {
            Plugin.Log?.LogInfo("[KnownTechPatches] Supplemental tech fact: " + name + " -> " + extraFact);
            registry?.Add(extraFact);
            mappedAny = true;
        }

        // Also check for fragment-completion facts (Cyclops sub-blueprints, Prawn).
        var cyclops = FactMapper.CyclopsFragmentFact(name);
        if (cyclops is not null)
        {
            Plugin.Log?.LogInfo("[KnownTechPatches] Mapped cyclops fragment: " + name + " -> " + cyclops);
            registry?.Add(cyclops);
            TryPromoteCyclopsBlueprint(registry);
            mappedAny = true;
        }

        var prawn = FactMapper.PrawnFragmentFact(name);
        if (prawn is not null)
        {
            Plugin.Log?.LogInfo("[KnownTechPatches] Mapped prawn fragment: " + name + " -> " + prawn);
            registry?.Add(prawn);
            if ((registry?.Add("prawn_blueprint_unlocked") ?? false))
                Plugin.Log?.LogInfo("[KnownTechPatches] Promoted prawn blueprint from fragment completion.");
            mappedAny = true;
        }

        if (!mappedAny)
            Plugin.Log?.LogDebug("[KnownTechPatches] Unmapped tech added: " + name);
    }

    private static bool ShouldApplyKnownTechFact(string fact, FactRegistry? registry)
    {
        // Depth upgrade availability should come from actual crafted modules.
        if (fact == "vehicle_depth_upgrade_available")
            return false;

        // Prawn/Cyclops blueprints are promoted from fragment completion only.
        if (fact == "prawn_blueprint_unlocked" || fact == "cyclops_blueprint_unlocked")
            return false;

        return true;
    }

    private static void TryPromoteCyclopsBlueprint(FactRegistry? registry)
    {
        if (registry is null)
            return;

        if (!registry.Contains("cyclops_bridge_fragments_complete"))
            return;
        if (!registry.Contains("cyclops_engine_fragments_complete"))
            return;
        if (!registry.Contains("cyclops_hull_fragments_complete"))
            return;

        if (registry.Add("cyclops_blueprint_unlocked"))
            Plugin.Log?.LogInfo("[KnownTechPatches] Promoted cyclops blueprint from fragment completion.");
    }
}
