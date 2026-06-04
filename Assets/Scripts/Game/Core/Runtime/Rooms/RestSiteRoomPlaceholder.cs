using System;
using Game.Core.Entities;
using Game.Core.Map;

namespace Game.Core.Rooms
{
    public sealed class RestSiteRoomPlaceholder : AbstractRoom
    {
        public RestSiteRoomPlaceholder(MapPoint mapPoint)
            : base(RoomType.RestSite, mapPoint)
        {
        }

        public int HealAmount => 20;

        public void Rest(Player player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            player.Creature.SetCurrentHp(Math.Min(player.Creature.MaxHp, player.Creature.CurrentHp + HealAmount));
        }
    }
}
