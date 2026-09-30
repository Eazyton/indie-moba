using UnityEngine;
using UnityEngine.InputSystem;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Presentation
{
    public sealed class HeroReviveTool : MonoBehaviour, ISimulationSystem
    {
        [SerializeField] private SimulationTickRunner runner;
        [SerializeField] private Health heroHealth;
        [SerializeField] private HeroActor heroActor;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private bool enableShortcut = true;

        private bool pending;

        public int SimulationOrder => Core.SimulationOrder.Input;
        public bool CanRevive => heroHealth != null && heroHealth.IsDead;

        public void RequestRevive()
        {
            pending = true;
        }

        private void Update()
        {
            if (!enableShortcut)
            {
                return;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame)
            {
                RequestRevive();
            }
        }

        public void SimulateTick(float deltaTime, int tick)
        {
            if (!pending)
            {
                return;
            }
            pending = false;
            if (heroHealth == null)
            {
                return;
            }
            heroHealth.ResetHealth();
            if (heroActor != null && respawnPoint != null)
            {
                heroActor.Teleport(respawnPoint.position);
            }
        }

        private void OnEnable()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<SimulationTickRunner>();
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
