# Changelog

## [0.1.1] - 2026-09-29 - Phase 1 polish: resolution testing

### Added
- `GameViewResolutionPresets` editor menu (`IndieMoba > Game View`): Desktop 16:9 and Mobile 19.5:9 fixed Game View sizes derived from the Pixel Perfect Camera reference height, avoiding the odd/too-small resolution warning.

### Changed
- Pixel Perfect reference resolution in `PrototypeSceneBuilder` is now a named setting (`ReferenceResolutionX/Y`, still 960x540). Scene output unchanged.
- `DEVELOPMENT.md` documents aspect-ratio behavior and resolution testing.

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
