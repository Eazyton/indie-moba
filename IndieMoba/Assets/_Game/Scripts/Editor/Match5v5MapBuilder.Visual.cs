using System.Collections.Generic;
using System.IO;
using IndieMoba.Combat;
using IndieMoba.Map;
using IndieMoba.Match;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace IndieMoba.EditorTools
{
    public static partial class Match5v5MapBuilder
    {
        private struct VisualPolygon
        {
            public Vector2[] points;
            public MapRegion region;
            public Vector2 min;
            public Vector2 max;
        }

        private struct VisualZone
        {
            public MapZoneType type;
            public Vector2 position;
            public float radius;
        }

        private static void BuildVisualPrefab(GameObject gameplayPrefab)
        {
            var walkable = new List<VisualPolygon>();
            var blockers = new List<VisualPolygon>();
            VisualGatherPolygons(gameplayPrefab, walkable, blockers);

            Rect bounds = VisualMapBounds(gameplayPrefab);
            int minX = Mathf.FloorToInt(bounds.xMin);
            int maxX = Mathf.CeilToInt(bounds.xMax);
            int minY = Mathf.FloorToInt(bounds.yMin);
            int maxY = Mathf.CeilToInt(bounds.yMax);

            var lanes = new List<Vector2>[3];
            VisualGatherLanes(gameplayPrefab, lanes);

            var zones = new List<VisualZone>();
            VisualGatherZones(gameplayPrefab, zones);

            Dictionary<string, Tile> tiles = VisualCreateTiles();

            var root = new GameObject("Map5v5_Visual");
            try
            {
                var gridGO = new GameObject("Grid");
                gridGO.transform.SetParent(root.transform, false);
                var grid = gridGO.AddComponent<Grid>();
                grid.cellSize = new Vector3(1f, 1f, 0f);

                Tilemap groundMap = VisualCreateTilemap(gridGO, "Ground_Base", "Ground", 0);
                Tilemap pathMap = VisualCreateTilemap(gridGO, "Paths", "Ground", 1);
                Tilemap waterMap = VisualCreateTilemap(gridGO, "Water", "Ground", 2);
                Tilemap decorMap = VisualCreateTilemap(gridGO, "GroundDecor", "GroundDecor", 0);

                var groundPositions = new List<Vector3Int>();
                var groundTiles = new List<TileBase>();
                var pathPositions = new List<Vector3Int>();
                var pathTiles = new List<TileBase>();
                var waterPositions = new List<Vector3Int>();
                var waterTiles = new List<TileBase>();
                var decorPositions = new List<Vector3Int>();
                var decorTiles = new List<TileBase>();

                int walkableCount = 0;
                int blockedCount = 0;

                for (int x = minX; x < maxX; x++)
                {
                    for (int y = minY; y < maxY; y++)
                    {
                        var center = new Vector2(x + 0.5f, y + 0.5f);
                        var position = new Vector3Int(x, y, 0);
                        if (VisualIsBlocked(center, walkable, blockers))
                        {
                            blockedCount++;
                            groundPositions.Add(position);
                            groundTiles.Add(tiles["Graybox_Forest"]);
                            continue;
                        }
                        walkableCount++;
                        MapRegion region = VisualRegionAt(center, walkable);
                        groundPositions.Add(position);
                        groundTiles.Add(VisualGroundTile(region, tiles));
                        if (region == MapRegion.River)
                        {
                            waterPositions.Add(position);
                            waterTiles.Add(tiles["Graybox_River"]);
                        }
                        if (region != MapRegion.River && region != MapRegion.Pit && region != MapRegion.HeartPlaza)
                        {
                            int laneIndex = VisualNearestLane(center, lanes);
                            if (laneIndex >= 0)
                            {
                                pathPositions.Add(position);
                                pathTiles.Add(laneIndex == 0 ? tiles["Graybox_PathMid"] : laneIndex == 1 ? tiles["Graybox_PathClash"] : tiles["Graybox_PathFarm"]);
                            }
                        }
                        if (region == MapRegion.Jungle || region == MapRegion.Clearing || region == MapRegion.ClashLane)
                        {
                            if (VisualHash01(x, y, 0x9E3779B9u) < 0.03f)
                            {
                                decorPositions.Add(position);
                                decorTiles.Add(tiles["Graybox_Rune"]);
                            }
                        }
                    }
                }

                groundMap.SetTiles(groundPositions.ToArray(), groundTiles.ToArray());
                groundMap.CompressBounds();
                pathMap.SetTiles(pathPositions.ToArray(), pathTiles.ToArray());
                pathMap.CompressBounds();
                waterMap.SetTiles(waterPositions.ToArray(), waterTiles.ToArray());
                waterMap.CompressBounds();
                decorMap.SetTiles(decorPositions.ToArray(), decorTiles.ToArray());
                decorMap.CompressBounds();

                int propCount = VisualBuildProps(root.transform, walkable, blockers, minX, maxX, minY, maxY);
                int zoneVisualCount = VisualBuildZones(root.transform, zones);
                int lightCount = VisualBuildLights(root.transform, zones);

                PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath);
                Log("Visual prefab: walkable cells " + walkableCount + ", blocked cells " + blockedCount + ", props " + propCount + ", zone visuals " + zoneVisualCount + ", lights " + lightCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void VisualGatherPolygons(GameObject root, List<VisualPolygon> walkable, List<VisualPolygon> blockers)
        {
            var shapes = root.GetComponentsInChildren<MapCollisionShape>(true);
            for (int i = 0; i < shapes.Length; i++)
            {
                var shape = shapes[i];
                var collider = shape.GetComponent<PolygonCollider2D>();
                if (collider == null)
                {
                    continue;
                }
                Vector2[] local = collider.GetPath(0);
                if (local == null || local.Length < 3)
                {
                    continue;
                }
                var poly = new VisualPolygon();
                poly.points = new Vector2[local.Length];
                poly.region = shape.Region;
                poly.min = new Vector2(float.MaxValue, float.MaxValue);
                poly.max = new Vector2(float.MinValue, float.MinValue);
                for (int p = 0; p < local.Length; p++)
                {
                    Vector2 world = collider.transform.TransformPoint(local[p]);
                    poly.points[p] = world;
                    poly.min.x = Mathf.Min(poly.min.x, world.x);
                    poly.min.y = Mathf.Min(poly.min.y, world.y);
                    poly.max.x = Mathf.Max(poly.max.x, world.x);
                    poly.max.y = Mathf.Max(poly.max.y, world.y);
                }
                if (shape.Role == MapShapeRole.Walkable)
                {
                    walkable.Add(poly);
                }
                else if (shape.Role == MapShapeRole.Blocker)
                {
                    blockers.Add(poly);
                }
            }
        }

        private static Rect VisualMapBounds(GameObject root)
        {
            Transform boundsTransform = FindChildRecursive(root.transform, "Bounds");
            if (boundsTransform != null)
            {
                var collider = boundsTransform.GetComponent<BoxCollider2D>();
                if (collider != null)
                {
                    Vector2 center = (Vector2)boundsTransform.position + collider.offset;
                    Vector2 half = collider.size * 0.5f;
                    return new Rect(center.x - half.x, center.y - half.y, collider.size.x, collider.size.y);
                }
            }
            return new Rect(-8f, -8f, 144f, 144f);
        }

        private static void VisualGatherLanes(GameObject root, List<Vector2>[] lanes)
        {
            var lanePaths = root.GetComponentsInChildren<LanePath>(true);
            for (int i = 0; i < lanePaths.Length; i++)
            {
                var lane = lanePaths[i];
                var points = new List<Vector2>();
                lane.GetWaypoints(Team.Blue, points);
                lanes[(int)lane.LaneId] = points;
            }
        }

        private static void VisualGatherZones(GameObject root, List<VisualZone> zones)
        {
            var markers = root.GetComponentsInChildren<MapZoneMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                var zone = new VisualZone();
                zone.type = marker.ZoneType;
                zone.position = marker.Position;
                zone.radius = marker.Radius;
                zones.Add(zone);
            }
        }

        private static bool VisualIsBlocked(Vector2 point, List<VisualPolygon> walkable, List<VisualPolygon> blockers)
        {
            for (int i = 0; i < blockers.Count; i++)
            {
                if (VisualContains(blockers[i], point))
                {
                    return true;
                }
            }
            for (int i = 0; i < walkable.Count; i++)
            {
                if (VisualContains(walkable[i], point))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool VisualContains(VisualPolygon poly, Vector2 point)
        {
            if (point.x < poly.min.x || point.x > poly.max.x || point.y < poly.min.y || point.y > poly.max.y)
            {
                return false;
            }
            return MapShapeGeometry.ContainsPoint(poly.points, point);
        }

        private static bool VisualInsideAny(List<VisualPolygon> polygons, Vector2 point)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                if (VisualContains(polygons[i], point))
                {
                    return true;
                }
            }
            return false;
        }

        private static MapRegion VisualRegionAt(Vector2 point, List<VisualPolygon> walkable)
        {
            MapRegion best = MapRegion.Jungle;
            int bestPriority = -1;
            for (int i = 0; i < walkable.Count; i++)
            {
                if (!VisualContains(walkable[i], point))
                {
                    continue;
                }
                int priority = VisualRegionPriority(walkable[i].region);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    best = walkable[i].region;
                }
            }
            return best;
        }

        private static int VisualRegionPriority(MapRegion region)
        {
            switch (region)
            {
                case MapRegion.HeartPlaza: return 9;
                case MapRegion.Pit: return 8;
                case MapRegion.River: return 7;
                case MapRegion.MidLane: return 6;
                case MapRegion.ClashLane: return 5;
                case MapRegion.FarmLane: return 4;
                case MapRegion.Base: return 3;
                case MapRegion.Clearing: return 2;
                case MapRegion.Jungle: return 1;
                default: return 0;
            }
        }

        private static Tile VisualGroundTile(MapRegion region, Dictionary<string, Tile> tiles)
        {
            switch (region)
            {
                case MapRegion.Base: return tiles["Graybox_Base"];
                case MapRegion.ClashLane: return tiles["Graybox_Clash"];
                case MapRegion.MidLane: return tiles["Graybox_Mid"];
                case MapRegion.FarmLane: return tiles["Graybox_Farm"];
                case MapRegion.River: return tiles["Graybox_Jungle"];
                case MapRegion.Pit: return tiles["Graybox_Pit"];
                case MapRegion.HeartPlaza: return tiles["Graybox_Plaza"];
                case MapRegion.Clearing: return tiles["Graybox_Clearing"];
                default: return tiles["Graybox_Jungle"];
            }
        }

        private static int VisualNearestLane(Vector2 point, List<Vector2>[] lanes)
        {
            float bestDistance = 2.25f;
            int bestIndex = -1;
            for (int i = 0; i < lanes.Length; i++)
            {
                List<Vector2> lane = lanes[i];
                if (lane == null || lane.Count < 2)
                {
                    continue;
                }
                float distance = VisualDistanceToPolyline(point, lane);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private static float VisualDistanceToPolyline(Vector2 point, List<Vector2> polyline)
        {
            float best = float.MaxValue;
            for (int i = 0; i < polyline.Count - 1; i++)
            {
                float d = VisualDistanceToSegment(point, polyline[i], polyline[i + 1]);
                if (d < best)
                {
                    best = d;
                }
            }
            return best;
        }

        private static float VisualDistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSqr = ab.sqrMagnitude;
            if (lengthSqr < 1e-8f)
            {
                return Vector2.Distance(point, a);
            }
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSqr);
            return Vector2.Distance(point, a + ab * t);
        }

        private static Dictionary<string, Tile> VisualCreateTiles()
        {
            EnsureFolder(GrayboxArtFolder);
            var result = new Dictionary<string, Tile>();
            VisualCreateTile("Graybox_Forest", new Color(0.10f, 0.16f, 0.12f), false, result);
            VisualCreateTile("Graybox_Jungle", new Color(0.20f, 0.30f, 0.20f), false, result);
            VisualCreateTile("Graybox_Clearing", new Color(0.27f, 0.35f, 0.22f), false, result);
            VisualCreateTile("Graybox_Base", new Color(0.55f, 0.55f, 0.48f), false, result);
            VisualCreateTile("Graybox_Clash", new Color(0.33f, 0.25f, 0.36f), false, result);
            VisualCreateTile("Graybox_Mid", new Color(0.45f, 0.43f, 0.40f), false, result);
            VisualCreateTile("Graybox_Farm", new Color(0.30f, 0.45f, 0.22f), false, result);
            VisualCreateTile("Graybox_River", new Color(0.08f, 0.22f, 0.26f), false, result);
            VisualCreateTile("Graybox_Pit", new Color(0.22f, 0.14f, 0.28f), false, result);
            VisualCreateTile("Graybox_Plaza", new Color(0.50f, 0.46f, 0.38f), false, result);
            VisualCreateTile("Graybox_PathClash", new Color(0.42f, 0.33f, 0.44f), false, result);
            VisualCreateTile("Graybox_PathMid", new Color(0.58f, 0.56f, 0.52f), false, result);
            VisualCreateTile("Graybox_PathFarm", new Color(0.45f, 0.40f, 0.28f), false, result);
            VisualCreateTile("Graybox_Rune", new Color(0.4f, 0.9f, 1.0f), true, result);
            return result;
        }

        private static void VisualCreateTile(string name, Color baseColor, bool rune, Dictionary<string, Tile> result)
        {
            string texturePath = GrayboxArtFolder + "/" + name + ".png";
            Texture2D texture = VisualBuildTexture(name, baseColor, rune);
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = 32;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            string tilePath = GrayboxArtFolder + "/" + name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            EditorUtility.SetDirty(tile);
            result[name] = tile;
        }

        private static Texture2D VisualBuildTexture(string name, Color baseColor, bool rune)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var random = new System.Random(VisualHashString(name));
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (rune)
                    {
                        pixels[y * 32 + x] = VisualRunePixel(x, y);
                    }
                    else
                    {
                        float jitter = (float)(random.NextDouble() * 2.0 - 1.0) * 0.035f;
                        var color = new Color(
                            Mathf.Clamp01(baseColor.r + jitter),
                            Mathf.Clamp01(baseColor.g + jitter),
                            Mathf.Clamp01(baseColor.b + jitter),
                            1f);
                        if (random.NextDouble() < 0.02)
                        {
                            color.r *= 0.75f;
                            color.g *= 0.75f;
                            color.b *= 0.75f;
                        }
                        pixels[y * 32 + x] = color;
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Color VisualRunePixel(int x, int y)
        {
            int dx = x - 16;
            int dy = y - 16;
            int diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
            bool cross = (Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 6) || (Mathf.Abs(dy) <= 1 && Mathf.Abs(dx) <= 6);
            if (diamond <= 4 || cross)
            {
                return new Color(0.4f, 0.9f, 1.0f, 1f);
            }
            if (diamond <= 7)
            {
                return new Color(0.4f, 0.9f, 1.0f, 0.3f);
            }
            return new Color(0f, 0f, 0f, 0f);
        }

        private static int VisualHashString(string text)
        {
            int hash = 17;
            for (int i = 0; i < text.Length; i++)
            {
                hash = hash * 31 + text[i];
            }
            return hash;
        }

        private static float VisualHash01(int x, int y, uint salt)
        {
            uint h = (uint)((long)x * 73856093L) ^ (uint)((long)y * 19349663L) ^ salt;
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (h & 0xFFFFFF) / 16777216f;
        }

        private static Tilemap VisualCreateTilemap(GameObject gridGO, string name, string sortingLayer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(gridGO.transform, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.mode = TilemapRenderer.Mode.Chunk;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            return tilemap;
        }

        private static int VisualBuildProps(Transform root, List<VisualPolygon> walkable, List<VisualPolygon> blockers, int minX, int maxX, int minY, int maxY)
        {
            var propsParent = new GameObject("Props");
            propsParent.transform.SetParent(root, false);
            Dictionary<string, Sprite> sprites = VisualLoadPropSprites();
            var missing = new List<string>();
            int count = 0;
            for (int x = minX; x < maxX && count < 2500; x++)
            {
                for (int y = minY; y < maxY && count < 2500; y++)
                {
                    var center = new Vector2(x + 0.5f, y + 0.5f);
                    if (!VisualIsBlocked(center, walkable, blockers))
                    {
                        continue;
                    }
                    bool hasNearby = VisualHasWalkableNearby(x, y, walkable, 2);
                    float threshold = hasNearby ? 0.30f : 0.035f;
                    if (VisualHash01(x, y, 0x85EBCA6Bu) >= threshold)
                    {
                        continue;
                    }
                    MapRegion region = hasNearby ? VisualNearestWalkableRegion(x, y, walkable) : MapRegion.Jungle;
                    string spriteName = VisualPickPropSprite(region, x, y);
                    Sprite sprite;
                    if (!sprites.TryGetValue(spriteName, out sprite) || sprite == null)
                    {
                        if (!missing.Contains(spriteName))
                        {
                            missing.Add(spriteName);
                        }
                        continue;
                    }
                    var go = new GameObject("Prop_" + count);
                    go.transform.SetParent(propsParent.transform, false);
                    float jitterX = (VisualHash01(x, y, 0x1B873593u) * 2f - 1f) * 0.3f;
                    float jitterY = (VisualHash01(x, y, 0x165667B1u) * 2f - 1f) * 0.3f;
                    go.transform.position = new Vector3(center.x + jitterX, center.y + jitterY, 0f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingLayerName = "Actors";
                    sr.sortingOrder = 0;
                    sr.spriteSortPoint = SpriteSortPoint.Pivot;
                    sr.flipX = VisualHash01(x, y, 0x0CC9E2F3u) < 0.5f;
                    count++;
                }
            }
            if (missing.Count > 0)
            {
                LogWarning("Missing prop sprites: " + string.Join(", ", missing.ToArray()));
            }
            return count;
        }

        private static Dictionary<string, Sprite> VisualLoadPropSprites()
        {
            var result = new Dictionary<string, Sprite>();
            string[] names = {
                "Tree_0", "Tree_1", "Tree_2",
                "Bush_0", "Bush_1",
                "Mushroom_0", "Mushroom_1",
                "Flower_0", "Flower_1", "Flower_2", "Flower_3",
                "Rock_0", "Rock_1", "Rock_2",
                "Pillar_0", "Pillar_1",
                "RuinWall", "Crystal"
            };
            for (int i = 0; i < names.Length; i++)
            {
                string path = VisualSpritePath(names[i]);
                if (path == null)
                {
                    continue;
                }
                Sprite sprite = LoadSprite(path);
                if (sprite != null)
                {
                    result[names[i]] = sprite;
                }
            }
            return result;
        }

        private static string VisualSpritePath(string name)
        {
            if (name.StartsWith("Tree")) return EnvironmentArtFolder + "/Trees/" + name + ".png";
            if (name.StartsWith("Bush") || name.StartsWith("Mushroom") || name.StartsWith("Flower")) return EnvironmentArtFolder + "/Vegetation/" + name + ".png";
            if (name.StartsWith("Rock")) return EnvironmentArtFolder + "/Rocks/" + name + ".png";
            if (name.StartsWith("Pillar") || name == "RuinWall" || name == "Crystal") return EnvironmentArtFolder + "/Ruins/" + name + ".png";
            return null;
        }

        private static bool VisualHasWalkableNearby(int x, int y, List<VisualPolygon> walkable, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var center = new Vector2(x + 0.5f + dx, y + 0.5f + dy);
                    if (VisualInsideAny(walkable, center))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static MapRegion VisualNearestWalkableRegion(int x, int y, List<VisualPolygon> walkable)
        {
            var center = new Vector2(x + 0.5f, y + 0.5f);
            float bestDistance = float.MaxValue;
            MapRegion bestRegion = MapRegion.Jungle;
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    var candidate = new Vector2(x + 0.5f + dx, y + 0.5f + dy);
                    if (!VisualInsideAny(walkable, candidate))
                    {
                        continue;
                    }
                    float distance = Vector2.Distance(center, candidate);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestRegion = VisualRegionAt(candidate, walkable);
                    }
                }
            }
            return bestRegion;
        }

        private static string VisualPickPropSprite(MapRegion region, int x, int y)
        {
            float roll = VisualHash01(x, y, 0x27D4EB2Fu);
            switch (region)
            {
                case MapRegion.ClashLane:
                {
                    string[] options = { "Rock_0", "Rock_1", "Rock_2", "Pillar_0", "Pillar_1", "RuinWall", "Crystal" };
                    return options[(int)(roll * options.Length) % options.Length];
                }
                case MapRegion.FarmLane:
                {
                    string[] options = { "Tree_0", "Tree_1", "Tree_2", "Bush_0", "Bush_1", "Mushroom_0", "Mushroom_1", "Flower_0", "Flower_1", "Flower_2", "Flower_3" };
                    return options[(int)(roll * options.Length) % options.Length];
                }
                case MapRegion.MidLane:
                case MapRegion.HeartPlaza:
                {
                    string[] options = { "Pillar_0", "Pillar_1", "RuinWall", "Rock_0", "Rock_1", "Rock_2" };
                    return options[(int)(roll * options.Length) % options.Length];
                }
                case MapRegion.River:
                case MapRegion.Pit:
                {
                    string[] options = { "Rock_0", "Rock_1", "Rock_2", "Crystal", "Mushroom_0", "Mushroom_1" };
                    return options[(int)(roll * options.Length) % options.Length];
                }
                case MapRegion.Base:
                {
                    string[] options = { "Pillar_0", "Pillar_1", "Crystal" };
                    return options[(int)(roll * options.Length) % options.Length];
                }
                default:
                {
                    string[] options = { "Tree_0", "Tree_1", "Tree_2", "Tree_0", "Tree_1", "Tree_2", "Tree_0", "Tree_1", "Tree_2", "Bush_0", "Bush_1", "Rock_0", "Rock_1", "Rock_2" };
                    return options[(int)(roll * options.Length) % options.Length];
                }
            }
        }

        private static int VisualBuildZones(Transform root, List<VisualZone> zones)
        {
            var zonesParent = new GameObject("Zones");
            zonesParent.transform.SetParent(root, false);
            Sprite circle = LoadSprite(VfxFolder + "Circle.png");
            Sprite ring = LoadSprite(VfxFolder + "Ring.png");
            Sprite bush0 = LoadSprite(EnvironmentArtFolder + "/Vegetation/Bush_0.png");
            Sprite bush1 = LoadSprite(EnvironmentArtFolder + "/Vegetation/Bush_1.png");
            Sprite ruinWall = LoadSprite(EnvironmentArtFolder + "/Ruins/RuinWall.png");
            Sprite pillar0 = LoadSprite(EnvironmentArtFolder + "/Ruins/Pillar_0.png");
            Sprite pillar1 = LoadSprite(EnvironmentArtFolder + "/Ruins/Pillar_1.png");
            int count = 0;
            for (int i = 0; i < zones.Count; i++)
            {
                VisualZone zone = zones[i];
                bool added = false;
                switch (zone.type)
                {
                    case MapZoneType.Brush:
                        if (circle != null)
                        {
                            VisualAddFilledCircle(zonesParent.transform, "Brush_" + i, circle, zone.position, zone.radius, new Color(0.20f, 0.60f, 0.25f, 0.35f), "Ground", 3);
                            added = true;
                        }
                        if (bush0 != null || bush1 != null)
                        {
                            for (int b = 0; b < 5; b++)
                            {
                                float angle = VisualHash01(i, b, 0x1B873593u) * Mathf.PI * 2f;
                                float distance = VisualHash01(i, b, 0x27D4EB2Fu) * 0.7f * zone.radius;
                                var pos = zone.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                                Sprite bush = b % 2 == 0 ? bush0 : bush1;
                                if (bush == null)
                                {
                                    bush = b % 2 == 0 ? bush1 : bush0;
                                }
                                if (bush == null)
                                {
                                    continue;
                                }
                                VisualAddSprite(zonesParent.transform, "BrushBush_" + i + "_" + b, bush, pos, "Actors", 0);
                            }
                            added = true;
                        }
                        break;
                    case MapZoneType.BuffClearing:
                        if (ring != null)
                        {
                            VisualAddRing(zonesParent.transform, "BuffClearing_" + i, ring, zone.position, zone.radius, new Color(1f, 0.55f, 0.2f, 0.55f), "GroundDecor", 1);
                            added = true;
                        }
                        break;
                    case MapZoneType.SmallClearing:
                        if (ring != null)
                        {
                            VisualAddRing(zonesParent.transform, "SmallClearing_" + i, ring, zone.position, zone.radius, new Color(0.95f, 0.9f, 0.35f, 0.5f), "GroundDecor", 1);
                            added = true;
                        }
                        break;
                    case MapZoneType.OptionalClearing:
                        if (ring != null)
                        {
                            VisualAddRing(zonesParent.transform, "OptionalClearing_" + i, ring, zone.position, zone.radius, new Color(0.7f, 0.7f, 0.7f, 0.45f), "GroundDecor", 1);
                            added = true;
                        }
                        break;
                    case MapZoneType.Pit:
                        if (ring != null)
                        {
                            VisualAddRing(zonesParent.transform, "Pit_" + i, ring, zone.position, zone.radius, new Color(0.75f, 0.35f, 1f, 0.6f), "GroundDecor", 1);
                            added = true;
                        }
                        for (int s = 0; s < 6; s++)
                        {
                            float angle = s * Mathf.PI * 2f / 6f;
                            var pos = zone.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * zone.radius;
                            Sprite sprite = s % 2 == 0 ? ruinWall : (s % 4 == 1 ? pillar0 : pillar1);
                            if (sprite == null)
                            {
                                sprite = ruinWall;
                            }
                            if (sprite == null)
                            {
                                sprite = pillar0;
                            }
                            if (sprite == null)
                            {
                                sprite = pillar1;
                            }
                            if (sprite == null)
                            {
                                continue;
                            }
                            VisualAddSprite(zonesParent.transform, "PitProp_" + i + "_" + s, sprite, pos, "Actors", 0);
                            added = true;
                        }
                        break;
                    case MapZoneType.HeartPlaza:
                        if (ring != null)
                        {
                            VisualAddRing(zonesParent.transform, "HeartPlaza_" + i, ring, zone.position, zone.radius, new Color(1f, 0.82f, 0.35f, 0.6f), "GroundDecor", 1);
                            added = true;
                        }
                        break;
                    case MapZoneType.Fountain:
                        if (circle != null)
                        {
                            VisualAddFilledCircle(zonesParent.transform, "Fountain_" + i, circle, zone.position, zone.radius, new Color(0.4f, 1f, 0.9f, 0.3f), "Ground", 3);
                            added = true;
                        }
                        break;
                }
                if (added)
                {
                    count++;
                }
            }
            return count;
        }

        private static void VisualAddRing(Transform parent, string name, Sprite ring, Vector2 position, float radius, Color color, string sortingLayer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            float scale = (radius * 2f) / ring.bounds.size.x;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ring;
            sr.color = color;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
        }

        private static void VisualAddFilledCircle(Transform parent, string name, Sprite circle, Vector2 position, float radius, Color color, string sortingLayer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            float scale = (radius * 2f) / circle.bounds.size.x;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.color = color;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
        }

        private static void VisualAddSprite(Transform parent, string name, Sprite sprite, Vector2 position, string sortingLayer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        private static int VisualBuildLights(Transform root, List<VisualZone> zones)
        {
            var lightsParent = new GameObject("Lights");
            lightsParent.transform.SetParent(root, false);
            int count = 0;
            for (int i = 0; i < zones.Count && count < 5; i++)
            {
                VisualZone zone = zones[i];
                Color color = Color.white;
                bool valid = true;
                switch (zone.type)
                {
                    case MapZoneType.HeartPlaza:
                        color = new Color(1f, 0.85f, 0.5f);
                        break;
                    case MapZoneType.Pit:
                        color = new Color(0.7f, 0.4f, 1f);
                        break;
                    case MapZoneType.Fountain:
                        color = new Color(0.4f, 1f, 0.9f);
                        break;
                    default:
                        valid = false;
                        break;
                }
                if (!valid)
                {
                    continue;
                }
                var go = new GameObject("Light_" + count);
                go.transform.SetParent(lightsParent.transform, false);
                go.transform.position = new Vector3(zone.position.x, zone.position.y, 0f);
                var light = go.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.pointLightOuterRadius = 8f;
                light.pointLightInnerRadius = 1f;
                light.intensity = 0.8f;
                light.color = color;
                count++;
            }
            return count;
        }
    }
}
