using UnityEngine;
using IndieMoba.Core;

namespace IndieMoba.Characters
{
    public sealed class CharacterMotor
    {
        private readonly CharacterMovementConfig config;
        private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];
        private readonly Collider2D[] overlapBuffer = new Collider2D[8];
        private ContactFilter2D contactFilter;

        public CharacterMovementConfig Config => config;

        public CharacterMotor(CharacterMovementConfig config)
        {
            this.config = config;
            RefreshFilter();
        }

        public void RefreshFilter()
        {
            contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = true;
            contactFilter.layerMask = config.ObstacleMask;
            contactFilter.useTriggers = false;
        }

        public void Simulate(ref CharacterMotorState state, in MovementIntent intent, float deltaTime)
        {
            Vector2 desiredDir = Vector2.zero;
            bool desiredFromDestination = false;
            float distToDestination = 0f;

            switch (intent.Kind)
            {
                case MovementIntentKind.Direction:
                    state.HasDestination = false;
                    desiredDir = Vector2.ClampMagnitude(intent.Value, 1f);
                    break;
                case MovementIntentKind.Destination:
                    state.HasDestination = true;
                    state.Destination = intent.Value;
                    break;
                case MovementIntentKind.Stop:
                    state.HasDestination = false;
                    desiredDir = Vector2.zero;
                    state.Velocity = Vector2.zero;
                    break;
                case MovementIntentKind.None:
                    desiredDir = Vector2.zero;
                    break;
            }

            if (state.HasDestination)
            {
                Vector2 toTarget = state.Destination - state.Position;
                distToDestination = toTarget.magnitude;
                if (distToDestination <= config.StopDistance)
                {
                    state.HasDestination = false;
                    desiredDir = Vector2.zero;
                }
                else
                {
                    desiredDir = toTarget / distToDestination;
                    desiredFromDestination = true;
                }
            }

            Vector2 targetVelocity = desiredDir * config.MaxSpeed;
            float rate = desiredDir == Vector2.zero ? config.Deceleration : config.Acceleration;
            state.Velocity = Vector2.MoveTowards(state.Velocity, targetVelocity, rate * deltaTime);

            Vector2 displacement = state.Velocity * deltaTime;
            if (state.HasDestination && displacement.magnitude > distToDestination)
            {
                displacement = displacement.normalized * distToDestination;
            }

            Vector2 position = state.Position;
            Depenetrate(ref position);
            CollideAndSlide(ref position, ref state.Velocity, displacement);

            state.Position = position;
            if (state.Velocity.magnitude > config.FacingThreshold)
            {
                state.Facing = state.Velocity.normalized;
            }

            if (state.HasDestination && (state.Destination - state.Position).magnitude <= config.StopDistance)
            {
                state.HasDestination = false;
                if (desiredFromDestination)
                {
                    state.Velocity = Vector2.zero;
                }
            }
        }

        public void SimulateForced(ref CharacterMotorState state, Vector2 velocity, float deltaTime)
        {
            state.HasDestination = false;
            Vector2 resolvedVelocity = velocity;
            Vector2 position = state.Position;
            Depenetrate(ref position);
            CollideAndSlide(ref position, ref resolvedVelocity, velocity * deltaTime);
            state.Position = position;
            state.Velocity = resolvedVelocity;
            if (velocity.magnitude > config.FacingThreshold)
            {
                state.Facing = velocity.normalized;
            }
        }

        private void Depenetrate(ref Vector2 position)
        {
            float radius = config.CollisionRadius;
            float skin = config.SkinWidth;
            int count = Physics2D.OverlapCircle(position, radius, contactFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = overlapBuffer[i];
                Vector2 closest = collider.ClosestPoint(position);
                Vector2 offset = position - closest;
                float distance = offset.magnitude;
                if (distance < 1e-5f)
                {
                    Vector2 push = (position - (Vector2)collider.bounds.center).normalized;
                    if (push == Vector2.zero)
                    {
                        push = Vector2.up;
                    }
                    position += push * (radius + skin);
                }
                else if (distance < radius)
                {
                    position += offset / distance * (radius - distance + skin);
                }
            }
        }

        private void CollideAndSlide(ref Vector2 position, ref Vector2 velocity, Vector2 displacement)
        {
            float radius = config.CollisionRadius;
            float skin = config.SkinWidth;
            Vector2 remaining = displacement;
            int iterations = 0;
            while (remaining.sqrMagnitude > 1e-8f && iterations < config.MaxSlideIterations)
            {
                iterations++;
                float distance = remaining.magnitude;
                Vector2 dir = remaining / distance;
                int count = Physics2D.CircleCast(position, radius, dir, contactFilter, hitBuffer, distance + skin);
                int nearestIndex = -1;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = hitBuffer[i];
                    if (hit.distance <= 0f && Vector2.Dot(dir, hit.normal) >= 0f)
                    {
                        continue;
                    }
                    if (hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                        nearestIndex = i;
                    }
                }
                if (nearestIndex < 0)
                {
                    position += remaining;
                    break;
                }
                RaycastHit2D nearestHit = hitBuffer[nearestIndex];
                float travel = Mathf.Max(0f, nearestHit.distance - skin);
                position += dir * travel;
                Vector2 leftover = dir * (distance - travel);
                Vector2 normal = nearestHit.normal;
                leftover -= Vector2.Dot(leftover, normal) * normal;
                if (Vector2.Dot(velocity, normal) < 0f)
                {
                    velocity -= Vector2.Dot(velocity, normal) * normal;
                }
                remaining = leftover;
            }
        }
    }
}
