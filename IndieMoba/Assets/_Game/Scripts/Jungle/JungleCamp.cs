using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Jungle
{
    public enum JungleCampState
    {
        WaitingFirstSpawn = 0,
        Idle = 1,
        Engaged = 2,
        Resetting = 3,
        Cleared = 4
    }

    // Strategic authority of one camp: aggro, shared target, leash, reset, clear and respawn.
    // Its monsters only execute orders. Ticked by JungleDirector (SimulationOrder.Neutral).
    [DisallowMultipleComponent]
    public sealed class JungleCamp : MonoBehaviour
    {
        [SerializeField] private JungleCampConfig config;
        [SerializeField] private string campId;
        [SerializeField] private JungleMonster[] members = Array.Empty<JungleMonster>();
        [SerializeField] private JungleMonster buffCarrier;

        private readonly List<JungleMonster> runtimeMembers = new List<JungleMonster>();
        private readonly List<ICombatTarget> attackers = new List<ICombatTarget>();
        private CombatWorld world;
        private JungleCampState state;
        private ICombatTarget currentTarget;
        private float spawnTimer;
        private float timeSinceHeroDamage;
        private float returnElapsed;
        private bool initialized;

        public event Action<JungleCamp, JungleCampState> StateChanged;

        public JungleCampConfig Config => config;
        public string CampId => string.IsNullOrEmpty(campId) ? name : campId;
        public IReadOnlyList<JungleMonster> Members => runtimeMembers;
        public JungleMonster BuffCarrier => buffCarrier;
        public JungleCampState State => state;
        public ICombatTarget CurrentTarget => currentTarget;
        public Vector2 Center => transform.position;
        public float SpawnTimeRemaining => state == JungleCampState.WaitingFirstSpawn || state == JungleCampState.Cleared ? Mathf.Max(0f, spawnTimer) : 0f;
        public float TimeSinceHeroDamage => timeSinceHeroDamage;
        public bool IsInitialized => initialized;

        public int AliveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < runtimeMembers.Count; i++)
                {
                    if (runtimeMembers[i].IsAlive)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public bool Initialize(SimulationTickRunner runner, CombatWorld combatWorld)
        {
            if (config == null)
            {
                Debug.LogError($"{name}: JungleCamp has no JungleCampConfig.", this);
                return false;
            }
            world = combatWorld;
            runtimeMembers.Clear();
            for (int i = 0; i < members.Length; i++)
            {
                JungleMonster monster = members[i];
                if (monster != null && monster.Initialize(this, runner, combatWorld))
                {
                    runtimeMembers.Add(monster);
                    monster.Despawn();
                }
            }
            if (runtimeMembers.Count == 0)
            {
                Debug.LogWarning($"{name}: JungleCamp has no valid members.", this);
            }
            spawnTimer = config.FirstSpawnTime;
            SetState(JungleCampState.WaitingFirstSpawn);
            initialized = true;
            return true;
        }

        public void Tick(float deltaTime, int tick)
        {
            if (!initialized)
            {
                return;
            }
            for (int i = 0; i < runtimeMembers.Count; i++)
            {
                runtimeMembers[i].TickDead(deltaTime);
            }
            switch (state)
            {
                case JungleCampState.WaitingFirstSpawn:
                case JungleCampState.Cleared:
                    spawnTimer -= deltaTime;
                    if (spawnTimer <= 0f)
                    {
                        SpawnAll();
                    }
                    break;
                case JungleCampState.Idle:
                    for (int i = 0; i < runtimeMembers.Count; i++)
                    {
                        if (runtimeMembers[i].IsAlive)
                        {
                            runtimeMembers[i].TickIdle(deltaTime);
                        }
                    }
                    break;
                case JungleCampState.Engaged:
                    TickEngaged(deltaTime, tick);
                    break;
                case JungleCampState.Resetting:
                    TickResetting(deltaTime);
                    break;
            }
        }

        // Called by the director when a hero damages one of this camp's monsters.
        public void NotifyHeroDamage(ICombatTarget attacker)
        {
            if (state != JungleCampState.Idle && state != JungleCampState.Engaged)
            {
                return;
            }
            if (attacker != null && !attackers.Contains(attacker))
            {
                attackers.Add(attacker);
            }
            timeSinceHeroDamage = 0f;
            if (state == JungleCampState.Idle)
            {
                currentTarget = IsValidTarget(attacker) ? attacker : SelectTarget();
                if (currentTarget == null)
                {
                    // Hit from outside the leash (long-range poke): the camp cannot respond, so it resets
                    // (immune while regenerating) instead of staying damaged forever.
                    BeginReset();
                    return;
                }
                SetState(JungleCampState.Engaged);
            }
            else if (!IsValidTarget(currentTarget))
            {
                currentTarget = SelectTarget();
            }
        }

        // Called by the director when one of this camp's monsters dies. Returns true when the camp is cleared.
        public bool NotifyMonsterKilled(JungleMonster monster)
        {
            if (AliveCount > 0)
            {
                return false;
            }
            currentTarget = null;
            attackers.Clear();
            spawnTimer = config.RespawnTime;
            SetState(JungleCampState.Cleared);
            return true;
        }

        // Debug: skip the remaining (first) spawn / respawn timer.
        public void ForceSpawnNow()
        {
            if (state == JungleCampState.WaitingFirstSpawn || state == JungleCampState.Cleared)
            {
                spawnTimer = 0f;
            }
        }

        private void TickEngaged(float deltaTime, int tick)
        {
            timeSinceHeroDamage += deltaTime;
            if (timeSinceHeroDamage >= config.ResetAfterNoDamage)
            {
                BeginReset();
                return;
            }
            // Retarget only when the target became invalid; validity is re-evaluated periodically.
            bool periodic = tick % config.RetargetIntervalTicks == 0;
            if (currentTarget == null || !currentTarget.IsAlive || (periodic && !IsValidTarget(currentTarget)))
            {
                currentTarget = SelectTarget();
                if (currentTarget == null)
                {
                    BeginReset();
                    return;
                }
            }
            for (int i = 0; i < runtimeMembers.Count; i++)
            {
                JungleMonster monster = runtimeMembers[i];
                if (monster.IsAlive)
                {
                    monster.TickCombat(deltaTime, currentTarget, Center, config.LeashRadius, runtimeMembers);
                }
            }
        }

        private void TickResetting(float deltaTime)
        {
            returnElapsed += deltaTime;
            bool allHome = true;
            for (int i = 0; i < runtimeMembers.Count; i++)
            {
                JungleMonster monster = runtimeMembers[i];
                if (!monster.IsAlive)
                {
                    continue;
                }
                if (!monster.TickReturn(deltaTime, config.ReturnSpeed, config.ResetHealDuration))
                {
                    allHome = false;
                }
            }
            if (allHome || returnElapsed >= config.ReturnTimeout)
            {
                for (int i = 0; i < runtimeMembers.Count; i++)
                {
                    runtimeMembers[i].FinishReset();
                }
                SetState(JungleCampState.Idle);
            }
        }

        private void BeginReset()
        {
            currentTarget = null;
            attackers.Clear();
            returnElapsed = 0f;
            for (int i = 0; i < runtimeMembers.Count; i++)
            {
                runtimeMembers[i].BeginReturn();
            }
            SetState(JungleCampState.Resetting);
        }

        private void SpawnAll()
        {
            currentTarget = null;
            attackers.Clear();
            timeSinceHeroDamage = 0f;
            for (int i = 0; i < runtimeMembers.Count; i++)
            {
                runtimeMembers[i].Spawn();
            }
            SetState(JungleCampState.Idle);
        }

        private bool IsValidTarget(ICombatTarget t)
        {
            if (t == null || !t.IsAlive || t.Kind != CombatTargetKind.Hero || t.IsInvulnerable || !TeamRules.AreHostile(Team.Neutral, t.Team))
            {
                return false;
            }
            float reach = config.LeashRadius + t.Radius;
            return (t.Position - Center).sqrMagnitude <= reach * reach;
        }

        // Preference: heroes that damaged the camp (nearest to center), then any hero within acquire radius.
        private ICombatTarget SelectTarget()
        {
            ICombatTarget best = null;
            float bestDistance = float.MaxValue;
            for (int i = attackers.Count - 1; i >= 0; i--)
            {
                ICombatTarget attacker = attackers[i];
                if (attacker == null || !attacker.IsAlive)
                {
                    attackers.RemoveAt(i);
                    continue;
                }
                if (!IsValidTarget(attacker))
                {
                    continue;
                }
                float d = (attacker.Position - Center).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = attacker;
                }
            }
            if (best != null || world == null)
            {
                return best;
            }
            IReadOnlyList<ICombatTarget> targets = world.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatTarget candidate = targets[i];
                if (!IsValidTarget(candidate))
                {
                    continue;
                }
                float reach = config.AcquireRadius + candidate.Radius;
                float d = (candidate.Position - Center).sqrMagnitude;
                if (d <= reach * reach && d < bestDistance)
                {
                    bestDistance = d;
                    best = candidate;
                }
            }
            return best;
        }

        private void SetState(JungleCampState next)
        {
            if (state == next && initialized)
            {
                return;
            }
            state = next;
            StateChanged?.Invoke(this, next);
        }

        private void OnDrawGizmos()
        {
            if (config == null)
            {
                return;
            }
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.5f);
            DrawCircle(transform.position, config.LeashRadius);
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.35f);
            DrawCircle(transform.position, config.AcquireRadius);
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            const int segments = 32;
            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
