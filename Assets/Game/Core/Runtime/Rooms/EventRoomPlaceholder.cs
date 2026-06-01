using Game.Core.Entities;
using Game.Core.Map;

namespace Game.Core.Rooms
{
    public sealed class EventRoomPlaceholder : AbstractRoom
    {
        public EventRoomPlaceholder(MapPoint mapPoint)
            : base(RoomType.Event, mapPoint)
        {
        }

        public int GoldDelta => 50;
        public int HpLoss => 5;

        public void TakeRisk(Player player)
        {
            player.GainGold(GoldDelta);
            player.Creature.SetCurrentHp(player.Creature.CurrentHp - HpLoss);
        }
    }
}
