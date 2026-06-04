using Game.Core.Models;

namespace Game.Core.Entities
{
    /// <summary>
    /// Enemy configuration model.
    /// BOUNDARY: StS Intent system (BuildIntent/ExecuteIntent) removed.
    /// Retained: Name, MaxHp as base data.
    /// Extend this class to add: BaseAttack, BaseDefense, Traits, movement behavior for the grid-based system.
    /// </summary>
    public abstract class EnemyModel : AbstractModel
    {
        public abstract string Name { get; }
        public abstract int MaxHp { get; }
    }
}
