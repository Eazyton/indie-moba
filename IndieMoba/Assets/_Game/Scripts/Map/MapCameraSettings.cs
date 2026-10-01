using UnityEngine;

namespace IndieMoba.Map
{
    [DisallowMultipleComponent]
    public sealed class MapCameraSettings : MonoBehaviour
    {
        [SerializeField] private Rect bounds = new Rect(-6f, -6f, 140f, 140f);
        [Min(0.1f)] [SerializeField] private float orthographicSize = 9.375f;

        public Rect Bounds => bounds;
        public float OrthographicSize => orthographicSize;

        public void Configure(Rect cameraBounds, float size)
        {
            bounds = cameraBounds;
            orthographicSize = size;
        }
    }
}
