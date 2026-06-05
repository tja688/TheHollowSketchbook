using System;
using System.Collections.Generic;
using Game.Core.Rooms;

namespace Game.Core.Domain.Rooms
{
    public sealed class DungeonMapGenerator
    {
        private static readonly RoomType[] EarlyChoicePool =
        {
            RoomType.Gold,
            RoomType.Chest,
            RoomType.StatUpgrade
        };

        private static readonly RoomType[] LateChoicePool =
        {
            RoomType.Gold,
            RoomType.Chest,
            RoomType.StatUpgrade,
            RoomType.Shop,
            RoomType.EliteCombat
        };

        public IReadOnlyList<RoomPlan> GenerateLayerPlans(int layerIndex)
        {
            if (layerIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }

            List<RoomPlan> plans = new List<RoomPlan>(9);
            for (int nodeIndex = 1; nodeIndex <= 9; nodeIndex++)
            {
                RoomType roomType = GetDefaultRoomType(nodeIndex);
                plans.Add(new RoomPlan(
                    roomType,
                    layerIndex,
                    nodeIndex,
                    roomType == RoomType.EliteCombat,
                    roomType == RoomType.BossCombat,
                    new RngState((uint)((layerIndex * 1000) + nodeIndex))));
            }

            return plans;
        }

        public IReadOnlyList<RoomType> GetChoicePoolAfterNode(int nodeIndex)
        {
            if (nodeIndex >= 1 && nodeIndex <= 3)
            {
                return EarlyChoicePool;
            }

            if (nodeIndex >= 4 && nodeIndex <= 6)
            {
                return LateChoicePool;
            }

            return Array.Empty<RoomType>();
        }

        public RoomType? GetForcedNextRoomAfterNode(int nodeIndex)
        {
            return nodeIndex == 7 ? RoomType.Restaurant : (RoomType?)null;
        }

        private static RoomType GetDefaultRoomType(int nodeIndex)
        {
            switch (nodeIndex)
            {
                case 1:
                    return RoomType.Reward;
                case 8:
                    return RoomType.Restaurant;
                case 9:
                    return RoomType.BossCombat;
                default:
                    return RoomType.Combat;
            }
        }
    }
}
