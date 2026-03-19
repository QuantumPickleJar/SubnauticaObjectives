# Coverage Audit Summary

## Tier A gaps — resolved in v0.2.1

The following gaps identified during the initial audit have been addressed in the full campaign graph:

- ✓ Lifepod 19 / Floating Island lead handoff — `investigate_lifepod_19` → `receive_floating_island_rendezvous_lead` chain with flexible already-satisfied rules
- ✓ Radio/lifepod chain — expanded into `bubble_follow_signals` + `follow_radio_leads` + `bubble_deeper_radio_targets`
- ✓ Floating Island requires clearer lead acquisition — `visit_floating_island` activates on `floating_island_lead_received OR lifepod_19_investigated OR degasi_island_base_visited`
- ✓ Degasi island survivor clue completeness — `search_degasi_island_habitats` safety barrier with `degasi_island_clues_complete` fact
- ✓ DRF access-clue representation — `collect_drf_access_clue` with `drf_access_clue_secured` fact
- ✓ Final access-item preparation before Primary Containment — `prepare_final_access_item` with `final_access_item_prepared` fact
- ✓ Explicit endgame launch barrier confirmation — `confirm_escape_barrier_removed` with `escape_barrier_confirmed_removed` fact

## Tier B gaps — status

- ✓ Deeper-radio signal bubble — `bubble_deeper_radio_targets` added
- ✗ Sanctuary cache / ion cube support lane — deferred; not represented in v0.2.1
- ✗ Better beacon/signal-management hinting — deferred; no explicit beacon-management node yet
- ✓ Stronger role tagging for Deep Grand Reef — `visit_deep_grand_reef_base` is a named node with its own hint layers
- ✓ More precise Floating Island completion semantics — `floating_island_visited OR degasi_island_base_visited` used as a flexible OR condition

## Intent

This audit is for completeness and clarity, not blind expansion.
Only missing or underrepresented guidance-critical content should be promoted into the graph.
The remaining Tier B deferred items (sanctuary cache, beacon management) are candidates for v0.3.x.

## Runtime Hook Coverage Pass (2026-03-18)

Exhaustive fact-coverage diff against `data/campaign.graph.json` produced 4 uncovered facts:

- `habitat_builder_blueprint_unlocked`
- `player_has_visited_aurora`
- `radio_repaired`
- `vehicle_depth_upgrade_available`

Implemented coverage in this pass:

- `KnownTech.Add` supplemental mappings now emit:
	- `Builder -> habitat_builder_blueprint_unlocked`
	- depth module unlocks -> `vehicle_depth_upgrade_available`
- Added low-frequency runtime detector polling (`RuntimeFactDetector`) for:
	- radio repaired inference from lifepod radio state
	- aurora visit inference from biome string
	- known-tech fallback inference for builder blueprint and depth modules
	- ongoing base/vehicle inferred facts during active gameplay
- Titanium first pickup no longer forces objective toast output.

Notes:

- `radio_repaired` and `player_has_visited_aurora` are inferred conservatively due unstable/unknown story-goal key strings across versions.
- Story-goal and known-tech debug logging remains enabled to refine these mappings over time.

## Critical Gap Patch (fixleaks / drive core)

Problem observed in live testing:

- `explodeship` advanced guidance correctly.
- `fixleaks` did not trigger `drive_core_repaired` updates.

Implemented fallback hooks:

- `LeakingRadiation.NotifyLeaksFixed` -> `drive_core_repaired`
- `LeakingRadiation.OnConsoleCommand_fixleaks` -> `drive_core_repaired`
- `LeakingRadiation.OnConsoleCommand_decontaminate` -> `drive_core_repaired`
- `Radio.OnRepair` -> `radio_repaired`

Runtime safety net:

- Polling fallback infers `drive_core_repaired` when `aurora_exploded` is present and leak count reaches zero.

This pass closes the major undetectable progression gap around Aurora reactor repair.