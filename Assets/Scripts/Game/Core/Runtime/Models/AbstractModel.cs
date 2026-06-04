using Game.Core.Logging;

namespace Game.Core.Models
{
    public abstract class AbstractModel
    {
        public abstract ModelId Id { get; }

        public bool IsCanonical { get; private set; } = true;

        public AbstractModel CloneMutable()
        {
            AbstractModel clone = (AbstractModel)MemberwiseClone();
            clone.IsCanonical = false;
            clone.AfterClonedFrom(this);
            clone.DeepCloneFieldsFrom(this);
            return clone;
        }

        public T CloneMutable<T>() where T : AbstractModel
        {
            return (T)CloneMutable();
        }

        protected void AssertMutable()
        {
            if (IsCanonical)
            {
                throw new GameException(Id + " is canonical and cannot be mutated.");
            }
        }

        protected virtual void AfterClonedFrom(AbstractModel source)
        {
        }

        protected virtual void DeepCloneFieldsFrom(AbstractModel source)
        {
        }
    }
}
