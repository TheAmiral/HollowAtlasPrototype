# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Hollow Atlas** is a roguelite auto-attack survivor (think Vampire Survivors + Hades 2 aesthetic) built with Unity 6 (6000.3.10f1) + URP 17. The player auto-attacks enemies, collects XP/gold, selects upgrade cards on level-up, and fights wave-based bosses.

## Development Workflow

This is a Unity project — there are no CLI build/test commands. Development is done entirely in the Unity Editor:

1. Open with Unity **6000.3.10f1**
2. Load `Assets/Scenes/Prototype_01.unity`
3. Press Play to run

For Android/iOS builds, use **File → Build Settings** in the Editor. Windows standalone is the primary demo target.

## Architecture

### Script Organization

```
Assets/Scripts/
├── Player/    PlayerMovement, PlayerHealth, PlayerLevelSystem, PlayerDamageFlash,
│              CharacterAnimatorBridge, CameraFollow
├── Enemy/     EnemyChaser, EnemyHealth, EnemyProjectile, EnemyShooter, EnemySpawner, EnemyVisuals
├── Boss/      BossSpawnSystem, BossSpecialAttack, BossRewardSystem, BossHealthUI
├── Combat/    AutoAttackAura
├── Pickup/    ExperiencePickup, GoldPickup, HealthPickup, PickupMagnet
├── Systems/   GameManager, GoldWallet, LevelUpCardSystem, LevelUpCardDefinition,
│              RunContractSystem, AudioManager
├── UI/        MainHudCanvasUI, LevelPulseRing, OrbBreathing, OrbRingRotator, XpBarFlowEffect
└── Scripting/ StudioIntroController
```

### Key Systems & How They Connect

**GameManager** (Singleton) is the central controller. It tracks `ElapsedTime`, `IsGameOver`, and `IsPaused`. It owns the pause/game-over overlays via `OnGUI()`. It blocks ESC from pausing if `LevelUpCardSystem.SelectionPending` or `BossRewardSystem.RewardPending` is true. All `Time.timeScale` changes flow through GameManager, LevelUpCardSystem, and BossRewardSystem — never set `timeScale` elsewhere.

**LevelUpCardSystem** (Singleton) builds its entire UI programmatically at runtime (no prefabs). It creates a `DontDestroyOnLoad` Canvas with `sortingOrder=200`. Cards are defined in `LevelUpCardDefinition.cs` via `LevelUpCard` structs with an `Action<GameObject> Apply` lambda. `CardPool.PickRandom()` handles rarity-weighted selection. The system is triggered via `TriggerSelection(playerLevel)` from `PlayerLevelSystem`, or `ShowCustomCards(...)` from `BossRewardSystem`.

**EnemyHealth** uses a static `AliveCount` counter — never call `FindObjectsByType<EnemyHealth>()` to count enemies; `EnemySpawner` reads `EnemyHealth.AliveCount` directly.

**GoldWallet** tracks two balances: `runGold` (current run, lost on death) and `bankGold` (persistent meta-currency). Debug keys (B/R) are `#if UNITY_EDITOR` only.

### UI Conventions

- All UI is **legacy `UnityEngine.UI.Text`**, not TextMeshPro (migration is on the roadmap).
- Game-over and pause overlays use `OnGUI()` with `Texture2D`-drawn panels.
- The card selection UI (`LevelUpCardSystem`) builds its entire hierarchy in C# — there are no UI prefabs for it.
- UI text is in **Turkish** (`DURAKLATILDI`, `SEVİYE ATLA`, `Bir lütuf seç`, etc.).

### Input

Uses the **new Unity Input System** (`UnityEngine.InputSystem`). Direct polling (`Keyboard.current`, `Mouse.current`) is used throughout — there are no Input Action asset bindings for in-game controls. `EventSystem` is configured with `InputSystemUIInputModule` (not the legacy `StandaloneInputModule`).

### Singletons

`GameManager`, `LevelUpCardSystem`, `BossSpawnSystem`, `RunContractSystem`, `AudioManager` all use the `static Instance` pattern with `Destroy(gameObject)` on duplicates in `Awake()`. Do not call `FindObjectOfType` for any of these.

### Time Scale

`Time.timeScale = 0f` is used for: card selection, boss reward selection, pause, game-over. Coroutines that must run during paused state must use `WaitForSecondsRealtime` and `Time.unscaledDeltaTime`.

### Cards & Progression

`LevelUpCardDefinition.cs` contains all 24 cards across 5 gods (Nyx, Thanatos, Atlas, Hermes, Khaos) with rarity tiers Common → Legendary → Cursed. Rarity odds are level-gated (defined in `CardPool.PickRandom`). Each card's `Apply` lambda receives the player `GameObject` and modifies stats directly on its components.

### Roadmap Items (Known Gaps)

- TextMeshPro migration (all UI uses legacy Text)
- WeaponSystem (currently only `AutoAttackAura`)
- MetaShop for `bankGold`
- SaveSystem via PlayerPrefs
- Minimap

## User Communication & Workflow Rules

- Technical reasoning, code comments when necessary, Unity/C# architecture notes, file names, class names, method names, and implementation plans may be written in English for accuracy.
- User-facing explanations must be in Turkish unless the user explicitly asks for another language.
- When explaining what changed, why it changed, how to test it, or what to do in Unity Inspector, explain in Turkish.
- Keep code, method names, variable names, Unity API names, class names, and error messages in their original English form.
- Do not translate code identifiers.
- Do not ask unnecessary clarification questions.
- If the task is clear, analyze the current code, apply the fix, and summarize the result.
- Before editing, state which files will be changed and why.
- After editing, summarize the changes in Turkish.
- Always mention required Unity Inspector setup steps in Turkish.
- Prefer small targeted patches over broad rewrites.
- Do not perform broad refactors unless explicitly requested.
- Do not rename serialized fields unless explicitly requested.
- Do not break prefab, scene, or Inspector references.
- Do not modify unrelated files.
- Do not assume a system is unused without searching references first.
- If compile errors exist, do not claim the task is complete until they are fixed.
- Do not run git commit, git push, git reset, git clean, or destructive shell commands unless the user explicitly requests them.
- Preserve the existing Player, HUD, XP, Gold, LevelUp, Boss, GameManager, and Build systems.