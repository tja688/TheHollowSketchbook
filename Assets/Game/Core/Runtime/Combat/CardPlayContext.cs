using Game.Core.Random;

namespace Game.Core.Combat
{
    public sealed class CardPlayContext
    {
        public CardPlayContext(CombatState combat, IRng rng)
        {
            Combat = combat;
            Rng = rng;
        }

        public CombatState Combat { get; }
        public IRng Rng { get; }
    }
}
