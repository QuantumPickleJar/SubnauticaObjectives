# Design Overview

## Core Idea

The mod uses a directed progression graph to represent the vanilla Subnautica campaign in a structured, testable way.

The graph is not a walkthrough script. It is a rule-driven campaign model.

## Node Types

- **major_milestone**  
  Large campaign phases such as Aurora, Sunbeam/QEP, Lost River, Cure, Escape.

- **bubble**  
  Flexible groups of objectives that can often be completed in varying order.

- **objective**  
  A concrete player-facing task.

- **safety_barrier**  
  A reminder/checkpoint designed to prevent common confusion or important omissions.

- **facility_interaction**  
  A meaningful interaction with an alien facility, often split into entry/inspection/progression substeps.

## Hint Depth

The same node can have multiple handholding levels:

- **1** = concise, spoiler-light
- **2** = more explicit, still intended to remain mostly spoiler-safe
- **3** = most explicit, may reveal more direct guidance

## Completion Scope

Nodes may be completed by:
- host only
- any player in a shared-world sense
- any player personally
- system-inferred conditions

## Drop-In Support

The graph must support installation into an already-progressed save.

Every important node should support:
- activation rules
- completion rules
- already-satisfied rules

Rules are authored as boolean expressions (e.g. `fact_a AND NOT fact_b`, `fact_a OR fact_b`) evaluated against a set of detected facts.

## Design Bias

The graph uses a **hybrid story-and-capability-driven** approach with safety barriers.

- Story nodes drive the main campaign arc (Aurora, QEP, Degasi trail, Lost River, cure, escape)
- Capability nodes handle early-game equipment that is required to safely reach story content
- Safety barriers act as checkpoints to prevent players from skipping critical scannable items or repairs
- Completion rules use flexible OR-based logic so players who arrive at a location by an unplanned route are still recognized as having progressed

This bias is declared in the graph as `design_bias: "hybrid_story_and_capability_driven_with_safety_barriers"`.

## Testing Strategy

Initial game integration uses two complementary mechanisms:

1. **Toast-like notifications** — the primary display mode for the current testing phase. These appear in-game when the active objective changes, driven by story-setting console commands (e.g. `explodeship`) and manual fact injection.

2. **PDA Objectives tab** — a new tab in the Databank section of the PDA called "Objectives". This tab is dynamically generated once at startup by checking the vanilla game's progression state and remains the canonical in-game display for the full release.

Testing proceeds by:
- Using story commands like `explodeship` to simulate milestone-triggering events
- Verifying that the correct toast notification fires for each transition
- Verifying that the PDA tab reflects the correct graph state

## Dependency Policy

**No Nautilus dependency.** Nautilus is incompatible with Nitrox (the co-op mod this project is designed for). All game APIs are accessed directly via BepInEx 6 + Harmony patches and raw interop assembly references. This is a hard constraint.

## Development Priority

1. ~~Author and validate the graph~~ ✓ (65 nodes, 77 facts, 9 chapters — fully authored)
2. ~~Build a standalone evaluator~~ ✓ (GraphInspector validates structural integrity)
3. **Add a tiny in-game test surface** ← current focus (toast notifications + PDA Objectives tab)
4. Add fact detection gradually (via vanilla progression state at startup and live Harmony patches)