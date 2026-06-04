using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Entities;
using Game.Core.Hooks;
using Game.Core.Powers;

namespace Game.Core.Combat.Commands
{
    public static class CreatureCmd
    {
        public static async Task<DamageResult> DealDamage(CombatState combat, Creature source, Creature target, int amount, DamageType type = DamageType.Attack)
        {
            DamageInfo info = new DamageInfo(source, target, amount, type);
            await Hook.BeforeDamageApplied(combat, info);

            int modified = Math.Max(0, amount);
            IReadOnlyList<PowerModel> sourcePowers = source.Powers;
            for (int i = 0; i < sourcePowers.Count; i++)
            {
                modified = sourcePowers[i].ModifyDamageDealt(info, modified);
            }

            IReadOnlyList<PowerModel> targetPowers = target.Powers;
            for (int i = 0; i < targetPowers.Count; i++)
            {
                modified = targetPowers[i].ModifyDamageTaken(info, modified);
            }

            modified = Math.Max(0, modified);
            int blocked = Math.Min(target.Block, modified);
            int hpLoss = Math.Max(0, modified - blocked);

            if (blocked > 0)
            {
                target.SetBlock(target.Block - blocked);
            }

            if (hpLoss > 0)
            {
                target.SetCurrentHp(target.CurrentHp - hpLoss);
            }

            DamageResult result = new DamageResult
            {
                OriginalAmount = amount,
                ModifiedAmount = modified,
                BlockedAmount = blocked,
                HpLoss = hpLoss,
                Killed = !target.IsAlive
            };

            await Hook.AfterDamageApplied(combat, info, result);
            return result;
        }

        public static async Task GainBlock(CombatState combat, Creature target, int amount)
        {
            int clampedAmount = Math.Max(0, amount);
            await Hook.BeforeBlockGained(combat, target, clampedAmount);
            target.SetBlock(target.Block + clampedAmount);
            await Hook.AfterBlockGained(combat, target, clampedAmount);
        }

        public static async Task ApplyPower(CombatState combat, Creature target, PowerModel power, int amount)
        {
            await Hook.BeforePowerApplied(combat, target, power, amount);

            PowerModel existing = target.Powers.FirstOrDefault(item => item.GetType() == power.GetType());
            if (existing != null)
            {
                existing.AddAmount(amount);
            }
            else
            {
                power.SetOwner(target);
                power.SetAmount(amount);
                target.AddPower(power);
            }

            await Hook.AfterPowerApplied(combat, target, power, amount);
        }

        public static Task RemovePower(Creature target, PowerModel power)
        {
            target.RemovePower(power);
            return Task.CompletedTask;
        }

        public static void TakeDamage(Creature target, int amount)
        {
            int hpLoss = Math.Max(0, amount);
            target.SetCurrentHp(Math.Max(0, target.CurrentHp - hpLoss));
        }
    }
}
