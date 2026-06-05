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

    public readonly struct ItemTargetSelection
    {
        private ItemTargetSelection(
            bool hasPrimaryCard,
            CardInstanceId primaryCard,
            bool hasSecondaryCard,
            CardInstanceId secondaryCard,
            bool hasGridCell,
            GridCoord gridCell,
            bool hasSecondaryGridCell,
            GridCoord secondaryGridCell,
            bool hasDirection,
            GridDirection direction)
        {
            HasPrimaryCard = hasPrimaryCard;
            PrimaryCard = primaryCard;
            HasSecondaryCard = hasSecondaryCard;
            SecondaryCard = secondaryCard;
            HasGridCell = hasGridCell;
            GridCell = gridCell;
            HasSecondaryGridCell = hasSecondaryGridCell;
            SecondaryGridCell = secondaryGridCell;
            HasDirection = hasDirection;
            Direction = direction;
        }

        public bool HasPrimaryCard { get; }
        public CardInstanceId PrimaryCard { get; }
        public bool HasSecondaryCard { get; }
        public CardInstanceId SecondaryCard { get; }
        public bool HasGridCell { get; }
        public GridCoord GridCell { get; }
        public bool HasSecondaryGridCell { get; }
        public GridCoord SecondaryGridCell { get; }
        public bool HasDirection { get; }
        public GridDirection Direction { get; }

        public static ItemTargetSelection None
        {
            get { return default; }
        }

        public static ItemTargetSelection GridCellTarget(GridCoord cell)
        {
            return new ItemTargetSelection(false, default, false, default, true, cell, false, default, false, default);
        }

        public static ItemTargetSelection CardTarget(CardInstanceId card)
        {
            return new ItemTargetSelection(true, card, false, default, false, default, false, default, false, default);
        }

        public static ItemTargetSelection CardThenDirection(CardInstanceId card, GridDirection direction)
        {
            return new ItemTargetSelection(true, card, false, default, false, default, false, default, true, direction);
        }

        public static ItemTargetSelection TwoCards(CardInstanceId firstCard, CardInstanceId secondCard)
        {
            return new ItemTargetSelection(true, firstCard, true, secondCard, false, default, false, default, false, default);
        }

        public static ItemTargetSelection CardThenCell(CardInstanceId card, GridCoord cell)
        {
            return new ItemTargetSelection(true, card, false, default, true, cell, false, default, false, default);
        }

        public static ItemTargetSelection TwoCells(GridCoord firstCell, GridCoord secondCell)
        {
            return new ItemTargetSelection(false, default, false, default, true, firstCell, true, secondCell, false, default);
        }
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
            : this(slot, ItemTargetSelection.None)
        {
        }

        public UseItemIntent(InventorySlot slot, ItemTargetSelection target)
            : base(IntentKind.UseItem)
        {
            Slot = slot;
            Target = target;
        }

        public InventorySlot Slot { get; }
        public ItemTargetSelection Target { get; }
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
