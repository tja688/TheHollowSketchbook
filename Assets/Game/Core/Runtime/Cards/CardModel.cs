using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Combat;
using Game.Core.Entities;
using Game.Core.Hooks;
using Game.Core.Logging;
using Game.Core.Models;

namespace Game.Core.Cards
{
    public abstract class CardModel : AbstractModel
    {
        private static readonly IReadOnlyList<CardKeyword> EmptyKeywords = Array.Empty<CardKeyword>();

        public abstract string Name { get; }
        public virtual string Description
        {
            get { return string.Empty; }
        }

        public abstract CardType Type { get; }
        public abstract CardRarity Rarity { get; }
        public abstract CardTargeting Targeting { get; }
        public abstract CardEnergyCost EnergyCost { get; }

        public virtual IReadOnlyList<CardKeyword> Keywords
        {
            get { return EmptyKeywords; }
        }

        public Player Owner { get; private set; }

        public CardPile CurrentPile { get; private set; }

        public int UpgradeLevel { get; private set; }

        public bool HasKeyword(CardKeyword keyword)
        {
            for (int i = 0; i < Keywords.Count; i++)
            {
                if (Keywords[i] == keyword)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetOwner(Player owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            AssertMutable();
            Owner = owner;
        }

        public bool CanPlay(out string reason)
        {
            if (Owner == null || Owner.PlayerCombatState == null)
            {
                reason = "Card has no combat owner.";
                return false;
            }

            int cost = EnergyCost.GetSpendAmount(Owner.PlayerCombatState.Energy);
            if (Owner.PlayerCombatState.Energy < cost)
            {
                reason = "Not enough energy.";
                return false;
            }

            reason = null;
            return true;
        }

        public void Upgrade()
        {
            AssertMutable();
            UpgradeLevel++;
            OnUpgrade();
        }

        public async Task OnPlayWrapper(CardPlayContext ctx, CardPlay play)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            if (play == null)
            {
                throw new ArgumentNullException(nameof(play));
            }

            await Hook.BeforeCardPlayed(ctx.Combat, ctx, play);
            await OnPlay(ctx, play);
            await Hook.AfterCardPlayed(ctx.Combat, ctx, play);
        }

        internal void SetCurrentPile(CardPile pile)
        {
            CurrentPile = pile;
        }

        protected virtual void OnUpgrade()
        {
        }

        protected override void DeepCloneFieldsFrom(AbstractModel source)
        {
            CardModel card = (CardModel)source;
            UpgradeLevel = card.UpgradeLevel;
            Owner = null;
            CurrentPile = null;
        }

        protected abstract Task OnPlay(CardPlayContext ctx, CardPlay play);
    }
}
