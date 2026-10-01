using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Map
{
    public sealed class MapZoneMarker : MonoBehaviour
    {
        [SerializeField] private string zoneId;
        [SerializeField] private MapZoneType zoneType;
        [SerializeField] private Team team;
        [Min(0f)] [SerializeField] private float radius;
        [SerializeField] private Vector2[] localPolygon;

        public string ZoneId => zoneId;
        public MapZoneType ZoneType => zoneType;
        public Team Team => team;
        public float Radius => radius;
        public Vector2[] LocalPolygon => localPolygon;
        public Vector2 Position => transform.position;

        public void Configure(string id, MapZoneType type, Team zoneTeam, float zoneRadius, Vector2[] polygon)
        {
            zoneId = id;
            zoneType = type;
            team = zoneTeam;
            radius = zoneRadius;
            localPolygon = polygon;
        }

        public bool Contains(Vector2 worldPoint)
        {
            Vector2 local = worldPoint - (Vector2)transform.position;
            if (localPolygon != null && localPolygon.Length >= 3)
            {
                return MapShapeGeometry.ContainsPoint(localPolygon, local);
            }
            return local.sqrMagnitude <= radius * radius;
        }

        public static Color ColorFor(MapZoneType type)
        {
            switch (type)
            {
                case MapZoneType.Base: return new Color(0.9f, 0.9f, 0.6f, 0.6f);
                case MapZoneType.Fountain: return new Color(0.4f, 1f, 0.9f, 0.8f);
                case MapZoneType.River: return new Color(0.2f, 0.6f, 0.9f, 0.6f);
                case MapZoneType.RiverCrossing: return new Color(0.4f, 0.8f, 1f, 0.8f);
                case MapZoneType.Pit: return new Color(0.8f, 0.3f, 1f, 0.8f);
                case MapZoneType.HeartPlaza: return new Color(1f, 0.8f, 0.3f, 0.8f);
                case MapZoneType.BuffClearing: return new Color(1f, 0.5f, 0.2f, 0.8f);
                case MapZoneType.SmallClearing: return new Color(0.9f, 0.9f, 0.3f, 0.8f);
                case MapZoneType.OptionalClearing: return new Color(0.7f, 0.7f, 0.7f, 0.8f);
                case MapZoneType.Brush: return new Color(0.2f, 0.9f, 0.3f, 0.8f);
                default: return Color.white;
            }
        }

        private void OnDrawGizmos()
        {
            MapDebugGizmos gizmos = GetComponentInParent<MapDebugGizmos>();
            if (gizmos != null && !gizmos.ShowZones)
            {
                return;
            }
            Gizmos.color = ColorFor(zoneType);
            if (localPolygon != null && localPolygon.Length >= 3)
            {
                for (int i = 0; i < localPolygon.Length; i++)
                {
                    Vector2 a = (Vector2)transform.position + localPolygon[i];
                    Vector2 b = (Vector2)transform.position + localPolygon[(i + 1) % localPolygon.Length];
                    Gizmos.DrawLine(a, b);
                }
            }
            else
            {
                MapGizmoUtility.DrawCircle(transform.position, radius, 24);
            }
        }
    }
}
