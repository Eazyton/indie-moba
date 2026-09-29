using System;
using UnityEngine;

namespace IndieMoba.Characters
{
    [Serializable]
    public struct CharacterMotorState
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Facing;
        public bool HasDestination;
        public Vector2 Destination;

        public bool IsMoving => Velocity.sqrMagnitude > 0.0001f;

        public static CharacterMotorState Create(Vector2 position)
        {
            return new CharacterMotorState
            {
                Position = position,
                Facing = Vector2.right
            };
        }
    }
}
