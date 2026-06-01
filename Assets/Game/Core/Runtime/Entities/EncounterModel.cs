using Game.Core.Models;

namespace Game.Core.Entities
{
    public abstract class EncounterModel : AbstractModel
    {
        public abstract string Name { get; }
        public abstract ModelId[] EnemyIds { get; }
    }
}
