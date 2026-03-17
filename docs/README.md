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