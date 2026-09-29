using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Presentation
{
    public sealed class ProjectileView : MonoBehaviour
    {
        [SerializeField] private float heightOffset = 0.5f;
        [SerializeField] private bool rotateToDirection = true;
        [SerializeField] private Transform visualRoot;

        private ProjectileState state;
        private SimulationTickRunner runner;
        private int boundId = -1;

        public int BoundId => boundId;

        public void Bind(ProjectileState state, SimulationTickRunner runner)
        {
            this.state = state;
            this.runner = runner;
            boundId = state.Id;
            ApplyPosition(runner != null ? runner.InterpolationAlpha : 1f);
            ApplyRotation();
        }

        public void Unbind()
        {
            state = null;
            runner = null;
            boundId = -1;
        }

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }
        }

        private void LateUpdate()
        {
            if (state == null || !state.Alive || state.Id != boundId)
            {
                return;
            }
            float alpha = runner != null ? runner.InterpolationAlpha : 1f;
            ApplyPosition(alpha);
            ApplyRotation();
        }

        private void ApplyPosition(float alpha)
        {
            Vector2 pos = Vector2.Lerp(state.PreviousPosition, state.Position, alpha);
            transform.position = new Vector3(pos.x, pos.y + heightOffset, transform.position.z);
        }

        private void ApplyRotation()
        {
            if (!rotateToDirection || visualRoot == null)
            {
                return;
            }
            Vector2 dir = state.Direction;
            if (dir.sqrMagnitude < 1e-6f)
            {
                return;
            }
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            visualRoot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
