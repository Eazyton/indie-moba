using System;
using UnityEngine;

namespace IndieMoba.Core
{
    [Serializable]
    public struct MovementIntent
    {
        [SerializeField] private MovementIntentKind kind;
        [SerializeField] private Vector2 value;

        public MovementIntentKind Kind => kind;
        public Vector2 Value => value;

        public static readonly MovementIntent None = new MovementIntent { kind = MovementIntentKind.None };

        public static MovementIntent FromDirection(Vector2 direction)
        {
            return new MovementIntent { kind = MovementIntentKind.Direction, value = Vector2.ClampMagnitude(direction, 1f) };
        }

        public static MovementIntent ToDestination(Vector2 worldPoint)
        {
            return new MovementIntent { kind = MovementIntentKind.Destination, value = worldPoint };
        }

        public static MovementIntent Stop()
        {
            return new MovementIntent { kind = MovementIntentKind.Stop };
        }
    }
}
