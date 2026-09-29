using System;
using UnityEngine;
using IndieMoba.Core;

namespace IndieMoba.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable, ISimulationSystem
    {
        [SerializeField] private SimulationTickRunner runner;
        [Min(1f)] [SerializeField] private float maxHealth = 640f;

        private float current;
        private float shield;
        private float shieldRemaining;
        private bool dead;

        public event Action<DamageResult> DamageTaken;
        public event Action<float> Healed;
        public event Action<float> ShieldChanged;
        public event Action<Health> Died;
        public event Action<Health> Revived;

        public float MaxHealth => maxHealth;
        public float Current => current;
        public float Normalized => current / maxHealth;
        public float Shield => shield;
        public float ShieldRemaining => shieldRemaining;
        public bool IsDead => dead;
        public bool IsAlive => !dead;
        public int SimulationOrder => Core.SimulationOrder.State;

        public DamageResult ApplyDamage(in DamageInfo info)
        {
            if (dead || info.Amount <= 0f)
            {
                return new DamageResult(info, 0f, 0f, false);
            }
            float remaining = info.Amount;
            float absorbed = Mathf.Min(shield, remaining);
            if (absorbed > 0f)
            {
                SetShield(shield - absorbed, shieldRemaining);
                remaining -= absorbed;
            }
            float healthDamage = Mathf.Min(current, remaining);
            current -= healthDamage;
            bool killed = current <= 0f;
            DamageResult result = new DamageResult(info, absorbed, healthDamage, killed);
            DamageTaken?.Invoke(result);
            if (killed)
            {
                current = 0f;
                dead = true;
                SetShield(0f, 0f);
                Died?.Invoke(this);
            }
            return result;
        }

        public void Heal(float amount)
        {
            if (dead || amount <= 0f)
            {
                return;
            }
            float healed = Mathf.Min(maxHealth - current, amount);
            if (healed <= 0f)
            {
                return;
            }
            current += healed;
            Healed?.Invoke(healed);
        }

        public void ApplyShield(float amount, float duration)
        {
            if (dead)
            {
                return;
            }
            SetShield(Mathf.Max(0f, amount), Mathf.Max(0f, duration));
        }

        public void ResetHealth()
        {
            bool wasDead = dead;
            dead = false;
            current = maxHealth;
            SetShield(0f, 0f);
            if (wasDead)
            {
                Revived?.Invoke(this);
            }
        }

        public void SimulateTick(float deltaTime, int tick)
        {
            if (shieldRemaining <= 0f)
            {
                return;
            }
            shieldRemaining -= deltaTime;
            if (shieldRemaining <= 0f)
            {
                SetShield(0f, 0f);
            }
        }

        private void SetShield(float amount, float duration)
        {
            float previous = shield;
            shield = amount;
            shieldRemaining = amount > 0f ? duration : 0f;
            if (!Mathf.Approximately(previous, shield))
            {
                ShieldChanged?.Invoke(shield);
            }
        }

        private void Awake()
        {
            current = maxHealth;
        }

        private void OnEnable()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<SimulationTickRunner>();
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
