using System;
using System.Collections.Generic;
using Game.Core.Rooms;

namespace Game.Core.Domain.Rooms
{
    public sealed class RunProgressionState
    {
        public RunProgressionState(int layerIndex, int nodeIndex, RoomType currentRoomType, IReadOnlyList<RoomType> pendingChoices)
        {
            if (layerIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }

            if (nodeIndex < 1 || nodeIndex > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeIndex));
            }

            LayerIndex = layerIndex;
            NodeIndex = nodeIndex;
            CurrentRoomType = currentRoomType;
            PendingChoices = pendingChoices ?? Array.Empty<RoomType>();
        }

        public int LayerIndex { get; }
        public int NodeIndex { get; }
        public RoomType CurrentRoomType { get; }
        public IReadOnlyList<RoomType> PendingChoices { get; }
    }
}
