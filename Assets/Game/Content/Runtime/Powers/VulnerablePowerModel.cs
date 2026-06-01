using System;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Powers;

namespace Game.Content
{
    public sealed class VulnerablePowerModel : PowerModel
    {
        public override ModelId Id => new ModelId("Power", "Vulnerable");
        public override string Name => "Vulnerable";
        public override PowerType Type => PowerType.Debuff;

        public override int ModifyDamageTaken(DamageInfo info, int amount)
        {
            if (!info.IsAttack)
            {
                return amount;
            }

            return (int)Math.Floor(amount * 1.5f);
        }
    }
}
