using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieMoba.Core
{
    [DefaultExecutionOrder(10)]
    [DisallowMultipleComponent]
    public sealed class SimulationTickRunner : MonoBehaviour
    {
        [Min(1f)] [SerializeField] private float tickRate = 60f;
        [Min(1)] [SerializeField] private int maxTicksPerFrame = 5;

        private readonly List<ISimulationSystem> systems = new List<ISimulationSystem>();
        private readonly HashSet<ISimulationSystem> active = new HashSet<ISimulationSystem>();
        private ISimulationSystem[] snapshot = Array.Empty<ISimulationSystem>();
        private bool dirty;
        private float accumulator;
        private int currentTick;

        public event Action<int> TickCompleted;

        public bool Paused { get; set; }

        public float TickRate => tickRate;
        public float TickDelta => 1f / tickRate;
        public int CurrentTick => currentTick;
        public float InterpolationAlpha => Mathf.Clamp01(accumulator / TickDelta);

        public void Register(ISimulationSystem system)
        {
            if (system == null || !active.Add(system))
            {
                return;
            }
            int index = systems.Count;
            while (index > 0 && systems[index - 1].SimulationOrder > system.SimulationOrder)
            {
                index--;
            }
            systems.Insert(index, system);
            dirty = true;
        }

        public void Unregister(ISimulationSystem system)
        {
            if (system == null || !active.Remove(system))
            {
                return;
            }
            systems.Remove(system);
            dirty = true;
        }

        public void Step()
        {
            if (dirty)
            {
                snapshot = systems.ToArray();
                dirty = false;
            }
            float dt = TickDelta;
            for (int i = 0; i < snapshot.Length; i++)
            {
                ISimulationSystem system = snapshot[i];
                if (active.Contains(system))
                {
                    try
                    {
                        system.SimulateTick(dt, currentTick);
                    }
                    catch (Exception exception)
                    {
                        UnityEngine.Object context = system as UnityEngine.Object;
                        Debug.LogError($"SimulationTickRunner: {system.GetType().Name} ({(context != null ? context.name : "non-Unity system")}) threw at tick {currentTick}; continuing with remaining systems.", context);
                        Debug.LogException(exception, context);
                    }
                }
            }
            currentTick++;
            TickCompleted?.Invoke(currentTick);
        }

        private void Update()
        {
            if (Paused)
            {
                accumulator = 0f;
                return;
            }
            accumulator += Time.deltaTime;
            int ticks = 0;
            while (accumulator >= TickDelta && ticks < maxTicksPerFrame)
            {
                accumulator -= TickDelta;
                Step();
                ticks++;
            }
            if (ticks == maxTicksPerFrame && accumulator >= TickDelta)
            {
                accumulator %= TickDelta;
            }
        }
    }
}
