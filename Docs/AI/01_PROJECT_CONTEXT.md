# Hollow Atlas — Project Context

## Core identity

- Project: Hollow Atlas
- Engine: Unity 6
- Main active gameplay scene: `Prototype_01`
- Genre: 2.5D isometric dark mythological roguelite action game
- Gameplay direction: horde combat, XP pickup, level-up card choices, boss encounters, relic/reward systems
- Visual target: premium dark mythological fantasy with high contrast, inspired by Hades-like readability and presentation
- Core palette: dark purple, red, gold, black-blue, with selective cyan/magic highlights

## Current product goal

Build a Steam-demo-ready vertical slice that looks strong in gameplay footage, screenshots, trailers, and devlogs.

The project direction is no longer grant/application-document focused. The main goal is a playable, readable, polished demo that feels attractive to players and useful for Steam visibility.

## Known important systems

- `GameManager`
- `MainHudCanvasUI`
- `PlayerMovement`
- `PlayerHealth`
- `PlayerLevelSystem`
- `LevelUpCardSystem`
- `BossRewardSystem`
- `BossSpawnSystem`
- `EnemySpawner`
- `EnemyHealth`
- `CameraFollow`
- `AutoAttackAura`
- `RelicSelectionSystem`
- `RelicChest`
- `RelicDatabase`
- `RelicInventory`
- `RelicRewardApplier`

## Technical preferences

- Use Unity Input System.
- Do not reintroduce `StandaloneInputModule`.
- Do not create duplicate `EventSystem` objects.
- Preserve serialized scene and prefab references.
- Prefer minimal patches and exact placement instructions.
- Do not rewrite full files unless required or explicitly requested.
- Always include Unity Play Mode test steps for gameplay/UI changes.

## Player-facing quality focus

Prioritize systems that improve what players immediately feel and see:

- HUD readability and premium styling
- Level-up card clarity and click/keyboard selection reliability
- Boss reward flow polish
- Hit feedback and SFX sync
- Camera stability and movement feel
- Boss fight presentation
- Trailer/devlog-friendly gameplay moments
