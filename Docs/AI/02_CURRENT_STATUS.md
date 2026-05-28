# Hollow Atlas — Current Status

Last updated: 2026-05-28

## Current direction

Hollow Atlas is moving toward a Steam-demo-ready vertical slice.

The development process now uses a shared project memory so every tool reads the same context, decisions, active task, and test expectations.

## Current top priority

HUD and UX polish for a stronger player-facing demo presentation.

## Current product focus

1. HUD and UX polish
2. Level-up card system polish
3. Boss reward and relic reward flow polish
4. Hit feedback and SFX sync
5. Camera and movement feel
6. Boss fight presentation
7. Gameplay footage suitable for Steam, trailer, and devlog use
8. WeaponSystem, MetaShop, SaveSystem, TextMeshPro migration, and real minimap as roadmap items

## Current workflow rule

For every meaningful task:

1. Read `Docs/AI/00_READ_FIRST.md`.
2. Update `Docs/AI/Tasks/CURRENT_TASK.md`.
3. Make the smallest safe change.
4. Run or request the Unity test checklist in `Docs/AI/06_TEST_CHECKLIST.md`.
5. Update status, log, and changelog files.
6. Commit changes to GitHub.
7. Mirror major summaries to Google Drive when useful.

## Known stable facts

- Repository: `TheAmiral/HollowAtlasPrototype`
- Default branch: `main`
- Main gameplay scene: `Prototype_01`
- Unity version target: Unity 6
- Input system preference: Unity Input System
- Default planning no longer includes KOSGEB work.
- Shared AI project memory is active under `Docs/AI/`.
- GitHub is the primary source of truth for technical project memory.

## Next recommended step

Start HUD and UX polish from the current demo scene. Focus first on readable HP/XP/gold/time/minimap presentation, then level-up card clarity and reward-flow polish.
