using HarmonyLib;
using SubnauticaObjectives.Facts;
using SubnauticaObjectives.Notifications;

namespace SubnauticaObjectives.Patches;

// Hooks inventory pickups so first-time material collection can drive guidance state.
[HarmonyPatch(typeof(Inventory))]
internal static class ItemPickupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Inventory.Pickup), typeof(Pickupable), typeof(bool))]
    private static void Pickup_Postfix(Pickupable pickupable, bool __result)
    {
        Plugin.Log?.LogDebug("[ItemPickupPatches] Pickup_Postfix fired: " + pickupable?.GetTechType() + " result=" + __result);

        if (!__result || pickupable == null)
            return;

        var techType = pickupable.GetTechType();
        string techTypeName = techType.ToString();

        // Always use titanium pickup as a runtime refresh hook for Databank/UI state.
        if (techTypeName == "Titanium")
            Plugin.RefreshNow("pickup:titanium", showToast: false);

        var fact = FactMapper.PickupFact(techTypeName);
        if (fact is null)
            return;

        bool added = Plugin.Registry?.Add(fact) ?? false;
        if (!added)
        {
            Plugin.Log?.LogDebug("[ItemPickupPatches] Pickup fact already present: " + fact);
            return;
        }

        Plugin.Log?.LogInfo("[ItemPickupPatches] First-time pickup fact added: " + fact);
    }
}

// Additional fallback hook for pickup flows that bypass Inventory.Pickup.
[HarmonyPatch(typeof(Pickupable))]
internal static class PickupablePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pickupable.Pickup), typeof(bool))]
    private static void Pickup_Postfix(Pickupable __instance)
    {
        Plugin.Log?.LogDebug("[PickupablePatches] Pickup_Postfix fired: " + __instance?.GetTechType());

        if (__instance == null)
            return;

        string techTypeName = __instance.GetTechType().ToString();
        if (techTypeName != "Titanium")
            return;

        Plugin.Log?.LogDebug("[PickupablePatches] Titanium pickup observed via Pickupable.Pickup.");
        Plugin.RefreshNow("pickupable:titanium", showToast: false);
    }
}