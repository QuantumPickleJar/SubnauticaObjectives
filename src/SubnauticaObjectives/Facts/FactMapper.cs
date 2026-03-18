using System.Collections.Generic;

namespace SubnauticaObjectives.Facts;

// Maps vanilla game identifiers (story goal keys, TechType values) to campaign graph fact names.
//
// Story goal keys come from StoryGoalManager.OnGoalComplete(string key).
// TechType names come from KnownTech.Add(TechType, bool).
//
// Where the exact internal key is uncertain it is marked with a TODO comment so it can be
// verified against the game binaries or community documentation.
public static class FactMapper
{
    // Story goal key → fact name.
    // Keys are the string IDs used internally by StoryGoalManager / StoryGoalCustomEventHandler.
    private static readonly Dictionary<string, string> StoryGoalToFact = new()
    {
        // Aurora
        ["AuroraExplosion"]              = "aurora_exploded",
        ["AuroraDriveRepair"]            = "drive_core_repaired",
        // TODO: verify exact key for captain's quarters access
        ["AuroraCaptainsQuarters"]       = "captains_quarters_accessed",

        // Sunbeam / QEP
        ["SunbeamCountdownStart"]        = "sunbeam_timer_started",
        // TODO: verify exact key for sunbeam result (destroyed or landed)
        ["SunbeamFired"]                 = "sunbeam_event_seen",
        ["PlayerOnPrecursorGun"]         = "player_on_qep_island",
        ["PrecursorGunEntered"]          = "qep_entered",
        // TODO: verify exact key for QEP terminal inspection
        ["PrecursorGunTerminalUnlocked"] = "qep_terminal_inspected",
        ["InfectionRevealed"]            = "infection_revealed",
        // TODO: verify exact key for gun block comprehension
        ["PrecursorGunBlockUnderstood"]  = "architect_gun_block_understood",
        ["PrecursorGunDeactivated"]      = "qep_deactivated",

        // Neptune message from Aurora radio
        // TODO: verify exact key for Neptune launch code radio message
        ["NeptuneLaunchCodeRadio"]       = "neptune_message_received",

        // Lifepod 19 / Floating Island
        // TODO: verify exact key for Lifepod 19 PDA pickup
        ["Lifepod19Investigated"]        = "lifepod_19_investigated",
        ["FloatingIslandLeadReceived"]   = "floating_island_lead_received",
        ["FloatingIslandVisited"]        = "floating_island_visited",

        // Degasi trail
        ["DegasiIslandCluesComplete"]    = "degasi_island_clues_complete",
        ["DegasiIslandBaseVisited"]      = "degasi_island_base_visited",
        ["DegasiJellybaseVisited"]       = "degasi_jellyshroom_base_visited",
        ["DegasiDeepBaseVisited"]        = "degasi_deep_grand_reef_base_visited",
        ["DRFLocationReceived"]          = "proposed_drf_location_received",

        // Lost River / DRF
        ["LostRiverEntered"]             = "lost_river_entered",
        ["DRFEntered"]                   = "disease_research_facility_entered",
        ["DRFScanned"]                   = "disease_research_facility_scanned",
        ["DRFAccessClueSecured"]         = "drf_access_clue_secured",
        ["OrangeTabletAcquired"]         = "orange_tablet_acquired",

        // Thermal Plant
        ["ThermalPlantEntered"]          = "alien_thermal_plant_entered",
        ["FinalAccessRequirementFound"]  = "final_access_requirement_discovered",
        ["BlueTabletBlueprintKnown"]     = "blue_tablet_blueprint_known",
        ["FinalAccessItemPrepared"]      = "final_access_item_prepared",

        // Primary Containment Facility / Cure
        ["PCFEntered"]                   = "primary_containment_facility_entered",
        ["SeaEmperorEncountered"]        = "sea_emperor_encountered",
        ["EnzymeRecipeStarted"]          = "enzyme_recipe_sequence_started",
        ["HatchingEnzymesCreated"]       = "hatching_enzymes_created",
        ["EnzymeReleased"]               = "enzyme_released",
        ["PlayerCured"]                  = "cured",

        // Neptune escape
        ["NeptunePlatformStarted"]       = "neptune_platform_started",
        ["NeptuneGantryBuilt"]           = "neptune_gantry_built",
        ["NeptuneBoostersBuilt"]         = "neptune_boosters_built",
        ["NeptuneFuelReserveBuilt"]      = "neptune_fuel_reserve_built",
        ["NeptuneCockpitBuilt"]          = "neptune_cockpit_built",
        ["TimeCapsulePrepared"]          = "time_capsule_prepared",
        ["NeptuneRocketReady"]           = "rocket_ready",
        ["EscapeBarrierConfirmed"]       = "escape_barrier_confirmed_removed",
        ["PlanetEscaped"]                = "planet_escaped",
    };

    // TechType name (enum.ToString()) → fact name.
    // These fire when KnownTech.Add is called (blueprint unlocked or item built for the first time).
    private static readonly Dictionary<string, string> TechTypeToFact = new()
    {
        // Tools / equipment built in fabricator
        ["RepairTool"]              = "repair_tool_built",
        ["Seaglide"]                = "seaglide_built",
        ["LaserCutter"]             = "laser_cutter_built",
        ["PropulsionCannon"]        = "propulsion_cannon_built",
        ["Builder"]                 = "habitat_builder_built",
        ["Beacon"]                  = "beacon_built",
        ["GravTrap"]                = "grav_trap_built",
        ["StasisRifle"]             = "stasis_rifle_built",
        ["ReinforcedDiveSuit"]      = "reinforced_dive_suit_built",
        ["UltraHighCapacityTank"]   = "ultra_high_capacity_tank_available",

        // Deployables built in fabricator
        ["Constructor"]             = "mobile_vehicle_bay_built",

        // Seabase pieces — blueprint unlock (triggers when first fragments scanned)
        ["Moonpool"]                = "moonpool_blueprint_unlocked",
        ["BaseUpgradeConsole"]      = "vehicle_upgrade_console_available",
        ["Workbench"]               = "modification_station_built",
        ["BatteryCharger"]          = "battery_charger_built",

        // Vehicles — blueprint from fragment scanning
        ["Seamoth"]                 = "seamoth_blueprint_unlocked",
        ["Exosuit"]                 = "prawn_blueprint_unlocked",
        ["Cyclops"]                 = "cyclops_blueprint_unlocked",

        // Vehicles — built via vehicle bay (KnownTech.Add fires for the built tech type too)
        // These are separate from the blueprint unlock and map to "built" facts.
        // TODO: confirm whether KnownTech.Add fires separately for the built instance
        //       or only once for the blueprint. May need CrafterLogic/Constructor patches.
    };

    // Cyclops fragment sub-types → partial-completion facts.
    private static readonly Dictionary<string, string> CyclopsFragmentToFact = new()
    {
        ["CyclopsBridgeFragment"]  = "cyclops_bridge_fragments_complete",
        ["CyclopsEngineFragment"]  = "cyclops_engine_fragments_complete",
        ["CyclopsHullFragment"]    = "cyclops_hull_fragments_complete",
    };

    // Prawn fragment sub-type → partial-completion fact.
    private static readonly Dictionary<string, string> PrawnFragmentToFact = new()
    {
        ["ExosuitFragment"] = "prawn_fragment_scan_count_complete",
    };

    // Pickup item tech type name -> fact name.
    // Used for runtime pickup events where KnownTech hooks are not invoked.
    private static readonly Dictionary<string, string> PickupToFact = new()
    {
        ["Titanium"] = "titanium_picked_up_first_time",
    };

    // Attempts to resolve a story goal key to a fact name.
    // Returns null if there is no mapping.
    public static string? StoryGoalFact(string goalKey) =>
        StoryGoalToFact.TryGetValue(goalKey, out var fact) ? fact : null;

    // Attempts to resolve a TechType.ToString() value to a fact name.
    // Returns null if there is no mapping.
    public static string? TechTypeFact(string techTypeName) =>
        TechTypeToFact.TryGetValue(techTypeName, out var fact) ? fact : null;

    // Returns facts for cyclops fragment completion events (fired by PDAScanner).
    public static string? CyclopsFragmentFact(string techTypeName) =>
        CyclopsFragmentToFact.TryGetValue(techTypeName, out var fact) ? fact : null;

    // Returns fact for prawn fragment scan completion.
    public static string? PrawnFragmentFact(string techTypeName) =>
        PrawnFragmentToFact.TryGetValue(techTypeName, out var fact) ? fact : null;

    // Returns fact for direct item pickup events.
    public static string? PickupFact(string techTypeName) =>
        PickupToFact.TryGetValue(techTypeName, out var fact) ? fact : null;
}
