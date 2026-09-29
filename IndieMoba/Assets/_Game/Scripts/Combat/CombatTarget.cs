using UnityEngine;

namespace IndieMoba.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatTarget : MonoBehaviour, ICombatTarget
    {
        [SerializeField] private CombatWorld world;
        [SerializeField] private Health health;
        [SerializeField] private Team team = Team.Neutral;
        [Min(0.01f)] [SerializeField] private float radius = 0.375f;

        public Vector2 Position => transform.position;
        public float Radius => radius;
        public Team Team => team;
        public bool IsAlive => health != null && health.IsAlive;
        public IDamageable Damageable => health;
        public Transform Transform => transform;
        public Health Health => health;
        public CombatWorld World => world;

        public void SetTeam(Team value)
        {
            team = value;
        }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
            }
            if (world != null)
            {
                world.RegisterTarget(this);
            }
        }

        private void OnDisable()
        {
            if (world != null)
            {
                world.UnregisterTarget(this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = team == Team.Red ? Color.red : team == Team.Blue ? Color.blue : Color.gray;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
