using UnityEngine;
using IndieMoba.Core;

namespace IndieMoba.Combat
{
    public enum Team
    {
        Neutral = 0,
        Blue = 1,
        Red = 2
    }

    public enum CombatTargetKind
    {
        Hero = 0,
        Minion = 1,
        Structure = 2,
        Other = 3
    }

    public enum DamageBlockReason
    {
        None = 0,
        Invulnerable = 1,
        Immune = 2,
        Protected = 3
    }

    public static class TeamRules
    {
        public static bool AreHostile(Team a, Team b) => a != b;
    }

    public struct Cooldown
    {
        public float Duration;
        public float Remaining;

        public bool IsReady => Remaining <= 0f;
        public float NormalizedRemaining => Duration > 0f ? Mathf.Clamp01(Remaining / Duration) : 0f;
        public float NormalizedProgress => 1f - NormalizedRemaining;

        public void Start(float duration)
        {
            Duration = Mathf.Max(0f, duration);
            Remaining = Duration;
        }

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f)
            {
                Remaining = Mathf.Max(0f, Remaining - deltaTime);
            }
        }

        public void Reset()
        {
            Remaining = 0f;
        }
    }

    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly GameObject Source;
        public readonly Team SourceTeam;
        public readonly AbilitySlot Slot;

        public DamageInfo(float amount, GameObject source, Team sourceTeam, AbilitySlot slot)
        {
            Amount = amount;
            Source = source;
            SourceTeam = sourceTeam;
            Slot = slot;
        }
    }

    public readonly struct DamageResult
    {
        public readonly DamageInfo Info;
        public readonly float Absorbed;
        public readonly float HealthDamage;
        public readonly bool Killed;
        public readonly float Mitigated;
        public readonly DamageBlockReason BlockReason;

        public DamageResult(DamageInfo info, float absorbed, float healthDamage, bool killed)
            : this(info, absorbed, healthDamage, killed, 0f, DamageBlockReason.None)
        {
        }

        public DamageResult(DamageInfo info, float absorbed, float healthDamage, bool killed, float mitigated, DamageBlockReason blockReason)
        {
            Info = info;
            Absorbed = absorbed;
            HealthDamage = healthDamage;
            Killed = killed;
            Mitigated = mitigated;
            BlockReason = blockReason;
        }

        public bool Applied => Absorbed > 0f || HealthDamage > 0f;
        public bool Blocked => BlockReason == DamageBlockReason.Invulnerable || BlockReason == DamageBlockReason.Immune;
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        DamageResult ApplyDamage(in DamageInfo info);
    }

    public interface IDamageFilter
    {
        bool IsInvulnerable { get; }
        float FilterDamage(in DamageInfo info, out DamageBlockReason reason);
    }

    public interface ICombatTarget
    {
        Vector2 Position { get; }
        float Radius { get; }
        Team Team { get; }
        bool IsAlive { get; }
        IDamageable Damageable { get; }
        Transform Transform { get; }
        CombatTargetKind Kind { get; }
        bool IsInvulnerable { get; }
    }
}
