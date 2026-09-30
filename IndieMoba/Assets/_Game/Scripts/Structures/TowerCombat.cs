using System;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Structures
{
    [DisallowMultipleComponent]
    public sealed class TowerCombat : MonoBehaviour, ISimulationSystem
    {
        [SerializeField] private StructureDefenseConfig config;
        [SerializeField] private TowerTargeting targeting;
        [SerializeField] private Structure structure;
        [SerializeField] private CombatWorld world;
        [SerializeField] private SimulationTickRunner runner;

        private Cooldown cooldown;

        public event Action<ICombatTarget> Fired;

        public float CooldownNormalized => cooldown.NormalizedProgress;
        public int SimulationOrder => Core.SimulationOrder.Abilities;

        public void SimulateTick(float deltaTime, int tick)
        {
            cooldown.Tick(deltaTime);
            if (structure == null || structure.IsDestroyed || config == null || targeting == null || world == null)
            {
                return;
            }
            ICombatTarget target = targeting.CurrentTarget;
            if (target == null || !target.IsAlive || !cooldown.IsReady)
            {
                return;
            }
            ProjectileSpec spec = new ProjectileSpec
            {
                Motion = ProjectileMotion.Homing,
                Origin = (Vector2)transform.position + config.ProjectileOriginOffset,
                Target = target,
                Speed = config.ProjectileSpeed,
                HitRadius = config.ProjectileHitRadius,
                MaxLifetime = config.ProjectileMaxLifetime,
                Damage = new DamageInfo(target.Kind == CombatTargetKind.Hero ? config.HeroDamage : config.MinionDamage, gameObject, structure.Team, AbilitySlot.BasicAttack),
                ViewPrefab = config.ProjectileViewPrefab
            };
            world.SpawnProjectile(spec);
            cooldown.Start(config.AttackInterval);
            Fired?.Invoke(target);
        }

        private void OnEnable()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<SimulationTickRunner>();
            }
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
            }
            if (runner != null)
            {
                runner.Register(this);
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.Unregister(this);
            }
        }
    }
}
