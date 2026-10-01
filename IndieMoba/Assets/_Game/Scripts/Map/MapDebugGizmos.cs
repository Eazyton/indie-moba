using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Match;

namespace IndieMoba.Map
{
    [DisallowMultipleComponent]
    public sealed class MapDebugGizmos : MonoBehaviour
    {
        [SerializeField] private bool showCollision = true;
        [SerializeField] private bool showLanes = true;
        [SerializeField] private bool showZones = true;
        [SerializeField] private bool showTowerRanges;
        [SerializeField] private bool showBounds = true;

        private readonly List<Vector2> pathBuffer = new List<Vector2>();

        public bool ShowZones => showZones;

        private void OnDrawGizmos()
        {
            if (showCollision)
            {
                DrawCollision();
            }
            if (showLanes)
            {
                DrawLanes();
            }
            if (showTowerRanges)
            {
                DrawTowerRanges();
            }
            if (showBounds)
            {
                DrawBounds();
            }
        }

        private void DrawCollision()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
            CompositeCollider2D[] composites = GetComponentsInChildren<CompositeCollider2D>();
            for (int c = 0; c < composites.Length; c++)
            {
                CompositeCollider2D composite = composites[c];
                Transform t = composite.transform;
                for (int p = 0; p < composite.pathCount; p++)
                {
                    pathBuffer.Clear();
                    composite.GetPath(p, pathBuffer);
                    for (int i = 0; i < pathBuffer.Count; i++)
                    {
                        Vector3 a = t.TransformPoint(pathBuffer[i]);
                        Vector3 b = t.TransformPoint(pathBuffer[(i + 1) % pathBuffer.Count]);
                        Gizmos.DrawLine(a, b);
                    }
                }
            }
        }

        private void DrawLanes()
        {
            LanePath[] lanes = GetComponentsInChildren<LanePath>();
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 1f);
            for (int l = 0; l < lanes.Length; l++)
            {
                pathBuffer.Clear();
                lanes[l].GetWaypoints(Team.Blue, pathBuffer);
                for (int i = 1; i < pathBuffer.Count; i++)
                {
                    Gizmos.DrawLine(pathBuffer[i - 1], pathBuffer[i]);
                }
            }
        }

        private void DrawTowerRanges()
        {
            StructureSlotMarker[] slots = GetComponentsInChildren<StructureSlotMarker>();
            for (int i = 0; i < slots.Length; i++)
            {
                StructureSlotMarker slot = slots[i];
                Gizmos.color = slot.Team == Team.Blue ? new Color(0.3f, 0.6f, 1f, 0.6f) : new Color(1f, 0.35f, 0.3f, 0.6f);
                MapGizmoUtility.DrawCircle(slot.transform.position, slot.PreviewRange, 32);
            }
        }

        private void DrawBounds()
        {
            MapCameraSettings settings = GetComponentInChildren<MapCameraSettings>();
            if (settings == null)
            {
                return;
            }
            Rect r = settings.Bounds;
            Gizmos.color = new Color(0.6f, 0.6f, 1f, 1f);
            Gizmos.DrawWireCube(new Vector3(r.center.x, r.center.y, 0f), new Vector3(r.width, r.height, 0f));
        }
    }
}
