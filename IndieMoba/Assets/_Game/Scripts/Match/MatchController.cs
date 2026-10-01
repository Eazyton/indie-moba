using System;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;
using IndieMoba.Structures;

namespace IndieMoba.Match
{
    public enum MatchState
    {
        Playing = 0,
        BlueVictory = 1,
        RedVictory = 2
    }

    [DisallowMultipleComponent]
    public sealed class MatchController : MonoBehaviour
    {
        [SerializeField] private SimulationTickRunner runner;
        [SerializeField] private NexusObjective[] nexuses;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private WaveSpawner[] waveSpawners;
        [SerializeField] private Team localTeam = Team.Blue;
        [SerializeField] private bool pauseSimulationOnEnd = true;

        private MatchState state = MatchState.Playing;
        private bool pausedSimulation;

        public event Action<MatchState> MatchEnded;

        public MatchState State => state;
        public Team LocalTeam => localTeam;
        public Team WinningTeam
        {
            get
            {
                switch (state)
                {
                    case MatchState.BlueVictory: return Team.Blue;
                    case MatchState.RedVictory: return Team.Red;
                    default: return Team.Neutral;
                }
            }
        }
        public bool IsLocalVictory => state == MatchState.Playing ? false : WinningTeam == localTeam;
        public SimulationTickRunner Runner => runner;

        private void HandleNexusDestroyed(NexusObjective nexus)
        {
            if (state != MatchState.Playing)
            {
                return;
            }
            state = nexus.Team == Team.Blue ? MatchState.RedVictory : MatchState.BlueVictory;
            if (waveSpawner != null)
            {
                waveSpawner.SetSpawningEnabled(false);
            }
            if (waveSpawners != null)
            {
                for (int i = 0; i < waveSpawners.Length; i++)
                {
                    if (waveSpawners[i] != null)
                    {
                        waveSpawners[i].SetSpawningEnabled(false);
                    }
                }
            }
            if (pauseSimulationOnEnd && runner != null)
            {
                runner.Paused = true;
                pausedSimulation = true;
            }
            MatchEnded?.Invoke(state);
        }

        private void Awake()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<SimulationTickRunner>();
            }
            if (runner != null)
            {
                runner.Paused = false;
            }
        }

        private void OnEnable()
        {
            if (nexuses == null)
            {
                return;
            }
            for (int i = 0; i < nexuses.Length; i++)
            {
                if (nexuses[i] != null)
                {
                    nexuses[i].NexusDestroyed += HandleNexusDestroyed;
                }
            }
        }

        private void OnDisable()
        {
            if (nexuses != null)
            {
                for (int i = 0; i < nexuses.Length; i++)
                {
                    if (nexuses[i] != null)
                    {
                        nexuses[i].NexusDestroyed -= HandleNexusDestroyed;
                    }
                }
            }
            if (pausedSimulation && runner != null)
            {
                runner.Paused = false;
                pausedSimulation = false;
            }
        }
    }
}
