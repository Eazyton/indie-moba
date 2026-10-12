using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Characters;
using IndieMoba.Core;

namespace IndieMoba.Combat
{
    public sealed class StatusEffectInstance
    {
        public StatusEffectDefinition Definition;
        public float Remaining;
        public GameObject Source;
        public Team SourceTeam;
        public float TickAccumulator;

        public float NormalizedRemaining => Definition != null && Definition.Duration > 0f ? Mathf.Clamp01(Remaining / Definition.Duration) : 0f;
    }

    // Owns the status effects of one unit. Ticked by the shared SimulationTickRunner (Status stage):
    // durations, regeneration and periodic damage all advance with the simulation, never with frame time.
    // Periodic damage is queued in CombatWorld with AbilitySlot.StatusEffect, so it never triggers
    // basic-attack on-hit effects and is rejected by structures.
    [DisallowMultipleComponent]
    public sealed class StatusEffectController : MonoBehaviour, ISimulationSystem
    {
        private const float TickEpsilon = 1e-4f;

        private static readonly List<StatusEffectController> active = new List<StatusEffectController>();

        [SerializeField] private SimulationTickRunner runner;
        [SerializeField] private CombatWorld world;
        [SerializeField] private Health health;
        [SerializeField] private CombatTarget target;
        [SerializeField] private HeroCombat heroCombat;
        [SerializeField] private HeroActor heroActor;

        private readonly List<StatusEffectInstance> effects = new List<StatusEffectInstance>();
        private readonly List<StatusEffectInstance> removed = new List<StatusEffectInstance>();
        private readonly List<StatusEffectOnHit> onHitBuffer = new List<StatusEffectOnHit>();
        private float cooldownRecoveryMultiplier = 1f;
        private float moveSpeedMultiplier = 1f;

        public static IReadOnlyList<StatusEffectController> Active => active;

        public event Action<StatusEffectController, StatusEffectInstance, bool> EffectApplied;
        public event Action<StatusEffectController, StatusEffectInstance> EffectRemoved;

        public int SimulationOrder => Core.SimulationOrder.Status;
        public IReadOnlyList<StatusEffectInstance> Effects => effects;
        public float CooldownRecoveryMultiplier => cooldownRecoveryMultiplier;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public CombatTarget Target => target;
        public Health Health => health;

        public StatusEffectInstance Find(StatusEffectDefinition definition)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Definition == definition)
                {
                    return effects[i];
                }
            }
            return null;
        }

        public bool Has(StatusEffectDefinition definition) => Find(definition) != null;

        // Applies or refreshes (same definition never stacks). Returns false when the unit is dead.
        public bool Apply(StatusEffectDefinition definition, GameObject source, Team sourceTeam)
        {
            if (definition == null || (health != null && health.IsDead))
            {
                return false;
            }
            StatusEffectInstance instance = Find(definition);
            bool refreshed = instance != null;
            if (!refreshed)
            {
                instance = new StatusEffectInstance { Definition = definition, TickAccumulator = 0f };
                effects.Add(instance);
            }
            // Refresh keeps the periodic tick accumulator so frequent re-application never delays damage ticks.
            instance.Remaining = definition.Duration;
            instance.Source = source;
            instance.SourceTeam = sourceTeam;
            RecalculateModifiers();
            EffectApplied?.Invoke(this, instance, refreshed);
            return true;
        }

        public bool Remove(StatusEffectDefinition definition)
        {
            StatusEffectInstance instance = Find(definition);
            if (instance == null)
            {
                return false;
            }
            effects.Remove(instance);
            RecalculateModifiers();
            EffectRemoved?.Invoke(this, instance);
            return true;
        }

        public void ClearAll()
        {
            if (effects.Count == 0)
            {
                RecalculateModifiers();
                return;
            }
            removed.Clear();
            removed.AddRange(effects);
            effects.Clear();
            RecalculateModifiers();
            for (int i = 0; i < removed.Count; i++)
            {
                EffectRemoved?.Invoke(this, removed[i]);
            }
            removed.Clear();
        }

        public void SimulateTick(float deltaTime, int tick)
        {
            if (effects.Count == 0)
            {
                return;
            }
            if (health != null && health.IsDead)
            {
                ClearAll();
                return;
            }
            removed.Clear();
            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectInstance instance = effects[i];
                StatusEffectDefinition definition = instance.Definition;
                if (definition == null)
                {
                    removed.Add(instance);
                    continue;
                }
                float step = Mathf.Min(deltaTime, Mathf.Max(0f, instance.Remaining));
                if (definition.HealthRegenPerSecond > 0f && health != null)
                {
                    health.Heal(definition.HealthRegenPerSecond * step);
                }
                if (definition.DamagePerSecond > 0f && world != null && target != null)
                {
                    float interval = definition.DamageTickInterval;
                    instance.TickAccumulator += step;
                    while (instance.TickAccumulator + TickEpsilon >= interval)
                    {
                        instance.TickAccumulator -= interval;
                        DamageInfo info = new DamageInfo(definition.DamagePerSecond * interval, instance.Source, instance.SourceTeam, AbilitySlot.StatusEffect);
                        world.QueueDamage(target, info);
                    }
                }
                instance.Remaining -= deltaTime;
                if (instance.Remaining <= TickEpsilon)
                {
                    removed.Add(instance);
                }
            }
            if (removed.Count == 0)
            {
                return;
            }
            for (int i = 0; i < removed.Count; i++)
            {
                effects.Remove(removed[i]);
            }
            RecalculateModifiers();
            for (int i = 0; i < removed.Count; i++)
            {
                EffectRemoved?.Invoke(this, removed[i]);
            }
            removed.Clear();
        }

        private void RecalculateModifiers()
        {
            float cooldown = 1f;
            float speed = 1f;
            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectDefinition definition = effects[i].Definition;
                if (definition == null)
                {
                    continue;
                }
                cooldown *= definition.CooldownRecoveryMultiplier;
                speed = Mathf.Min(speed, definition.MoveSpeedMultiplier);
            }
            cooldownRecoveryMultiplier = cooldown;
            moveSpeedMultiplier = speed;
            PushModifiers();
        }

        private void PushModifiers()
        {
            if (heroCombat != null)
            {
                heroCombat.CooldownRecoveryRate = cooldownRecoveryMultiplier;
            }
            if (heroActor != null)
            {
                heroActor.SpeedMultiplier = moveSpeedMultiplier;
            }
        }

        private void HandleBasicAttackHit(ICombatTarget hitTarget, DamageResult result)
        {
            if (hitTarget == null || !hitTarget.IsAlive || result.Info.Slot != AbilitySlot.BasicAttack || effects.Count == 0)
            {
                return;
            }
            onHitBuffer.Clear();
            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectDefinition definition = effects[i].Definition;
                if (definition == null || definition.OnBasicAttackHit == null)
                {
                    continue;
                }
                for (int r = 0; r < definition.OnBasicAttackHit.Length; r++)
                {
                    StatusEffectOnHit rule = definition.OnBasicAttackHit[r];
                    if (rule != null && rule.effect != null && rule.Allows(hitTarget.Kind))
                    {
                        onHitBuffer.Add(rule);
                    }
                }
            }
            if (onHitBuffer.Count == 0 || hitTarget.Transform == null)
            {
                return;
            }
            StatusEffectController other = hitTarget.Transform.GetComponent<StatusEffectController>();
            if (other == null || other == this)
            {
                return;
            }
            Team team = target != null ? target.Team : Team.Neutral;
            for (int i = 0; i < onHitBuffer.Count; i++)
            {
                other.Apply(onHitBuffer[i].effect, gameObject, team);
            }
            onHitBuffer.Clear();
        }

        private void HandleDied(Health _)
        {
            ClearAll();
        }

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (target == null) target = GetComponent<CombatTarget>();
            if (heroCombat == null) heroCombat = GetComponent<HeroCombat>();
            if (heroActor == null) heroActor = GetComponent<HeroActor>();
        }

        private void OnEnable()
        {
            if (runner == null) runner = FindAnyObjectByType<SimulationTickRunner>();
            if (world == null) world = FindAnyObjectByType<CombatWorld>();
            if (runner != null) runner.Register(this);
            if (health != null) health.Died += HandleDied;
            if (heroCombat != null) heroCombat.BasicAttackHit += HandleBasicAttackHit;
            if (!active.Contains(this)) active.Add(this);
        }

        private void OnDisable()
        {
            if (runner != null) runner.Unregister(this);
            if (health != null) health.Died -= HandleDied;
            if (heroCombat != null) heroCombat.BasicAttackHit -= HandleBasicAttackHit;
            active.Remove(this);
            effects.Clear();
            RecalculateModifiers();
        }
    }
}
