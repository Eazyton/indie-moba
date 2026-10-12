using System;
using UnityEngine;

namespace IndieMoba.Combat
{
    // Applies another status effect to the target of the bearer's basic attack hits.
    [Serializable]
    public sealed class StatusEffectOnHit
    {
        public StatusEffectDefinition effect;
        public CombatTargetKind[] targetKinds = { CombatTargetKind.Hero, CombatTargetKind.Monster };

        public bool Allows(CombatTargetKind kind)
        {
            if (targetKinds == null)
            {
                return false;
            }
            for (int i = 0; i < targetKinds.Length; i++)
            {
                if (targetKinds[i] == kind)
                {
                    return true;
                }
            }
            return false;
        }
    }

    // Data for one status effect. Stacking rule: one instance per definition; re-applying refreshes
    // the duration (no stacking). Different definitions combine: speed takes the strongest slow,
    // cooldown recovery multiplies, regeneration and periodic damage add.
    [CreateAssetMenu(menuName = "IndieMoba/Combat/Status Effect", fileName = "StatusEffect")]
    public sealed class StatusEffectDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Status";
        [SerializeField] private bool isBuff;
        [Min(0.01f)] [SerializeField] private float duration = 3f;
        [Tooltip("Cooldown seconds recovered per second. 1 = normal, 1.15 = 15% faster recovery.")]
        [Min(0f)] [SerializeField] private float cooldownRecoveryMultiplier = 1f;
        [Min(0f)] [SerializeField] private float healthRegenPerSecond;
        [Min(0f)] [SerializeField] private float damagePerSecond;
        [Min(0.05f)] [SerializeField] private float damageTickInterval = 0.5f;
        [Tooltip("Movement speed multiplier while active. 0.85 = 15% slow.")]
        [Range(0f, 1f)] [SerializeField] private float moveSpeedMultiplier = 1f;
        [SerializeField] private StatusEffectOnHit[] onBasicAttackHit = Array.Empty<StatusEffectOnHit>();
        [Tooltip("Presentation only (aura/debug tint). Never read by gameplay.")]
        [SerializeField] private Color displayColor = Color.white;

        public string DisplayName => displayName;
        public bool IsBuff => isBuff;
        public float Duration => duration;
        public float CooldownRecoveryMultiplier => cooldownRecoveryMultiplier;
        public float HealthRegenPerSecond => healthRegenPerSecond;
        public float DamagePerSecond => damagePerSecond;
        public float DamageTickInterval => damageTickInterval;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public StatusEffectOnHit[] OnBasicAttackHit => onBasicAttackHit;
        public Color DisplayColor => displayColor;
    }
}
