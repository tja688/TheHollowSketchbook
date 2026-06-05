using System;
using Game.Core.Rooms;

namespace Game.Core.Domain.Rooms
{
    public sealed class RoomPlan
    {
        public RoomPlan(RoomType roomType, int layerIndex, int nodeIndex, bool isElite, bool isBoss, RngState generationRngState)
        {
            if (layerIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }

            if (nodeIndex < 1 || nodeIndex > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeIndex));
            }

            RoomType = roomType;
            LayerIndex = layerIndex;
            NodeIndex = nodeIndex;
            IsElite = isElite;
            IsBoss = isBoss;
            GenerationRngState = generationRngState;
        }

        public RoomType RoomType { get; }
        public int LayerIndex { get; }
        public int NodeIndex { get; }
        public bool IsElite { get; }
        public bool IsBoss { get; }
        public RngState GenerationRngState { get; }
    }
}
