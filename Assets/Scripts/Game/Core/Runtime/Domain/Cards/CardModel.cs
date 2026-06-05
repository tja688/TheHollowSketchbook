using System.Threading.Tasks;
using Game.Core.Domain.ContentContracts;
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

        public virtual bool CanInteractWithPlayer(CardInteractionContext ctx)
        {
            return CardType != CardType.Player;
        }

        public virtual Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnRevealedAsync(CardRevealContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDestroyedAsync(CardDestroyedContext ctx)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterPlayerActionCommittedAsync(PlayerActionContext ctx)
        {
            return Task.CompletedTask;
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
