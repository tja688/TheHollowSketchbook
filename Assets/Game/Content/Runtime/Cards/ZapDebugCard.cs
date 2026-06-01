using System.Threading.Tasks;
using Game.Core;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Commands;
using Game.Core.Random;

namespace Game.Content
{
    public sealed class ZapDebugCard : CardModel
    {
        public override ModelId Id => new ModelId("Card", "ZapDebug");
        public override string Name => "Zap Debug";
        public override string Description => "Deal 3 damage. Draw 1 card.";
        public override CardType Type => CardType.Skill;
        public override CardRarity Rarity => CardRarity.Common;
        public override CardTargeting Targeting => CardTargeting.SingleEnemy;
        public override CardEnergyCost EnergyCost => CardEnergyCost.Fixed(1);

        protected override async Task OnPlay(CardPlayContext ctx, CardPlay play)
        {
            await CreatureCmd.DealDamage(ctx, Owner.Creature, play.Target.Creature, 3, this);
            CardPileCmd.Draw(Owner, 1, ctx.Rng);
        }
    }
}
