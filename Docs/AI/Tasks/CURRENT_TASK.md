# Current Task

HUD and UX polish for Hollow Atlas vertical slice.

## Status

Ready to start.

## Context

The shared AI project memory system is now active under `Docs/AI/`. Future AI-assisted work should begin from `Docs/AI/00_READ_FIRST.md` and follow `Docs/AI/04_AI_RULES.md` plus `Docs/AI/06_TEST_CHECKLIST.md`.

## Recommended first implementation focus

1. Inspect `MainHudCanvasUI`, `PlayerHealth`, `PlayerLevelSystem`, `GoldWallet`, and `Prototype_01` HUD references.
2. Improve player-facing HUD readability without breaking existing runtime fallback behavior.
3. Keep changes minimal and preserve serialized scene/prefab references.
4. Test in Unity Play Mode using `Docs/AI/06_TEST_CHECKLIST.md`.
