# ADR-0003 — Supported platforms, input and distribution constraints

**Status:** Accepted on merge of its introducing PR (SOV-P00-T07).  
**Decision date:** 2026-10-09  
**Scope:** Initial desktop release targets, input model and distribution constraints. Store choice is deferred.

## Context

The spec commits to desktop PC, mouse/keyboard-first play, customizable keybindings and accessibility features (§26, §34 "Player platform?"). Multiplayer is excluded (§1). ADR-0001 chose Godot 4 .NET for the client and independent C#/.NET for the simulation; both export to the three mainstream desktop operating systems.

## Decision

**Operating systems.** The initial release supports **Windows, macOS and Linux** desktop builds:

| OS | Architectures | Notes |
|---|---|---|
| Windows | x64 | Primary audience for the genre. |
| macOS | arm64 (Apple Silicon); x64 only if a measured need appears | Distribution requires code signing and notarization. |
| Linux | x64 | Desktop Linux only; handheld/gamepad-only devices are not a target. |

Minimum OS versions follow the pinned Godot/.NET releases chosen in Phase 01 and are recorded there, not guessed here. Mobile, web and console targets are out of scope.

**Input.** Mouse and keyboard are the only required input devices. All player actions go through an input-action layer with rebindable bindings (SOV-P01-T12). No feature may require a gamepad, touch or a specific keyboard layout. Gamepad support is not planned; the action layer must not prevent adding it later.

**Distribution constraints (store undecided).**

- Single-player and fully playable offline. No account, server or network connection is required to play, save or load.
- Simulation and client core contain no store SDK, DRM or platform-account dependency. Any later store integration (achievements, cloud saves, workshop) sits behind an optional client-side adapter and is never required for gameplay or saves.
- Saves, settings and mods live in OS-conventional per-user locations; paths come from one client helper, not hard-coded per feature.
- Third-party dependencies and assets must allow redistribution on all three OSes (license tracking: SOV-P01-T14).
- Store selection, signing identities and packaging formats are decided in Phase 53.

## Alternatives considered

- **Windows only first:** smaller test matrix, but the developer works on macOS and Godot/.NET make portability cheap if kept from the start; retrofitting path, case-sensitivity and line-ending issues later costs more.
- **Steam-first with Steamworks integration now:** premature lock-in; nothing in Phases 01–52 needs store features.

## Consequences

- Phase 01 CI should build and run headless simulation tests on at least one non-macOS OS early (Linux runners are cheapest) to catch case-sensitive path and filesystem differences; full three-OS packaging smoke checks arrive with packaging (SOV-P01-T07, Phase 53).
- Determinism evidence must state whether checksums match across OS/CPU architectures once CI covers several OSes; see ADR-0004 §3.
- macOS release builds incur signing/notarization work in Phase 53.
