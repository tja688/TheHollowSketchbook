using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core;
using Game.Core.Models;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.Inventory;

namespace Game.Core.Domain.ContentContracts
{
    public enum ItemTargetMode
    {
        None,
        Player,
        GridCell,
        MonsterCard,
        CardThenDirection,
        TwoCards,
        AnyCard,
        AnyCardThenAnyCell
    }

    public enum RelicRarity
    {
        Common,
        Rare,
        Legendary
    }

    public enum RelicKind
    {
        Passive,
        Active,
        Initial
    }

    public abstract class MonsterCardModel : CardModel
    {
        public override CardType CardType
        {
            get { return CardType.Monster; }
        }

        public abstract int Level { get; }
        public abstract int MaxHp { get; }
        public abstract int Attack { get; }
        public abstract int Defense { get; }

        public virtual int GoldOnRemoved
        {
            get { return 10; }
        }

        public virtual IReadOnlyList<ModelId> TraitIds
        {
            get { return Array.Empty<ModelId>(); }
        }

        public override async Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            await ctx.Domain.Combat.ResolvePlayerVsMonsterAsync(ctx.PlayerCard, ctx.TargetCard, ctx.Events).ConfigureAwait(false);
        }

        protected override void ConfigureCreatedInstance(CardInstance instance)
        {
            base.ConfigureCreatedInstance(instance);
            instance.ConfigureCombatStats(MaxHp, Attack, Defense, 0, GoldOnRemoved);
        }
    }

    public abstract class TrapCardModel : CardModel
    {
        public override CardType CardType
        {
            get { return CardType.Trap; }
        }

        public abstract int MaxHp { get; }

        public virtual int Defense
        {
            get { return 0; }
        }

        public virtual int ContactDamageToPlayer
        {
            get { return 0; }
        }

        public override async Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            await ctx.Domain.Combat.ResolvePlayerVsTrapAsync(ctx.PlayerCard, ctx.TargetCard, ctx.Events).ConfigureAwait(false);
        }

        protected override void ConfigureCreatedInstance(CardInstance instance)
        {
            base.ConfigureCreatedInstance(instance);
            instance.ConfigureCombatStats(MaxHp, 0, Defense, ContactDamageToPlayer, 0);
        }

        public virtual Task OnRevealedAsync(TrapContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDestroyedAsync(TrapContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterPlayerActionCommittedAsync(TrapContext ctx)
        {
            return Task.CompletedTask;
        }

        public sealed override Task OnRevealedAsync(CardRevealContext ctx)
        {
            return OnRevealedAsync(new TrapContext(ctx.Domain, ctx.Card, ctx.Reason, null, ctx.Domain.ActionCounter.Value, ctx.Events));
        }

        public sealed override Task OnDestroyedAsync(CardDestroyedContext ctx)
        {
            return OnDestroyedAsync(new TrapContext(ctx.Domain, ctx.Card, ctx.Reason, null, ctx.Domain.ActionCounter.Value, ctx.Events));
        }

        public sealed override Task OnAfterPlayerActionCommittedAsync(PlayerActionContext ctx)
        {
            return OnAfterPlayerActionCommittedAsync(new TrapContext(ctx.Domain, ctx.ObservedCard, string.Empty, ctx.SourceIntent, ctx.ActionIndex, ctx.Events));
        }
    }

    public abstract class ItemCardModel : CardModel
    {
        public override CardType CardType
        {
            get { return CardType.Item; }
        }

        public override bool CanBeStoredInInventory
        {
            get { return true; }
        }

        public override bool CanInteractWithPlayer(CardInteractionContext ctx)
        {
            return false;
        }

        public abstract ItemTargetMode TargetMode { get; }

        public virtual int MaxUses
        {
            get { return 1; }
        }

        public virtual bool CountsAsPlayerAction
        {
            get { return false; }
        }

        public virtual bool CanUse(ItemUseContext ctx)
        {
            return true;
        }

        public abstract Task UseAsync(ItemUseContext ctx);
    }

    public abstract class RoomCardModel : CardModel
    {
    }

    public abstract class RelicModel : AbstractModel
    {
        public abstract RelicRarity Rarity { get; }
        public abstract RelicKind Kind { get; }

        public virtual bool IsInitialRelic
        {
            get { return false; }
        }

        public virtual bool CanAppearInRelicPool
        {
            get { return !IsInitialRelic; }
        }

        public virtual int AttackBonus
        {
            get { return 0; }
        }

        public virtual int DefenseBonus
        {
            get { return 0; }
        }

        public virtual int MaxHpBonus
        {
            get { return 0; }
        }

        public virtual bool CanActivate(ActiveRelicContext ctx)
        {
            return Kind == RelicKind.Active && ctx != null && ctx.Slot.CanActivate(Id);
        }

        public virtual Task ActivateAsync(ActiveRelicContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnRoomEnteredAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnRoomClearedAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnEliteOrBossDefeatedAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnBeforeDamageAsync(DamageContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterDamageAsync(DamageContext ctx, DamageResult result)
        {
            return Task.CompletedTask;
        }

        public virtual int ModifyDamageDealt(DamageContext ctx, int current)
        {
            return current;
        }

        public virtual int ModifyDamageTaken(DamageContext ctx, int current)
        {
            return current;
        }
    }

    public abstract class TraitModel : AbstractModel
    {
        public virtual Task OnCardFlippedAsync(CardRevealContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnPlayerActionCommittedAsync(PlayerActionContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnCardRemovedAsync(CardDestroyedContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnBeforeDamageAsync(DamageContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterDamageAsync(DamageContext ctx, DamageResult result)
        {
            return Task.CompletedTask;
        }

        public virtual int ModifyDamageDealt(DamageContext ctx, int current)
        {
            return current;
        }

        public virtual int ModifyDamageTaken(DamageContext ctx, int current)
        {
            return current;
        }
    }
}
