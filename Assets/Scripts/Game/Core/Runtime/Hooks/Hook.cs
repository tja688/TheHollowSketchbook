using System.Threading.Tasks;
using Game.Core.Combat;
using Game.Core.Entities;
using Game.Core.Powers;

namespace Game.Core.Hooks
{
    /// <summary>
    /// Hook framework for relics and keyword/trait systems to介入 game lifecycle.
    /// All methods are static and return Task.CompletedTask by default.
    /// Systems (relics, keywords, traits) subscribe to these events to介入 without modifying core logic.
    /// </summary>
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

        // Turn lifecycle (kept for compatibility; in action-driven combat these represent action boundaries)
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

        // Damage / Block / Power
        public static Task BeforeDamageApplied(CombatState combat, DamageInfo info)
        {
            return Task.CompletedTask;
        }

        public static Task AfterDamageApplied(CombatState combat, DamageInfo info, DamageResult result)
        {
            return Task.CompletedTask;
        }

        public static Task BeforeBlockGained(CombatState combat, Creature target, int amount)
        {
            return Task.CompletedTask;
        }

        public static Task AfterBlockGained(CombatState combat, Creature target, int amount)
        {
            return Task.CompletedTask;
        }

        public static Task BeforePowerApplied(CombatState combat, Creature target, PowerModel power, int amount)
        {
            return Task.CompletedTask;
        }

        public static Task AfterPowerApplied(CombatState combat, Creature target, PowerModel power, int amount)
        {
            return Task.CompletedTask;
        }

        // Creature death
        public static Task BeforeCreatureDied(Creature creature)
        {
            return Task.CompletedTask;
        }

        public static Task AfterCreatureDied(Creature creature)
        {
            return Task.CompletedTask;
        }
    }
}
