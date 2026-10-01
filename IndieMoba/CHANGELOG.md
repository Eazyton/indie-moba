# Changelog

## [0.4.0] - 2026-10-01 - Phase 4B: 5v5 graybox map

### Added
- `IndieMoba.Map` assembly: `MapLayoutData`, map markers, collision shapes, camera settings, debug gizmos.
- `IndieMoba/Map 5v5/` menus: non-destructive layout asset creation, guarded geometry generation, graybox visuals, non-destructive scene wiring, collision mode switch, guarded layout reset.
- `Map5v5_Gameplay` and `Map5v5_Visual` prefabs and the `Scenes/Match/Match5v5.unity` scene (generated in the Editor).
- `Structure.RequirementMode` (`AllDestroyed` default, `AnyDestroyed` for the 5v5 nexus).
- `WaveSpawner.waveSpawnOffset` (applies to every wave), `MatchController.waveSpawners`, per-lane wave rows and a scrollable structure list in `LaneDebugOverlay`.

### Changed
- `MinionController` lateral offset and separation use the current lane segment direction.

## [0.3.1] - 2026-09-30

### Added
- Nexus defense: the nexus attacks enemy minions, then the enemy hero, using `TowerTargeting`/`TowerCombat`, a separate `NexusConfig`, a placeholder `NexusProjectile` and the same range ring rules as towers. It attacks even while invulnerable.

### Changed
- `TowerConfig` fields moved to the shared `StructureDefenseConfig` base (no value or behaviour change).

## [0.3.0] - 2026-09-29 - Phase 3: Functional lane

### Added
- Assemblies `IndieMoba.Minions`, `IndieMoba.Structures`, `IndieMoba.Match`.
- Minion waves (melee/ranged) with lane waypoints, configurable target priority, soft separation, corpse delay.
- Towers (sticky target, minions before hero, homing projectiles) and nexus (invulnerable while its tower stands).
- Structure damage rules: BasicAttack only, minion multiplier 0.6, configurable anti-backdoor damage reduction when no attacking minions are near.
- `MatchController` victory/defeat, simulation pause, `MatchResultView` with Restart.
- `CombatTargetKind`, `IDamageFilter`, `DamageBlockReason`; dummies are `Other` and ignored by lane AI.
- Temporary health bars, tower range warning, structure views, minion presenter, `LaneDebugOverlay`, `HeroReviveTool` (F2).
- Exported placeholder art: minion sheets, towers, nexus, crystals, rubble, arrow and tower projectiles; full-lane terrain crop (2240x864 px).

### Changed
- Prototype area widened to 70x27 world units; hero spawn (12.5, 11.25); dummies moved to the north clearing.
- Damage numbers only for interactions involving the player hero.

## [0.2.2]

### Added
- `AbilityConfig.canBasicAttackWithoutTarget` (per basic attack, default off). When on and no valid target exists, the basic attack fires a linear projectile toward the cursor up to its range, damages the first hostile it crosses, and consumes the cooldown even on a miss. Enabled for Nilo (`Hero_BasicAttack`). Targeted behaviour is unchanged.
- `HeroCombat` events `BasicAttackRequested`, `BasicAttackPerformed` (`BasicAttackPerformedInfo`) and `BasicAttackHit`.

## [0.2.1]

### Fixed
- `CombatFeedbackSpawner` added a second `MeshRenderer` after `TextMesh` (which already requires one), producing a null renderer and a NullReferenceException per damage event.
- Damage feedback failures are caught and logged; `CombatWorld` isolates `DamageApplied` subscribers and always clears pending damage.
- `ConfigureTagsAndSortingLayers` keeps existing valid sorting layer IDs and only writes the TagManager when something actually changed.

### Added
- `BasicAttackTargeting` (AutoTarget, ExplicitTarget, AttackMoveTarget) carried on `AbilityCommand`/`AbilityContext`. Current input still uses AutoTarget (cone); AttackMoveTarget currently falls back to AutoTarget.

## [0.2.0] - 2026-09-29 - Phase 2: Hero combat foundation

### Added
- `SimulationTickRunner` shared 60 Hz gameplay clock with ordered `ISimulationSystem` stages (input, actors, abilities, projectiles, damage, state).
- `IndieMoba.Combat` assembly: `Team`, `Cooldown`, `DamageInfo`/`DamageResult`, `IDamageable`, `Health` (HP, shield, death/revive events), `ICombatTarget`/`CombatTarget`, `CombatWorld` (target registry, cone target selection, homing/linear projectiles, delayed areas, damage queue), `AbilityConfig`, ability behaviours, `HeroCombat`, `CombatDummy`.
- Targeted basic attack (cursor-assisted cone selection, homing projectile), Q linear projectile, W shield, E collision-aware dash, R delayed targeted area.
- `AbilityCommand` / `IAbilityInputSource`; `KeyboardMouseInputSource` queues LMB (hold) and Q/W/E/R commands at the cursor.
- `CharacterMotor.SimulateForced` and `HeroActor.StartForcedMove` for dashes using the normal collide-and-slide.
- Placeholder presentation: damage flash, floating damage text, sparks, projectile views, shield ring, dash trail, area telegraph, attack animator trigger, `CombatAudioHooks`, `CombatDebugOverlay`.
- Prefabs `CombatDummy`, `BasicAttackProjectile`, `AbilityProjectile_Q`; ability configs in `Data/Abilities`; Nilo attack clip and `Attack` trigger.
- Exported placeholder art: Vex sheet (dummy), Q projectile, shield ring, spark, ember; generated circle/ring sprites.
- Menu alias `IndieMoba > Build Prototype`.

### Changed
- `HeroActor` is ticked by the scene's `SimulationTickRunner` (falls back to its own loop without one); movement feel unchanged.
- Scene builder adds the simulation runner, combat world, hero combat components, two dummies and combat presentation.
- `DEVELOPMENT.md` documents the combat architecture and the project roadmap.

### Removed
- WASD movement bindings (W is now an ability). Movement: hold right mouse or arrow keys.

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
