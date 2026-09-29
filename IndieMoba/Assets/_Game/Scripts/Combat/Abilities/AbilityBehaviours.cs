using UnityEngine;
using IndieMoba.Characters;
using IndieMoba.Core;

namespace IndieMoba.Combat
{
    public enum AbilityFailReason
    {
        None = 0,
        NotConfigured,
        OnCooldown,
        CasterDead,
        Busy,
        NoTarget,
        InvalidAim
    }

    public readonly struct AbilityContext
    {
        public readonly AbilitySlot Slot;
        public readonly GameObject Caster;
        public readonly Vector2 Position;
        public readonly Vector2 AimOrigin;
        public readonly Vector2 AimPoint;
        public readonly bool HasAim;
        public readonly Vector2 Facing;
        public readonly Team Team;
        public readonly CombatWorld World;
        public readonly Health Health;
        public readonly HeroActor Actor;
        public readonly ICombatTarget Self;
        public readonly BasicAttackTargeting Targeting;
        public readonly ICombatTarget ExplicitTarget;

        public AbilityContext(AbilitySlot slot, GameObject caster, Vector2 position, Vector2 aimOrigin, Vector2 aimPoint, bool hasAim,
            Vector2 facing, Team team, CombatWorld world, Health health, HeroActor actor, ICombatTarget self,
            BasicAttackTargeting targeting = BasicAttackTargeting.AutoTarget, ICombatTarget explicitTarget = null)
        {
            Targeting = targeting;
            ExplicitTarget = explicitTarget;
            Slot = slot;
            Caster = caster;
            Position = position;
            AimOrigin = aimOrigin;
            AimPoint = aimPoint;
            HasAim = hasAim;
            Facing = facing;
            Team = team;
            World = world;
            Health = health;
            Actor = actor;
            Self = self;
        }

        public Vector2 AimDirection
        {
            get
            {
                Vector2 dir = HasAim ? AimPoint - AimOrigin : Facing;
                return dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector2.right;
            }
        }

        public DamageInfo MakeDamage(float amount) => new DamageInfo(amount, Caster, Team, Slot);
    }

    public interface IAbilityBehaviour
    {
        bool IsActive { get; }
        bool TryExecute(in AbilityContext context, AbilityConfig config, out AbilityFailReason reason);
        void Tick(in AbilityContext context, AbilityConfig config, float deltaTime);
        void Cancel();
    }

    public static class AbilityBehaviourFactory
    {
        public static IAbilityBehaviour Create(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.BasicAttack: return new BasicAttackBehaviour();
                case AbilityKind.LinearProjectile: return new LinearProjectileBehaviour();
                case AbilityKind.Shield: return new ShieldBehaviour();
                case AbilityKind.Dash: return new DashBehaviour();
                case AbilityKind.DelayedArea: return new DelayedAreaBehaviour();
                default: return null;
            }
        }
    }

    public sealed class BasicAttackBehaviour : IAbilityBehaviour
    {
        public ICombatTarget LastTarget { get; private set; }
        public bool IsActive => false;

        public bool TryExecute(in AbilityContext context, AbilityConfig config, out AbilityFailReason reason)
        {
            BasicAttackQuery query = new BasicAttackQuery(context.Position, context.AimDirection, config.Range,
                config.HalfArcRadians, config.CloseRange, context.Team, context.Self);
            ICombatTarget target = ResolveTarget(context, query);
            if (target == null)
            {
                reason = AbilityFailReason.NoTarget;
                return false;
            }
            LastTarget = target;
            ProjectileSpec spec = new ProjectileSpec
            {
                Motion = ProjectileMotion.Homing,
                Origin = context.Position,
                Target = target,
                Speed = config.Speed,
                HitRadius = config.Radius,
                MaxLifetime = 3f,
                Damage = context.MakeDamage(config.Damage),
                ViewPrefab = config.ProjectileViewPrefab
            };
            context.World.SpawnProjectile(spec);
            reason = AbilityFailReason.None;
            return true;
        }

        private static ICombatTarget ResolveTarget(in AbilityContext context, in BasicAttackQuery query)
        {
            switch (context.Targeting)
            {
                case BasicAttackTargeting.ExplicitTarget:
                    return IsValidExplicitTarget(context.ExplicitTarget, query) ? context.ExplicitTarget : null;
                case BasicAttackTargeting.AttackMoveTarget:
                case BasicAttackTargeting.AutoTarget:
                default:
                    return context.World.FindBasicAttackTarget(query);
            }
        }

        private static bool IsValidExplicitTarget(ICombatTarget target, in BasicAttackQuery query)
        {
            if (target == null || target == query.Self || !target.IsAlive || !TeamRules.AreHostile(query.Team, target.Team))
            {
                return false;
            }
            return (target.Position - query.Origin).sqrMagnitude <= query.Range * query.Range;
        }

        public void Tick(in AbilityContext context, AbilityConfig config, float deltaTime) { }
        public void Cancel() { }
    }

    public sealed class LinearProjectileBehaviour : IAbilityBehaviour
    {
        public bool IsActive => false;

        public bool TryExecute(in AbilityContext context, AbilityConfig config, out AbilityFailReason reason)
        {
            ProjectileSpec spec = new ProjectileSpec
            {
                Motion = ProjectileMotion.Linear,
                Origin = context.Position,
                Direction = context.AimDirection,
                Speed = config.Speed,
                HitRadius = config.Radius,
                MaxDistance = config.Range,
                MaxLifetime = config.Speed > 0f ? config.Range / config.Speed + 0.5f : 5f,
                Damage = context.MakeDamage(config.Damage),
                ViewPrefab = config.ProjectileViewPrefab
            };
            context.World.SpawnProjectile(spec);
            reason = AbilityFailReason.None;
            return true;
        }

        public void Tick(in AbilityContext context, AbilityConfig config, float deltaTime) { }
        public void Cancel() { }
    }

    public sealed class ShieldBehaviour : IAbilityBehaviour
    {
        public bool IsActive => false;

        public bool TryExecute(in AbilityContext context, AbilityConfig config, out AbilityFailReason reason)
        {
            context.Health.ApplyShield(config.Amount, config.Duration);
            reason = AbilityFailReason.None;
            return true;
        }

        public void Tick(in AbilityContext context, AbilityConfig config, float deltaTime) { }
        public void Cancel() { }
    }

    public sealed class DashBehaviour : IAbilityBehaviour
    {
        private HeroActor activeActor;

        public bool IsActive => activeActor != null && activeActor.IsForcedMoving;

        public bool TryExecute(in AbilityContext context, AbilityConfig config, out AbilityFailReason reason)
        {
            if (context.Actor == null || config.Duration <= 0f)
            {
                reason = AbilityFailReason.NotConfigured;
                return false;
            }
            if (context.HasAim && (context.AimPoint - context.Position).magnitude <= config.MinAimDistance)
            {
                reason = AbilityFailReason.InvalidAim;
                return false;
            }
            Vector2 dir = context.HasAim ? context.AimPoint - context.Position : context.Facing;
            dir = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector2.right;
            context.Actor.StartForcedMove(dir * (config.Range / config.Duration), config.Duration);
            activeActor = context.Actor;
            reason = AbilityFailReason.None;
            return true;
        }

        public void Tick(in AbilityContext context, AbilityConfig config, float deltaTime)
        {
            if (activeActor != null && !activeActor.IsForcedMoving)
            {
                activeActor = null;
            }
        }

        public void Cancel()
        {
            if (activeActor != null)
            {
                activeActor.CancelForcedMove();
                activeActor = null;
            }
        }
    }

    public sealed class DelayedAreaBehaviour : IAbilityBehaviour
    {
        public bool IsActive => false;

        public bool TryExecute(in AbilityContext context, AbilityConfig config, out AbilityFailReason reason)
        {
            Vector2 target = context.HasAim ? context.AimPoint : context.Position + context.Facing * config.CastRange;
            Vector2 offset = Vector2.ClampMagnitude(target - context.Position, config.CastRange);
            AreaSpec spec = new AreaSpec
            {
                Center = context.Position + offset,
                Radius = config.Radius,
                Delay = config.Delay,
                Damage = context.MakeDamage(config.Damage)
            };
            context.World.SpawnDelayedArea(spec);
            reason = AbilityFailReason.None;
            return true;
        }

        public void Tick(in AbilityContext context, AbilityConfig config, float deltaTime) { }
        public void Cancel() { }
    }
}
