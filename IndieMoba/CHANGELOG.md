# Changelog

## [0.1.0] - 2026-09-29 - Phase 1: Movement prototype

### Added
- `GameplayPrototype.unity` forest test area built from the Mossbound demo layout (344 props, blocked-cell collision grid).
- Placeholder hero (`Hero_Placeholder` + `Nilo_Visual_Placeholder`) with idle/walk animations.
- Fixed-tick (60 Hz) movement: `CharacterMotor` collide-and-slide against static obstacles, `HeroActor` tick loop, `CharacterMovementConfig`.
- Presentation layer: `CharacterPresenter` (interpolation, flip) and `CharacterAnimatorBridge`.
- Input abstraction (`IMovementInputSource`, `MovementIntent`) and prototype `KeyboardMouseInputSource`.
- `MobaControls.inputactions` (WASD/arrows, gamepad stick, hold right mouse to move to point).
- `MobaCameraRig` follow camera with bounds; URP `PixelPerfectCamera` at 32 PPU, 960x540.
- Sorting layers (Ground, GroundDecor, Actors, Overhead, VFX, UI) and physics layers (Obstacle, Hero).
- `PrototypeSceneBuilder` editor tool (menu and batch mode).
- `Tools/export` placeholder art/layout exporter.
- `DEVELOPMENT.md`.

### Changed
- 2D gravity set to zero; Renderer2D uses custom-axis (Y) transparency sorting.
- Build settings contain only `GameplayPrototype`.

### Removed
- `SampleScene.unity` and the template `InputSystem_Actions.inputactions`.
