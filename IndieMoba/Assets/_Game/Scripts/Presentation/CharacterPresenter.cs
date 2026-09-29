using UnityEngine;
using IndieMoba.Characters;

namespace IndieMoba.Presentation
{
    [DefaultExecutionOrder(100)]
    public sealed class CharacterPresenter : MonoBehaviour
    {
        [SerializeField] private HeroActor actor;
        [SerializeField] private Transform flipRoot;
        [SerializeField] private bool interpolate = true;
        [SerializeField] private bool artFacesRight = true;

        private void Reset()
        {
            if (actor == null)
            {
                actor = GetComponentInParent<HeroActor>();
            }
        }

        private void Awake()
        {
            if (actor == null)
            {
                actor = GetComponentInParent<HeroActor>();
            }
            if (flipRoot == null)
            {
                flipRoot = transform;
            }
        }

        private void LateUpdate()
        {
            if (actor == null)
            {
                return;
            }
            Vector2 pos = interpolate
                ? Vector2.Lerp(actor.PreviousState.Position, actor.State.Position, actor.InterpolationAlpha)
                : actor.State.Position;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            float facingX = actor.State.Facing.x;
            if (Mathf.Abs(facingX) > 0.01f)
            {
                bool faceLeft = facingX < 0f;
                float sign = (faceLeft == artFacesRight) ? -1f : 1f;
                Vector3 localScale = flipRoot.localScale;
                localScale.x = Mathf.Abs(localScale.x) * sign;
                flipRoot.localScale = localScale;
            }
        }
    }
}
