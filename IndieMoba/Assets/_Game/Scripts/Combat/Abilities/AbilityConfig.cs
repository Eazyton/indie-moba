using UnityEngine;

namespace IndieMoba.Combat
{
    public enum AbilityKind
    {
        BasicAttack = 0,
        LinearProjectile = 1,
        Shield = 2,
        Dash = 3,
        DelayedArea = 4
    }

    [CreateAssetMenu(menuName = "IndieMoba/Combat/Ability Config", fileName = "AbilityConfig")]
    public sealed class AbilityConfig : ScriptableObject
    {
        [SerializeField] private string displayName = "Ability";
        [SerializeField] private AbilityKind kind = AbilityKind.LinearProjectile;
        [Min(0f)] [SerializeField] private float cooldown = 1f;
        [Min(0f)] [SerializeField] private float damage;
        [Tooltip("Attack range, projectile travel distance or dash distance.")]
        [Min(0f)] [SerializeField] private float range;
        [Min(0f)] [SerializeField] private float speed;
        [Tooltip("Projectile hit radius or area radius.")]
        [Min(0f)] [SerializeField] private float radius;
        [Tooltip("Shield or dash duration.")]
        [Min(0f)] [SerializeField] private float duration;
        [Tooltip("Shield amount.")]
        [Min(0f)] [SerializeField] private float amount;
        [Min(0f)] [SerializeField] private float castRange;
        [Min(0f)] [SerializeField] private float delay;
        [Tooltip("Basic attack aim cone half-angle.")]
        [Range(0f, 180f)] [SerializeField] private float halfArcDegrees = 43f;
        [Tooltip("Basic attack: targets this close ignore the aim cone.")]
        [Min(0f)] [SerializeField] private float closeRange = 0.875f;
        [Tooltip("Basic attack: when no valid target exists, still attack with a linear projectile toward the aim direction.")]
        [SerializeField] private bool canBasicAttackWithoutTarget;
        [Tooltip("Minimum aim distance required to cast.")]
        [Min(0f)] [SerializeField] private float minAimDistance;
        [Tooltip("Presentation only. Gameplay never reads it.")]
        [SerializeField] private GameObject projectileViewPrefab;

        public string DisplayName => displayName;
        public AbilityKind Kind => kind;
        public float Cooldown => cooldown;
        public float Damage => damage;
        public float Range => range;
        public float Speed => speed;
        public float Radius => radius;
        public float Duration => duration;
        public float Amount => amount;
        public float CastRange => castRange;
        public float Delay => delay;
        public float HalfArcRadians => halfArcDegrees * Mathf.Deg2Rad;
        public float CloseRange => closeRange;
        public bool CanBasicAttackWithoutTarget => canBasicAttackWithoutTarget;
        public float MinAimDistance => minAimDistance;
        public GameObject ProjectileViewPrefab => projectileViewPrefab;
    }
}
