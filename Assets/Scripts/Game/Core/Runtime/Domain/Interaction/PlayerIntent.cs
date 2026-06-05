using Game.Core;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Inventory;

namespace Game.Core.Domain.Interaction
{
    public enum IntentKind
    {
        None,
        MovePlayer,
        InteractWithCard,
        StoreItem,
        UseItem,
        ChooseOption,
        ActivateRelic
    }

    public abstract class PlayerIntent
    {
        protected PlayerIntent(IntentKind kind)
        {
            Kind = kind;
        }

        public IntentKind Kind { get; }
    }

    public sealed class MovePlayerIntent : PlayerIntent
    {
        public MovePlayerIntent(GridCoord to)
            : base(IntentKind.MovePlayer)
        {
            To = to;
        }

        public GridCoord To { get; }
    }

    public sealed class InteractWithCardIntent : PlayerIntent
    {
        public InteractWithCardIntent(CardInstanceId target)
            : base(IntentKind.InteractWithCard)
        {
            Target = target;
        }

        public CardInstanceId Target { get; }
    }

    public sealed class StoreItemIntent : PlayerIntent
    {
        public StoreItemIntent(CardInstanceId itemCard)
            : base(IntentKind.StoreItem)
        {
            ItemCard = itemCard;
        }

        public CardInstanceId ItemCard { get; }
    }

    public sealed class UseItemIntent : PlayerIntent
    {
        public UseItemIntent(InventorySlot slot)
            : base(IntentKind.UseItem)
        {
            Slot = slot;
        }

        public InventorySlot Slot { get; }
    }

    public sealed class ChooseOptionIntent : PlayerIntent
    {
        public ChooseOptionIntent(string sessionId, int optionIndex)
            : base(IntentKind.ChooseOption)
        {
            SessionId = sessionId ?? string.Empty;
            OptionIndex = optionIndex;
        }

        public string SessionId { get; }
        public int OptionIndex { get; }
    }

    public sealed class ActivateRelicIntent : PlayerIntent
    {
        public ActivateRelicIntent(ModelId relicId)
            : base(IntentKind.ActivateRelic)
        {
            RelicId = relicId;
        }

        public ModelId RelicId { get; }
    }
}
