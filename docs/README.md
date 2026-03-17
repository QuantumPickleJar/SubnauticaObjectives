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
   - Authored as JSON
   - Defines milestones, bubbles, objectives, safety barriers, facility interactions
   - Defines activation, completion, and already-satisfied rules
   - Defines hint text at multiple handholding levels

2. **Runtime Integration**
   - Will eventually be implemented with BepInEx + Harmony
   - Detects runtime facts from the game
   - Feeds those facts into the graph evaluator
   - Displays the selected objective in-game

## Current Status

This repository currently focuses on:
- authoring the progression graph
- validating graph structure
- preparing for a standalone evaluator

Game integration is intentionally deferred until the graph structure is stable.