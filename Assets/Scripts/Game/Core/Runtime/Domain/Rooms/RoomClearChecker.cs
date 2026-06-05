using System.Linq;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Rooms
{
    public sealed class RoomClearChecker
    {
        public bool IsRoomCleared(GridState grid)
        {
            return !grid.AllGridCards.Any(card => card.CardType == CardType.Monster && !card.IsRemoved);
        }
    }
}
