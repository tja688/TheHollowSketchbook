using Game.Core.Cards;
using Game.Core.Models;

namespace Game.Core.Entities
{
    public abstract class CharacterModel : AbstractModel
    {
        public abstract string Name { get; }
        public abstract int StartingMaxHp { get; }
        public abstract int StartingMaxEnergy { get; }
        public abstract ModelId[] StarterDeck { get; }
    }
}
