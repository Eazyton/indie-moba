using UnityEngine;
using IndieMoba.Characters;

namespace IndieMoba.Jungle
{
    public enum JungleAttackType
    {
        Melee = 0,
        Ranged = 1
    }

    // Gameplay stats of one monster archetype (prototype playtest values, not final balance).
    [CreateAssetMenu(menuName = "IndieMoba/Jungle/Monster Config", fileName = "JungleMonsterConfig")]
    public sealed class JungleMonsterConfig : ScriptableObject
    {
        [SerializeField] private string displayName = "Monster";
        [SerializeField] private JungleAttackType attackType = JungleAttackType.Melee;
        [Min(1f)] [SerializeField] private float maxHealth = 500f;
        [Min(0f)] [SerializeField] private float damage = 15f;
        [Min(0.05f)] [SerializeField] private float attackInterval = 1.2f;
        [Tooltip("Edge-to-edge distance at which the monster can attack.")]
        [Min(0f)] [SerializeField] private float attackRange = 1f;
        [Min(0.01f)] [SerializeField] private float radius = 0.4f;
        [Min(0f)] [SerializeField] private float projectileSpeed = 10f;
        [Min(0f)] [SerializeField] private float projectileHitRadius = 0.125f;
        [SerializeField] private Vector2 projectileOriginOffset = new Vector2(0f, 0.4f);
        [Tooltip("Presentation only: projectile view prefab, never read by gameplay logic.")]
        [SerializeField] private GameObject projectileViewPrefab;
        [SerializeField] private CharacterMovementConfig movementConfig;
        [Min(0f)] [SerializeField] private float corpseDuration = 0.75f;
        [Min(0f)] [SerializeField] private float separationRadius = 0.9f;
        [Min(0f)] [SerializeField] private float separationStrength = 1f;

        public string DisplayName => displayName;
        public JungleAttackType AttackType => attackType;
        public float MaxHealth => maxHealth;
        public float Damage => damage;
        public float AttackInterval => attackInterval;
        public float AttackRange => attackRange;
        public float Radius => radius;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileHitRadius => projectileHitRadius;
        public Vector2 ProjectileOriginOffset => projectileOriginOffset;
        public GameObject ProjectileViewPrefab => projectileViewPrefab;
        public CharacterMovementConfig MovementConfig => movementConfig;
        public float CorpseDuration => corpseDuration;
        public float SeparationRadius => separationRadius;
        public float SeparationStrength => separationStrength;
    }
}
