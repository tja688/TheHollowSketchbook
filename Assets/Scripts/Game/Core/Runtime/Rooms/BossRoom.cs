using Game.Core.Map;

namespace Game.Core.Rooms
{
    public sealed class BossRoom : CombatRoom
    {
        public BossRoom(MapPoint mapPoint)
            : base(RoomType.Boss, mapPoint, false)
        {
        }
    }
}
