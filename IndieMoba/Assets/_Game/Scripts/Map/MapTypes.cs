using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Match;

namespace IndieMoba.Map
{
    public enum MapCollisionMode
    {
        FilledPolygons = 0,
        Outlines = 1
    }

    public enum MapShapeKind
    {
        Segment = 0,
        Circle = 1,
        Polygon = 2
    }

    public enum MapRegion
    {
        Jungle = 0,
        Base = 1,
        MidLane = 2,
        ClashLane = 3,
        FarmLane = 4,
        River = 5,
        Pit = 6,
        HeartPlaza = 7,
        Clearing = 8,
        Border = 9
    }

    public enum MapShapeRole
    {
        Bounds = 0,
        Walkable = 1,
        Blocker = 2
    }

    public enum MapZoneType
    {
        Base = 0,
        Fountain = 1,
        River = 2,
        RiverCrossing = 3,
        Pit = 4,
        HeartPlaza = 5,
        BuffClearing = 6,
        SmallClearing = 7,
        OptionalClearing = 8,
        Brush = 9
    }

    public enum StructureTier
    {
        Outer = 0,
        Inner = 1,
        Base = 2,
        Nexus = 3
    }

    public enum MapSpawnKind
    {
        MinionSpawn = 0,
        Fountain = 1
    }

    [Serializable]
    public sealed class MapShape
    {
        public string id;
        public MapShapeKind kind;
        public MapRegion region;
        public Vector2 a;
        public Vector2 b;
        public float width = 5f;
        public float radius = 4f;
        public Vector2[] points;
    }

    [Serializable]
    public sealed class MapLaneLayout
    {
        public LaneId lane;
        public Vector2[] waypoints;
        public float width = 7f;
        public Vector2 blueSpawn;
        public Vector2 redSpawn;
        [Min(0f)] public float waveSpawnOffset;
    }

    [Serializable]
    public sealed class MapStructureSlot
    {
        public string id;
        public Team team;
        public LaneId lane;
        public StructureTier tier;
        public Vector2 position;
    }

    [Serializable]
    public sealed class MapZoneLayout
    {
        public string id;
        public MapZoneType type;
        public Team team;
        public MapShape shape;
    }

    public static class MapShapeGeometry
    {
        public static void BuildPolygon(MapShape shape, float scale, int circleSegments, List<Vector2> result)
        {
            result.Clear();
            if (shape == null)
            {
                return;
            }
            int segments = Mathf.Max(6, circleSegments);
            switch (shape.kind)
            {
                case MapShapeKind.Circle:
                    AddCircle(shape.a * scale, shape.radius * scale, segments, result);
                    break;
                case MapShapeKind.Segment:
                    AddCapsule(shape.a * scale, shape.b * scale, shape.width * 0.5f * scale, Mathf.Max(4, segments / 2), result);
                    break;
                case MapShapeKind.Polygon:
                    if (shape.points != null)
                    {
                        for (int i = 0; i < shape.points.Length; i++)
                        {
                            result.Add(shape.points[i] * scale);
                        }
                    }
                    break;
            }
        }

        public static void AddCircle(Vector2 center, float radius, int segments, List<Vector2> result)
        {
            for (int i = 0; i < segments; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / segments;
                result.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        public static void AddCapsule(Vector2 a, Vector2 b, float halfWidth, int capSegments, List<Vector2> result)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude < 1e-8f)
            {
                AddCircle(a, halfWidth, capSegments * 2, result);
                return;
            }
            direction.Normalize();
            Vector2 normal = new Vector2(-direction.y, direction.x);
            float baseAngle = Mathf.Atan2(normal.y, normal.x);
            for (int i = 0; i <= capSegments; i++)
            {
                float angle = baseAngle - Mathf.PI * i / capSegments;
                result.Add(b + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * halfWidth);
            }
            for (int i = 0; i <= capSegments; i++)
            {
                float angle = baseAngle + Mathf.PI - Mathf.PI * i / capSegments;
                result.Add(a + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * halfWidth);
            }
        }

        public static bool ContainsPoint(IList<Vector2> polygon, Vector2 point)
        {
            bool inside = false;
            int count = polygon.Count;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Vector2 pi = polygon[i];
                Vector2 pj = polygon[j];
                if ((pi.y > point.y) != (pj.y > point.y) &&
                    point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
