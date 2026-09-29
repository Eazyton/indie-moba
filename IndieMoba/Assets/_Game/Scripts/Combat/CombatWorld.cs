using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Core;

namespace IndieMoba.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatWorld : MonoBehaviour
    {
        [SerializeField] private SimulationTickRunner runner;
        [Min(0f)] [SerializeField] private float hitTolerance = 0.125f;

        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<ProjectileState> projectiles = new List<ProjectileState>();
        private readonly Stack<ProjectileState> projectilePool = new Stack<ProjectileState>();
        private readonly List<DelayedAreaState> areas = new List<DelayedAreaState>();
        private readonly List<PendingDamage> pendingDamage = new List<PendingDamage>();
        private readonly List<ICombatTarget> queryBuffer = new List<ICombatTarget>();
        private IBasicAttackTargetSelector basicAttackSelector = new ConeTargetSelector();
        private ProjectileStage projectileStage;
        private DamageStage damageStage;
        private int nextId = 1;

        public event Action<ProjectileState> ProjectileSpawned;
        public event Action<ProjectileState, bool> ProjectileDestroyed;
        public event Action<DelayedAreaState> AreaStarted;
        public event Action<DelayedAreaState> AreaResolved;
        public event Action<ICombatTarget, DamageResult> DamageApplied;

        public IReadOnlyList<ICombatTarget> Targets => targets;
        public IReadOnlyList<ProjectileState> Projectiles => projectiles;
        public IReadOnlyList<DelayedAreaState> Areas => areas;
        public SimulationTickRunner Runner => runner;

        public void SetBasicAttackSelector(IBasicAttackTargetSelector selector)
        {
            basicAttackSelector = selector ?? new ConeTargetSelector();
        }

        public void RegisterTarget(ICombatTarget target)
        {
            if (target != null && !targets.Contains(target))
            {
                targets.Add(target);
            }
        }

        public void UnregisterTarget(ICombatTarget target)
        {
            targets.Remove(target);
        }

        public ICombatTarget FindBasicAttackTarget(in BasicAttackQuery query)
        {
            return basicAttackSelector.Select(query, targets);
        }

        public int FindHostileTargetsInCircle(Vector2 center, float radius, Team team, List<ICombatTarget> results)
        {
            results.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatTarget target = targets[i];
                if (!target.IsAlive || !TeamRules.AreHostile(team, target.Team))
                {
                    continue;
                }
                float reach = radius + target.Radius;
                if ((target.Position - center).sqrMagnitude <= reach * reach)
                {
                    results.Add(target);
                }
            }
            return results.Count;
        }

        public void QueueDamage(ICombatTarget target, in DamageInfo info)
        {
            if (target != null && target.Damageable != null)
            {
                pendingDamage.Add(new PendingDamage(target, info));
            }
        }

        public ProjectileState SpawnProjectile(in ProjectileSpec spec)
        {
            ProjectileState p = projectilePool.Count > 0 ? projectilePool.Pop() : new ProjectileState();
            p.Id = nextId++;
            p.Motion = spec.Motion;
            p.Position = spec.Origin;
            p.PreviousPosition = spec.Origin;
            p.Target = spec.Target;
            p.LastTargetPosition = spec.Target != null ? spec.Target.Position : spec.Origin + spec.Direction;
            Vector2 direction = spec.Motion == ProjectileMotion.Homing ? p.LastTargetPosition - spec.Origin : spec.Direction;
            p.Direction = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector2.right;
            p.Speed = spec.Speed;
            p.HitRadius = spec.HitRadius;
            p.MaxDistance = spec.MaxDistance;
            p.Traveled = 0f;
            p.Lifetime = 0f;
            p.MaxLifetime = spec.MaxLifetime > 0f ? spec.MaxLifetime : 5f;
            p.Damage = spec.Damage;
            p.ViewPrefab = spec.ViewPrefab;
            p.Alive = true;
            projectiles.Add(p);
            ProjectileSpawned?.Invoke(p);
            return p;
        }

        public DelayedAreaState SpawnDelayedArea(in AreaSpec spec)
        {
            DelayedAreaState area = new DelayedAreaState
            {
                Id = nextId++,
                Center = spec.Center,
                Radius = spec.Radius,
                Delay = Mathf.Max(0f, spec.Delay),
                Elapsed = 0f,
                Damage = spec.Damage,
                Resolved = false
            };
            areas.Add(area);
            AreaStarted?.Invoke(area);
            return area;
        }

        private void SimulateProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                ProjectileState p = projectiles[i];
                p.PreviousPosition = p.Position;
                p.Lifetime += dt;
                bool hit = p.Motion == ProjectileMotion.Homing ? StepHoming(p, dt) : StepLinear(p, dt);
                bool expired = p.Lifetime >= p.MaxLifetime || (p.MaxDistance > 0f && p.Traveled >= p.MaxDistance);
                if (hit || expired)
                {
                    projectiles.RemoveAt(i);
                    p.Alive = false;
                    ProjectileDestroyed?.Invoke(p, hit);
                    p.Target = null;
                    p.ViewPrefab = null;
                    projectilePool.Push(p);
                }
            }
        }

        private bool StepHoming(ProjectileState p, float dt)
        {
            bool targetValid = p.Target != null && p.Target.IsAlive;
            if (targetValid)
            {
                p.LastTargetPosition = p.Target.Position;
            }
            Vector2 toTarget = p.LastTargetPosition - p.Position;
            float distance = toTarget.magnitude;
            float step = p.Speed * dt;
            if (distance <= step + hitTolerance)
            {
                p.Position = p.LastTargetPosition;
                p.Traveled += distance;
                if (targetValid)
                {
                    QueueDamage(p.Target, p.Damage);
                    return true;
                }
                p.Lifetime = p.MaxLifetime;
                return false;
            }
            p.Direction = toTarget / distance;
            p.Position += p.Direction * step;
            p.Traveled += step;
            return false;
        }

        private bool StepLinear(ProjectileState p, float dt)
        {
            float step = p.Speed * dt;
            if (p.MaxDistance > 0f)
            {
                step = Mathf.Min(step, p.MaxDistance - p.Traveled);
            }
            Vector2 start = p.Position;
            Vector2 end = start + p.Direction * step;
            ICombatTarget best = null;
            float bestT = float.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatTarget target = targets[i];
                if (!target.IsAlive || !TeamRules.AreHostile(p.Damage.SourceTeam, target.Team))
                {
                    continue;
                }
                float along = Mathf.Clamp(Vector2.Dot(target.Position - start, p.Direction), 0f, step);
                Vector2 closest = start + p.Direction * along;
                float reach = p.HitRadius + target.Radius + hitTolerance;
                if ((target.Position - closest).sqrMagnitude <= reach * reach && along < bestT)
                {
                    bestT = along;
                    best = target;
                }
            }
            if (best != null)
            {
                p.Position = start + p.Direction * bestT;
                p.Traveled += bestT;
                QueueDamage(best, p.Damage);
                return true;
            }
            p.Position = end;
            p.Traveled += step;
            return false;
        }

        private void SimulateAreas(float dt)
        {
            for (int i = areas.Count - 1; i >= 0; i--)
            {
                DelayedAreaState area = areas[i];
                area.Elapsed += dt;
                if (area.Elapsed < area.Delay)
                {
                    continue;
                }
                FindHostileTargetsInCircle(area.Center, area.Radius, area.Damage.SourceTeam, queryBuffer);
                for (int t = 0; t < queryBuffer.Count; t++)
                {
                    QueueDamage(queryBuffer[t], area.Damage);
                }
                area.Resolved = true;
                areas.RemoveAt(i);
                AreaResolved?.Invoke(area);
            }
        }

        private void ResolveDamage()
        {
            try
            {
                for (int i = 0; i < pendingDamage.Count; i++)
                {
                    PendingDamage pending = pendingDamage[i];
                    if (!pending.Target.IsAlive)
                    {
                        continue;
                    }
                    DamageResult result = pending.Target.Damageable.ApplyDamage(pending.Info);
                    NotifyDamageApplied(pending.Target, result);
                }
            }
            finally
            {
                pendingDamage.Clear();
            }
        }

        private void NotifyDamageApplied(ICombatTarget target, DamageResult result)
        {
            System.Action<ICombatTarget, DamageResult> handlers = DamageApplied;
            if (handlers == null)
            {
                return;
            }
            foreach (System.Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((System.Action<ICombatTarget, DamageResult>)handler)(target, result);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        private void Awake()
        {
            projectileStage = new ProjectileStage(this);
            damageStage = new DamageStage(this);
        }

        private void OnEnable()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<SimulationTickRunner>();
            }
            if (runner == null)
            {
                Debug.LogError($"{name}: CombatWorld requires a SimulationTickRunner in the scene.", this);
                return;
            }
            runner.Register(projectileStage);
            runner.Register(damageStage);
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.Unregister(projectileStage);
                runner.Unregister(damageStage);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            for (int i = 0; i < areas.Count; i++)
            {
                Gizmos.DrawWireSphere(areas[i].Center, areas[i].Radius);
            }
            Gizmos.color = Color.yellow;
            for (int i = 0; i < projectiles.Count; i++)
            {
                Gizmos.DrawWireSphere(projectiles[i].Position, projectiles[i].HitRadius);
            }
        }

        private readonly struct PendingDamage
        {
            public readonly ICombatTarget Target;
            public readonly DamageInfo Info;

            public PendingDamage(ICombatTarget target, DamageInfo info)
            {
                Target = target;
                Info = info;
            }
        }

        private sealed class ProjectileStage : ISimulationSystem
        {
            private readonly CombatWorld world;
            public ProjectileStage(CombatWorld world) { this.world = world; }
            public int SimulationOrder => Core.SimulationOrder.Projectiles;
            public void SimulateTick(float deltaTime, int tick)
            {
                world.SimulateProjectiles(deltaTime);
                world.SimulateAreas(deltaTime);
            }
        }

        private sealed class DamageStage : ISimulationSystem
        {
            private readonly CombatWorld world;
            public DamageStage(CombatWorld world) { this.world = world; }
            public int SimulationOrder => Core.SimulationOrder.Damage;
            public void SimulateTick(float deltaTime, int tick)
            {
                world.ResolveDamage();
            }
        }
    }
}
