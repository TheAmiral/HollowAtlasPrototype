# Hollow Atlas — AI Rules

These rules must be followed by every AI tool working on Hollow Atlas.

## Language and communication

- User-facing explanations must be in Turkish unless the user explicitly asks for another language.
- Keep code identifiers, file names, class names, Unity API names, and error messages in English.
- Do not ask unnecessary clarification questions. If the task is clear, inspect the repository, make the smallest safe plan, and proceed.
- When explaining changes, include what changed, why it changed, and how to test it in Unity.

## Source of truth

- GitHub repository `TheAmiral/HollowAtlasPrototype` is the primary technical source of truth.
- Start every meaningful task from `Docs/AI/00_READ_FIRST.md`.
- Do not rely on stale chat memory when repository files can be inspected.
- Google Drive may be used as a human-readable mirror or backup, but GitHub remains authoritative for technical state.

## Code change rules

- Prefer small targeted patches over broad rewrites.
- Do not rename serialized fields unless explicitly requested.
- Preserve prefab, scene, and Inspector references.
- Do not modify unrelated files.
- Do not remove a system because it looks unused without searching references first.
- Do not perform broad refactors unless explicitly requested.
- Do not run destructive git operations such as reset, clean, or force-push unless the user explicitly requests them.

## Unity architecture rules

- Unity target: Unity 6.
- Main gameplay scene: `Assets/Scenes/Prototype_01.unity`.
- Use the Unity Input System.
- Do not reintroduce `StandaloneInputModule`.
- Do not create duplicate `EventSystem` objects.
- Preserve existing Player, HUD, XP, Gold, LevelUp, Boss, GameManager, Build, and Relic systems unless the task specifically targets them.
- `Time.timeScale` changes must remain controlled by the relevant gameplay/UI systems and must not be scattered casually across unrelated scripts.

## UI and gameplay quality rules

- Prioritize changes that improve the playable demo, gameplay footage, screenshots, trailers, and devlogs.
- HUD readability, level-up card clarity, boss reward clarity, hit feedback, SFX sync, camera stability, and boss presentation are high-priority polish areas.
- Keep Turkish player-facing UI text consistent.
- If UI changes are made, include a Play Mode checklist for resolution, visibility, interaction, and overlap checks.

## Required task ending

After every meaningful task, update the relevant project memory files:

- `Docs/AI/02_CURRENT_STATUS.md` for current state.
- `Docs/AI/03_DECISIONS.md` for durable decisions, only if a real decision changed.
- `Docs/AI/08_CHANGELOG.md` for change summaries.
- One tool-specific log under `Docs/AI/Logs/`.
- `Docs/AI/Tasks/CURRENT_TASK.md` and backlog files when priorities change.
