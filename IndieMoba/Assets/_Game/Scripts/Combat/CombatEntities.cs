using UnityEngine;

namespace IndieMoba.Combat
{
    public enum ProjectileMotion
    {
        Homing = 0,
        Linear = 1
    }

    public struct ProjectileSpec
    {
        public ProjectileMotion Motion;
        public Vector2 Origin;
        public Vector2 Direction;
        public ICombatTarget Target;
        public float Speed;
        public float HitRadius;
        public float MaxDistance;
        public float MaxLifetime;
        public DamageInfo Damage;
        public GameObject ViewPrefab;
    }

    public sealed class ProjectileState
    {
        public int Id;
        public ProjectileMotion Motion;
        public Vector2 Position;
        public Vector2 PreviousPosition;
        public Vector2 Direction;
        public ICombatTarget Target;
        public Vector2 LastTargetPosition;
        public float Speed;
        public float HitRadius;
        public float MaxDistance;
        public float Traveled;
        public float Lifetime;
        public float MaxLifetime;
        public DamageInfo Damage;
        public GameObject ViewPrefab;
        public bool Alive;
    }

    public struct AreaSpec
    {
        public Vector2 Center;
        public float Radius;
        public float Delay;
        public DamageInfo Damage;
    }

    public sealed class DelayedAreaState
    {
        public int Id;
        public Vector2 Center;
        public float Radius;
        public float Delay;
        public float Elapsed;
        public DamageInfo Damage;
        public bool Resolved;

        public float NormalizedProgress => Delay > 0f ? Mathf.Clamp01(Elapsed / Delay) : 1f;
    }
}
