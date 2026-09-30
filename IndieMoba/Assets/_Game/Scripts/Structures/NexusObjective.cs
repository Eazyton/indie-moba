using System;
using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Structures
{
    [DisallowMultipleComponent]
    public sealed class NexusObjective : MonoBehaviour
    {
        [SerializeField] private Structure structure;

        public event Action<NexusObjective> NexusDestroyed;

        public Team Team => structure != null ? structure.Team : Team.Neutral;
        public Structure Structure => structure;
        public bool IsDestroyed => structure == null || structure.IsDestroyed;

        private void Awake()
        {
            if (structure == null)
            {
                structure = GetComponent<Structure>();
            }
        }

        private void OnEnable()
        {
            if (structure != null)
            {
                structure.Destroyed += HandleDestroyed;
            }
        }

        private void OnDisable()
        {
            if (structure != null)
            {
                structure.Destroyed -= HandleDestroyed;
            }
        }

        private void HandleDestroyed(Structure _)
        {
            NexusDestroyed?.Invoke(this);
        }
    }
}
