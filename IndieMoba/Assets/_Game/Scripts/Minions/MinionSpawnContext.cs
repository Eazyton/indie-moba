using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Minions
{
    public readonly struct MinionSpawnContext
    {
        public readonly Team Team;
        public readonly IReadOnlyList<Vector2> Waypoints;
        public readonly Vector2 LaneNormal;
        public readonly float LateralOffset;
        public readonly CombatWorld World;
        public readonly SimulationTickRunner Runner;
        public readonly MinionRegistry Registry;
        public readonly int SpawnIndex;

        public MinionSpawnContext(Team team, IReadOnlyList<Vector2> waypoints, Vector2 laneNormal, float lateralOffset,
            CombatWorld world, SimulationTickRunner runner, MinionRegistry registry, int spawnIndex)
        {
            Team = team;
            Waypoints = waypoints;
            LaneNormal = laneNormal;
            LateralOffset = lateralOffset;
            World = world;
            Runner = runner;
            Registry = registry;
            SpawnIndex = spawnIndex;
        }
    }
}
