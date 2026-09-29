# IndieMoba - Development Guide

Unity 6.3 (6000.3.25f1), URP 2D, new Input System only. This document covers the architecture, workflows and technical decisions of the project. Release notes live in `CHANGELOG.md`.

## Current scope (Phase 1)

- Forest test area built from the Mossbound demo layout.
- Controllable placeholder hero with responsive, collision-aware movement.
- Top-down follow camera with pixel-perfect rendering.
- Input abstraction ready for mobile; PC bindings are for prototyping only.

Not implemented yet: minions, combat, towers, abilities, jungle, UI, networking.

## Folder layout

```
Assets/_Game/
  Art/
    Characters/_Placeholder/Nilo/        placeholder hero sheet, animations, animator controller
    Environment/_Placeholder/...         placeholder terrain, props, debug collision tile
  Data/
    Characters/HeroMovementConfig.asset  tunable movement parameters
    Prototype/PrototypeLayout.json       exported demo layout (props, blocked grid, spawn)
  Input/MobaControls.inputactions
  Prefabs/
    Characters/Hero_Placeholder.prefab
    Characters/Visuals/Nilo_Visual_Placeholder.prefab
    Environment/_Placeholder/*.prefab
  Scenes/Prototype/GameplayPrototype.unity
  Scripts/
    Core/           IndieMoba.Core          intents and input interface (no Unity scene deps)
    Characters/     IndieMoba.Characters    movement config, motor state, motor, HeroActor
    Input/          IndieMoba.Input         KeyboardMouseInputSource
    CameraSystem/   IndieMoba.CameraSystem  MobaCameraRig
    Presentation/   IndieMoba.Presentation  CharacterPresenter, CharacterAnimatorBridge
    Editor/         IndieMoba.Editor        PrototypeSceneBuilder (editor only)
```

Every script folder has its own assembly definition. Dependencies flow one way: `Core` <- `Characters` <- `Presentation`; `Input` and `CameraSystem` depend on `Core` only.

`Tools/export/` (repo root, outside Unity) holds the one-off exporter that extracts placeholder art and layout from the Phaser demo.

## Gameplay vs presentation

The hero is split into two layers:

- **Gameplay (`HeroActor`, `CharacterMotor`)** owns the authoritative `CharacterMotorState` (position, velocity, facing, destination). It runs on a fixed tick (default 60 Hz, `tickRate`) using an accumulator in `Update`, capped by `maxTicksPerFrame` to avoid spirals. It exposes `PreviousState`, `State`, `InterpolationAlpha` and a `Ticked` event.
- **Presentation (`CharacterPresenter`, `CharacterAnimatorBridge`)** lives under the `VisualRoot` child. It interpolates between `PreviousState` and `State` every frame, flips the sprite by facing, and drives Animator parameters `IsMoving` (bool) and `Speed` (float). The bridge only sets parameters that exist in the controller, so any replacement controller works.

Script execution order: `HeroActor` (default) -> `CharacterPresenter` (100) -> `MobaCameraRig` (200).

Gameplay never reads sprites, animators or renderers, so art can be replaced freely.

## Movement motor

`CharacterMotor.Simulate(ref state, in intent, dt)` is a plain C# function of state + intent + delta time. It does not use Rigidbody2D simulation.

1. Resolve the intent: `Direction` (clamped analog vector), `Destination` (world point), `Stop`, or `None`.
2. Compute target velocity = direction * `maxSpeed`; approach it with `acceleration` or `deceleration` (`Vector2.MoveTowards`).
3. Clamp displacement so destination moves never overshoot.
4. Depenetrate against overlapping obstacles (`Physics2D.OverlapCircle` + `ClosestPoint`).
5. Collide-and-slide: `Physics2D.CircleCast` along the displacement, stop at the nearest hit minus `skinWidth`, project the remainder onto the contact plane, repeat up to `maxSlideIterations`.
6. Update facing when speed exceeds `facingThreshold`; clear the destination within `stopDistance`.

Obstacles are static colliders on the `Obstacle` layer (`obstacleMask` in `HeroMovementConfig`). The hero itself has no collider that participates in the query.

All parameters live in `Assets/_Game/Data/Characters/HeroMovementConfig.asset`.

## Input

- `IMovementInputSource.ReadIntent()` returns a `MovementIntent`. `HeroActor` reads it once per tick through a serialized `MonoBehaviour` reference (`inputSourceBehaviour`).
- `KeyboardMouseInputSource` (prototype only) uses `MobaControls.inputactions`, map `Gameplay`:
  - `Move`: WASD, arrow keys, gamepad left stick -> `Direction` intent.
  - `MoveToPoint` (hold right mouse button) + `PointerPosition` -> `Destination` intent, updated continuously while held.
  - Keyboard/stick input overrides a pending destination.
- Control schemes: `KeyboardMouse`, `Gamepad`.

**Mobile:** add a new component implementing `IMovementInputSource` (e.g. a virtual joystick producing `Direction`, or tap-to-move producing `Destination`) and assign it to the hero's `inputSourceBehaviour`. No gameplay code changes are needed. The same intents are what a client would send to a server later.

## Camera and pixel-perfect rendering

- `Main Camera` has a URP `PixelPerfectCamera`: 32 PPU, reference resolution 960x540, grid snapping enabled, crop frame None. Change these on the component if the art scale changes; the builder defaults are `ReferenceResolutionX/Y` in `PrototypeSceneBuilder.cs`.
- The reference height defines vertical world coverage (540 px / 32 PPU = 16.875 units). With crop frame None, wider screens show more of the world horizontally instead of adding black bars: 16:9 shows 30 units, 19.5:9 about 36.6 units (map is 40 wide, so bounds clamping still applies).
- The PixelPerfectCamera draws a red on-screen warning in the Editor when the Game View is odd-sized or smaller than the reference resolution (typical with a docked "Free Aspect" Game View). Test with a fixed, even resolution at an integer multiple of the reference height:
  - `IndieMoba > Game View > Desktop 16:9` (1920x1080 at the default reference)
  - `IndieMoba > Game View > Mobile 19.5:9` (2340x1080 at the default reference)
  These add the size to the Game View dropdown if missing and select it. Sizes are computed as 2x the scene's reference height. If the menu fails (it uses internal Editor APIs), add the size manually as a Fixed Resolution in the Game View dropdown.
- All placeholder textures: Point filtering, 32 PPU, no compression, no mipmaps.
- `MobaCameraRig` follows the target with smoothing and clamps to map bounds (40 x 27 world units for the prototype map).

## Sorting and physics layers

- Sorting layers: `Default`, `Ground`, `GroundDecor`, `Actors`, `Overhead`, `VFX`, `UI`.
- `Renderer2D.asset` uses custom-axis transparency sorting (0, 1, 0): lower Y renders in front. Props and the hero use `Actors`; the hero's `VisualRoot` has a `SortingGroup`.
- Physics layers: 6 = `Obstacle`, 7 = `Hero`. 2D gravity is (0, 0).
- The map border/blocked cells come from the layout grid (16 px cells) and are painted into `Environment/CollisionGrid/Collision` (Tilemap + TilemapCollider2D + CompositeCollider2D, layer `Obstacle`). Its `TilemapRenderer` is disabled; enable it to see the collision debug overlay.

## Swapping art (designer workflow)

All art under `_Placeholder` folders is temporary and is not referenced by gameplay code.

- **Hero:** create a new visual prefab (sprite + Animator with `IsMoving`/`Speed` parameters) and replace the nested `Nilo_Visual_Placeholder` under `Hero_Placeholder/VisualRoot`. Collision radius stays in `HeroMovementConfig`.
- **Props:** each environment prefab has a `Visual` child (sprite) and, for blocking props, a `Collision` child on layer `Obstacle`. Replace the sprite on `Visual`; adjust the `Collision` shape if the footprint changes. Scene instances update automatically.
- **Terrain:** `Environment/Terrain` is currently one large sprite. Replace it with a Tilemap or new sprite; it has no gameplay role.

## Placeholder export tool

Re-extracts placeholder art and `PrototypeLayout.json` from the Phaser demo in `Reference/mossbound/` (the demo is read-only):

```bash
sh Tools/export/run-export.sh
```

Outputs 22 PNGs under `Assets/_Game/Art/{Characters,Environment}/_Placeholder/` and the layout JSON (344 props, 80x54 blocked grid, hero spawn). `Tools/export/dist/` is gitignored.

## Prototype scene builder

`GameplayPrototype.unity`, its prefabs and settings are generated by `PrototypeSceneBuilder` (idempotent; safe to re-run):

- Menu: `IndieMoba > Prototype > Rebuild Gameplay Prototype`.
- Batch mode:

```bash
Unity.exe -batchmode -nographics -quit -projectPath <path>/IndieMoba -executeMethod IndieMoba.EditorTools.PrototypeSceneBuilder.BuildFromCommandLine -logFile <log>
```

Steps: project settings (layers, sorting layers, 2D gravity, sort axis, build settings) -> texture import settings -> assets (sprite slicing, tile, config, animations, controller) -> prefabs -> scene. Manual edits to generated assets are overwritten on rebuild; once the prototype stabilises, the builder can be retired and assets edited by hand.

## Networking readiness

- Movement is deterministic-by-design per tick: `Simulate(state, intent, dt)` with a fixed dt and no Rigidbody integration.
- Input is already reduced to small intent messages.
- State and presentation are separated, and presentation already interpolates between ticks.

A future server-authoritative model can run the same motor on the server, send intents from clients, and add client prediction/reconciliation. No networking exists yet.

## Known limitations

- Play mode and visual output have not been verified by the automated build; only compilation and asset generation were checked in batch mode.
- 27 props lie slightly outside the 0..40 x 0..27 camera bounds; they sit inside the blocked border and are unreachable.
- The terrain is a single large sprite.
- Physics queries use `Physics2D`, so exact float determinism across platforms is not guaranteed.

## Next steps

Phase 2 (not started): to be scoped separately (minions, combat, towers, abilities, jungle, UI, networking).
