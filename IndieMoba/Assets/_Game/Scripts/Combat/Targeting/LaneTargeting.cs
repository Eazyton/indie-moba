using System.Collections.Generic;
using UnityEngine;

namespace IndieMoba.Combat
{
    public enum LaneTargetCategory
    {
        EngagedEnemyMinion = 0,
        EnemyMinion = 1,
        EnemyStructure = 2,
        EnemyHero = 3
    }

    public interface ILaneEngagementProvider
    {
        ICombatTarget GetEngagedTarget(ICombatTarget unit);
    }

    public readonly struct LaneTargetQuery
    {
        public readonly Vector2 Origin;
        public readonly float Range;
        public readonly Team Team;
        public readonly ICombatTarget Self;
        public readonly ICombatTarget Current;
        public readonly float Stickiness;

        public LaneTargetQuery(Vector2 origin, float range, Team team, ICombatTarget self, ICombatTarget current, float stickiness)
        {
            Origin = origin;
            Range = range;
            Team = team;
            Self = self;
            Current = current;
            Stickiness = stickiness;
        }
    }

    public interface ILaneTargetPolicy
    {
        ICombatTarget Select(in LaneTargetQuery query, IReadOnlyList<ICombatTarget> candidates);
    }

    public static class LaneTargetRules
    {
        public static bool IsLaneTargetable(ICombatTarget t, Team team)
        {
            return t != null && t.IsAlive && t.Kind != CombatTargetKind.Other && !t.IsInvulnerable && TeamRules.AreHostile(team, t.Team);
        }

        public static float EdgeDistance(Vector2 origin, ICombatTarget t)
        {
            return (t.Position - origin).magnitude - t.Radius;
        }
    }

    public sealed class MinionTargetPolicy : ILaneTargetPolicy
    {
        private readonly IReadOnlyList<LaneTargetCategory> priority;
        private readonly ILaneEngagementProvider engagement;

        public MinionTargetPolicy(IReadOnlyList<LaneTargetCategory> priority, ILaneEngagementProvider engagement)
        {
            this.priority = priority != null && priority.Count > 0 ? priority : CreateDefaultPriority();
            this.engagement = engagement;
        }

        public static LaneTargetCategory[] CreateDefaultPriority()
        {
            return new LaneTargetCategory[]
            {
                LaneTargetCategory.EngagedEnemyMinion,
                LaneTargetCategory.EnemyMinion,
                LaneTargetCategory.EnemyStructure,
                LaneTargetCategory.EnemyHero
            };
        }

        public ICombatTarget Select(in LaneTargetQuery query, IReadOnlyList<ICombatTarget> candidates)
        {
            ICombatTarget best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                ICombatTarget candidate = candidates[i];
                if (candidate == null || candidate == query.Self || !LaneTargetRules.IsLaneTargetable(candidate, query.Team))
                {
                    continue;
                }
                float edge = LaneTargetRules.EdgeDistance(query.Origin, candidate);
                if (edge > query.Range)
                {
                    continue;
                }
                LaneTargetCategory category = Classify(candidate, query.Team);
                int rank = IndexOf(category);
                if (rank < 0)
                {
                    continue;
                }
                float score = rank * 100000f + edge - (candidate == query.Current ? query.Stickiness : 0f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        private LaneTargetCategory Classify(ICombatTarget candidate, Team team)
        {
            switch (candidate.Kind)
            {
                case CombatTargetKind.Minion:
                    if (engagement != null)
                    {
                        ICombatTarget engaged = engagement.GetEngagedTarget(candidate);
                        if (engaged != null && engaged.IsAlive && engaged.Team == team)
                        {
                            return LaneTargetCategory.EngagedEnemyMinion;
                        }
                    }
                    return LaneTargetCategory.EnemyMinion;
                case CombatTargetKind.Structure:
                    return LaneTargetCategory.EnemyStructure;
                case CombatTargetKind.Hero:
                    return LaneTargetCategory.EnemyHero;
                default:
                    return LaneTargetCategory.EnemyMinion;
            }
        }

        private int IndexOf(LaneTargetCategory category)
        {
            for (int i = 0; i < priority.Count; i++)
            {
                if (priority[i] == category)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public sealed class TowerTargetPolicy : ILaneTargetPolicy
    {
        public ICombatTarget Select(in LaneTargetQuery query, IReadOnlyList<ICombatTarget> candidates)
        {
            if (query.Current != null && LaneTargetRules.IsLaneTargetable(query.Current, query.Team) && InRange(query.Origin, query.Current, query.Range))
            {
                return query.Current;
            }
            ICombatTarget nearestMinion = null;
            float nearestMinionDistance = float.MaxValue;
            ICombatTarget nearestHero = null;
            float nearestHeroDistance = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                ICombatTarget candidate = candidates[i];
                if (candidate == null || !LaneTargetRules.IsLaneTargetable(candidate, query.Team) || !InRange(query.Origin, candidate, query.Range))
                {
                    continue;
                }
                float distance = (candidate.Position - query.Origin).sqrMagnitude;
                if (candidate.Kind == CombatTargetKind.Minion)
                {
                    if (distance < nearestMinionDistance)
                    {
                        nearestMinionDistance = distance;
                        nearestMinion = candidate;
                    }
                }
                else if (candidate.Kind == CombatTargetKind.Hero)
                {
                    if (distance < nearestHeroDistance)
                    {
                        nearestHeroDistance = distance;
                        nearestHero = candidate;
                    }
                }
            }
            return nearestMinion != null ? nearestMinion : nearestHero;
        }

        private static bool InRange(Vector2 origin, ICombatTarget t, float range)
        {
            float reach = range + t.Radius;
            return (t.Position - origin).sqrMagnitude <= reach * reach;
        }
    }
}
