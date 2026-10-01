using UnityEngine;

namespace IndieMoba.Map
{
    public static class MapGizmoUtility
    {
        public static void DrawCircle(Vector3 center, float radius, int segments)
        {
            if (radius <= 0f)
            {
                return;
            }
            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
