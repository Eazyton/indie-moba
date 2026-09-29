using UnityEngine;

namespace IndieMoba.Characters
{
    [CreateAssetMenu(menuName = "IndieMoba/Characters/Movement Config", fileName = "CharacterMovementConfig")]
    public sealed class CharacterMovementConfig : ScriptableObject
    {
        [Min(0f)] [SerializeField] private float maxSpeed = 5.78f;
        [Min(0f)] [SerializeField] private float acceleration = 90f;
        [Min(0f)] [SerializeField] private float deceleration = 120f;
        [Min(0f)] [SerializeField] private float stopDistance = 0.1f;
        [Min(0f)] [SerializeField] private float collisionRadius = 0.375f;
        [Min(0f)] [SerializeField] private float skinWidth = 0.02f;
        [Min(1)] [SerializeField] private int maxSlideIterations = 3;
        [SerializeField] private LayerMask obstacleMask = 0;
        [Min(0f)] [SerializeField] private float facingThreshold = 0.05f;

        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float StopDistance => stopDistance;
        public float CollisionRadius => collisionRadius;
        public float SkinWidth => skinWidth;
        public int MaxSlideIterations => maxSlideIterations;
        public LayerMask ObstacleMask => obstacleMask;
        public float FacingThreshold => facingThreshold;
    }
}
