using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;

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
}
