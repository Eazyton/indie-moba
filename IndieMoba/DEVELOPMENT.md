# IndieMoba - Development Guide

Unity 6.3 (6000.3.25f1), URP 2D, new Input System only. This document covers the architecture, workflows and technical decisions of the project. Release notes live in `CHANGELOG.md`.

## Current scope (Phase 5B complete)

- Phase 1: forest test area, controllable placeholder hero with collision-aware movement, pixel-perfect follow camera, input abstraction.
- Phase 2: hero combat foundation - shared simulation tick, health/shield/damage, target abstraction, targeted basic attack, four placeholder abilities (Q/W/E/R) with cooldowns, two combat dummies, placeholder visual feedback, audio hook points and a debug overlay.
- Phase 3: one functional prototype lane (70x27 world units) - minion waves, lane AI, towers, nexus, structure damage rules, win/lose result, debug revive. Regression scene: `GameplayPrototype.unity`.
- Phase 4B: full 5v5 graybox map (`Match5v5.unity`) - 3 lanes, river, two jungles, 18 towers, 2 nexuses, 3 wave spawners (current `Wave_Default`: first wave 4 s, interval 35 s; Mid offset +7.8 s).
- Phase 5B (complete, manually validated in Play Mode): jungle camps, monsters, aggro/leash/reset/respawn, status effects and the Verdant Bloom / Ember Blight buffs. See "Phase 5B - Jungle" below.

Not implemented yet: shop, items, XP, gold, levels, competitive respawn, epic objectives, concealment/vision, bots/AI, networking, backend, menus, HUD, audio content, final animation pipeline.

## Folder layout

```
Assets/_Game/
  Art/
    Characters/_Placeholder/Nilo/        placeholder hero sheet, animations, animator controller
    Characters/_Placeholder/Vex/         placeholder combat dummy sheet
    Environment/_Placeholder/...         placeholder terrain, props, debug collision tile
    VFX/_Placeholder/                    projectile, shield ring, spark, ember, generated circle/ring
  Data/
    Characters/HeroMovementConfig.asset  tunable movement parameters
    Abilities/Hero_*.asset               AbilityConfig per slot (basic attack, Q, W, E, R)
    Prototype/PrototypeLayout.json       exported demo layout (props, blocked grid, spawn)
  Input/MobaControls.inputactions
  Prefabs/
    Characters/Hero_Placeholder.prefab
    Characters/Visuals/Nilo_Visual_Placeholder.prefab
    Characters/CombatDummy.prefab
    Gameplay/Projectiles/BasicAttackProjectile.prefab   view only
    Gameplay/Projectiles/AbilityProjectile_Q.prefab     view only
    Environment/_Placeholder/*.prefab
  Scenes/Prototype/GameplayPrototype.unity
  Scripts/
    Core/           IndieMoba.Core          intents, ability commands, input interfaces, SimulationTickRunner
    Characters/     IndieMoba.Characters    movement config, motor state, motor, HeroActor
    Combat/         IndieMoba.Combat        Health, CombatTarget, CombatWorld, HeroCombat, abilities, CombatDummy
    Input/          IndieMoba.Input         KeyboardMouseInputSource
    CameraSystem/   IndieMoba.CameraSystem  MobaCameraRig
    Presentation/   IndieMoba.Presentation  character presenter/animator bridge, combat views, audio hooks, debug overlay
    Editor/         IndieMoba.Editor        PrototypeSceneBuilder (editor only)
```

Every script folder has its own assembly definition. Dependencies flow one way: `Core` <- `Characters` <- `Combat` <- `Presentation`; `Input` and `CameraSystem` depend on `Core` only. Gameplay assemblies never reference `Presentation`; removing every view component leaves gameplay fully functional.

`Tools/export/` (repo root, outside Unity) holds the one-off exporter that extracts placeholder art and layout from the Phaser demo.

## Gameplay vs presentation

The hero is split into two layers:

- **Gameplay (`HeroActor`, `CharacterMotor`)** owns the authoritative `CharacterMotorState` (position, velocity, facing, destination). It is ticked by the scene's `SimulationTickRunner` (see below). Without a runner assigned it falls back to its own 60 Hz accumulator (Phase 1 behaviour). It exposes `PreviousState`, `State`, `InterpolationAlpha` and a `Ticked` event.
- **Presentation (`CharacterPresenter`, `CharacterAnimatorBridge`)** lives under the `VisualRoot` child. It interpolates between `PreviousState` and `State` every frame, flips the sprite by facing, and drives Animator parameters `IsMoving` (bool) and `Speed` (float). The bridge only sets parameters that exist in the controller, so any replacement controller works.

Script execution order: `KeyboardMouseInputSource` (-50) -> `SimulationTickRunner` (10) -> `CharacterPresenter` (100) -> `MobaCameraRig` (200). Combat views run in `Update`/`LateUpdate`.

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

Two input interfaces live in `Core`; gameplay only sees their outputs:

- `IMovementInputSource.ReadIntent()` returns a `MovementIntent`. `HeroActor` reads it once per tick through `inputSourceBehaviour`.
- `IAbilityInputSource.DrainCommands(list)` returns queued `AbilityCommand`s (`Slot`, `AimPoint`, `HasAim`). `HeroCombat` drains it once per tick through `abilityInputBehaviour`. Commands are requests; gameplay decides whether they succeed.

`KeyboardMouseInputSource` (prototype PC scheme) implements both, using `MobaControls.inputactions`, map `Gameplay`:

| Action | Binding | Result |
|---|---|---|
| `MoveToPoint` + `PointerPosition` | hold right mouse | `Destination` intent, updated while held |
| `Move` | arrow keys, gamepad left stick | `Direction` intent (overrides a pending destination) |
| `BasicAttack` | left mouse (hold repeats) | basic attack command every frame while held; the cooldown gates the rate |
| `Ability1` / `Ability2` / `Ability3` / `Ultimate` | Q / W / E / R | ability command aimed at the cursor |

WASD is intentionally not bound (W is an ability). Presses are queued in `Update` (edge detection is frame-based) and consumed on the next simulation tick, so no press is lost or duplicated when a frame runs zero or several ticks. Only the latest command per slot is kept.

**Other schemes / mobile:** add components implementing `IMovementInputSource` and/or `IAbilityInputSource` (virtual joystick -> `Direction`; ability buttons -> `AbilityCommand`; drag-to-aim or joystick aiming -> `AimPoint`; a cancel zone simply never emits the command) and assign them on the hero. `HeroCombat.RequestAbility(command)` also accepts commands directly (UI buttons, tests). No gameplay code changes are needed; the same commands are what a client would send to a server.

## Simulation tick

`SimulationTickRunner` (Core, one per scene under `Gameplay/Simulation`) is the single authoritative local gameplay clock: fixed 60 Hz, accumulator in `Update`, at most `maxTicksPerFrame` ticks per frame. Systems implement `ISimulationSystem` (`SimulationOrder`, `SimulateTick(dt, tick)`) and register in `OnEnable`. Each tick runs them in a stable order (`SimulationOrder` constants):

1. `Input` (0) - reserved; input is already queued before the tick.
2. `Actors` (100) - `HeroActor` movement and dashes.
3. `Abilities` (200) - `HeroCombat` drains commands, validates, executes, starts cooldowns.
4. `Projectiles` (300) - `CombatWorld` moves projectiles and advances delayed areas; hits are queued as damage.
5. `Damage` (400) - `CombatWorld` applies queued damage to `IDamageable`s.
6. `State` (500) - shield expiry, dummy reset timers.

Presentation stays frame-based and reads state (interpolating with `runner.InterpolationAlpha`). A server tick can later call `SimulationTickRunner.Step()` directly. No ECS, no networking framework.

## Combat

### Health and damage

- `Health` (MonoBehaviour, `IDamageable`): `maxHealth`, current HP, shield amount + remaining duration, `IsDead`. `ApplyDamage(DamageInfo)` consumes the shield first, then HP, and returns a `DamageResult` (absorbed, health damage, killed). `Heal`, `ApplyShield(amount, duration)`, `ResetHealth`.
- Events: `DamageTaken`, `Healed`, `ShieldChanged`, `Died`, `Revived`. Death never destroys or disables the GameObject.
- `DamageInfo` carries amount, source GameObject, source `Team` and `AbilitySlot`.
- All combat damage goes through `CombatWorld.QueueDamage` and is resolved in the `Damage` stage.
- `Team` is a plain enum (`Neutral`, `Blue`, `Red`); `TeamRules.AreHostile` = different teams.

### Targets

- `ICombatTarget`: `Position`, `Radius`, `Team`, `IsAlive`, `Damageable`, `Transform`. `CombatTarget` implements it and registers with `CombatWorld`.
- `CombatWorld` (under `Gameplay/CombatWorld`) owns the target registry, queries (`FindHostileTargetsInCircle`, `FindBasicAttackTarget`), projectile and delayed-area simulation, and the damage queue. It uses pure math, not Physics2D. Events for presentation: `ProjectileSpawned/Destroyed`, `AreaStarted/Resolved`, `DamageApplied`.
- Future heroes, minions, monsters and structures only need a `Health` + `CombatTarget`.

### Abilities

- `AbilityConfig` (ScriptableObject): kind, cooldown, damage, range, speed, radius, duration, amount, cast range, delay, cone half-angle, close range, min aim distance, and an optional projectile view prefab (presentation only, never read by gameplay logic).
- `HeroCombat` holds five slots (`BasicAttack`, `Ability1..3`, `Ultimate`). Each slot is an `AbilitySlotRuntime` = config + `Cooldown` (struct: `Duration`, `Remaining`, `IsReady`, `NormalizedRemaining/Progress`) + an `IAbilityBehaviour` created from the config kind. Behaviours are small composable classes, not an inheritance tree.
- Activation flow per command: configured? -> cooldown ready? -> not busy (dashing)? -> behaviour `TryExecute` (validation + execution) -> start cooldown. Failures raise `ActivationDenied(slot, reason)` and do not consume the cooldown.
- Events: `AbilityStarted`, `AbilityCompleted` (immediately for instant abilities, at the end for the dash), `CooldownStarted`, `ActivationDenied`.
- Dead heroes ignore commands; death cancels an active dash and blocks movement.

Prototype kit (values from the demo `balance.ts`, 32 px = 1 unit):

| Slot | Behaviour | Values |
|---|---|---|
| Basic attack (LMB) | `BasicAttackBehaviour`: targeted, not a skillshot. Selects a hostile `ICombatTarget` whose edge is within range and inside the aim cone around the cursor direction (or within close range regardless of angle), scored by distance + angle. Spawns a homing projectile that damages that target on arrival. No target: if the config has `canBasicAttackWithoutTarget`, fires a linear projectile toward the cursor up to range that damages the first hostile it crosses (cooldown consumed even on a miss); otherwise denied, no cooldown. Nilo: enabled. | cd 0.8 s, dmg 58, range 4.69, cone 43 deg, close 0.875, speed 18 |
| Q | `LinearProjectileBehaviour`: skillshot toward the cursor, hits the first hostile along its path | cd 8 s, dmg 80, range 12.5, speed 20, radius 0.34 |
| W | `ShieldBehaviour`: shield on own `Health` | cd 15 s, 200 for 4 s |
| E | `DashBehaviour`: `HeroActor.StartForcedMove` toward the cursor; uses the same collide-and-slide as movement (`CharacterMotor.SimulateForced`), so it cannot cross blocked terrain | cd 10 s, 6.25 u in 0.17 s |
| R | `DelayedAreaBehaviour`: area at the cursor clamped to cast range; damages hostiles within radius after the delay | cd 60 s, dmg 150, radius 6.25, cast range 20.3, delay 1 s |

Target selection is isolated behind `IBasicAttackTargetSelector` (`ConeTargetSelector` today; swap with `CombatWorld.SetBasicAttackSelector`). Target priority, mobile auto-targeting and target lock belong there later.

### Projectiles

Projectiles are data (`ProjectileState`, pooled inside `CombatWorld`) created from a `ProjectileSpec`: `Homing` (follows an `ICombatTarget`, hits when within one step + tolerance, expires if the target dies) or `Linear` (swept segment vs target circles, expires at max distance). Views are spawned by `ProjectileViewSpawner` through `IProjectileViewFactory` (`InstantiateProjectileViewFactory` now; replace with a pooled factory later) and only follow the state.

### Combat dummy

`CombatDummy.prefab`: `Health` (600) + `CombatTarget` (Red) + `CombatDummy` (resets 3 s after death, heals to full after 5 s without damage; context menu `Reset Dummy`). The scene has `CombatDummy_A` (10.5, 11.25) and `CombatDummy_B` (13.5, 9.75).

### Presentation, audio and debug

All placeholder, all optional, all under `IndieMoba.Presentation`:

- `HealthFlashView` (90 ms tint on damage, dimmed while dead), `CombatFeedbackSpawner` (floating damage numbers / "(absorbed)" and sparks), `ProjectileView`/`ProjectileViewSpawner`, `ShieldRingView` (ring while shield > 0), `DashTrailView` (afterimages while dashing), `AreaTelegraphSpawner` (growing telegraph, flash on impact), `CombatAnimatorBridge` (`Attack` trigger if the controller has it).
- `CombatAudioHooks`: AudioClip slots (basic attack, Q, W, E, R, hit, deny) played through an AudioSource; silent while empty. No audio system yet.
- `CombatDebugOverlay` (on `Gameplay/CombatPresentation`): HP, shield, cooldowns, last denied reason, dummy HP, control help. Gizmos: attack range and R cast range (`HeroCombat`), target radius (`CombatTarget`), live projectiles/areas (`CombatWorld`).

### Designer workflow

- Tune abilities in `Assets/_Game/Data/Abilities/*.asset`; create new ones via `Create > IndieMoba > Combat > Ability Config` and assign them to the `HeroCombat` slots.
- Swap projectile looks by replacing the prefab's `Visual` child or pointing `projectileViewPrefab` to another prefab with a `ProjectileView`.
- Replace hero/dummy art by swapping the visual children; gameplay never references sprites, animation clips or animation events.
- Note: the prototype builder rewrites the ability config values on rebuild.


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

- **Hero:** create a new visual prefab (sprite + Animator with `IsMoving`/`Speed` parameters and optionally an `Attack` trigger) and replace the nested `Nilo_Visual_Placeholder` under `Hero_Placeholder/VisualRoot`. Collision radius stays in `HeroMovementConfig`.
- **Props:** each environment prefab has a `Visual` child (sprite) and, for blocking props, a `Collision` child on layer `Obstacle`. Replace the sprite on `Visual`; adjust the `Collision` shape if the footprint changes. Scene instances update automatically.
- **Terrain:** `Environment/Terrain` is currently one large sprite. Replace it with a Tilemap or new sprite; it has no gameplay role.

## Placeholder export tool

Re-extracts placeholder art and `PrototypeLayout.json` from the Phaser demo in `Reference/mossbound/` (the demo is read-only):

```bash
sh Tools/export/run-export.sh
```

Outputs 27 PNGs under `Assets/_Game/Art/{Characters,Environment,VFX}/_Placeholder/` and the layout JSON (344 props, 80x54 blocked grid, hero spawn). `Tools/export/dist/` is gitignored.

## Prototype scene builder

`GameplayPrototype.unity`, its prefabs and settings are generated by `PrototypeSceneBuilder` (idempotent; safe to re-run):

- Menu: `IndieMoba > Build Prototype` (alias of `IndieMoba > Prototype > Rebuild Gameplay Prototype`). Requires Unity to have compiled the latest scripts.
- Batch mode:

```bash
Unity.exe -batchmode -nographics -quit -projectPath <path>/IndieMoba -executeMethod IndieMoba.EditorTools.PrototypeSceneBuilder.BuildFromCommandLine -logFile <log>
```

Steps: project settings (layers, sorting layers, 2D gravity, sort axis, build settings) -> texture import settings -> assets (sprite slicing, tile, config, animations incl. attack, controller) -> prefabs (projectiles, ability configs, Nilo visual, hero with combat components, combat dummy, environment) -> scene (simulation runner, combat world, hero, dummies, combat presentation). Manual edits to generated assets are overwritten on rebuild; once the prototype stabilises, the builder can be retired and assets edited by hand.

## Networking readiness

- Movement is deterministic-by-design per tick: `Simulate(state, intent, dt)` with a fixed dt and no Rigidbody integration.
- Input is already reduced to small intent messages.
- State and presentation are separated, and presentation already interpolates between ticks.

- One gameplay clock (`SimulationTickRunner`) with a fixed stage order; a server can drive `Step()`.
- Abilities are requested by small `AbilityCommand`s (slot + aim point); validation, cooldowns, targeting and damage are resolved in gameplay, so the server can own them.
- Projectiles and areas are plain data with ids; clients can render them from replicated state or spawn events.
- Damage flows through one queue per tick, which is the natural place for server-side resolution and replication.

A future server-authoritative model can run the same motor and combat code on the server, send intents/commands from clients, and add client prediction/reconciliation. No networking exists yet.

## Known limitations

- Play mode and visual output have not been verified by the automated build; only compilation and asset generation were checked in batch mode.
- Some props lie slightly outside the 0..70 x 0..27 camera bounds; they sit inside the blocked border and are unreachable.
- The terrain is a single large sprite.
- Physics queries use `Physics2D`, so exact float determinism across platforms is not guaranteed.
- Combat Play mode behaviour has not been observed by the agent; it was verified by compilation only.
- The damage flash tints the sprite (the default sprite shader has no white-fill mode).
- Gamepad ability buttons exist but aim at the mouse pointer; gamepad aiming is not implemented.
- `HeroCombat` blocks other abilities while dashing.
- Lane Play mode behaviour (waves, fights, towers, result screen) has been verified by compilation only.
- Minions are steered along waypoints with soft separation; there is no pathfinding, so minions pushed far off the lane rely on collide-and-slide.
- The hero does not respawn; use the debug revive (F2 or the overlay button).

## Roadmap

Historical planning list. Phases 1-5B are done. Later phases are not implemented.

- **Phase 2 - Hero combat foundation**: health, damage, targeting, basic attack, Q/W/E/R, cooldowns, dummy.
- **Phase 3 - Functional lane** (current): minions, waves, towers, nexus/base, structure damage, simple win condition.
- **Phase 4 - Full MOBA map:** 3 lanes, two jungles, central river, two bases, tower placements, jungle entrances, camp areas, neutral objective pits, collision/navigation layout, 5v5-ready spatial scale.
- **Phase 5 - Jungle:** monsters, camps, aggro, leash, reset, rewards, respawn, buffs.
- **Later:** shop, items, economy, epic objectives, bots, multiplayer, server authority, matchmaking, account/backend, progression/meta, monetization.

## Basic attack targeting modes (preparation)

`AbilityCommand` carries a `BasicAttackTargeting` mode and an optional explicit target; `BasicAttackBehaviour.ResolveTarget` picks the target:
- AutoTarget: cone selector via `CombatWorld.FindBasicAttackTarget` (current Phase 2 behaviour, used by all current input).
- ExplicitTarget: the given hostile, alive target if within range; otherwise no target.
- AttackMoveTarget: not implemented yet; falls back to AutoTarget.

If no valid target results (including an invalid explicit target), `canBasicAttackWithoutTarget` decides between a directional attack and a `NoTarget` denial.

## Basic attack events

`HeroCombat` exposes three distinct events:
- `BasicAttackRequested(AbilityCommand)`: every basic attack command handled, before cooldown/target checks.
- `BasicAttackPerformed(BasicAttackPerformedInfo)`: the attack actually happened and its cooldown started, whether or not it will hit. Info: `AttackId`, `Origin`, `Direction`, `Target` (null when directional), `IsTargeted`, `Targeting`.
- `BasicAttackHit(ICombatTarget, DamageResult)`: basic attack damage from this hero was applied to a target (filtered from `CombatWorld.DamageApplied`).

Passives that trigger on attacking should use Performed; lifesteal and on-hit effects should use Hit. No passives, items or on-hit systems exist yet. Future directional, melee and modified attacks should keep raising the same events.

Intended future controls. PC: right click ground = move, right click enemy = move/attack that enemy, attack-move = move and attack an eligible target, Q/W/E/R = abilities. Mobile: attack button = auto target, future hero/minion/tower priority buttons, future target lock.

Sorting layer IDs are stable: the scene builder never regenerates a valid existing ID and only saves the TagManager when it changes, because scenes, prefabs and Light2D reference layers by ID.

## Phase 3 - Functional lane

### Assemblies
- `IndieMoba.Minions`: `MinionConfig`, `MinionController` (state machine Walking/Chasing/Attacking/Dead), `MinionCombat`, `MinionRegistry`, `IMinionFactory`.
- `IndieMoba.Structures`: `Structure` (health gate, damage filter, blocking collider), `TowerConfig`, `TowerTargeting`, `TowerCombat`, `NexusObjective`.
- `IndieMoba.Match`: `LanePath`, `WaveConfig`, `WaveSpawner`, `MatchController`.
- Lane targeting policies live in `IndieMoba.Combat` (`Combat/Targeting/LaneTargeting.cs`).

### Tick order
Input (hero input, debug revive) -> Spawning (`WaveSpawner`) -> Actors (hero, minions, tower targeting) -> Abilities (hero abilities, tower attacks) -> Projectiles -> Damage -> State. `MatchController` pauses `SimulationTickRunner` when a nexus is destroyed.

### Targets and damage
- `ICombatTarget.Kind` is Hero, Minion, Structure or Other. Lane AI and towers only consider Hero, Minion and Structure; dummies are Other and are ignored by lane AI and towers but can still be damaged by the hero.
- `Health` consults an optional `IDamageFilter`. `Structure` implements it:
  - Only slots in `allowedDamageSlots` (default: BasicAttack) damage structures. Hero Q/W/E/R do not. Minion attacks are BasicAttack and are scaled by `MinionConfig.structureDamageMultiplier` (0.6).
  - A structure with alive `requiredStructures` is invulnerable (the nexus requires its tower).
  - Protection: when no attacking-team minions are within `protectionRadius`, incoming damage is multiplied by `protectedDamageMultiplier` (0.2). This is heavy damage reduction, not invulnerability. It is our prototype anti-backdoor rule, not an Honor of Kings value; disable with `protectionEnabled` or retune per structure.
- Linear projectiles and areas skip structures unless `HitsStructures` is set (only directional basic attacks set it).

### Minion target priority
Default table (`MinionConfig.targetPriority`, editable per config): 0 enemy minion engaged with one of my allies, 1 other enemy minion, 2 enemy structure, 3 enemy hero. This order is our prototype decision, not the Honor of Kings implementation. There is no hero-aggression aggro. Minions keep a sticky target, retarget every `retargetIntervalTicks`, and drop targets beyond `leashRange`.

### Towers
Keep the current target while valid, else the nearest enemy minion, else the enemy hero. Homing BasicAttack projectiles; separate hero and minion damage in `TowerConfig`.

### Nexus defense
The nexus reuses `TowerTargeting` and `TowerCombat` with its own `NexusConfig` (range 9, hero 180, minion 140, interval 1.2 s, projectile speed 14, warning range 14; prototype values). `TowerConfig` and `NexusConfig` share the `StructureDefenseConfig` base. Invulnerability only blocks incoming damage, so the nexus attacks even while its tower stands.

### Waves
`WaveConfig`: first wave 4 s, interval 30 s at Phase 3 (the 5v5 tuning raised `Wave_Default.waveInterval` to 35 s), spawn gap 0.7 s, composition melee/ranged/melee/ranged/melee, lateral offsets -0.75/0/0.75. These are prototype values taken from our demo, not Honor of Kings values; they will be tuned for 15-20 minute matches.

### Movement and collision
Minions have no colliders or rigidbodies. They use the shared `CharacterMotor` against the Obstacle layer, soft separation between minions and a small structure-avoidance steer. Heroes and minions do not collide physically. Structures have a `CircleCollider2D` on the Obstacle layer and block movement; the collider is disabled when destroyed.

### Match result
The nexus is invulnerable while its tower stands. Destroying a nexus ends the match, pauses the simulation and shows VICTORY/DEFEAT with a Restart button (`MatchResultView`).

### Presentation and debug (replaceable)
- `MinionPresenter`, `StructureView` (rubble swap, invulnerable bubble, protection tint), `HealthBarView` (temporary world-space bars on minions, towers and nexus), `TowerRangeView` (range ring on towers and nexuses, shown when the hero is near an enemy structure or the structure has a target: subtle neutral with no target, blue when targeting a non-player target, red when targeting the player; presentation only).
- Damage numbers only appear for interactions involving the player hero (`CombatFeedbackSpawner.onlyPlayerInvolved`); blocked hits show Invulnerable/Immune and protected hits append "(protected)".
- `LaneDebugOverlay` (top right): wave timer, minion counts, structure HP/state, Revive Hero button. `HeroReviveTool`: F2 revives the hero at `HeroRespawnPoint`. Dev tooling only; there is no respawn gameplay.

### Future work (not implemented)
- Tower aggro switch: a tower should switch to an enemy hero who attacks an allied hero inside tower range.
- Advanced minion aggro (reacting to hero aggression).

## Phase 4B - 5v5 graybox map

### Assemblies and assets
- `IndieMoba.Map` (references Core, Combat, Match; nothing references it except Editor): `MapLayoutData`, map types, slot/spawn/zone markers, `MapCollisionShape`, `MapGenerationStamp`, `MapCameraSettings`, `MapDebugGizmos`.
- `Data/Map/Map5v5Layout.asset`: authoring data for the first generation.
- `Prefabs/Map/Map5v5_Gameplay.prefab`: collision, lanes, structure slots, spawns, zones, camera bounds.
- `Prefabs/Map/Map5v5_Visual.prefab`: graybox tilemaps, props, zone visuals, lights. It is derived from the gameplay prefab.
- `Scenes/Match/Match5v5.unity`: separate scene. `GameplayPrototype.unity` (Phase 3) is unchanged.

### Layout (prototype values)
128x128 u playable, 8 u apron, 32 px = 1 u. Blue is bottom-left, Red top-right. Mid and the river run diagonally (river axis x + y = 128). Top = Clash lane (corrupted), Bottom = Farm lane (living). Side lanes 7 u wide, Mid 8 u. Nexus Blue (15, 15), Red (113, 113); fountains (7, 7) and (121, 121). 18 towers + 2 nexuses. River 10 u wide with two bridges and two fords; Heart Plaza r8 at (64, 64) with two pillars; pits at (38, 90) and (90, 38) with rim arcs and four openings. Four jungle clearings per quadrant, 24 brush zones (data and visuals only, no concealment).

### Source of truth
- First generation: `MapLayoutData` -> `Map5v5_Gameplay` prefab.
- After that, the gameplay prefab is the hand-authored source of truth. Edit it directly.
- Menu 1 never overwrites an existing layout asset. Only `Danger/Reset Layout To Defaults` resets it (two confirmations).
- Menu 2 regenerates the gameplay prefab and asks for two confirmations if it exists.
- Menu 3 rebuilds the visual prefab from the gameplay prefab (one confirmation if it exists).
- Menu 4 never touches gameplay geometry. It finds or creates scene objects and keeps existing ones; wave offsets come from the layout only when a spawner is first created.

### Menus (`IndieMoba/Map 5v5/`)
Run `IndieMoba/Build Prototype` once first (shared prefabs and configs), then 1 -> 2 -> 3 -> 4. Later: edit the prefab, re-run 3 for visuals, 4 to rewire.

### Collision
The `Collision` object holds a static composite on the Obstacle layer: a `Bounds` box (Merge) minus walkable carves (Difference) plus blockers (Merge). Default mode is Filled Polygons because `CharacterMotor` depenetrates using overlaps before its circle-cast slide, and edge-only outlines have no interior. Outlines can be selected with `Collision Mode/Outlines` for comparison; the shapes are unchanged.

### Structures
Inner towers require the Outer tower, Base towers require the Inner tower (`RequirementMode.AllDestroyed`). Each nexus uses `RequirementMode.AnyDestroyed` with its three Base towers: it becomes vulnerable once any one dies.

### Waves
One `WaveSpawner` per lane, all listed in `MatchController.waveSpawners` and stopped at match end. `WaveSpawner.waveSpawnOffset` delays the first wave and, since the interval is shared, every wave of that lane. Mid uses +7.8 s, so Mid reaches its meeting point (~24.3 s natural) about 5 s before the side lanes (~37.1 s). Prototype value; tune via spawn positions, geometry, speed or offset.
`MinionController` computes the lateral formation and separation direction from the current lane segment, so bent lanes keep their spacing.

### Camera and respawn
Orthographic size 9.375, bounds (-6, -6, 140, 140), no PixelPerfectCamera in this scene. No rotation for Red; red-side orientation is a future UX test. The Blue hero spawns and debug-revives at the Blue fountain (7, 7): development only, not the final respawn system.

### Out of scope
Jungle monsters, buffs, objective monsters, concealment, fog of war, final art, bots, multiplayer, economy, shop, XP/leveling.

## Phase 5B - Jungle

Status: complete. Compiled and manually validated in Unity Play Mode (2026-10-11): 12 camps on the map, spawn/respawn, damage-only aggro, whole-camp engage, leash and reset (immunity, HP recovery, partial reset), Verdant Bloom (cooldown recovery, regen, duration), Ember Blight (Scorch, slow, refresh without stacking, target restrictions), status effects against another hero, kill attribution by periodic damage, buff removal on death, and regression of lanes, waves, towers, nexus, abilities, victory and Restart in `Match5v5.unity` and `GameplayPrototype.unity`. There are no automated Play Mode tests.

### Assemblies and tick order
- New `IndieMoba.Jungle` (references Core, Characters, Combat). `Presentation` and `Editor` reference it; Map, Match, Minions and Structures do not.
- `SimulationOrder` adds `Neutral = 110` (`JungleDirector`, after Actors) and `Status = 350` (`StatusEffectController`, after Projectiles, before Damage). Full order: Input 0 -> Spawning 50 -> Actors 100 -> Neutral 110 -> Abilities 200 -> Projectiles 300 -> Status 350 -> Damage 400 -> State 500.

### Targeting
- `CombatTargetKind.Monster = 4` (existing values unchanged).
- `LaneTargetRules.IsLaneTargetable` uses an allowlist (Hero, Minion, Structure): minions, towers and the nexus ignore jungle monsters and dummies.
- Monsters are `Team.Neutral`; `TeamRules.AreHostile` is unchanged, so heroes (basic attack, Q, R) damage monsters and monsters never fight each other.

### Status effects (`Combat/StatusEffects`)
- `StatusEffectDefinition` (ScriptableObject): duration, cooldown recovery multiplier, HP regen/s, damage/s with tick interval, move speed multiplier, `onBasicAttackHit` rules (`StatusEffectOnHit`: effect + allowed target kinds), display color (presentation only).
- `StatusEffectController` (per unit, ticked at `Status`): one instance per definition; re-applying refreshes the duration (no stacking) and keeps the periodic tick accumulator. Different definitions combine: slowest speed wins, cooldown recovery multiplies, regen/DoT add. All effects are cleared on death.
- Periodic damage is queued in `CombatWorld` with `AbilitySlot.StatusEffect`: it never triggers `HeroCombat.BasicAttackHit` (no on-hit recursion) and structures reject it. `HeroCombat` ignores commands for that slot.
- On-hit: the controller listens to its own `HeroCombat.BasicAttackHit` and applies the rules of its active effects to the hit target's `StatusEffectController`, only for allowed kinds. Lane minions have no controller and are not in the allowlist, so they are never affected.
- Cooldown recovery: `HeroCombat.CooldownRecoveryRate` (default 1.0) multiplies the cooldown tick (`Cooldown` struct unchanged). 1.15 = 15% more recovery per second, not a shorter cooldown. It returns to 1.0 when the effect expires or is cleared.
- Slow: `CharacterMotor.SpeedMultiplier` / `HeroActor.SpeedMultiplier` (default 1.0) scale normal movement only; dashes are unaffected.

### Jungle runtime
- `JungleDirector` ticks every camp, listens to `CombatWorld.DamageApplied` (hero aggro, kills) and grants carrier buffs. Events `MonsterKilled`, `CampCleared`, `BuffGranted`, `CampStateChanged` are the reward hooks for future economy.
- `JungleCamp` is the strategic authority: states WaitingFirstSpawn -> Idle -> Engaged -> Resetting -> Idle, or Cleared -> (respawn) Idle.
  - Aggro: only hero damage (no proximity aggro). The whole camp engages one shared target.
  - Retarget: only when the target becomes invalid (dead, invulnerable, outside the leash); validity is re-checked every `retargetIntervalTicks`. Candidates: heroes that damaged the camp, then heroes within `acquireRadius` of the camp center. None -> reset.
  - Leash: monsters never step outside `leashRadius` around the clearing center.
  - Reset: after `resetAfterNoDamage` s without hero damage, or no valid target. The camp disengages, members are immune, clear their status effects, return at `returnSpeed`, regenerate over `resetHealDuration` and snap home at full HP on arrival or after `returnTimeout`. A camp hit from outside its leash also resets.
  - Clear: only when every member is dead. Respawn: all members together after `respawnTime`; GameObjects are deactivated/reactivated, never destroyed or recreated.
- `JungleMonster` executes orders (move, attack, return) with the shared `CharacterMotor` against the Obstacle layer. It is the `Health` damage filter for reset immunity. Health supports a single `IDamageFilter`; monsters have no other filter (known limitation, not refactored).
- Buffs: the living hero who lands the killing blow on the carrier (including through Scorch) receives the camp's `JungleRewardProfile.carrierBuff`. Kill attribution uses `DamageInfo.Source` (GameObject); multiplayer will need stable ids.

### Data (`Data/Jungle`, created by menu 5 only when missing; existing values are never overwritten)

| Parameter | Small camp | Buff camp |
|---|---:|---:|
| First spawn | 20 s | 20 s |
| Respawn after clear | 55 s | 90 s |
| Leash radius | 6.5 u | 8 u |
| Acquire radius | 5 u | 6 u |
| Reset without hero damage | 6 s | 6 s |
| Return speed | 7 u/s | 7 u/s |
| Reset heal | 1.5 s + full snap | 1.5 s + full snap |
| Return timeout | 4 s | 4 s |

| Archetype (`Monsters/JNG_Config_*`) | HP | Damage / interval | Attack range |
|---|---:|---|---:|
| BuffElite | 1500 | 22 / 1.5 s | 1.2 |
| LargeMelee | 900 | 22 / 1.25 s | 1.1 |
| MediumMelee | 520 | 16 / 1.2 s | 1.0 |
| SmallMelee | 220 | 10 / 1.2 s | 0.9 |
| SmallRanged | 200 | 9 / 1.5 s | 4.5 |

Monster chase speed 3.5 u/s (`JNG_MonsterMovement`). Buffs (`StatusEffects/SE_*`): Verdant Bloom 80 s, cooldown recovery x1.15, 8 HP/s. Ember Blight 80 s, basic attacks on Heroes/Monsters apply Scorch (12 dmg/s for 3 s, 0.5 s ticks) and Ember Slow (-15% for 1.5 s), both refresh without stacking. All values are playtest prototypes.

Camp placement (`JungleSetup_Match5v5`, editable): Clash-side quadrants (BlueUpper, RedLower) have Blight Warden + Ash Mites (Ember Blight), Twisted Boar and Shade Stalker + Crystal Mites. Farm-side quadrants (BlueLower, RedUpper) have Root Guardian + Spore Wisps (Verdant Bloom), Moss Boar and Fungal Brute + Spore Wisps. The four optional clearings stay empty. Monster names are provisional placeholders; visuals are the Vex placeholder sheet tinted and scaled (`Prefabs/Jungle/JNG_*`).

### Menu `IndieMoba/Map 5v5/5. Wire Jungle (Non-Destructive)`
Run after menu 4 (Unity must not be in Play Mode). It creates missing jungle assets/prefabs, creates missing camps under `Match5v5_Systems/Jungle` from the `MapZoneMarker`s in the `Map5v5_Gameplay` instance (existing camps are kept as they are; only scene references are re-wired), adds a `StatusEffectController` to every hero in the scene and adds `Match5v5_Systems/JunglePresentation`. It never touches map geometry, prefabs from menus 2/3, waves or structures. To regenerate a camp, delete it from the scene and re-run; to regenerate a data asset or prefab, delete it and re-run. Expected Console output: `4 buff, 8 small, 4 optional (left empty)` clearings and `12 camps`; a second run reports `0 created, 12 kept`. Editor note: opening a scene in Single mode unloads assets no scene references, so the builder reloads `JungleSetup` after `OpenScene` (same pattern as menu 4).

### Presentation and debug (replaceable)
- `JungleMonsterPresenter` (interpolation, flip, attack punch, immune bubble while resetting, carrier marker), `HealthFlashView` and `HealthBarView` on monsters.
- `StatusEffectAuraSpawner`: pulsing ring under any unit with active effects, tinted with the buff/debuff color.
- `JungleDebugOverlay` (F3, hidden by default): jungle time, every camp's state, alive count, HP, spawn/respawn timer, target and reset countdown; player effects, speed and cooldown rate; last buff/clear event; "Force spawn all".
- `JungleTestHeroTool` (overlay button "Spawn test hero (Red)"): spawns at runtime a `CombatDummy` reclassified as a Red Hero with a `StatusEffectController`, to test hero-vs-hero effects such as Ember Blight. Never saved in the scene. As a Hero it can also be targeted by blue towers/minions.

### Known limitations
- Single `IDamageFilter` per `Health` (reset immunity uses it).
- No proximity aggro, no smite-like spell, no gold/XP, no epic monsters (out of scope).
- Monsters steer with collide-and-slide only (no pathfinding); clearings are open areas so this is sufficient for camps.
- Buff carrier kill credit goes to the killing blow's source GameObject; assists and team buffs do not exist yet.
- Data assets and monster prefabs are created once; changing defaults in the builder does not update existing assets.
- Only one hero exists in `Match5v5`; hero-vs-hero effects are tested with the runtime-only test hero.
- Jungle monster placeholders reuse the Vex sheet; no monster animations, attack VFX or final art.
- Verification: assemblies compile; jungle behaviour is validated manually only (no automated tests).
