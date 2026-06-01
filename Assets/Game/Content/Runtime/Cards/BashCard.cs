using System.Threading.Tasks;
using Game.Core;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Commands;

namespace Game.Content
{
    public sealed class BashCard : CardModel
    {
        public override ModelId Id => new ModelId("Card", "Bash");
        public override string Name => "Bash";
        public override string Description => "Deal 8 damage. Apply 2 Vulnerable.";
        public override CardType Type => CardType.Attack;
        public override CardRarity Rarity => CardRarity.Basic;
        public override CardTargeting Targeting => CardTargeting.SingleEnemy;
        public override CardEnergyCost EnergyCost => CardEnergyCost.Fixed(2);

        protected override async Task OnPlay(CardPlayContext ctx, CardPlay play)
        {
            await CreatureCmd.DealDamage(ctx, Owner.Creature, play.Target.Creature, 8, this);
            await CreatureCmd.ApplyPower(ctx, play.Target.Creature, Game.Core.Models.ModelDb.CreateMutable<Game.Core.Powers.PowerModel>(new ModelId("Power", "Vulnerable")), 2);
        }
    }
}
