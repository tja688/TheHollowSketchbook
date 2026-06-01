using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Cards;
using Game.Core.Entities;
using Game.Core.Hooks;
using Game.Core.Powers;
using Game.Core.Random;

namespace Game.Core.Combat.Commands
{
    public static class CardPileCmd
    {
        public static int Draw(Player player, int amount, IRng rng)
        {
            PlayerCombatState state = player.PlayerCombatState;
            int drawn = 0;
            for (int i = 0; i < amount; i++)
            {
                if (state.Hand.Count >= CardPile.MaxHandSize)
                {
                    break;
                }

                if (state.DrawPile.Count == 0)
                {
                    if (state.DiscardPile.Count == 0)
                    {
                        break;
                    }

                    ShuffleDiscardIntoDraw(player, rng);
                }

                CardModel card = state.DrawPile.DrawTop();
                if (card == null)
                {
                    break;
                }

                state.Hand.Add(card);
                drawn++;
            }

            return drawn;
        }

        public static void Move(CardModel card, CardPile destination)
        {
            destination.Add(card);
        }

        public static void Discard(Player player, CardModel card)
        {
            player.PlayerCombatState.DiscardPile.Add(card);
        }

        public static void Exhaust(Player player, CardModel card)
        {
            player.PlayerCombatState.ExhaustPile.Add(card);
        }

        public static void ShuffleDiscardIntoDraw(Player player, IRng rng)
        {
            PlayerCombatState state = player.PlayerCombatState;
            List<CardModel> cards = state.DiscardPile.Cards.ToList();
            for (int i = 0; i < cards.Count; i++)
            {
                state.DiscardPile.Remove(cards[i]);
                state.DrawPile.Add(cards[i]);
            }

            List<CardModel> drawCards = state.DrawPile.Cards.ToList();
            rng.Shuffle(drawCards);
            state.DrawPile.Clear();
            for (int i = 0; i < drawCards.Count; i++)
            {
                state.DrawPile.Add(drawCards[i]);
            }
        }
    }

    public static class CreatureCmd
    {
        public static async Task<DamageResult> DealDamage(CardPlayContext ctx, Creature source, Creature target, int amount, CardModel cardSource = null, DamageType type = DamageType.Attack)
        {
            DamageInfo info = new DamageInfo(source, target, amount, type, cardSource);
            await Hook.BeforeDamageApplied(ctx.Combat, info);

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

            await Hook.AfterDamageApplied(ctx.Combat, info, result);
            return result;
        }

        public static async Task GainBlock(CardPlayContext ctx, Creature target, int amount)
        {
            int clampedAmount = Math.Max(0, amount);
            await Hook.BeforeBlockGained(ctx.Combat, target, clampedAmount);
            target.SetBlock(target.Block + clampedAmount);
            await Hook.AfterBlockGained(ctx.Combat, target, clampedAmount);
        }

        public static async Task ApplyPower(CardPlayContext ctx, Creature target, PowerModel power, int amount)
        {
            await Hook.BeforePowerApplied(ctx.Combat, target, power, amount);

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

            await Hook.AfterPowerApplied(ctx.Combat, target, power, amount);
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

    public static class PlayerCmd
    {
        public static Task SpendEnergy(Player player, int amount)
        {
            player.PlayerCombatState.SpendEnergy(amount);
            return Task.CompletedTask;
        }

        public static Task GainEnergy(Player player, int amount)
        {
            player.PlayerCombatState.GainEnergy(amount);
            return Task.CompletedTask;
        }

        public static Task GainGold(Player player, int amount)
        {
            player.GainGold(amount);
            return Task.CompletedTask;
        }
    }
}
