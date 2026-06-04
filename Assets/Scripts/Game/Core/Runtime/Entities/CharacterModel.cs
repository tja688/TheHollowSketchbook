using Game.Core.Models;

namespace Game.Core.Entities
{
    /// <summary>
    /// Character configuration model.
    /// BOUNDARY: StS-specific fields (StartingMaxEnergy, StarterDeck) removed.
    /// Extend this class to add starting attributes (Attack, Defense) for the grid-based system.
    /// </summary>
    public abstract class CharacterModel : AbstractModel
    {
        public abstract string Name { get; }
        public abstract int StartingMaxHp { get; }
    }
}
