using System;
using UnityEngine;
using IndieMoba.Core;

namespace IndieMoba.Characters
{
    [DisallowMultipleComponent]
    public sealed class HeroActor : MonoBehaviour
    {
        [SerializeField] private CharacterMovementConfig movementConfig;
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [Min(1f)] [SerializeField] private float tickRate = 60f;
        [SerializeField] private int maxTicksPerFrame = 5;

        private CharacterMotor motor;
        private CharacterMotorState state;
        private CharacterMotorState previousState;
        private float accumulator;
        private IMovementInputSource inputSource;

        public event Action<HeroActor> Ticked;

        public CharacterMotorState State => state;
        public CharacterMotorState PreviousState => previousState;
        public float InterpolationAlpha => Mathf.Clamp01(accumulator / TickDelta);
        public float TickDelta => 1f / tickRate;
        public CharacterMovementConfig MovementConfig => movementConfig;

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

        private void Update()
        {
            MovementIntent intent = inputSource?.ReadIntent() ?? MovementIntent.None;
            accumulator += Time.deltaTime;
            int ticks = 0;
            while (accumulator >= TickDelta && ticks < maxTicksPerFrame)
            {
                previousState = state;
                motor.Simulate(ref state, intent, TickDelta);
                accumulator -= TickDelta;
                ticks++;
                Ticked?.Invoke(this);
            }
            if (ticks == maxTicksPerFrame && accumulator >= TickDelta)
            {
                accumulator %= TickDelta;
            }
            transform.position = new Vector3(state.Position.x, state.Position.y, transform.position.z);
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
