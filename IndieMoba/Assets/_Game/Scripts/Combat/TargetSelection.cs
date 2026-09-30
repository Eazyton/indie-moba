using System.Collections.Generic;
using UnityEngine;

namespace IndieMoba.Combat
{
    public readonly struct BasicAttackQuery
    {
        public readonly Vector2 Origin;
        public readonly Vector2 AimDirection;
        public readonly float Range;
        public readonly float HalfArcRadians;
        public readonly float CloseRange;
        public readonly Team Team;
        public readonly ICombatTarget Self;

        public BasicAttackQuery(Vector2 origin, Vector2 aimDirection, float range, float halfArcRadians, float closeRange, Team team, ICombatTarget self)
        {
            Origin = origin;
            AimDirection = aimDirection;
            Range = range;
            HalfArcRadians = halfArcRadians;
            CloseRange = closeRange;
            Team = team;
            Self = self;
        }
    }

    public interface IBasicAttackTargetSelector
    {
        ICombatTarget Select(in BasicAttackQuery query, IReadOnlyList<ICombatTarget> candidates);
    }

    public sealed class ConeTargetSelector : IBasicAttackTargetSelector
    {
        private const float AnglePenalty = 2.5f;

        public ICombatTarget Select(in BasicAttackQuery query, IReadOnlyList<ICombatTarget> candidates)
        {
            ICombatTarget best = null;
            float bestScore = float.MaxValue;
            bool hasAim = query.AimDirection.sqrMagnitude > 1e-6f;
            Vector2 aim = hasAim ? query.AimDirection.normalized : Vector2.zero;
            for (int i = 0; i < candidates.Count; i++)
            {
                ICombatTarget candidate = candidates[i];
                if (candidate == null || candidate == query.Self || !candidate.IsAlive || !TeamRules.AreHostile(query.Team, candidate.Team) || candidate.IsInvulnerable)
                {
                    continue;
                }
                Vector2 offset = candidate.Position - query.Origin;
                float distance = offset.magnitude;
                float edgeDistance = distance - candidate.Radius;
                if (edgeDistance > query.Range)
                {
                    continue;
                }
                float angle = 0f;
                if (hasAim && distance > 1e-5f)
                {
                    angle = Vector2.Angle(aim, offset / distance) * Mathf.Deg2Rad;
                }
                if (angle > query.HalfArcRadians && edgeDistance > query.CloseRange)
                {
                    continue;
                }
                float score = edgeDistance + angle * AnglePenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }
    }
}
