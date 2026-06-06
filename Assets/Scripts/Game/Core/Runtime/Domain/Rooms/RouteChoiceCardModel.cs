using System.Threading.Tasks;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Rooms;

namespace Game.Core.Domain.Rooms
{
    /// <summary>
    /// Base contract for route choice cards that appear on the grid after a room is cleared.
    /// Each instance represents one possible next room. The player selects a route by
    /// interacting with (dragging the player card onto) the desired route choice card.
    ///
    /// L1 content can subclass this to add custom visuals, descriptions, or
    /// pre-entry effects. For batch 2 infrastructure, GenericRouteChoiceModel
    /// provides the default implementation used by RoomTransitionService.
    /// </summary>
    public abstract class RouteChoiceCardModel : CardModel
    {
        public override CardType CardType
        {
            get { return CardType.RouteChoice; }
        }

        public override bool CanBeFaceDown
        {
            get { return false; }
        }

        /// <summary>
        /// The room type this route card leads to when selected.
        /// </summary>
        public abstract RoomType TargetRoomType { get; }

        /// <summary>
        /// Route choice cards should always be interactable when on the grid
        /// and face-up. The room-cleared precondition is enforced by
        /// PlayerInteractAction before calling this method.
        /// </summary>
        public override bool CanInteractWithPlayer(CardInteractionContext ctx)
        {
            return true;
        }

        /// <summary>
        /// When the player interacts with a route choice card, trigger the
        /// room transition through the domain's RoomTransitionService.
        /// </summary>
        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            if (ctx.Domain.RoomTransition != null)
            {
                ctx.Domain.RoomTransition.EnterRoom(ctx, TargetRoomType);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Default route choice card model used by the infrastructure layer.
    /// L1 should replace these with themed/named variants.
    /// </summary>
    public sealed class GenericRouteChoiceModel : RouteChoiceCardModel
    {
        private readonly ModelId _id;
        private readonly RoomType _targetRoomType;

        public GenericRouteChoiceModel(ModelId id, RoomType targetRoomType)
        {
            _id = id;
            _targetRoomType = targetRoomType;
        }

        public override ModelId Id
        {
            get { return _id; }
        }

        public override RoomType TargetRoomType
        {
            get { return _targetRoomType; }
        }

        public override string TitleKey
        {
            get { return "route." + _targetRoomType.ToString().ToLowerInvariant(); }
        }
    }
}
