namespace Game.Core.Cards
{
    public readonly struct ResourceInfo
    {
        public int EnergySpent { get; }
        public int StarsSpent { get; }

        public ResourceInfo(int energySpent, int starsSpent = 0)
        {
            EnergySpent = energySpent;
            StarsSpent = starsSpent;
        }
    }
}
