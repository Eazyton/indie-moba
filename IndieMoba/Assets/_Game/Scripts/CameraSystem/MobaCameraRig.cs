using UnityEngine;

namespace IndieMoba.CameraSystem
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Camera))]
    public sealed class MobaCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = Vector2.zero;
        [SerializeField] private float followSharpness = 10f;
        [SerializeField] private bool clampToBounds = true;
        [SerializeField] private Rect worldBounds = new Rect(0f, 0f, 40f, 27f);

        private Camera cam;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Rect WorldBounds
        {
            get => worldBounds;
            set => worldBounds = value;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }
            Vector2 desired = (Vector2)target.position + offset;
            transform.position = new Vector3(desired.x, desired.y, transform.position.z);
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }
            Vector2 desired = (Vector2)target.position + offset;
            Vector2 current = transform.position;
            float t = followSharpness > 0f ? 1f - Mathf.Exp(-followSharpness * Time.deltaTime) : 1f;
            Vector2 next = Vector2.Lerp(current, desired, t);
            if (clampToBounds)
            {
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;
                if (worldBounds.width <= halfWidth * 2f)
                {
                    next.x = worldBounds.center.x;
                }
                else
                {
                    next.x = Mathf.Clamp(next.x, worldBounds.xMin + halfWidth, worldBounds.xMax - halfWidth);
                }
                if (worldBounds.height <= halfHeight * 2f)
                {
                    next.y = worldBounds.center.y;
                }
                else
                {
                    next.y = Mathf.Clamp(next.y, worldBounds.yMin + halfHeight, worldBounds.yMax - halfHeight);
                }
            }
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 center = new Vector3(worldBounds.center.x, worldBounds.center.y, transform.position.z);
            Vector3 size = new Vector3(worldBounds.width, worldBounds.height, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
