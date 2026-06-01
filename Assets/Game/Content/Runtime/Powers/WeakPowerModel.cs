using System;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Powers;

namespace Game.Content
{
    public sealed class WeakPowerModel : PowerModel
    {
        public override ModelId Id => new ModelId("Power", "Weak");
        public override string Name => "Weak";
        public override PowerType Type => PowerType.Debuff;

        public override int ModifyDamageDealt(DamageInfo info, int amount)
        {
            if (!info.IsAttack)
            {
                return amount;
            }

            return (int)Math.Floor(amount * 0.75f);
        }
    }
}
