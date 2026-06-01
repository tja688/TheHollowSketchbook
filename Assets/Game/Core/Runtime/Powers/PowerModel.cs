using System;
using Game.Core.Combat;
using Game.Core.Entities;
using Game.Core.Models;

namespace Game.Core.Powers
{
    public abstract class PowerModel : AbstractModel
    {
        public abstract string Name { get; }
        public abstract PowerType Type { get; }

        public Creature Owner { get; private set; }
        public int Amount { get; private set; }

        public virtual int ModifyDamageDealt(DamageInfo info, int amount)
        {
            return amount;
        }

        public virtual int ModifyDamageTaken(DamageInfo info, int amount)
        {
            return amount;
        }

        public void SetOwner(Creature owner)
        {
            AssertMutable();
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void SetAmount(int amount)
        {
            AssertMutable();
            Amount = amount;
        }

        public void AddAmount(int amount)
        {
            AssertMutable();
            Amount += amount;
        }

        protected override void DeepCloneFieldsFrom(AbstractModel source)
        {
            PowerModel power = (PowerModel)source;
            Amount = power.Amount;
            Owner = null;
        }
    }
}
