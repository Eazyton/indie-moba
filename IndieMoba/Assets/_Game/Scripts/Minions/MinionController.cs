using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Minions
{
    public enum MinionState
    {
        Walking = 0,
        Chasing = 1,
        Attacking = 2,
        Dead = 3
    }

    [DisallowMultipleComponent]
    public sealed class MinionController : MonoBehaviour, ISimulationSystem
    {
        [SerializeField] private MinionConfig config;
        [SerializeField] private CombatTarget target;
        [SerializeField] private Health health;
        [SerializeField] private MinionCombat combat;

        private CharacterMotor motor;
        private CharacterMotorState state;
        private CharacterMotorState previousState;
        private List<Vector2> waypoints;
        private List<Vector2> segmentNormals;
        private int waypointIndex;
        private Vector2 laneNormal;
        private float lateralOffset;
        private ICombatTarget currentTarget;
        private MinionState mode;
        private float corpseTimer;
        private SimulationTickRunner runner;
        private CombatWorld world;
        private MinionRegistry registry;
        private MinionTargetPolicy policy;
        private int spawnIndex;
        private bool initialized;

        public event Action<MinionController> Died;

        public MinionConfig Config => config;
        public Team Team => target != null ? target.Team : Team.Neutral;
        public CombatTarget CombatTarget => target;
        public Health Health => health;
        public MinionCombat Combat => combat;
        public ICombatTarget CurrentTarget => currentTarget;
        public MinionState Mode => mode;
        public CharacterMotorState State => state;
        public CharacterMotorState PreviousState => previousState;
        public float InterpolationAlpha => runner != null ? runner.InterpolationAlpha : 1f;
        public int SpawnIndex => spawnIndex;
        public bool IsAlive => health != null && health.IsAlive;
        public int SimulationOrder => Core.SimulationOrder.Actors;

        public void Initialize(in MinionSpawnContext ctx)
        {
            if (config == null || config.MovementConfig == null)
            {
                Debug.LogError($"{name}: MinionConfig or MovementConfig is missing.", this);
                enabled = false;
                return;
            }
            world = ctx.World;
            runner = ctx.Runner;
            registry = ctx.Registry;
            spawnIndex = ctx.SpawnIndex;
            laneNormal = ctx.LaneNormal;
            lateralOffset = ctx.LateralOffset;
            target.SetTeam(ctx.Team);
            health.SetMaxHealth(config.MaxHealth, true);
            motor = new CharacterMotor(config.MovementConfig);
            state = CharacterMotorState.Create(transform.position);
            waypoints = new List<Vector2>(ctx.Waypoints);
            BuildSegmentNormals();
            if (waypoints.Count > 1)
            {
                Vector2 firstSegment = waypoints[1] - waypoints[0];
                state.Facing = firstSegment.sqrMagnitude > 1e-8f ? firstSegment.normalized : Vector2.right;
                waypointIndex = 1;
            }
            else
            {
                state.Facing = Vector2.right;
                waypointIndex = 0;
            }
            previousState = state;
            policy = new MinionTargetPolicy(config.TargetPriority, ctx.Registry);
            combat.Initialize(config, world, ctx.Team);
            health.Died += HandleDied;
            registry.Register(this);
            runner.Register(this);
            initialized = true;
        }

        public void SimulateTick(float deltaTime, int tick)
        {
            previousState = state;
            if (mode == MinionState.Dead)
            {
                corpseTimer -= deltaTime;
                state.Velocity = Vector2.zero;
                if (corpseTimer <= 0f)
                {
                    Destroy(gameObject);
                }
                return;
            }
            combat.Tick(deltaTime);
            bool shouldRetarget = currentTarget == null || !LaneTargetRules.IsLaneTargetable(currentTarget, Team) ||
                LaneTargetRules.EdgeDistance(state.Position, currentTarget) > config.LeashRange ||
                (tick + spawnIndex) % config.RetargetIntervalTicks == 0;
            if (shouldRetarget)
            {
                LaneTargetQuery query = new LaneTargetQuery(state.Position, config.AggroRange, Team, target, currentTarget, config.TargetStickiness);
                currentTarget = policy.Select(query, world.Targets);
            }
            Vector2 desired = Vector2.zero;
            Vector2 facing = state.Facing;
            if (currentTarget != null)
            {
                Vector2 toTarget = currentTarget.Position - state.Position;
                float distance = toTarget.magnitude;
                float gap = distance - currentTarget.Radius - target.Radius;
                if (gap <= config.AttackRange)
                {
                    mode = MinionState.Attacking;
                    if (distance > 1e-5f)
                    {
                        facing = toTarget / distance;
                        state.Facing = facing;
                    }
                    desired = Separation() * 0.5f;
                    combat.TryAttack(currentTarget);
                }
                else
                {
                    mode = MinionState.Chasing;
                    desired = (distance > 1e-5f ? toTarget / distance : Vector2.zero) + Separation();
                }
            }
            else
            {
                mode = MinionState.Walking;
                while (waypointIndex < waypoints.Count && IsWaypointReached(waypointIndex))
                {
                    waypointIndex++;
                }
                if (waypointIndex >= waypoints.Count)
                {
                    desired = Separation() * 0.5f;
                }
                else
                {
                    Vector2 point = GetWaypointPoint(waypointIndex);
                    Vector2 toPoint = point - state.Position;
                    float distance = toPoint.magnitude;
                    desired = (distance > 1e-5f ? toPoint / distance : Vector2.zero) + Separation();
                }
            }
            if (mode != MinionState.Attacking && desired.sqrMagnitude > 1e-8f)
            {
                desired = ApplyStructureAvoidance(desired);
            }
            MovementIntent intent = desired.sqrMagnitude > 1e-4f
                ? MovementIntent.FromDirection(desired.normalized * Mathf.Min(1f, desired.magnitude))
                : MovementIntent.Stop();
            motor.Simulate(ref state, intent, deltaTime);
            if (mode == MinionState.Attacking)
            {
                state.Facing = facing;
            }
            transform.position = new Vector3(state.Position.x, state.Position.y, transform.position.z);
        }

        private void BuildSegmentNormals()
        {
            segmentNormals = new List<Vector2>(waypoints.Count);
            for (int i = 0; i < waypoints.Count; i++)
            {
                int from = i > 0 ? i - 1 : 0;
                int to = i > 0 ? i : 1;
                Vector2 normal = laneNormal;
                if (to < waypoints.Count)
                {
                    Vector2 direction = waypoints[to] - waypoints[from];
                    if (direction.sqrMagnitude > 1e-8f)
                    {
                        normal = new Vector2(-direction.y, direction.x).normalized;
                    }
                }
                segmentNormals.Add(normal);
            }
        }

        private Vector2 CurrentLaneNormal()
        {
            if (segmentNormals == null || segmentNormals.Count == 0)
            {
                return laneNormal;
            }
            return segmentNormals[Mathf.Clamp(waypointIndex, 0, segmentNormals.Count - 1)];
        }

        private bool IsWaypointReached(int index)
        {
            Vector2 point = GetWaypointPoint(index);
            if ((point - state.Position).magnitude <= config.WaypointReachDistance)
            {
                return true;
            }
            if (index > 0)
            {
                Vector2 segment = waypoints[index] - waypoints[index - 1];
                if (Vector2.Dot(segment, waypoints[index] - state.Position) <= 0f)
                {
                    return true;
                }
            }
            return false;
        }

        private Vector2 GetWaypointPoint(int index)
        {
            Vector2 point = waypoints[index];
            if (index < waypoints.Count - 1)
            {
                point += segmentNormals[index] * lateralOffset;
            }
            return point;
        }

        private Vector2 ApplyStructureAvoidance(Vector2 desired)
        {
            IReadOnlyList<ICombatTarget> targets = world.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatTarget s = targets[i];
                if (s == null || s.Kind != CombatTargetKind.Structure || !s.IsAlive)
                {
                    continue;
                }
                Vector2 to = s.Position - state.Position;
                float dist = to.magnitude;
                float clearance = s.Radius + target.Radius + 0.1f;
                if (dist >= clearance + config.StructureAvoidanceDistance || Vector2.Dot(desired, to) <= 0f)
                {
                    continue;
                }
                Vector2 n = to / dist;
                Vector2 tangent = new Vector2(-n.y, n.x);
                if (Vector2.Dot(tangent, desired) < 0f)
                {
                    tangent = -tangent;
                }
                if (Mathf.Abs(Vector2.Dot(tangent, desired.normalized)) < 0.05f)
                {
                    if (Vector2.Dot(tangent, CurrentLaneNormal()) < 0f)
                    {
                        tangent = -tangent;
                    }
                }
                float weight = config.StructureAvoidanceStrength * (1f - Mathf.Clamp01((dist - clearance) / config.StructureAvoidanceDistance));
                desired += tangent * weight * desired.magnitude;
            }
            return desired;
        }

        private Vector2 Separation()
        {
            Vector2 push = Vector2.zero;
            if (registry == null)
            {
                return push;
            }
            IReadOnlyList<MinionController> minions = registry.Minions;
            for (int i = 0; i < minions.Count; i++)
            {
                MinionController other = minions[i];
                if (other == null || other == this || !other.IsAlive || other.Team != Team)
                {
                    continue;
                }
                Vector2 away = state.Position - other.State.Position;
                float d = away.magnitude;
                if (d < 1e-4f)
                {
                    d = 1e-4f;
                    away = CurrentLaneNormal() * (spawnIndex < other.SpawnIndex ? d : -d);
                }
                if (d >= config.SeparationRadius)
                {
                    continue;
                }
                push += away / d * (1f - d / config.SeparationRadius);
            }
            return push * config.SeparationStrength;
        }

        private void HandleDied(Health _)
        {
            mode = MinionState.Dead;
            currentTarget = null;
            corpseTimer = config.CorpseDuration;
            Died?.Invoke(this);
        }

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<CombatTarget>();
            }
            if (health == null)
            {
                health = GetComponent<Health>();
            }
            if (combat == null)
            {
                combat = GetComponent<MinionCombat>();
            }
        }

        private void OnEnable()
        {
            if (!initialized)
            {
                return;
            }
            if (runner != null)
            {
                runner.Register(this);
            }
            if (registry != null)
            {
                registry.Register(this);
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.Unregister(this);
            }
            if (registry != null)
            {
                registry.Unregister(this);
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }
    }
}
