using System;
using System.Collections.Generic;
using IndieMoba.Combat;
using IndieMoba.Map;
using IndieMoba.Match;
using UnityEditor;
using UnityEngine;

namespace IndieMoba.EditorTools
{
    public static partial class Match5v5MapBuilder
    {
        private static void BuildGameplayPrefab(MapLayoutData layout, string layoutGuid)
        {
            var root = new GameObject("Map5v5_Gameplay");
            try
            {
                var stamp = root.AddComponent<MapGenerationStamp>();
                stamp.Configure(layoutGuid, LayoutAssetPath, GeneratorVersion, DateTime.UtcNow.ToString("o"));
                root.AddComponent<MapDebugGizmos>();

                BuildCollision(root, layout);
                BuildLanes(root, layout);
                BuildStructureSlots(root, layout);
                BuildSpawns(root, layout);
                BuildZones(root, layout);
                BuildCameraBounds(root, layout);

                PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildCollision(GameObject root, MapLayoutData layout)
        {
            var collision = new GameObject("Collision");
            collision.transform.SetParent(root.transform, false);
            collision.layer = ObstacleLayer;
            var body = collision.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = collision.AddComponent<CompositeCollider2D>();
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            composite.geometryType = layout.CollisionMode == MapCollisionMode.FilledPolygons
                ? CompositeCollider2D.GeometryType.Polygons
                : CompositeCollider2D.GeometryType.Outlines;

            float scale = layout.Scale;
            float apron = layout.ApronMargin * scale;
            float playable = layout.PlayableSize * scale;

            var boundsGO = new GameObject("Bounds");
            boundsGO.transform.SetParent(collision.transform, false);
            boundsGO.layer = ObstacleLayer;
            var boundsCollider = boundsGO.AddComponent<BoxCollider2D>();
            boundsCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            boundsCollider.compositeOrder = 0;
            boundsCollider.size = new Vector2(playable + apron * 2f, playable + apron * 2f);
            boundsCollider.offset = Vector2.zero;
            boundsGO.transform.position = new Vector3(playable * 0.5f, playable * 0.5f, 0f);
            var boundsShape = boundsGO.AddComponent<MapCollisionShape>();
            boundsShape.Configure("Bounds", MapShapeRole.Bounds, MapRegion.Border);

            var points = new List<Vector2>();
            var walkableParent = new GameObject("Walkable");
            walkableParent.transform.SetParent(collision.transform, false);
            walkableParent.layer = ObstacleLayer;
            var blockersParent = new GameObject("Blockers");
            blockersParent.transform.SetParent(collision.transform, false);
            blockersParent.layer = ObstacleLayer;
            BuildShapeChildren(walkableParent.transform, layout.Walkable, MapShapeRole.Walkable, layout, points, Collider2D.CompositeOperation.Difference, 1);
            BuildShapeChildren(blockersParent.transform, layout.Blockers, MapShapeRole.Blocker, layout, points, Collider2D.CompositeOperation.Merge, 2);

            composite.GenerateGeometry();
        }

        private static void BuildShapeChildren(Transform parent, MapShape[] shapes, MapShapeRole role, MapLayoutData layout,
            List<Vector2> points, Collider2D.CompositeOperation operation, int order)
        {
            if (shapes == null)
            {
                return;
            }
            for (int i = 0; i < shapes.Length; i++)
            {
                MapShape shape = shapes[i];
                if (shape == null)
                {
                    continue;
                }
                MapShapeGeometry.BuildPolygon(shape, layout.Scale, layout.CircleSegments, points);
                if (points.Count < 3)
                {
                    LogWarning("Skipping collision shape with fewer than 3 points: " + shape.id);
                    continue;
                }
                var go = new GameObject(shape.id);
                go.transform.SetParent(parent, false);
                go.layer = ObstacleLayer;
                var polygon = go.AddComponent<PolygonCollider2D>();
                polygon.pathCount = 1;
                polygon.SetPath(0, points.ToArray());
                polygon.compositeOperation = operation;
                polygon.compositeOrder = order;
                var marker = go.AddComponent<MapCollisionShape>();
                marker.Configure(shape.id, role, shape.region);
            }
        }

        private static void BuildLanes(GameObject root, MapLayoutData layout)
        {
            var lanesParent = new GameObject("Lanes");
            lanesParent.transform.SetParent(root.transform, false);
            if (layout.Lanes == null)
            {
                return;
            }
            for (int i = 0; i < layout.Lanes.Length; i++)
            {
                MapLaneLayout laneLayout = layout.Lanes[i];
                if (laneLayout == null || laneLayout.waypoints == null)
                {
                    continue;
                }
                var laneGO = new GameObject("Lane_" + laneLayout.lane);
                laneGO.transform.SetParent(lanesParent.transform, false);
                var lane = laneGO.AddComponent<LanePath>();
                var waypoints = new Transform[laneLayout.waypoints.Length];
                for (int w = 0; w < laneLayout.waypoints.Length; w++)
                {
                    var wp = new GameObject("Waypoint_" + w);
                    wp.transform.SetParent(laneGO.transform, false);
                    Vector2 scaled = layout.ToWorld(laneLayout.waypoints[w]);
                    wp.transform.position = new Vector3(scaled.x, scaled.y, 0f);
                    waypoints[w] = wp.transform;
                }
                var laneSo = new SerializedObject(lane);
                laneSo.FindProperty("laneId").intValue = (int)laneLayout.lane;
                var wpProp = laneSo.FindProperty("waypoints");
                wpProp.arraySize = waypoints.Length;
                for (int w = 0; w < waypoints.Length; w++)
                {
                    wpProp.GetArrayElementAtIndex(w).objectReferenceValue = waypoints[w];
                }
                laneSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildStructureSlots(GameObject root, MapLayoutData layout)
        {
            var slotsParent = new GameObject("StructureSlots");
            slotsParent.transform.SetParent(root.transform, false);
            if (layout.Structures == null)
            {
                return;
            }
            for (int i = 0; i < layout.Structures.Length; i++)
            {
                MapStructureSlot slot = layout.Structures[i];
                if (slot == null)
                {
                    continue;
                }
                var slotGO = new GameObject(slot.id);
                slotGO.transform.SetParent(slotsParent.transform, false);
                Vector2 scaled = layout.ToWorld(slot.position);
                slotGO.transform.position = new Vector3(scaled.x, scaled.y, 0f);
                var marker = slotGO.AddComponent<StructureSlotMarker>();
                marker.Configure(slot.id, slot.team, slot.lane, slot.tier);
            }
        }

        private static void BuildSpawns(GameObject root, MapLayoutData layout)
        {
            var spawnsParent = new GameObject("Spawns");
            spawnsParent.transform.SetParent(root.transform, false);
            if (layout.Lanes != null)
            {
                for (int i = 0; i < layout.Lanes.Length; i++)
                {
                    MapLaneLayout lane = layout.Lanes[i];
                    if (lane == null)
                    {
                        continue;
                    }
                    AddSpawnMarker(spawnsParent.transform, "MinionSpawn_Blue_" + lane.lane, MapSpawnKind.MinionSpawn, Team.Blue, lane.lane, layout.ToWorld(lane.blueSpawn));
                    AddSpawnMarker(spawnsParent.transform, "MinionSpawn_Red_" + lane.lane, MapSpawnKind.MinionSpawn, Team.Red, lane.lane, layout.ToWorld(lane.redSpawn));
                }
            }
            AddSpawnMarker(spawnsParent.transform, "Fountain_Blue", MapSpawnKind.Fountain, Team.Blue, LaneId.Mid, layout.ToWorld(layout.BlueFountain));
            AddSpawnMarker(spawnsParent.transform, "Fountain_Red", MapSpawnKind.Fountain, Team.Red, LaneId.Mid, layout.ToWorld(layout.RedFountain));
        }

        private static void AddSpawnMarker(Transform parent, string name, MapSpawnKind kind, Team team, LaneId lane, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var marker = go.AddComponent<MapSpawnMarker>();
            marker.Configure(kind, team, lane);
        }

        private static void BuildZones(GameObject root, MapLayoutData layout)
        {
            var zonesParent = new GameObject("Zones");
            zonesParent.transform.SetParent(root.transform, false);
            if (layout.Zones == null)
            {
                return;
            }
            var points = new List<Vector2>();
            for (int i = 0; i < layout.Zones.Length; i++)
            {
                MapZoneLayout zone = layout.Zones[i];
                if (zone == null || zone.shape == null)
                {
                    continue;
                }
                var zoneGO = new GameObject(zone.id);
                zoneGO.transform.SetParent(zonesParent.transform, false);
                var marker = zoneGO.AddComponent<MapZoneMarker>();
                if (zone.shape.kind == MapShapeKind.Circle)
                {
                    Vector2 center = layout.ToWorld(zone.shape.a);
                    zoneGO.transform.position = new Vector3(center.x, center.y, 0f);
                    marker.Configure(zone.id, zone.type, zone.team, zone.shape.radius * layout.Scale, null);
                }
                else
                {
                    MapShapeGeometry.BuildPolygon(zone.shape, layout.Scale, layout.CircleSegments, points);
                    if (points.Count < 3)
                    {
                        LogWarning("Skipping zone with fewer than 3 points: " + zone.id);
                        UnityEngine.Object.DestroyImmediate(zoneGO);
                        continue;
                    }
                    Vector2 centroid = Vector2.zero;
                    for (int p = 0; p < points.Count; p++)
                    {
                        centroid += points[p];
                    }
                    centroid /= points.Count;
                    zoneGO.transform.position = new Vector3(centroid.x, centroid.y, 0f);
                    var local = new Vector2[points.Count];
                    float maxRadius = 0f;
                    for (int p = 0; p < points.Count; p++)
                    {
                        local[p] = points[p] - centroid;
                        float distance = local[p].magnitude;
                        if (distance > maxRadius)
                        {
                            maxRadius = distance;
                        }
                    }
                    marker.Configure(zone.id, zone.type, zone.team, maxRadius, local);
                }
            }
        }

        private static void BuildCameraBounds(GameObject root, MapLayoutData layout)
        {
            var cameraBoundsGO = new GameObject("CameraBounds");
            cameraBoundsGO.transform.SetParent(root.transform, false);
            var settings = cameraBoundsGO.AddComponent<MapCameraSettings>();
            settings.Configure(layout.ScaledCameraBounds, layout.OrthographicSize * layout.Scale);
        }

        internal static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }
            if (root.name == name)
            {
                return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildRecursive(root.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
