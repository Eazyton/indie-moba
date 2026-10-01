using System;
using System.Collections.Generic;
using IndieMoba.Combat;
using IndieMoba.Map;
using IndieMoba.Match;
using UnityEngine;

namespace IndieMoba.EditorTools
{
    public static class Match5v5DefaultLayout
    {
        private const float Size = 128f;

        private static Vector2 D(Vector2 p) => new Vector2(p.y, p.x);
        private static Vector2 R(Vector2 p) => new Vector2(Size - p.y, Size - p.x);

        private static readonly (Vector2 a, Vector2 b, float w)[] BlueUpperCorridors =
        {
            (new Vector2(13f, 38f), new Vector2(30f, 42f), 5f),
            (new Vector2(28f, 34f), new Vector2(30f, 42f), 5f),
            (new Vector2(30f, 42f), new Vector2(26f, 58f), 6f),
            (new Vector2(26f, 58f), new Vector2(13f, 66f), 5f),
            (new Vector2(26f, 58f), new Vector2(42f, 70f), 6f),
            (new Vector2(42f, 70f), new Vector2(52f, 72f), 5f),
            (new Vector2(26f, 58f), new Vector2(44f, 48f), 4f),
            (new Vector2(24f, 82f), new Vector2(13f, 94f), 5f),
            (new Vector2(24f, 82f), new Vector2(26f, 58f), 5f),
            (new Vector2(24f, 82f), new Vector2(31f, 86f), 5f),
            (new Vector2(42f, 70f), new Vector2(41f, 80f), 5f),
            (new Vector2(24f, 82f), new Vector2(21f, 100f), 4f),
            (new Vector2(50f, 58f), new Vector2(46f, 52f), 3.5f)
        };

        private static readonly (Vector2 p, MapZoneType type, float r, string name)[] BlueUpperClearings =
        {
            (new Vector2(26f, 58f), MapZoneType.BuffClearing, 5f, "Buff"),
            (new Vector2(24f, 82f), MapZoneType.SmallClearing, 4f, "SmallA"),
            (new Vector2(42f, 70f), MapZoneType.SmallClearing, 4f, "SmallB"),
            (new Vector2(30f, 42f), MapZoneType.OptionalClearing, 3.5f, "Optional")
        };

        private static readonly Vector2[] BlueUpperBrush =
        {
            new Vector2(15f, 65f), new Vector2(19f, 104f), new Vector2(30f, 86f),
            new Vector2(34f, 64f), new Vector2(50f, 58f), new Vector2(49f, 72f)
        };

        private static readonly Vector2[] TopWaypoints =
        {
            new Vector2(13f, 21f), new Vector2(10f, 32f), new Vector2(10f, 56f), new Vector2(10f, 86f), new Vector2(10f, 108f),
            new Vector2(13f, 115f), new Vector2(20f, 118f), new Vector2(42f, 118f), new Vector2(72f, 118f), new Vector2(96f, 118f),
            new Vector2(107f, 115f)
        };

        private static readonly Vector2[] MidWaypoints =
        {
            new Vector2(20f, 20f), new Vector2(28f, 28f), new Vector2(36f, 36f), new Vector2(48f, 48f), new Vector2(64f, 64f),
            new Vector2(80f, 80f), new Vector2(92f, 92f), new Vector2(100f, 100f), new Vector2(108f, 108f)
        };

        private static readonly Vector2[] BottomWaypoints =
        {
            new Vector2(21f, 13f), new Vector2(32f, 10f), new Vector2(56f, 10f), new Vector2(86f, 10f), new Vector2(108f, 10f),
            new Vector2(115f, 13f), new Vector2(118f, 20f), new Vector2(118f, 42f), new Vector2(118f, 72f), new Vector2(118f, 96f),
            new Vector2(115f, 107f)
        };

        private static readonly Vector2[] BlueBase =
        {
            new Vector2(3f, 3f), new Vector2(30f, 3f), new Vector2(30f, 20f), new Vector2(20f, 30f), new Vector2(3f, 30f)
        };

        public static void Populate(MapLayoutData layout)
        {
            layout.Scale = 1f;
            layout.PlayableSize = Size;
            layout.ApronMargin = 8f;
            layout.BorderThickness = 3f;
            layout.CameraBounds = new Rect(-6f, -6f, 140f, 140f);
            layout.OrthographicSize = 9.375f;
            layout.CollisionMode = MapCollisionMode.FilledPolygons;
            layout.CircleSegments = 16;
            layout.BlueFountain = new Vector2(7f, 7f);
            layout.RedFountain = new Vector2(121f, 121f);
            layout.Lanes = BuildLanes();
            layout.Structures = BuildStructures();
            layout.Walkable = BuildWalkable();
            layout.Blockers = BuildBlockers();
            layout.Zones = BuildZones();
        }

        private static MapLaneLayout[] BuildLanes()
        {
            return new[]
            {
                new MapLaneLayout { lane = LaneId.Top, waypoints = (Vector2[])TopWaypoints.Clone(), width = 7f, blueSpawn = new Vector2(13f, 21f), redSpawn = new Vector2(107f, 115f), waveSpawnOffset = 0f },
                new MapLaneLayout { lane = LaneId.Mid, waypoints = (Vector2[])MidWaypoints.Clone(), width = 8f, blueSpawn = new Vector2(20f, 20f), redSpawn = new Vector2(108f, 108f), waveSpawnOffset = 7.8f },
                new MapLaneLayout { lane = LaneId.Bottom, waypoints = (Vector2[])BottomWaypoints.Clone(), width = 7f, blueSpawn = new Vector2(21f, 13f), redSpawn = new Vector2(115f, 107f), waveSpawnOffset = 0f }
            };
        }

        private static MapStructureSlot[] BuildStructures()
        {
            var list = new List<MapStructureSlot>();
            AddTowers(list, LaneId.Top, new Vector2(10f, 28f), new Vector2(10f, 56f), new Vector2(10f, 86f));
            AddTowers(list, LaneId.Mid, new Vector2(24f, 24f), new Vector2(36f, 36f), new Vector2(48f, 48f));
            AddTowers(list, LaneId.Bottom, new Vector2(28f, 10f), new Vector2(56f, 10f), new Vector2(86f, 10f));
            list.Add(Slot("Nexus_Blue", Team.Blue, LaneId.Mid, StructureTier.Nexus, new Vector2(15f, 15f)));
            list.Add(Slot("Nexus_Red", Team.Red, LaneId.Mid, StructureTier.Nexus, R(new Vector2(15f, 15f))));
            return list.ToArray();
        }

        private static void AddTowers(List<MapStructureSlot> list, LaneId lane, Vector2 baseTier, Vector2 inner, Vector2 outer)
        {
            Vector2[] blue = { outer, inner, baseTier };
            StructureTier[] tiers = { StructureTier.Outer, StructureTier.Inner, StructureTier.Base };
            for (int i = 0; i < 3; i++)
            {
                list.Add(Slot("Tower_Blue_" + lane + "_" + tiers[i], Team.Blue, lane, tiers[i], blue[i]));
            }
            for (int i = 0; i < 3; i++)
            {
                list.Add(Slot("Tower_Red_" + lane + "_" + tiers[i], Team.Red, lane, tiers[i], R(blue[i])));
            }
        }

        private static MapStructureSlot Slot(string id, Team team, LaneId lane, StructureTier tier, Vector2 position)
        {
            return new MapStructureSlot { id = id, team = team, lane = lane, tier = tier, position = position };
        }

        private static MapShape[] BuildWalkable()
        {
            var list = new List<MapShape>();
            list.Add(Polygon("Base_Blue", MapRegion.Base, BlueBase));
            list.Add(Polygon("Base_Red", MapRegion.Base, Array.ConvertAll(BlueBase, R)));
            AddLaneStrips(list, "Top", TopWaypoints, 7f, MapRegion.ClashLane);
            AddLaneStrips(list, "Mid", MidWaypoints, 8f, MapRegion.MidLane);
            AddLaneStrips(list, "Bottom", BottomWaypoints, 7f, MapRegion.FarmLane);
            list.Add(Segment("River_Main", MapRegion.River, new Vector2(13f, 115f), new Vector2(115f, 13f), 10f));
            list.Add(Circle("River_PitUpper_Widen", MapRegion.River, new Vector2(38f, 90f), 12f));
            list.Add(Circle("River_PitLower_Widen", MapRegion.River, new Vector2(90f, 38f), 12f));
            list.Add(Circle("Pit_Upper_Basin", MapRegion.Pit, new Vector2(38f, 90f), 6f));
            list.Add(Circle("Pit_Lower_Basin", MapRegion.Pit, new Vector2(90f, 38f), 6f));
            list.Add(Circle("HeartPlaza", MapRegion.HeartPlaza, new Vector2(64f, 64f), 8f));
            list.Add(Segment("Bridge_Upper", MapRegion.River, new Vector2(52f, 72f), new Vector2(56f, 76f), 5f));
            list.Add(Segment("Bridge_Lower", MapRegion.River, new Vector2(72f, 52f), new Vector2(76f, 56f), 5f));

            string[] quadrant = { "BlueUpper", "BlueLower", "RedUpper", "RedLower" };
            for (int q = 0; q < 4; q++)
            {
                for (int i = 0; i < BlueUpperClearings.Length; i++)
                {
                    var c = BlueUpperClearings[i];
                    list.Add(Circle("Clearing_" + quadrant[q] + "_" + c.name, MapRegion.Clearing, Mirror(c.p, q), c.r));
                }
                for (int i = 0; i < BlueUpperCorridors.Length; i++)
                {
                    var c = BlueUpperCorridors[i];
                    list.Add(Segment("Corridor_" + quadrant[q] + "_" + i, MapRegion.Jungle, Mirror(c.a, q), Mirror(c.b, q), c.w));
                }
            }
            return list.ToArray();
        }

        private static MapShape[] BuildBlockers()
        {
            var list = new List<MapShape>();
            AddPitRim(list, "Upper", new Vector2(38f, 90f));
            AddPitRim(list, "Lower", new Vector2(90f, 38f));
            list.Add(Circle("Plaza_Pillar_A", MapRegion.HeartPlaza, new Vector2(58f, 70f), 1.5f));
            list.Add(Circle("Plaza_Pillar_B", MapRegion.HeartPlaza, new Vector2(70f, 58f), 1.5f));
            return list.ToArray();
        }

        private static void AddPitRim(List<MapShape> list, string name, Vector2 center)
        {
            const float inner = 6f;
            const float outer = 8f;
            const int steps = 6;
            float halfGap = Mathf.Asin(2.5f / ((inner + outer) * 0.5f)) * Mathf.Rad2Deg;
            float[] axes = { -45f, 45f, 135f, 225f };
            for (int i = 0; i < axes.Length; i++)
            {
                float start = axes[i] + halfGap;
                float end = axes[i] + 90f - halfGap;
                var points = new List<Vector2>();
                for (int s = 0; s <= steps; s++)
                {
                    float a = Mathf.Lerp(start, end, s / (float)steps) * Mathf.Deg2Rad;
                    points.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * outer);
                }
                for (int s = steps; s >= 0; s--)
                {
                    float a = Mathf.Lerp(start, end, s / (float)steps) * Mathf.Deg2Rad;
                    points.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * inner);
                }
                list.Add(Polygon("Pit_" + name + "_Rim_" + i, MapRegion.Pit, points.ToArray()));
            }
        }

        private static MapZoneLayout[] BuildZones()
        {
            var list = new List<MapZoneLayout>();
            list.Add(Zone("Base_Blue", MapZoneType.Base, Team.Blue, Polygon("Base_Blue", MapRegion.Base, BlueBase)));
            list.Add(Zone("Base_Red", MapZoneType.Base, Team.Red, Polygon("Base_Red", MapRegion.Base, Array.ConvertAll(BlueBase, R))));
            list.Add(Zone("Fountain_Blue", MapZoneType.Fountain, Team.Blue, Circle("Fountain_Blue", MapRegion.Base, new Vector2(7f, 7f), 3f)));
            list.Add(Zone("Fountain_Red", MapZoneType.Fountain, Team.Red, Circle("Fountain_Red", MapRegion.Base, new Vector2(121f, 121f), 3f)));
            list.Add(Zone("River", MapZoneType.River, Team.Neutral, Segment("River", MapRegion.River, new Vector2(13f, 115f), new Vector2(115f, 13f), 10f)));
            list.Add(Zone("Crossing_TopFord", MapZoneType.RiverCrossing, Team.Neutral, Circle("Crossing_TopFord", MapRegion.River, new Vector2(13f, 115f), 5f)));
            list.Add(Zone("Crossing_BridgeUpper", MapZoneType.RiverCrossing, Team.Neutral, Circle("Crossing_BridgeUpper", MapRegion.River, new Vector2(54f, 74f), 3f)));
            list.Add(Zone("Crossing_BridgeLower", MapZoneType.RiverCrossing, Team.Neutral, Circle("Crossing_BridgeLower", MapRegion.River, new Vector2(74f, 54f), 3f)));
            list.Add(Zone("Crossing_BottomFord", MapZoneType.RiverCrossing, Team.Neutral, Circle("Crossing_BottomFord", MapRegion.River, new Vector2(115f, 13f), 5f)));
            list.Add(Zone("Pit_Upper", MapZoneType.Pit, Team.Neutral, Circle("Pit_Upper", MapRegion.Pit, new Vector2(38f, 90f), 8f)));
            list.Add(Zone("Pit_Lower", MapZoneType.Pit, Team.Neutral, Circle("Pit_Lower", MapRegion.Pit, new Vector2(90f, 38f), 8f)));
            list.Add(Zone("HeartPlaza", MapZoneType.HeartPlaza, Team.Neutral, Circle("HeartPlaza", MapRegion.HeartPlaza, new Vector2(64f, 64f), 8f)));

            string[] quadrant = { "BlueUpper", "BlueLower", "RedUpper", "RedLower" };
            for (int q = 0; q < 4; q++)
            {
                Team team = q < 2 ? Team.Blue : Team.Red;
                for (int i = 0; i < BlueUpperClearings.Length; i++)
                {
                    var c = BlueUpperClearings[i];
                    string id = "Clearing_" + quadrant[q] + "_" + c.name;
                    list.Add(Zone(id, c.type, team, Circle(id, MapRegion.Clearing, Mirror(c.p, q), c.r)));
                }
                for (int i = 0; i < BlueUpperBrush.Length; i++)
                {
                    string id = "Brush_" + quadrant[q] + "_" + i;
                    float radius = i % 2 == 0 ? 2.5f : 2f;
                    list.Add(Zone(id, MapZoneType.Brush, Team.Neutral, Circle(id, MapRegion.Jungle, Mirror(BlueUpperBrush[i], q), radius)));
                }
            }
            return list.ToArray();
        }

        private static Vector2 Mirror(Vector2 p, int quadrant)
        {
            switch (quadrant)
            {
                case 1: return D(p);
                case 2: return R(p);
                case 3: return R(D(p));
                default: return p;
            }
        }

        private static void AddLaneStrips(List<MapShape> list, string name, Vector2[] waypoints, float width, MapRegion region)
        {
            for (int i = 1; i < waypoints.Length; i++)
            {
                list.Add(Segment("Lane_" + name + "_" + i, region, waypoints[i - 1], waypoints[i], width));
            }
        }

        private static MapZoneLayout Zone(string id, MapZoneType type, Team team, MapShape shape)
        {
            return new MapZoneLayout { id = id, type = type, team = team, shape = shape };
        }

        private static MapShape Segment(string id, MapRegion region, Vector2 a, Vector2 b, float width)
        {
            return new MapShape { id = id, kind = MapShapeKind.Segment, region = region, a = a, b = b, width = width };
        }

        private static MapShape Circle(string id, MapRegion region, Vector2 center, float radius)
        {
            return new MapShape { id = id, kind = MapShapeKind.Circle, region = region, a = center, radius = radius };
        }

        private static MapShape Polygon(string id, MapRegion region, Vector2[] points)
        {
            return new MapShape { id = id, kind = MapShapeKind.Polygon, region = region, points = (Vector2[])points.Clone() };
        }
    }
}
