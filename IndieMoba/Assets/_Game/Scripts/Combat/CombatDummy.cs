using UnityEngine;
using IndieMoba.Core;

namespace IndieMoba.Combat
{
    [RequireComponent(typeof(Health))]
    public sealed class CombatDummy : MonoBehaviour, ISimulationSystem
    {
        [SerializeField] private SimulationTickRunner runner;
        [SerializeField] private Health health;
        [Min(0f)] [SerializeField] private float resetDelayAfterDeath = 3f;
        [Tooltip("Heal to full after this many seconds without taking damage. 0 disables.")]
        [Min(0f)] [SerializeField] private float resetDelayAfterHit = 5f;

        private float timeSinceHit;
        private float timeSinceDeath;

        public int SimulationOrder => Core.SimulationOrder.State;

        [ContextMenu("Reset Dummy")]
        public void ResetDummy()
        {
            if (health != null)
            {
                health.ResetHealth();
            }
            timeSinceHit = 0f;
            timeSinceDeath = 0f;
        }

        public void SimulateTick(float deltaTime, int tick)
        {
            if (health == null)
            {
                return;
            }
            if (health.IsDead)
            {
                timeSinceDeath += deltaTime;
                if (timeSinceDeath >= resetDelayAfterDeath)
                {
                    ResetDummy();
                }
                return;
            }
            if (resetDelayAfterHit > 0f && health.Current < health.MaxHealth)
            {
                timeSinceHit += deltaTime;
                if (timeSinceHit >= resetDelayAfterHit)
                {
                    health.Heal(health.MaxHealth);
                    timeSinceHit = 0f;
                }
            }
        }

        private void HandleDamaged(DamageResult result)
        {
            timeSinceHit = 0f;
        }

        private void HandleDied(Health _)
        {
            timeSinceDeath = 0f;
        }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (runner == null) runner = FindAnyObjectByType<SimulationTickRunner>();
            if (runner != null) runner.Register(this);
            health.DamageTaken += HandleDamaged;
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (runner != null) runner.Unregister(this);
            health.DamageTaken -= HandleDamaged;
            health.Died -= HandleDied;
        }
    }
}
