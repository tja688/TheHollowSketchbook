using System;
using System.Collections.Generic;
using Game.Core.Map;
using Game.Core.Rewards;
using Game.Core.Runs;

namespace Game.Core.Rooms
{
    /// <summary>
    /// Combat room placeholder. StS EncounterModel dependency removed.
    /// BOUNDARY: This is a skeleton. A new room system should override GenerateRewards
    /// and add grid generation logic (GenerateFieldCards) for the grid-based system.
    /// </summary>
    public class CombatRoom : AbstractRoom
    {
        public CombatRoom(RoomType roomType, MapPoint mapPoint, bool isElite)
            : base(roomType, mapPoint)
        {
            IsElite = isElite;
        }

        public bool IsElite { get; }

        public override IReadOnlyList<Reward> GenerateRewards(RunState run)
        {
            return RewardGenerator.GenerateCombatRewards(run, IsElite, RoomType == RoomType.Boss);
        }
    }
}
