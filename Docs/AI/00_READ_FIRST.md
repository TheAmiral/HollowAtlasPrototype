# Hollow Atlas — Read First

This folder is the shared AI-readable project memory for Hollow Atlas.

## Source of truth

- Primary source of truth: GitHub repository `TheAmiral/HollowAtlasPrototype`, folder `Docs/AI/`.
- Human-readable mirror/backup: Google Drive document `Hollow Atlas - AI Project Memory`.
- ChatGPT memory may remember broad direction and preferences, but technical truth must be read from this folder.

## Required reading order for every AI tool

Before planning, coding, reviewing, or changing anything, read these files in order:

1. `Docs/AI/01_PROJECT_CONTEXT.md`
2. `Docs/AI/02_CURRENT_STATUS.md`
3. `Docs/AI/03_DECISIONS.md`
4. `Docs/AI/04_AI_RULES.md`
5. `Docs/AI/Tasks/CURRENT_TASK.md`
6. `Docs/AI/06_TEST_CHECKLIST.md`

## Required write-back rule

After every meaningful task, update the relevant files:

- `Docs/AI/02_CURRENT_STATUS.md` for current project state.
- `Docs/AI/03_DECISIONS.md` for durable decisions.
- `Docs/AI/08_CHANGELOG.md` for change summaries.
- One tool-specific log under `Docs/AI/Logs/`.
- `Docs/AI/Tasks/CURRENT_TASK.md` and backlog files when priorities change.

## Do not guess

If project state is missing or unclear, inspect the repository first. Do not rely on stale chat memory, assumptions, or undocumented decisions.

## User workflow shortcut

When starting a new AI session, the user can say:

> Read `Docs/AI/00_READ_FIRST.md` in `TheAmiral/HollowAtlasPrototype` and continue from the shared project memory.
