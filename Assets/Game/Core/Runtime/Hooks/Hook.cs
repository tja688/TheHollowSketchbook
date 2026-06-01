using System.Threading.Tasks;
using Game.Core.Combat;

namespace Game.Core.Hooks
{
    public static class Hook
    {
        public static Task BeforeCardPlayed(CombatState combat, CardPlayContext ctx, CardPlay play)
        {
            return Task.CompletedTask;
        }

        public static Task AfterCardPlayed(CombatState combat, CardPlayContext ctx, CardPlay play)
        {
            return Task.CompletedTask;
        }

        public static Task BeforeDamageApplied(CombatState combat, DamageInfo info)
        {
            return Task.CompletedTask;
        }

        public static Task AfterDamageApplied(CombatState combat, DamageInfo info, DamageResult result)
        {
            return Task.CompletedTask;
        }

        public static Task BeforeBlockGained(CombatState combat, Game.Core.Entities.Creature target, int amount)
        {
            return Task.CompletedTask;
        }

        public static Task AfterBlockGained(CombatState combat, Game.Core.Entities.Creature target, int amount)
        {
            return Task.CompletedTask;
        }

        public static Task BeforePowerApplied(CombatState combat, Game.Core.Entities.Creature target, Game.Core.Powers.PowerModel power, int amount)
        {
            return Task.CompletedTask;
        }

        public static Task AfterPowerApplied(CombatState combat, Game.Core.Entities.Creature target, Game.Core.Powers.PowerModel power, int amount)
        {
            return Task.CompletedTask;
        }
    }
}
