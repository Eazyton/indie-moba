using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Minions;

namespace IndieMoba.Presentation
{
    [DefaultExecutionOrder(100)]
    public sealed class MinionPresenter : MonoBehaviour
    {
        private static readonly int MovingHash = Animator.StringToHash("Moving");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [SerializeField] private MinionController minion;
        [SerializeField] private Transform flipRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private bool interpolate = true;
        [SerializeField] private bool artFacesRight = true;

        private MinionCombat subscribedCombat;

        private void Awake()
        {
            if (minion == null)
            {
                minion = GetComponentInParent<MinionController>();
            }
            if (flipRoot == null)
            {
                flipRoot = transform;
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            if (minion != null && minion.Combat != null)
            {
                subscribedCombat = minion.Combat;
                subscribedCombat.AttackPerformed += HandleAttack;
            }
        }

        private void OnDisable()
        {
            if (subscribedCombat != null)
            {
                subscribedCombat.AttackPerformed -= HandleAttack;
                subscribedCombat = null;
            }
        }

        private void HandleAttack(ICombatTarget target)
        {
            if (target != null)
            {
                Face(target.Position.x - minion.State.Position.x);
            }
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(AttackHash);
            }
        }

        private void LateUpdate()
        {
            if (minion == null)
            {
                return;
            }
            Vector2 pos = interpolate
                ? Vector2.Lerp(minion.PreviousState.Position, minion.State.Position, minion.InterpolationAlpha)
                : minion.State.Position;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            bool dead = minion.Mode == MinionState.Dead;
            bool moving = !dead && minion.State.Velocity.sqrMagnitude > 0.01f;
            if (moving)
            {
                Face(minion.State.Velocity.x);
            }
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool(MovingHash, moving);
                animator.SetBool(DeadHash, dead);
            }
        }

        private void Face(float directionX)
        {
            if (Mathf.Abs(directionX) <= 0.01f)
            {
                return;
            }
            bool faceLeft = directionX < 0f;
            float sign = (faceLeft == artFacesRight) ? -1f : 1f;
            Vector3 localScale = flipRoot.localScale;
            localScale.x = Mathf.Abs(localScale.x) * sign;
            flipRoot.localScale = localScale;
        }
    }
}
