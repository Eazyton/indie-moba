using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Jungle
{
    public enum JungleMonsterMode
    {
        Inactive = 0,
        Idle = 1,
        Combat = 2,
        Returning = 3,
        Dead = 4
    }

    // Executes the orders of its JungleCamp (move, attack, return). It never picks targets itself.
    // It is also the Health damage filter: immune while returning home (reset). Health supports a single
    // IDamageFilter; monsters have no other filter, so this is safe for Phase 5B.
    [DisallowMultipleComponent]
    public sealed class JungleMonster : MonoBehaviour, IDamageFilter
    {
        private const float HomeArriveDistance = 0.15f;

        [SerializeField] private JungleMonsterConfig config;
        [SerializeField] private CombatTarget target;
        [SerializeField] private Health health;
        [SerializeField] private StatusEffectController statusEffects;

        private CharacterMotor motor;
        private CharacterMotorState state;
        private CharacterMotorState previousState;
        private Cooldown attackCooldown;
        private SimulationTickRunner runner;
        private CombatWorld world;
        private JungleCamp camp;
        private Vector2 home;
        private Vector2 homeFacing = Vector2.down;
        private JungleMonsterMode mode;
        private float corpseTimer;

        public event Action<JungleMonster, ICombatTarget> AttackPerformed;

        public JungleMonsterConfig Config => config;
        public CombatTarget CombatTarget => target;
        public Health Health => health;
        public StatusEffectController StatusEffects => statusEffects;
        public JungleCamp Camp => camp;
        public JungleMonsterMode Mode => mode;
        public bool IsAlive => health != null && health.IsAlive && mode != JungleMonsterMode.Inactive && mode != JungleMonsterMode.Dead;
        public Vector2 Home => home;
        public CharacterMotorState State => state;
        public CharacterMotorState PreviousState => previousState;
        public float InterpolationAlpha => runner != null ? runner.InterpolationAlpha : 1f;
        public bool IsInvulnerable => mode == JungleMonsterMode.Returning;

        public float FilterDamage(in DamageInfo info, out DamageBlockReason reason)
        {
            if (mode == JungleMonsterMode.Returning)
            {
                reason = DamageBlockReason.Immune;
                return 0f;
            }
            reason = DamageBlockReason.None;
            return info.Amount;
        }

        public bool Initialize(JungleCamp owner, SimulationTickRunner tickRunner, CombatWorld combatWorld)
        {
            if (config == null || config.MovementConfig == null || target == null || health == null)
            {
                Debug.LogError($"{name}: JungleMonster is missing its config, movement config, CombatTarget or Health.", this);
                return false;
            }
            camp = owner;
            runner = tickRunner;
            world = combatWorld;
            home = transform.position;
            motor = new CharacterMotor(config.MovementConfig);
            health.SetDamageFilter(this);
            target.SetTeam(Team.Neutral);
            target.SetKind(CombatTargetKind.Monster);
            health.Died += HandleDied;
            Teleport(home);
            return true;
        }

        public void Spawn()
        {
            health.SetMaxHealth(config.MaxHealth, true);
            health.ResetHealth();
            attackCooldown.Reset();
            Teleport(home);
            state.Facing = homeFacing;
            previousState = state;
            mode = JungleMonsterMode.Idle;
            gameObject.SetActive(true);
            // Health.Awake runs on the first activation and re-reads the filter component; re-assert ours.
            health.SetDamageFilter(this);
        }

        public void Despawn()
        {
            mode = JungleMonsterMode.Inactive;
            if (statusEffects != null)
            {
                statusEffects.ClearAll();
            }
            gameObject.SetActive(false);
        }

        public void TickIdle(float deltaTime)
        {
            previousState = state;
            if (mode == JungleMonsterMode.Combat)
            {
                mode = JungleMonsterMode.Idle;
            }
            attackCooldown.Tick(deltaTime);
            Move(MovementIntent.Stop(), 1f, deltaTime);
        }

        public void TickCombat(float deltaTime, ICombatTarget enemy, Vector2 leashCenter, float leashRadius, IReadOnlyList<JungleMonster> mates)
        {
            previousState = state;
            mode = JungleMonsterMode.Combat;
            attackCooldown.Tick(deltaTime);
            float slow = statusEffects != null ? statusEffects.MoveSpeedMultiplier : 1f;
            if (enemy == null)
            {
                Move(MovementIntent.Stop(), slow, deltaTime);
                return;
            }
            Vector2 toEnemy = enemy.Position - state.Position;
            float distance = toEnemy.magnitude;
            float gap = distance - enemy.Radius - target.Radius;
            if (gap <= config.AttackRange)
            {
                if (distance > 1e-5f)
                {
                    state.Facing = toEnemy / distance;
                }
                Vector2 push = Separation(mates) * 0.5f;
                Move(push.sqrMagnitude > 1e-4f ? MovementIntent.FromDirection(Vector2.ClampMagnitude(push, 1f)) : MovementIntent.Stop(), slow, deltaTime);
                if (distance > 1e-5f)
                {
                    state.Facing = toEnemy / distance;
                }
                TryAttack(enemy);
                return;
            }
            // Chase, but never step outside the leash circle.
            Vector2 desired = (distance > 1e-5f ? toEnemy / distance : Vector2.zero) + Separation(mates);
            Vector2 next = state.Position + desired.normalized * 0.25f;
            Vector2 fromCenter = next - leashCenter;
            float limit = Mathf.Max(0f, leashRadius - target.Radius);
            if (fromCenter.magnitude > limit && Vector2.Dot(desired, fromCenter) > 0f)
            {
                Vector2 outward = fromCenter.normalized;
                desired -= Vector2.Dot(desired, outward) * outward;
            }
            Move(desired.sqrMagnitude > 1e-4f ? MovementIntent.FromDirection(Vector2.ClampMagnitude(desired, 1f)) : MovementIntent.Stop(), slow, deltaTime);
        }

        // Returns true when home. Heals toward full while returning (reset heal); slows do not apply.
        public bool TickReturn(float deltaTime, float returnSpeed, float healDuration)
        {
            previousState = state;
            mode = JungleMonsterMode.Returning;
            health.Heal(health.MaxHealth / Mathf.Max(0.01f, healDuration) * deltaTime);
            Vector2 toHome = home - state.Position;
            if (toHome.magnitude <= HomeArriveDistance)
            {
                Move(MovementIntent.Stop(), 1f, deltaTime);
                return true;
            }
            float multiplier = returnSpeed / Mathf.Max(0.01f, config.MovementConfig.MaxSpeed);
            Move(MovementIntent.ToDestination(home), multiplier, deltaTime);
            return (home - state.Position).magnitude <= HomeArriveDistance;
        }

        public void BeginReturn()
        {
            if (!IsAlive)
            {
                return;
            }
            mode = JungleMonsterMode.Returning;
            if (statusEffects != null)
            {
                statusEffects.ClearAll();
            }
        }

        // End of reset: snap home, full HP, idle.
        public void FinishReset()
        {
            if (mode != JungleMonsterMode.Returning)
            {
                return;
            }
            Teleport(home);
            state.Facing = homeFacing;
            previousState = state;
            health.Heal(health.MaxHealth);
            attackCooldown.Reset();
            mode = JungleMonsterMode.Idle;
        }

        // Returns true once the corpse has been hidden.
        public bool TickDead(float deltaTime)
        {
            if (mode != JungleMonsterMode.Dead)
            {
                return mode == JungleMonsterMode.Inactive;
            }
            previousState = state;
            corpseTimer -= deltaTime;
            if (corpseTimer <= 0f)
            {
                Despawn();
                mode = JungleMonsterMode.Dead;
                return true;
            }
            return false;
        }

        private void TryAttack(ICombatTarget enemy)
        {
            if (!attackCooldown.IsReady || world == null || enemy == null || !enemy.IsAlive)
            {
                return;
            }
            DamageInfo info = new DamageInfo(config.Damage, gameObject, Team.Neutral, AbilitySlot.BasicAttack);
            if (config.AttackType == JungleAttackType.Melee)
            {
                world.QueueDamage(enemy, info);
            }
            else
            {
                world.SpawnProjectile(new ProjectileSpec
                {
                    Motion = ProjectileMotion.Homing,
                    Origin = state.Position + config.ProjectileOriginOffset,
                    Target = enemy,
                    Speed = config.ProjectileSpeed,
                    HitRadius = config.ProjectileHitRadius,
                    MaxLifetime = 3f,
                    Damage = info,
                    ViewPrefab = config.ProjectileViewPrefab
                });
            }
            attackCooldown.Start(config.AttackInterval);
            AttackPerformed?.Invoke(this, enemy);
        }

        private Vector2 Separation(IReadOnlyList<JungleMonster> mates)
        {
            Vector2 push = Vector2.zero;
            if (mates == null || config.SeparationRadius <= 0f)
            {
                return push;
            }
            for (int i = 0; i < mates.Count; i++)
            {
                JungleMonster other = mates[i];
                if (other == null || other == this || !other.IsAlive)
                {
                    continue;
                }
                Vector2 away = state.Position - other.state.Position;
                float d = away.magnitude;
                if (d >= config.SeparationRadius)
                {
                    continue;
                }
                if (d < 1e-4f)
                {
                    away = i % 2 == 0 ? Vector2.right : Vector2.left;
                    d = 1e-4f;
                }
                push += away / d * (1f - d / config.SeparationRadius);
            }
            return push * config.SeparationStrength;
        }

        private void Move(in MovementIntent intent, float speedMultiplier, float deltaTime)
        {
            motor.SpeedMultiplier = speedMultiplier;
            Vector2 facing = state.Facing;
            motor.Simulate(ref state, intent, deltaTime);
            if (mode == JungleMonsterMode.Combat && intent.Kind == MovementIntentKind.Stop)
            {
                state.Facing = facing;
            }
            transform.position = new Vector3(state.Position.x, state.Position.y, transform.position.z);
        }

        private void Teleport(Vector2 position)
        {
            Vector2 facing = state.Facing.sqrMagnitude > 1e-6f ? state.Facing : homeFacing;
            state = CharacterMotorState.Create(position);
            state.Facing = facing;
            previousState = state;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        private void HandleDied(Health _)
        {
            mode = JungleMonsterMode.Dead;
            corpseTimer = config != null ? config.CorpseDuration : 0f;
            state.Velocity = Vector2.zero;
        }

        private void Awake()
        {
            if (target == null) target = GetComponent<CombatTarget>();
            if (health == null) health = GetComponent<Health>();
            if (statusEffects == null) statusEffects = GetComponent<StatusEffectController>();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null)
            {
                return;
            }
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, config.Radius + config.AttackRange);
        }
    }
}
