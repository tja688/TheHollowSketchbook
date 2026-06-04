using Game.Core.Models;

namespace Game.Core.Entities
{
    /// <summary>
    /// Act (floor/level) configuration model.
    /// BOUNDARY: StS EncounterIds removed. Map parameters retained for map generation.
    /// Extend this class to add room generation parameters for the grid-based system.
    /// </summary>
    public abstract class ActModel : AbstractModel
    {
        public abstract string Name { get; }

        public virtual int MapLength
        {
            get { return 8; }
        }

        public virtual int ColumnCount
        {
            get { return 7; }
        }
    }
}
