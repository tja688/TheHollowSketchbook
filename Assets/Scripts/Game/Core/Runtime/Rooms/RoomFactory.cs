using System;
using Game.Core.Map;

namespace Game.Core.Rooms
{
    /// <summary>
    /// Room factory. StS EncounterModel dependency removed.
    /// BOUNDARY: This is a skeleton. Extend CreateRoomForMapPoint to add new room types
    /// for the grid-based system (RestaurantRoom, StatRoom, GoldRoom, etc.).
    /// </summary>
    public sealed class RoomFactory
    {
        public AbstractRoom CreateRoomForMapPoint(Runs.RunState run, MapPoint point)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            return point.PointType switch
            {
                MapPointType.Monster => new CombatRoom(RoomType.Combat, point, false),
                MapPointType.Elite => new CombatRoom(RoomType.Combat, point, true),
                MapPointType.Treasure => new TreasureRoom(point),
                MapPointType.Event => new EventRoomPlaceholder(point),
                MapPointType.Rest => new RestSiteRoomPlaceholder(point),
                MapPointType.Shop => new ShopRoomPlaceholder(point),
                MapPointType.Boss => new BossRoom(point),
                _ => throw new InvalidOperationException("Unsupported map point type: " + point.PointType)
            };
        }
    }
}
