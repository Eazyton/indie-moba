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

        public DamageResult(DamageInfo info, float absorbed, float healthDamage, bool killed)
        {
            Info = info;
            Absorbed = absorbed;
            HealthDamage = healthDamage;
            Killed = killed;
        }

        public bool Applied => Absorbed > 0f || HealthDamage > 0f;
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        DamageResult ApplyDamage(in DamageInfo info);
    }

    public interface ICombatTarget
    {
        Vector2 Position { get; }
        float Radius { get; }
        Team Team { get; }
        bool IsAlive { get; }
        IDamageable Damageable { get; }
        Transform Transform { get; }
    }
}
