using System.Collections.Generic;
using Game.Core.Entities;
using Game.Core.Random;

namespace Game.Core.Runs
{
    public sealed class RunState
    {
        public RunState(int seed, IRng rng, IReadOnlyList<Player> players, IReadOnlyList<ActModel> acts)
        {
            Seed = seed;
            Rng = rng;
            Players = players;
            Acts = acts;
        }

        public int Seed { get; }
        public IRng Rng { get; }
        public IReadOnlyList<Player> Players { get; }
        public IReadOnlyList<ActModel> Acts { get; }

        public int CurrentActIndex { get; set; }
        public bool IsGameOver { get; set; }

        public ActModel CurrentAct
        {
            get { return Acts[CurrentActIndex]; }
        }
    }
}
