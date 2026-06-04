using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Map;
using Game.Core.Rewards;
using Game.Core.Runs;

namespace Game.Core.Rooms
{
    public abstract class AbstractRoom
    {
        private readonly List<Reward> _rewards = new List<Reward>();

        protected AbstractRoom(RoomType roomType, MapPoint mapPoint)
        {
            RoomType = roomType;
            MapPoint = mapPoint ?? throw new ArgumentNullException(nameof(mapPoint));
        }

        public RoomType RoomType { get; }
        public MapPoint MapPoint { get; }
        public bool IsCompleted { get; private set; }

        public IReadOnlyList<Reward> Rewards
        {
            get { return _rewards; }
        }

        public bool HasPendingRewards
        {
            get { return _rewards.Any(reward => !reward.IsResolved); }
        }

        public void SetCompleted(bool value)
        {
            IsCompleted = value;
        }

        public void SetRewards(IEnumerable<Reward> rewards)
        {
            _rewards.Clear();
            if (rewards == null)
            {
                return;
            }

            _rewards.AddRange(rewards);
        }

        public virtual IReadOnlyList<Reward> GenerateRewards(RunState run)
        {
            return Array.Empty<Reward>();
        }
    }
}
