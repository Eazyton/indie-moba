using UnityEngine;
using IndieMoba.Characters;

namespace IndieMoba.Presentation
{
    public sealed class CharacterAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private HeroActor actor;
        [SerializeField] private Animator animator;
        [SerializeField] private string isMovingParameter = "IsMoving";
        [SerializeField] private string speedParameter = "Speed";

        private int isMovingHash;
        private int speedHash;
        private bool hasIsMoving;
        private bool hasSpeed;

        private void Awake()
        {
            if (actor == null)
            {
                actor = GetComponentInParent<HeroActor>();
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            isMovingHash = Animator.StringToHash(isMovingParameter);
            speedHash = Animator.StringToHash(speedParameter);
            if (animator != null)
            {
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.name == isMovingParameter)
                    {
                        hasIsMoving = true;
                    }
                    else if (parameter.name == speedParameter)
                    {
                        hasSpeed = true;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (animator == null || actor == null || animator.runtimeAnimatorController == null)
            {
                return;
            }
            float speed = actor.State.Velocity.magnitude;
            if (hasIsMoving)
            {
                animator.SetBool(isMovingHash, actor.State.IsMoving);
            }
            if (hasSpeed)
            {
                animator.SetFloat(speedHash, speed);
            }
        }
    }
}
