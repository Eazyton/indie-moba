using System.Collections.Generic;
using UnityEngine;

namespace IndieMoba.Core
{
    public enum AbilitySlot
    {
        BasicAttack = 0,
        Ability1 = 1,
        Ability2 = 2,
        Ability3 = 3,
        Ultimate = 4
    }

    public enum BasicAttackTargeting
    {
        AutoTarget = 0,
        ExplicitTarget = 1,
        AttackMoveTarget = 2
    }

    public readonly struct AbilityCommand
    {
        public readonly AbilitySlot Slot;
        public readonly Vector2 AimPoint;
        public readonly bool HasAim;
        public readonly BasicAttackTargeting Targeting;
        public readonly object ExplicitTarget;

        public AbilityCommand(AbilitySlot slot, Vector2 aimPoint, bool hasAim,
            BasicAttackTargeting targeting = BasicAttackTargeting.AutoTarget, object explicitTarget = null)
        {
            Slot = slot;
            AimPoint = aimPoint;
            HasAim = hasAim;
            Targeting = targeting;
            ExplicitTarget = explicitTarget;
        }

        public static AbilityCommand AtPoint(AbilitySlot slot, Vector2 aimPoint) => new AbilityCommand(slot, aimPoint, true);
        public static AbilityCommand WithoutAim(AbilitySlot slot) => new AbilityCommand(slot, Vector2.zero, false);
    }

    public interface IAbilityInputSource
    {
        void DrainCommands(List<AbilityCommand> into);
    }
}
