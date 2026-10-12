using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Jungle;

namespace IndieMoba.Presentation
{
    // Placeholder view for a jungle monster: interpolated position, facing flip, attack punch,
    // immune (reset) indicator and buff-carrier marker. Body tint/dimming stays with HealthFlashView.
    // Replaceable without touching gameplay.
    [DefaultExecutionOrder(100)]
    public sealed class JungleMonsterPresenter : MonoBehaviour
    {
        [SerializeField] private JungleMonster monster;
        [SerializeField] private Transform flipRoot;
        [SerializeField] private GameObject immuneIndicator;
        [SerializeField] private GameObject carrierMarker;
        [SerializeField] private bool interpolate = true;
        [SerializeField] private bool artFacesRight = true;
        [SerializeField] private float attackPunch = 0.15f;
        [SerializeField] private float attackPunchDuration = 0.15f;

        private Vector3 baseScale = Vector3.one;
        private float punchTimer;
        private JungleMonster subscribed;

        private void Awake()
        {
            if (monster == null) monster = GetComponentInParent<JungleMonster>();
            if (flipRoot == null) flipRoot = transform;
            baseScale = flipRoot.localScale;
        }

        private void OnEnable()
        {
            if (monster != null)
            {
                subscribed = monster;
                subscribed.AttackPerformed += HandleAttack;
            }
        }

        private void OnDisable()
        {
            if (subscribed != null)
            {
                subscribed.AttackPerformed -= HandleAttack;
                subscribed = null;
            }
        }

        private void HandleAttack(JungleMonster _, ICombatTarget target)
        {
            punchTimer = attackPunchDuration;
            if (target != null)
            {
                Face(target.Position.x - monster.State.Position.x);
            }
        }

        private void LateUpdate()
        {
            if (monster == null)
            {
                return;
            }
            Vector2 pos = interpolate
                ? Vector2.Lerp(monster.PreviousState.Position, monster.State.Position, monster.InterpolationAlpha)
                : monster.State.Position;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            if (monster.State.Velocity.sqrMagnitude > 0.01f)
            {
                Face(monster.State.Velocity.x);
            }
            float punch = 0f;
            if (punchTimer > 0f)
            {
                punchTimer -= Time.deltaTime;
                punch = attackPunch * Mathf.Sin(Mathf.Clamp01(punchTimer / Mathf.Max(0.01f, attackPunchDuration)) * Mathf.PI);
            }
            float sign = Mathf.Sign(flipRoot.localScale.x);
            flipRoot.localScale = new Vector3(Mathf.Abs(baseScale.x) * (1f + punch) * sign, baseScale.y * (1f + punch), baseScale.z);
            SetActive(immuneIndicator, monster.Mode == JungleMonsterMode.Returning);
            SetActive(carrierMarker, monster.Camp != null && monster.Camp.BuffCarrier == monster && monster.IsAlive);
        }

        private static void SetActive(GameObject go, bool value)
        {
            if (go != null && go.activeSelf != value)
            {
                go.SetActive(value);
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
