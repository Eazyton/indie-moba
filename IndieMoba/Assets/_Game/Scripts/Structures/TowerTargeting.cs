using System;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Structures
{
    [DisallowMultipleComponent]
    public sealed class TowerTargeting : MonoBehaviour, ISimulationSystem
    {
        [SerializeField] private StructureDefenseConfig config;
        [SerializeField] private Structure structure;
        [SerializeField] private CombatWorld world;
        [SerializeField] private SimulationTickRunner runner;

        private readonly TowerTargetPolicy policy = new TowerTargetPolicy();
        private ICombatTarget current;

        public event Action<ICombatTarget, ICombatTarget> TargetChanged;

        public ICombatTarget CurrentTarget => current;
        public StructureDefenseConfig Config => config;
        public Structure Structure => structure;
        public int SimulationOrder => Core.SimulationOrder.Actors;

        public void SimulateTick(float deltaTime, int tick)
        {
            if (structure == null || structure.IsDestroyed || config == null || world == null)
            {
                SetTarget(null);
                return;
            }
            LaneTargetQuery query = new LaneTargetQuery(structure.Position, config.Range, structure.Team, structure.Target, current, 0f);
            SetTarget(policy.Select(query, world.Targets));
        }

        private void SetTarget(ICombatTarget next)
        {
            if (current == next)
            {
                return;
            }
            ICombatTarget previous = current;
            current = next;
            TargetChanged?.Invoke(previous, next);
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

        private void OnDrawGizmosSelected()
        {
            if (config == null)
            {
                return;
            }
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, config.Range);
        }
    }
}
