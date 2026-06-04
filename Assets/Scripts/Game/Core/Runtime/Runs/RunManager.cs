using System;
using System.Collections.Generic;
using Game.Core.Entities;
using Game.Core.Map;
using Game.Core.Random;
using Game.Core.Rooms;
using Game.Core.Saves;

namespace Game.Core.Runs
{
    public sealed class RunManager
    {
        private readonly StandardActMapGenerator _mapGenerator;
        private readonly RoomFactory _roomFactory;
        private readonly SaveManager _saveManager;

        public RunManager(StandardActMapGenerator mapGenerator = null, RoomFactory roomFactory = null, SaveManager saveManager = null)
        {
            _mapGenerator = mapGenerator ?? new StandardActMapGenerator();
            _roomFactory = roomFactory ?? new RoomFactory();
            _saveManager = saveManager ?? new SaveManager();
        }

        public RunState State { get; private set; }

        public event Action<RunState> RunStarted;
        public event Action<RunState> MapChanged;
        public event Action<AbstractRoom> RoomEntered;
        public event Action<AbstractRoom> RoomCompleted;
        public event Action<RunState> RunEnded;

        public RunState StartNewRun(CharacterModel character, int seed, IReadOnlyList<ActModel> acts)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (acts == null || acts.Count == 0)
            {
                throw new ArgumentException("Acts are required.", nameof(acts));
            }

            Player player = new Player(character);
            State = new RunState(seed, new DeterministicRng(seed), new[] { player }, acts);
            State.CurrentActIndex = 0;
            State.Map = _mapGenerator.Generate(State.Rng, State.CurrentAct);
            State.CurrentRoom = null;
            State.CurrentMapCoord = null;
            SaveRun();
            RunStarted?.Invoke(State);
            MapChanged?.Invoke(State);
            return State;
        }

        public ActMap GenerateActMap()
        {
            EnsureState();
            State.Map = _mapGenerator.Generate(State.Rng, State.CurrentAct);
            SaveRun();
            MapChanged?.Invoke(State);
            return State.Map;
        }

        public AbstractRoom EnterMapCoord(MapCoord coord)
        {
            EnsureState();
            MapPoint point = State.Map.GetPoint(coord);
            if (!CanEnterPoint(point))
            {
                throw new InvalidOperationException("Map point cannot be entered: " + coord);
            }

            point.IsVisited = true;
            State.CurrentMapCoord = coord;
            State.CurrentRoom = _roomFactory.CreateRoomForMapPoint(State, point);
            SaveRun();
            RoomEntered?.Invoke(State.CurrentRoom);
            return State.CurrentRoom;
        }

        public void CompleteCurrentRoom()
        {
            EnsureState();
            if (State.CurrentRoom == null)
            {
                throw new InvalidOperationException("No current room.");
            }

            IReadOnlyList<Rewards.Reward> rewards = State.CurrentRoom.GenerateRewards(State);
            State.CurrentRoom.SetRewards(rewards);
            State.CurrentRoom.SetCompleted(true);
            State.CurrentRoom.MapPoint.IsCompleted = true;
            SaveRun();
            RoomCompleted?.Invoke(State.CurrentRoom);
        }

        public void ProceedToMap()
        {
            EnsureState();
            if (State.CurrentRoom == null)
            {
                return;
            }

            if (State.CurrentRoom.HasPendingRewards)
            {
                throw new InvalidOperationException("Room still has pending rewards.");
            }

            if (State.CurrentRoom.RoomType == RoomType.Boss)
            {
                State.IsGameOver = true;
                SaveRun();
                RunEnded?.Invoke(State);
                return;
            }

            State.CurrentRoom = null;
            SaveRun();
            MapChanged?.Invoke(State);
        }

        public void SaveRun()
        {
            EnsureState();
            _saveManager.SaveCurrentRun(State);
        }

        public RunState LoadRun()
        {
            State = _saveManager.TryLoadCurrentRun();
            if (State != null)
            {
                MapChanged?.Invoke(State);
            }

            return State;
        }

        public void DeleteRun()
        {
            _saveManager.DeleteCurrentRun();
            State = null;
        }

        private bool CanEnterPoint(MapPoint point)
        {
            if (point == null)
            {
                return false;
            }

            if (State.CurrentMapCoord == null)
            {
                return HasChild(State.Map.StartingMapPoint, point);
            }

            MapPoint current = State.Map.GetPoint(State.CurrentMapCoord.Value);
            return HasChild(current, point);
        }

        private static bool HasChild(MapPoint parent, MapPoint child)
        {
            foreach (MapPoint candidate in parent.Children)
            {
                if (candidate.Coord == child.Coord)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureState()
        {
            if (State == null)
            {
                throw new InvalidOperationException("Run has not started.");
            }
        }
    }
}
