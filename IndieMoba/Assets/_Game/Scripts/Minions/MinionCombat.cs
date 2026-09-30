using System;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Minions
{
    [DisallowMultipleComponent]
    public sealed class MinionCombat : MonoBehaviour
    {
        private Cooldown cooldown;
        private MinionConfig config;
        private CombatWorld world;
        private Team team;

        public event Action<ICombatTarget> AttackPerformed;

        public float CooldownNormalized => cooldown.NormalizedProgress;

        public void Initialize(MinionConfig config, CombatWorld world, Team team)
        {
            this.config = config;
            this.world = world;
            this.team = team;
        }

        public void Tick(float deltaTime)
        {
            cooldown.Tick(deltaTime);
        }

        public bool TryAttack(ICombatTarget target)
        {
            if (target == null || !target.IsAlive || !cooldown.IsReady || config == null || world == null)
            {
                return false;
            }
            float amount = config.Damage * (target.Kind == CombatTargetKind.Structure ? config.StructureDamageMultiplier : 1f);
            DamageInfo info = new DamageInfo(amount, gameObject, team, AbilitySlot.BasicAttack);
            if (config.AttackType == MinionAttackType.Melee)
            {
                world.QueueDamage(target, info);
            }
            else
            {
                ProjectileSpec spec = new ProjectileSpec
                {
                    Motion = ProjectileMotion.Homing,
                    Origin = (Vector2)transform.position + config.ProjectileOriginOffset,
                    Target = target,
                    Speed = config.ProjectileSpeed,
                    HitRadius = config.ProjectileHitRadius,
                    MaxLifetime = 3f,
                    Damage = info,
                    ViewPrefab = config.ProjectileViewPrefab
                };
                world.SpawnProjectile(spec);
            }
            cooldown.Start(config.AttackInterval);
            AttackPerformed?.Invoke(target);
            return true;
        }

        public void ResetCooldown()
        {
            cooldown.Reset();
        }
    }
}
