using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Minions
{
    [DisallowMultipleComponent]
    public sealed class MinionRegistry : MonoBehaviour, ILaneEngagementProvider
    {
        private readonly List<MinionController> minions = new List<MinionController>();
        private readonly Dictionary<ICombatTarget, MinionController> byTarget = new Dictionary<ICombatTarget, MinionController>();

        public event Action<MinionController> MinionRegistered;
        public event Action<MinionController> MinionUnregistered;

        public IReadOnlyList<MinionController> Minions => minions;

        public void Register(MinionController minion)
        {
            if (minion == null || minions.Contains(minion))
            {
                return;
            }
            minions.Add(minion);
            if (minion.CombatTarget != null)
            {
                byTarget[minion.CombatTarget] = minion;
            }
            MinionRegistered?.Invoke(minion);
        }

        public void Unregister(MinionController minion)
        {
            if (minion == null || !minions.Remove(minion))
            {
                return;
            }
            if (minion.CombatTarget != null)
            {
                byTarget.Remove(minion.CombatTarget);
            }
            MinionUnregistered?.Invoke(minion);
        }

        public ICombatTarget GetEngagedTarget(ICombatTarget unit)
        {
            if (unit == null || !byTarget.TryGetValue(unit, out MinionController minion))
            {
                return null;
            }
            return minion != null ? minion.CurrentTarget : null;
        }

        public int CountAlive(Team team)
        {
            int count = 0;
            for (int i = 0; i < minions.Count; i++)
            {
                MinionController minion = minions[i];
                if (minion != null && minion.IsAlive && minion.Team == team)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
