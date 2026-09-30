using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;
using IndieMoba.Minions;

namespace IndieMoba.Match
{
    [DisallowMultipleComponent]
    public sealed class WaveSpawner : MonoBehaviour, ISimulationSystem
    {
        [Serializable]
        public sealed class TeamSpawn
        {
            public Team team;
            public Transform spawnPoint;
            public MinionController meleePrefab;
            public MinionController rangedPrefab;
            public bool enabled = true;
        }

        private struct PendingSpawn
        {
            public int Tick;
            public int TeamIndex;
            public MinionAttackType Type;
            public int Slot;
        }

        [SerializeField] private SimulationTickRunner runner;
        [SerializeField] private CombatWorld world;
        [SerializeField] private MinionRegistry registry;
        [SerializeField] private WaveConfig config;
        [SerializeField] private LanePath lane;
        [SerializeField] private TeamSpawn[] teams;
        [SerializeField] private Transform minionParent;
        [SerializeField] private bool spawningEnabled = true;

        private readonly List<PendingSpawn> pendingSpawns = new List<PendingSpawn>();
        private IMinionFactory factory;
        private int elapsedTicks;
        private int nextWaveTick = -1;
        private int waveNumber;
        private int nextSpawnIndex;

        public event Action<int> WaveStarted;

        public int WaveNumber => waveNumber;
        public bool SpawningEnabled => spawningEnabled;
        public MinionRegistry Registry => registry;
        public int SimulationOrder => Core.SimulationOrder.Spawning;

        public float SecondsUntilNextWave
        {
            get
            {
                if (runner == null)
                {
                    return 0f;
                }
                return Mathf.Max(0f, (nextWaveTick - elapsedTicks) / runner.TickRate);
            }
        }

        public void SetSpawningEnabled(bool value)
        {
            spawningEnabled = value;
        }

        public void SetFactory(IMinionFactory value)
        {
            factory = value ?? new InstantiateMinionFactory();
        }

        public void SimulateTick(float deltaTime, int tick)
        {
            if (!spawningEnabled)
            {
                return;
            }
            elapsedTicks++;
            if (nextWaveTick < 0)
            {
                nextWaveTick = Mathf.RoundToInt(config.FirstWaveDelay * runner.TickRate);
            }
            if (elapsedTicks >= nextWaveTick && (config.MaxWaves == 0 || waveNumber < config.MaxWaves))
            {
                waveNumber++;
                ScheduleWave();
                nextWaveTick += Mathf.Max(1, Mathf.RoundToInt(config.WaveInterval * runner.TickRate));
                WaveStarted?.Invoke(waveNumber);
            }
            ProcessDueSpawns();
        }

        private void ScheduleWave()
        {
            if (config == null || config.Composition == null || teams == null)
            {
                return;
            }
            for (int i = 0; i < config.Composition.Count; i++)
            {
                int tick = elapsedTicks + Mathf.RoundToInt(i * config.SpawnGap * runner.TickRate);
                for (int t = 0; t < teams.Length; t++)
                {
                    TeamSpawn teamSpawn = teams[t];
                    if (teamSpawn == null || !teamSpawn.enabled)
                    {
                        continue;
                    }
                    pendingSpawns.Add(new PendingSpawn
                    {
                        Tick = tick,
                        TeamIndex = t,
                        Type = config.Composition[i],
                        Slot = i
                    });
                }
            }
        }

        private void ProcessDueSpawns()
        {
            for (int i = pendingSpawns.Count - 1; i >= 0; i--)
            {
                PendingSpawn pending = pendingSpawns[i];
                if (pending.Tick > elapsedTicks)
                {
                    continue;
                }
                pendingSpawns.RemoveAt(i);
                SpawnMinion(pending);
            }
        }

        private void SpawnMinion(in PendingSpawn pending)
        {
            TeamSpawn teamSpawn = teams[pending.TeamIndex];
            MinionController prefab = pending.Type == MinionAttackType.Ranged ? teamSpawn.rangedPrefab : teamSpawn.meleePrefab;
            if (prefab == null || teamSpawn.spawnPoint == null)
            {
                Debug.LogWarning($"{name}: skipping minion spawn, prefab or spawn point is null.", this);
                return;
            }
            if (lane == null || config == null || world == null || runner == null || registry == null)
            {
                Debug.LogWarning($"{name}: skipping minion spawn, lane/world/runner/registry missing.", this);
                return;
            }
            List<Vector2> waypoints = new List<Vector2>();
            lane.GetWaypoints(teamSpawn.team, waypoints);
            Vector2 normal = lane.GetLaneNormal(teamSpawn.team);
            float offset = 0f;
            if (config.LateralOffsets != null && config.LateralOffsets.Count > 0)
            {
                offset = config.LateralOffsets[pending.Slot % config.LateralOffsets.Count];
            }
            Vector2 position = (Vector2)teamSpawn.spawnPoint.position + normal * offset;
            MinionSpawnContext context = new MinionSpawnContext(teamSpawn.team, waypoints, normal, offset, world, runner, registry, nextSpawnIndex++);
            factory.Spawn(prefab, position, context, minionParent);
        }

        private void Awake()
        {
            factory = new InstantiateMinionFactory();
        }

        private void OnEnable()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<SimulationTickRunner>();
            }
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
            }
            if (runner != null)
            {
                runner.Register(this);
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.Unregister(this);
            }
        }
    }
}
