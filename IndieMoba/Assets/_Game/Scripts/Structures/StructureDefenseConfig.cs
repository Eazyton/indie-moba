using UnityEngine;

namespace IndieMoba.Structures
{
    public abstract class StructureDefenseConfig : ScriptableObject
    {
        [Min(0f)] [SerializeField] private float range = 7.8125f;
        [Min(0f)] [SerializeField] private float heroDamage = 145f;
        [Min(0f)] [SerializeField] private float minionDamage = 115f;
        [Min(0f)] [SerializeField] private float attackInterval = 1.1f;
        [Min(0f)] [SerializeField] private float projectileSpeed = 13.4375f;
        [Min(0f)] [SerializeField] private float projectileHitRadius = 0.1875f;
        [Min(0f)] [SerializeField] private float projectileMaxLifetime = 3f;
        [SerializeField] private Vector2 projectileOriginOffset = new Vector2(0f, 1.3f);
        [Min(0f)] [SerializeField] private float warningRange = 13.125f;
        [SerializeField] private GameObject projectileViewPrefab;

        public float Range => range;
        public float HeroDamage => heroDamage;
        public float MinionDamage => minionDamage;
        public float AttackInterval => attackInterval;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileHitRadius => projectileHitRadius;
        public float ProjectileMaxLifetime => projectileMaxLifetime;
        public Vector2 ProjectileOriginOffset => projectileOriginOffset;
        public float WarningRange => warningRange;
        public GameObject ProjectileViewPrefab => projectileViewPrefab;
    }
}
