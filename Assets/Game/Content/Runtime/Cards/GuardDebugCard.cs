using System.Threading.Tasks;
using Game.Core;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Commands;

namespace Game.Content
{
    public sealed class GuardDebugCard : CardModel
    {
        public override ModelId Id => new ModelId("Card", "GuardDebug");
        public override string Name => "Guard Debug";
        public override string Description => "Gain 3 block.";
        public override CardType Type => CardType.Skill;
        public override CardRarity Rarity => CardRarity.Common;
        public override CardTargeting Targeting => CardTargeting.Self;
        public override CardEnergyCost EnergyCost => CardEnergyCost.Free();

        protected override Task OnPlay(CardPlayContext ctx, CardPlay play)
        {
            return CreatureCmd.GainBlock(ctx, Owner.Creature, 3);
        }
    }
}
