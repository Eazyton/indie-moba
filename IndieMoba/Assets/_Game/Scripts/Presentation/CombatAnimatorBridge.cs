using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Presentation
{
    public sealed class CombatAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private HeroCombat combat;
        [SerializeField] private Animator animator;
        [SerializeField] private string attackTrigger = "Attack";

        private int attackHash;
        private bool hasAttack;

        private void Awake()
        {
            if (combat == null)
            {
                combat = GetComponentInParent<HeroCombat>();
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            attackHash = Animator.StringToHash(attackTrigger);
            if (animator != null)
            {
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.name == attackTrigger)
                    {
                        hasAttack = true;
                        break;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (combat == null)
            {
                combat = GetComponentInParent<HeroCombat>();
            }
            if (combat != null)
            {
                combat.AbilityStarted += HandleAbilityStarted;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.AbilityStarted -= HandleAbilityStarted;
            }
        }

        private void HandleAbilityStarted(AbilitySlot slot, AbilityConfig config)
        {
            if (!hasAttack || animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }
            if (slot == AbilitySlot.BasicAttack || slot == AbilitySlot.Ability1)
            {
                animator.SetTrigger(attackHash);
            }
        }
    }
}
