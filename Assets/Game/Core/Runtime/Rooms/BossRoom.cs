using Game.Core.Entities;
using Game.Core.Map;

namespace Game.Core.Rooms
{
    public sealed class BossRoom : CombatRoom
    {
        public BossRoom(MapPoint mapPoint, EncounterModel encounter)
            : base(RoomType.Boss, mapPoint, encounter, false)
        {
        }
    }
}
