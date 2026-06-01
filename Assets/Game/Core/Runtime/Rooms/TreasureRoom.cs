using System.Collections.Generic;
using Game.Core.Map;
using Game.Core.Rewards;
using Game.Core.Runs;

namespace Game.Core.Rooms
{
    public sealed class TreasureRoom : AbstractRoom
    {
        public TreasureRoom(MapPoint mapPoint)
            : base(RoomType.Treasure, mapPoint)
        {
        }

        public override IReadOnlyList<Reward> GenerateRewards(RunState run)
        {
            return RewardGenerator.GenerateTreasureRewards(run);
        }
    }
}
