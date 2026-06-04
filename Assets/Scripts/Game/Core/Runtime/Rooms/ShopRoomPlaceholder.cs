using Game.Core.Map;

namespace Game.Core.Rooms
{
    public sealed class ShopRoomPlaceholder : AbstractRoom
    {
        public ShopRoomPlaceholder(MapPoint mapPoint)
            : base(RoomType.Shop, mapPoint)
        {
        }
    }
}
