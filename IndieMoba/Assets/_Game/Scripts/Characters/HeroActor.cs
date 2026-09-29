using System;
using UnityEngine;
using IndieMoba.Core;

namespace IndieMoba.Characters
{
    [DisallowMultipleComponent]
    public sealed class HeroActor : MonoBehaviour, ISimulationSystem
    {
        [SerializeField] private CharacterMovementConfig movementConfig;
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [SerializeField] private SimulationTickRunner runner;
        [Min(1f)] [SerializeField] private float tickRate = 60f;
        [SerializeField] private int maxTicksPerFrame = 5;

        private CharacterMotor motor;
        private CharacterMotorState state;
        private CharacterMotorState previousState;
        private float accumulator;
        private IMovementInputSource inputSource;
        private Vector2 forcedVelocity;
        private float forcedRemaining;
        private bool movementEnabled = true;

        public event Action<HeroActor> Ticked;

        public CharacterMotorState State => state;
        public CharacterMotorState PreviousState => previousState;
        public float InterpolationAlpha => runner != null ? runner.InterpolationAlpha : Mathf.Clamp01(accumulator / TickDelta);
        public float TickDelta => runner != null ? runner.TickDelta : 1f / tickRate;
        public CharacterMovementConfig MovementConfig => movementConfig;
        public int SimulationOrder => Core.SimulationOrder.Actors;
        public bool IsForcedMoving => forcedRemaining > 0f;
        public bool MovementEnabled
        {
            get => movementEnabled;
            set => movementEnabled = value;
        }

        public void SetInputSource(IMovementInputSource source)
        {
            inputSource = source;
        }

        public void Teleport(Vector2 position)
        {
            Vector2 facing = state.Facing;
            state = CharacterMotorState.Create(position);
            state.Facing = facing;
            previousState = state;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            accumulator = 0f;
            forcedRemaining = 0f;
        }

        public void StartForcedMove(Vector2 velocity, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }
            forcedVelocity = velocity;
            forcedRemaining = duration;
            state.HasDestination = false;
        }

        public void CancelForcedMove()
        {
            if (forcedRemaining <= 0f)
            {
                return;
            }
            forcedRemaining = 0f;
            state.Velocity = Vector2.zero;
        }

        public void SimulateTick(float deltaTime, int tick)
        {
            previousState = state;
            if (forcedRemaining > 0f)
            {
                float step = Mathf.Min(deltaTime, forcedRemaining);
                motor.SimulateForced(ref state, forcedVelocity * (step / deltaTime), deltaTime);
                forcedRemaining -= deltaTime;
                if (forcedRemaining <= 0f)
                {
                    forcedRemaining = 0f;
                    state.Velocity = Vector2.zero;
                }
            }
            else
            {
                MovementIntent intent = MovementIntent.None;
                if (!movementEnabled)
                {
                    intent = MovementIntent.Stop();
                }
                else if (inputSource != null)
                {
                    intent = inputSource.ReadIntent();
                }
                motor.Simulate(ref state, intent, deltaTime);
            }
            transform.position = new Vector3(state.Position.x, state.Position.y, transform.position.z);
            Ticked?.Invoke(this);
        }

        private void Awake()
        {
            if (movementConfig == null)
            {
                Debug.LogError($"{name}: CharacterMovementConfig is missing.", this);
                enabled = false;
                return;
            }
            motor = new CharacterMotor(movementConfig);
            inputSource = inputSourceBehaviour as IMovementInputSource;
            state = CharacterMotorState.Create(transform.position);
            previousState = state;
        }

        private void OnEnable()
        {
            if (runner != null && motor != null)
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

        private void Update()
        {
            if (runner != null)
            {
                return;
            }
            accumulator += Time.deltaTime;
            int ticks = 0;
            while (accumulator >= TickDelta && ticks < maxTicksPerFrame)
            {
                accumulator -= TickDelta;
                SimulateTick(TickDelta, 0);
                ticks++;
            }
            if (ticks == maxTicksPerFrame && accumulator >= TickDelta)
            {
                accumulator %= TickDelta;
            }
        }

        private void OnValidate()
        {
            if (inputSourceBehaviour != null && !(inputSourceBehaviour is IMovementInputSource))
            {
                Debug.LogWarning($"{name}: inputSourceBehaviour does not implement IMovementInputSource.", this);
                inputSourceBehaviour = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (movementConfig == null)
            {
                return;
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, movementConfig.CollisionRadius);
            if (Application.isPlaying && state.HasDestination)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, state.Destination);
            }
        }
    }
}
