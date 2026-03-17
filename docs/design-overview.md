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

## Development Priority

1. Author and validate the graph
2. Build a standalone evaluator
3. Add a tiny in-game test surface
4. Add fact detection gradually