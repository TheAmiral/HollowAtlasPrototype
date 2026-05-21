# Hollow Atlas — Durable Decisions

This file stores project decisions that every AI tool must respect unless the user explicitly changes them.

## 2026-05-21 — Shared project memory is required

Decision:
Hollow Atlas will use a shared project memory inside the GitHub repository under `Docs/AI/`.

Reason:
The user does not want separate memories for ChatGPT, Claude, Codex, or other tools. Every tool should read the same project context and write back to the same record.

Applies to:
- Planning
- Prompting
- Code review
- Unity development
- Gameplay analysis
- Tool handoffs

## 2026-05-21 — GitHub is the source of truth

Decision:
The GitHub repository is the primary source of truth for project memory and technical state.

Reason:
GitHub gives file history, commits, branches, diffs, and reliable rollback.

Drive usage:
Google Drive can be used as a readable mirror, summary, or backup, but not as the primary technical source of truth.

## 2026-05-21 — KOSGEB removed from default Hollow Atlas planning

Decision:
KOSGEB-related planning is no longer part of the default Hollow Atlas workflow.

Reason:
The KOSGEB process is finished. The project direction is now focused on Steam, demo, trailer, devlog, player-facing quality, and production workflow.

Rule:
Do not include KOSGEB topics unless the user explicitly asks for them again.

## 2026-05-21 — Player-facing quality has priority

Decision:
Development priorities should be evaluated by how much they improve the playable demo and how good the game looks and feels in footage.

Priority examples:
- HUD readability
- Level-up card presentation
- Boss reward clarity
- Hit feedback
- SFX and visual sync
- Camera stability
- Boss fight presentation
- Trailer/devlog-ready moments
