# Hollow Atlas — Unity Test Checklist

Use this checklist after gameplay, UI, camera, input, enemy, reward, or progression changes.

## Baseline setup

1. Open the project with Unity 6.
2. Load `Assets/Scenes/Prototype_01.unity`.
3. Wait for scripts to compile without console errors.
4. Press Play.

## Core smoke test

- Game starts without compiler errors or runtime exception spam.
- Player spawns correctly on the ground without visible drop, jitter, or foot clipping.
- WASD / arrow movement works.
- Dash works and does not break movement.
- Camera follows smoothly without obvious jitter.
- Enemies spawn and chase/shoot as expected.
- Player can damage and kill enemies.
- Player can take damage and die.
- Restart flow works if the current build supports it.

## HUD and UI test

- Main HUD is visible at runtime.
- HP display updates when the player takes damage or heals.
- XP bar starts empty for a new run and fills as XP is collected.
- Level display starts at 1.
- Gold display updates when gold is collected.
- Timer updates during active gameplay.
- Minimap placeholder or minimap area does not overlap critical UI.
- Pause, game-over, level-up, boss reward, and relic reward UI do not overlap in a broken way.
- Mouse selection and keyboard shortcuts work for card/reward screens when applicable.

## Progression test

- Basic enemies drop expected XP/gold.
- Tank, fast, shooter, elite, or boss enemies use the intended reward values when present.
- Level-up triggers at the intended XP threshold.
- Level-up card/reward selection applies the selected upgrade once.
- Boss reward/relic reward flow resumes gameplay correctly after selection.
- `Time.timeScale` returns to normal after paused selection screens.

## Boss test

- Boss spawns at the intended timing or trigger.
- Boss health UI appears and updates.
- Boss attacks work without null reference errors.
- Boss phase changes, if present, trigger at the intended health threshold.
- Boss death grants the intended XP/reward/relic flow.

## Visual and trailer-readiness test

- HUD is readable at 16:9.
- Important UI is not cut off or hidden behind screen edges.
- Hit feedback is visible enough to understand damage.
- SFX, visual feedback, and gameplay actions feel synchronized.
- No debug-only UI appears in a normal player-facing run unless intentionally enabled.

## Build sanity test

For demo-facing changes, also test a Windows standalone build:

- Correct first scene loads.
- Display target is correct.
- Controls work in build.
- HUD and UI scale correctly in build.
- No editor-only debug controls are available in build.
