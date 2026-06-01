using System.Threading.Tasks;
using Game.Core.Cards;
using Game.Core.Combat;

namespace Game.Core.Hooks
{
    public static class Hook
    {
        // Combat lifecycle
        public static Task BeforeCombatStart(CombatState combat)
        {
            return Task.CompletedTask;
        }

        public static Task AfterCombatEnd(CombatState combat)
        {
            return Task.CompletedTask;
        }

        // Turn lifecycle
        public static Task BeforeTurnStart(CombatState combat)
        {
            return Task.CompletedTask;
        }

        public static Task AfterTurnStart(CombatState combat)
        {
            return Task.CompletedTask;
        }

        public static Task BeforeTurnEnd(CombatState combat)
        {
            return Task.CompletedTask;
        }

        public static Task AfterTurnEnd(CombatState combat)
        {
            return Task.CompletedTask;
        }

        // Card play
        public static Task BeforeCardPlayed(CombatState combat, CardPlayContext ctx, CardPlay play)
        {
            return Task.CompletedTask;
        }

        public static Task AfterCardPlayed(CombatState combat, CardPlayContext ctx, CardPlay play)
        {
            return Task.CompletedTask;
        }

        // Card pile movement
        public static Task BeforeCardMovedPile(CardModel card, CardPile from, CardPile to)
        {
            return Task.CompletedTask;
        }

        public static Task AfterCardMovedPile(CardModel card, CardPile from, CardPile to)
        {
            return Task.CompletedTask;
        }

        // Damage / Block / Power
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

        // Creature death
        public static Task BeforeCreatureDied(Game.Core.Entities.Creature creature)
        {
            return Task.CompletedTask;
        }

        public static Task AfterCreatureDied(Game.Core.Entities.Creature creature)
        {
            return Task.CompletedTask;
        }
    }
}
