# Open Questions

## Detection Questions
- Which facts are best detected live via Harmony (vs. startup scan)?
  - Currently: story goals and KnownTech additions are patched live; base/vehicle state is scanned at startup.
- Which facts are unsafe or unreliable under Nitrox?
  - Host-authoritative facts (completion_scope: host_only) should only be updated on the host.
  - Player-personal facts (e.g. repair_tool_built) may desync between clients — currently flagged in the graph but not yet enforced in the plugin.
- Exact story goal key strings for some facts still need verification against game binaries:
  - Captain's Quarters access, Neptune radio message, various Degasi/DRF/PCF events.
  - See the TODO comments in `Facts/FactMapper.cs`.

## Multiplayer Questions
- The graph uses `host_authoritative: true`. The plugin does not yet enforce this — all clients run full detection.
- Nitrox-safe fact sharing mechanism: to be designed. For now all facts are local-only.
- Which objectives should suppress client-side toasts vs. host-only display? TBD.

## Content Questions
- Sanctuary cache / ion cube support lane: deferred to v0.3.x.
- Beacon/signal-management hinting: deferred to v0.3.x.

## UX Questions
- Hint depth is currently hardcoded to 1 in the plugin. A BepInEx config entry (`BepInEx/config/`) should expose this setting.
- The PDA "Objectives" tab currently adds entries to the existing Databank via PDAEncyclopedia.
  A proper custom tab (requiring UI patching of uGUI_PDA) is a future milestone.
- Should the toast fire on every fact change, or only when the primary objective changes? Currently fires on every new fact; debouncing may be needed.
- How many secondary suggestions should appear alongside the primary? Currently only one primary is shown.

## Technical Questions
- Rule expressions are parsed as boolean strings at runtime. This is sufficient for v0.2.x.
- Validation of rule syntax is performed at load time in GraphInspector (offline tool).
- The startup fact detector checks for SeaMoth/Cyclops/Exosuit via FindObjectOfType, which has a performance cost. If the game's scene structure makes this slow, consider checking VehicleManager or a save-data path instead.
- Campaign graph is loaded from `BepInEx/plugins/SubnauticaObjectives/data/campaign.graph.json`. Keeping it as an external file (not embedded) allows updating the graph without recompiling the DLL.