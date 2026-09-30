using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Characters;
using IndieMoba.Combat;

namespace IndieMoba.Minions
{
    public enum MinionAttackType
    {
        Melee = 0,
        Ranged = 1
    }

    [CreateAssetMenu(menuName = "IndieMoba/Minions/Minion Config", fileName = "MinionConfig")]
    public sealed class MinionConfig : ScriptableObject
    {
        [SerializeField] private MinionAttackType attackType = MinionAttackType.Melee;
        [Min(1f)] [SerializeField] private float maxHealth = 300f;
        [Min(0f)] [SerializeField] private float damage = 40f;
        [Min(0f)] [SerializeField] private float attackRange = 1.3125f;
        [Min(0f)] [SerializeField] private float attackInterval = 1f;
        [Min(0.01f)] [SerializeField] private float radius = 0.28125f;
        [Min(0f)] [SerializeField] private float aggroRange = 7.8125f;
        [Min(0f)] [SerializeField] private float leashRange = 10f;
        [Min(0f)] [SerializeField] private float structureDamageMultiplier = 0.6f;
        [Min(0f)] [SerializeField] private float projectileSpeed = 11.875f;
        [Min(0f)] [SerializeField] private float projectileHitRadius = 0.125f;
        [SerializeField] private Vector2 projectileOriginOffset = new Vector2(0f, 0.4f);
        [SerializeField] private GameObject projectileViewPrefab;
        [SerializeField] private CharacterMovementConfig movementConfig;
        [Min(1)] [SerializeField] private int retargetIntervalTicks = 15;
        [Min(0f)] [SerializeField] private float targetStickiness = 1.25f;
        [Min(0f)] [SerializeField] private float corpseDuration = 1f;
        [Min(0f)] [SerializeField] private float waypointReachDistance = 0.5f;
        [Min(0f)] [SerializeField] private float separationRadius = 0.6f;
        [Min(0f)] [SerializeField] private float separationStrength = 1.2f;
        [Min(0f)] [SerializeField] private float structureAvoidanceDistance = 1f;
        [Min(0f)] [SerializeField] private float structureAvoidanceStrength = 1.5f;
        [SerializeField] private LaneTargetCategory[] targetPriority = MinionTargetPolicy.CreateDefaultPriority();

        public MinionAttackType AttackType => attackType;
        public float MaxHealth => maxHealth;
        public float Damage => damage;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;
        public float Radius => radius;
        public float AggroRange => aggroRange;
        public float LeashRange => leashRange;
        public float StructureDamageMultiplier => structureDamageMultiplier;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileHitRadius => projectileHitRadius;
        public Vector2 ProjectileOriginOffset => projectileOriginOffset;
        public GameObject ProjectileViewPrefab => projectileViewPrefab;
        public CharacterMovementConfig MovementConfig => movementConfig;
        public int RetargetIntervalTicks => retargetIntervalTicks;
        public float TargetStickiness => targetStickiness;
        public float CorpseDuration => corpseDuration;
        public float WaypointReachDistance => waypointReachDistance;
        public float SeparationRadius => separationRadius;
        public float SeparationStrength => separationStrength;
        public float StructureAvoidanceDistance => structureAvoidanceDistance;
        public float StructureAvoidanceStrength => structureAvoidanceStrength;
        public IReadOnlyList<LaneTargetCategory> TargetPriority => targetPriority;
    }
}
