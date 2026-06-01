using System.Threading.Tasks;
using Game.Core;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Commands;

namespace Game.Content
{
    public sealed class DefendCard : CardModel
    {
        public override ModelId Id => new ModelId("Card", "Defend");
        public override string Name => "Defend";
        public override string Description => "Gain 5 block.";
        public override CardType Type => CardType.Skill;
        public override CardRarity Rarity => CardRarity.Basic;
        public override CardTargeting Targeting => CardTargeting.Self;
        public override CardEnergyCost EnergyCost => CardEnergyCost.Fixed(1);

        protected override Task OnPlay(CardPlayContext ctx, CardPlay play)
        {
            return CreatureCmd.GainBlock(ctx, Owner.Creature, 5);
        }
    }
}
