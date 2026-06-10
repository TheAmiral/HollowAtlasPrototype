# Current Status

**Last updated:** 2026-05-31

## Active Branch
`feature/relic-chest-spawn-system`

## Recently Fixed
- Animation bug (Idle/Walk/Dash): `Player_FemaleSamurai_Animator.controller` GUID fix, `CharacterAnimatorBridge` deadzone + state logging.
- Player ground alignment (Play Mode): `PlayerMovement.cs` now aligns the correct Samurai visual root through `CharacterAnimatorBridge`, ignores disabled old `Body_010` renderers, and keeps the visual grounded during the startup animation settle window. Verified in Play Mode batch with final feet delta `0.001`.
- Enemy spawn Y: `EnemySpawner.SpawnEnemy()` now raycasts to find ground Y then aligns bounds.min.y to ground.

## Known State
- `GroundBoundsAligner.cs` (utility) was created then deleted; not used.
- `Docs/AI/` directory exists; `00_READ_FIRST.md`, `01_PROJECT_CONTEXT.md`, `03_DECISIONS.md` do not exist yet.
- `Prototype_01.unity` scene is still modified in the working tree from prior work.
- The local Unity Editor is currently open on this project, so source-project batchmode cannot open the same project simultaneously.

## Pending
- No pending work remains for the player ground alignment task.
- Before merge, review the broader dirty working tree and decide which unrelated changes belong in the branch.
