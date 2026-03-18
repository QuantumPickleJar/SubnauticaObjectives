# AGENTS.md

## Repository purpose

This repository contains a data-driven Subnautica guidance mod intended primarily for private Nitrox co-op use.

The project has two major layers:

1. Campaign graph authoring and validation
2. Runtime integration with Subnautica via BepInEx + Harmony

Do not blur these layers together unnecessarily.

## Current development priority

Until explicitly told otherwise, prefer work in this order:

1. Graph JSON correctness
2. Validation and inspection tooling
3. C# graph model and evaluator
4. Minimal runtime integration
5. Expanded UI integration
6. Broader patch coverage

Do not jump straight into game patches if the graph, validator, or evaluator is still unstable.

## Architectural rules

- Treat `data/campaign.graph.json` as the source of truth for authored progression logic.
- Keep graph authoring concerns separate from runtime game detection.
- Do not hardcode campaign logic into Harmony patch classes if it belongs in the graph or evaluator.
- Prefer small, testable classes over large patch-heavy files.
- Runtime integration should feed facts into the evaluator, not replace it.

## Editing rules

- Preserve stable node IDs unless explicitly instructed to rename them.
- Do not silently rewrite large sections of the campaign graph.
- When editing the graph, prefer surgical changes.
- If adding new facts or nodes, keep names consistent with the existing naming style.
- If changing graph semantics, explain why in comments, docs, or the PR summary.

## Validation expectations

Before considering a change complete, check for:

- duplicate node IDs
- broken predecessor/successor references
- missing required hint layers where applicable
- contradictory activation/completion/already-satisfied rules
- obvious graph logic regressions

## Testing expectations

When working outside the game:
- prefer validator or inspector output
- prefer deterministic console output
- prefer small repro inputs

When working toward runtime integration:
- assume toast notifications are the first UI surface for testing
- PDA persistence/logging is secondary
- avoid assuming undocumented in-game state is available until verified

## Nitrox constraints

This project is host-authoritative.
Other players may assist shared-world progression, but should not create conflicting personal progression truth.

Be conservative with assumptions about synchronization.
If something is likely unreliable in Nitrox, surface it as a risk instead of pretending it is solved.

## Code style

- Favor readable C#
- Avoid overengineering
- Avoid speculative abstractions
- Keep comments focused on intent and constraints
- Keep public-facing method and type names clear and literal

## When uncertain

If the repository does not already prove a pattern, do not invent a complicated framework.
Choose the simplest structure that preserves:
- graph correctness
- evaluator clarity
- debuggability
- future integration with BepInEx
