using Game.Core.Models;

namespace Game.Core.Domain.Cards
{
    public abstract class CardModel : AbstractModel
    {
        public abstract CardType CardType { get; }

        public virtual string TitleKey
        {
            get { return Id.ToString(); }
        }

        public virtual string DescriptionKey
        {
            get { return Id.ToString() + ".description"; }
        }

        public virtual bool CanBeFaceDown
        {
            get { return true; }
        }

        public virtual bool CanBeStoredInInventory
        {
            get { return false; }
        }

        public virtual bool BlocksAutoReveal
        {
            get { return false; }
        }

        public virtual CardInstance CreateInstance(CardInstanceId id)
        {
            CardInstance instance = new CardInstance(id, Id, CardType);
            ConfigureCreatedInstance(instance);
            return instance;
        }

        protected virtual void ConfigureCreatedInstance(CardInstance instance)
        {
        }
    }
}
