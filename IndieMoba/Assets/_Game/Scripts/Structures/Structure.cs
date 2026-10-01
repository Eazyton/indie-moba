using System;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Structures
{
    public enum RequirementMode
    {
        AllDestroyed = 0,
        AnyDestroyed = 1
    }

    [DisallowMultipleComponent]
    public sealed class Structure : MonoBehaviour, IDamageFilter
    {
        [SerializeField] private CombatWorld world;
        [SerializeField] private Health health;
        [SerializeField] private CombatTarget target;
        [SerializeField] private Structure[] requiredStructures;
        [SerializeField] private RequirementMode requirementMode = RequirementMode.AllDestroyed;
        [SerializeField] private AbilitySlot[] allowedDamageSlots = { AbilitySlot.BasicAttack };
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private bool protectionEnabled = true;
        [Min(0f)] [SerializeField] private float protectionRadius = 7.8125f;
        [Range(0f, 1f)] [SerializeField] private float protectedDamageMultiplier = 0.2f;

        public event Action<Structure> Destroyed;
        public event Action<Structure> Restored;

        public Team Team => target != null ? target.Team : Team.Neutral;
        public Vector2 Position => transform.position;
        public Health Health => health;
        public CombatTarget Target => target;
        public bool IsDestroyed => health == null || health.IsDead;
        public bool IsInvulnerable
        {
            get
            {
                if (requiredStructures == null)
                {
                    return false;
                }
                if (requirementMode == RequirementMode.AnyDestroyed)
                {
                    bool anyRequired = false;
                    for (int i = 0; i < requiredStructures.Length; i++)
                    {
                        Structure required = requiredStructures[i];
                        if (required == null)
                        {
                            continue;
                        }
                        anyRequired = true;
                        if (required.IsDestroyed)
                        {
                            return false;
                        }
                    }
                    return anyRequired;
                }
                for (int i = 0; i < requiredStructures.Length; i++)
                {
                    Structure required = requiredStructures[i];
                    if (required != null && !required.IsDestroyed)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public bool IsProtectionActive => protectionEnabled && !IsDestroyed && world != null && target != null &&
            world.CountHostileUnits(transform.position, protectionRadius, target.Team, CombatTargetKind.Minion) == 0;
        public bool ProtectionEnabled
        {
            get => protectionEnabled;
            set => protectionEnabled = value;
        }
        public float ProtectionRadius => protectionRadius;
        public float ProtectedDamageMultiplier
        {
            get => protectedDamageMultiplier;
            set => protectedDamageMultiplier = Mathf.Clamp01(value);
        }

        public float FilterDamage(in DamageInfo info, out DamageBlockReason reason)
        {
            if (IsInvulnerable)
            {
                reason = DamageBlockReason.Invulnerable;
                return 0f;
            }
            if (!IsSlotAllowed(info.Slot))
            {
                reason = DamageBlockReason.Immune;
                return 0f;
            }
            if (IsProtectionActive)
            {
                reason = DamageBlockReason.Protected;
                return info.Amount * protectedDamageMultiplier;
            }
            reason = DamageBlockReason.None;
            return info.Amount;
        }

        private bool IsSlotAllowed(AbilitySlot slot)
        {
            if (allowedDamageSlots == null)
            {
                return false;
            }
            for (int i = 0; i < allowedDamageSlots.Length; i++)
            {
                if (allowedDamageSlots[i] == slot)
                {
                    return true;
                }
            }
            return false;
        }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
            if (target == null)
            {
                target = GetComponent<CombatTarget>();
            }
            if (blockingCollider == null)
            {
                blockingCollider = GetComponent<Collider2D>();
            }
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDied;
                health.Revived += HandleRevived;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
                health.Revived -= HandleRevived;
            }
        }

        private void HandleDied(Health _)
        {
            if (blockingCollider != null)
            {
                blockingCollider.enabled = false;
            }
            Destroyed?.Invoke(this);
        }

        private void HandleRevived(Health _)
        {
            if (blockingCollider != null)
            {
                blockingCollider.enabled = true;
            }
            Restored?.Invoke(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, protectionRadius);
        }
    }
}
