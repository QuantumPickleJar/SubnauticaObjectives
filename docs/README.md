# SubnauticaObjectives

A data-driven guidance mod for Subnautica, designed primarily for private Nitrox co-op use.

## Goals

- Help players avoid getting lost in the vanilla story
- Support configurable handholding depth
- Preserve spoiler-light guidance at lower hint levels
- Allow drop-in installation on an already-progressed save
- Follow host-authoritative progression in multiplayer
- Keep authored story logic separate from game integration logic

## Architecture

This project is split into two major parts:

1. **Campaign Graph**
   - Authored as JSON (`data/campaign.graph.json`)
   - Defines milestones, bubbles, objectives, safety barriers, and facility interactions
   - Defines activation, completion, and already-satisfied rules
   - Defines hint text at multiple handholding levels (1–3)
   - Designed to handle non-linear player progression through flexible OR-based rules

2. **Runtime Integration**
   - Will be implemented with BepInEx + Harmony
   - Detects runtime facts from the game (via vanilla progression state at startup and live events)
   - Feeds those facts into the graph evaluator
   - Displays the active objective via toast-like notifications
   - Adds an "Objectives" tab to the PDA Databank, dynamically generated at startup

## Current Status

The campaign graph is fully authored:
- **65 nodes** across 9 campaign chapters
- **77 tracked facts** covering the full vanilla story arc from lifepod survival to Neptune escape
- Graph validated with no structural issues

The project is now moving into the early game-integration phase:

- **Testing surface (in progress):** toast-like notifications triggered by story commands (e.g. `explodeship`) and manual fact injection
- **Upcoming:** a new "Objectives" PDA Databank tab, dynamically generated at startup by checking the vanilla game's progression state

See `docs/design-overview.md` for the full design approach and testing strategy.

## Building the Plugin

The BepInEx plugin lives in `src/SubnauticaObjectives/`. It targets **net6.0** and requires **BepInEx 6 (IL2CPP)** — the Subnautica Living Large update switched the game to IL2CPP, so BepInEx 5 and Nautilus are not supported.

### Prerequisites

1. [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
2. **Tobey's BepInEx 6 IL2CPP pack** installed in Subnautica:
   <https://github.com/toebeann/BepInEx.Subnautica>
3. Launch Subnautica once after installing BepInEx so it generates the interop assemblies in `<GameDir>/BepInEx/interop/`.

### Local setup

```
cd src/SubnauticaObjectives
cp GamePath.props.example GamePath.props
# Edit GamePath.props and set <GameDir> to your Subnautica install path
dotnet build
```

A successful build automatically copies the DLL to `<GameDir>/BepInEx/plugins/SubnauticaObjectives/`.

Also copy `data/campaign.graph.json` to `<GameDir>/BepInEx/plugins/SubnauticaObjectives/data/campaign.graph.json` — only needed once (or after graph updates).

### Testing locally

The full test loop is:

1. Prerequisites met and BepInEx interop assemblies generated (one-time, see above)
2. Build and deploy the DLL (`dotnet build`)
3. Campaign JSON copied to the plugin data folder (one-time, see above)
4. Launch Subnautica and load or create a **new survival save**
5. Open the developer console and run story commands
6. Observe the toast notification and check the PDA Databank

That is the entire setup — there is nothing else that needs to be configured before running the story commands. Steps 1–3 are one-time actions per machine. After that it is just launch → load save → console commands.

#### Opening the developer console

1. While in-game, press **F3** to toggle the debug HUD. This also enables the console.
2. Press **\`** (backtick / tilde) to open the console input field.
3. Type a command and press **Enter**.

> **Note:** If the backtick key does not work, try pressing **Shift+Enter** or re-pressing **F3** to confirm the console is enabled. Some keyboard layouts require remapping.

#### Story commands and expected results

Each command below simulates a story milestone. After entering a command the plugin should:
- Show a **toast notification** (top-left of the screen) with the new primary objective.
- Update the **PDA → Databank → Objectives** section to reflect the current active objectives.

| Console command | What it simulates | Expected toast |
|---|---|---|
| `explodeship` | Aurora explosion | *"Objective: Prepare for the Aurora"* (or the current Aurora objective) |
| `sunbeam` | Sunbeam countdown starts (triggers QEP chapter) | *"Objective: Respond to the Sunbeam"* |
| `goto floatingisland` | Teleport to the Floating Island | Floating Island / Degasi trail objective fires on arrival |
| `goto lostriver` | Teleport to the Lost River entrance | Lost River chapter objective |
| `goto ilz` | Teleport to the Inactive Lava Zone (near Thermal Plant) | Thermal Plant chapter objective |

> **Toast text** is drawn from hint layer 1 by default (the most concise, spoiler-light wording). To see the fuller description, hint depth can be raised by editing `Plugin.cs` — `HintDepth` is a plain integer field at the top of the class.

> **Known limitation:** Some chapter transitions require multiple facts to be true simultaneously (e.g. the Degasi trail chapter requires `floating_island_visited` *and* the `aurora_exploded` fact). If a command does not produce the expected toast, use `goto` commands or chain commands (e.g. run `explodeship` before `goto floatingisland`) to satisfy all predecessor facts.

#### Checking the BepInEx log

Every fact addition and objective change is written to the BepInEx log. This is the primary diagnostic tool.

Log file location:
```
<GameDir>\BepInEx\LogOutput.log
```

What to look for:

```
[Info   : SubnauticaObjectives] Loading SubnauticaObjectives v0.2.1
[Info   : SubnauticaObjectives] [GraphLoader] Loaded vincent.subnautica.guidance v... — 65 nodes, 77 facts.
[Info   : SubnauticaObjectives] [StartupFactDetector] Loaded N facts from save state.
[Info   : SubnauticaObjectives] [Startup Objective] [objective] build_repair_tool — "Build a repair tool."
[Info   : SubnauticaObjectives] [Objective] Active: [objective] aurora_prepare — "Prepare to investigate the Aurora."
```

If the plugin loaded but no objective line appears, the graph could not find any active node for the current fact set. Check the BepInEx log for `[Error]` lines from `GraphLoader`.

#### Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| No toast appears at all after loading a save | Plugin DLL not in the right folder | Confirm `<GameDir>\BepInEx\plugins\SubnauticaObjectives\SubnauticaObjectives.dll` exists |
| `[Error] Campaign graph not found` in log | `campaign.graph.json` is missing | Copy `data\campaign.graph.json` to `<GameDir>\BepInEx\plugins\SubnauticaObjectives\data\` |
| `[Error] Plugin disabled — campaign graph could not be loaded` | JSON is malformed | Run `dotnet run --project tools/GraphInspector` from the repo root to validate |
| Toast appears on load but not after `explodeship` | Story goal key not matched in `FactMapper` | Check `LogOutput.log` — the goal key will appear; add a mapping to `Facts/FactMapper.cs` if missing |
| Console does not open | F3 not pressed first, or Living Large keybind change | Press F3 first to enable debug mode, then backtick |
| Objectives PDA section is empty | `PDAEncyclopedia.Add` silent failure | Check log for `[ObjectivesPdaTab]` lines; may require a PDA open/close cycle to refresh UI |

### Validating the graph (offline)

```
dotnet run --project tools/GraphInspector
```