using Game.Core.Models;

namespace Game.Core.Entities
{
    public abstract class ActModel : AbstractModel
    {
        public abstract string Name { get; }
        public abstract ModelId[] EncounterIds { get; }
    }
}
