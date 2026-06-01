using Game.Core;
using Game.Core.Combat;
using Game.Core.Powers;

namespace Game.Content
{
    public sealed class StrengthPowerModel : PowerModel
    {
        public override ModelId Id => new ModelId("Power", "Strength");
        public override string Name => "Strength";
        public override PowerType Type => PowerType.Buff;

        public override int ModifyDamageDealt(DamageInfo info, int amount)
        {
            return info.IsAttack ? amount + Amount : amount;
        }
    }
}
