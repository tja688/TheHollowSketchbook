using System.Threading.Tasks;
using Game.Core;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Commands;

namespace Game.Content
{
    public sealed class StrikeCard : CardModel
    {
        public override ModelId Id => new ModelId("Card", "Strike");
        public override string Name => "Strike";
        public override string Description => "Deal 6 damage.";
        public override CardType Type => CardType.Attack;
        public override CardRarity Rarity => CardRarity.Basic;
        public override CardTargeting Targeting => CardTargeting.SingleEnemy;
        public override CardEnergyCost EnergyCost => CardEnergyCost.Fixed(1);

        protected override Task OnPlay(CardPlayContext ctx, CardPlay play)
        {
            return CreatureCmd.DealDamage(ctx, Owner.Creature, play.Target.Creature, 6, this);
        }
    }
}
