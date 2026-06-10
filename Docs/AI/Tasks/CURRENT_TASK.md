# Current Task

**Task:** Player ground alignment bug fix  
**Status:** Done - Play Mode verified  
**Branch:** feature/relic-chest-spawn-system

## What was done
- Added startup visual ground alignment to `PlayerMovement.cs`.
- Alignment runs from `LateUpdate()` during the first 1.25 seconds, so Animator pose changes settle while logging only once.
- `AlignVisualToGround()` uses `CharacterAnimatorBridge` + enabled skinned bounds to locate the correct visual child, not `Body_010`.
- Disabled `SkinnedMeshRenderer` components are filtered explicitly with `smr.enabled`.
- Ground raycasts skip Player-owned colliders and pick the closest non-player hit below the CharacterController.

## How to verify
1. Open `Assets/Scenes/Prototype_01.unity`
2. Press Play
3. Check Console for `[GroundAlign] Player feet delta:X groundY:Y feetY:Z`
4. If delta > 0: feet were below ground, now corrected.
5. If delta ~= 0: already aligned, or the scene was saved after a previous fix run.
6. Visual check: feet should sit on ground, not float or clip.

## Verification Result
- 2026-05-31: Unity 6000.3.10f1 Play Mode batch verification passed in a temporary project copy.
- Console showed `[GroundAlign] Player feet delta:0.278 groundY:0.000 feetY:-0.278`.
- Automated verifier measured final feet delta `0.001`.
- No `NullReferenceException`, C# compiler error, or console spam from ground alignment.

## Definition of Done
- Feet visually on ground in Play Mode. Done.
- No NullReferenceException. Done.
- No console spam; `[GroundAlign]` fires once at startup only. Done.
- Movement, dash, camera, and animation still run in Play Mode. Done.
