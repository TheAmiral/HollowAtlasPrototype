# Hollow Atlas — Current Status

Last updated: 2026-05-21

## Current direction

Hollow Atlas is moving toward a Steam-demo-ready vertical slice.

The development process should use a shared project memory so every tool reads the same context, decisions, active task, and test expectations.

## Current top priority

Create and adopt the shared AI project memory system.

## Current product focus

1. AI workflow foundation
2. HUD and UX polish
3. Level-up card system polish
4. Boss reward and relic reward flow polish
5. Hit feedback and SFX sync
6. Camera and movement feel
7. Boss fight presentation
8. Gameplay footage suitable for Steam, trailer, and devlog use

## Current workflow rule

For every meaningful task:

1. Read `Docs/AI/00_READ_FIRST.md`.
2. Update `Docs/AI/Tasks/CURRENT_TASK.md`.
3. Make the smallest safe change.
4. Run or request the Unity test checklist.
5. Update status, log, and changelog files.
6. Commit changes to GitHub.
7. Mirror major summaries to Google Drive when useful.

## Known stable facts

- Repository: `TheAmiral/HollowAtlasPrototype`
- Default branch: `main`
- AI memory setup branch: `ai/project-memory-system`
- Main gameplay scene: `Prototype_01`
- Unity version target: Unity 6
- Input system preference: Unity Input System
- Default planning no longer includes KOSGEB work.

## Next recommended step

Review and approve the `ai/project-memory-system` branch. If approved, merge it into `main` and start using `Docs/AI/00_READ_FIRST.md` as the entry point for every AI-assisted session.
