using System;
using System.Collections.Generic;
using Game.Core.Entities;
using Game.Core.Map;
using Game.Core.Rewards;
using Game.Core.Runs;

namespace Game.Core.Rooms
{
    public class CombatRoom : AbstractRoom
    {
        public CombatRoom(RoomType roomType, MapPoint mapPoint, EncounterModel encounter, bool isElite)
            : base(roomType, mapPoint)
        {
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            IsElite = isElite;
        }

        public EncounterModel Encounter { get; }
        public bool IsElite { get; }

        public override IReadOnlyList<Reward> GenerateRewards(RunState run)
        {
            return RewardGenerator.GenerateCombatRewards(run, IsElite, RoomType == RoomType.Boss);
        }
    }
}
