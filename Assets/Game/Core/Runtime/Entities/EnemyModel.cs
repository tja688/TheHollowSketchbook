using System.Threading.Tasks;
using Game.Core.Combat;
using Game.Core.Models;
using Game.Core.Random;

namespace Game.Core.Entities
{
    public abstract class EnemyModel : AbstractModel
    {
        public abstract string Name { get; }
        public abstract int MaxHp { get; }

        public abstract EnemyIntent BuildIntent(CombatState combat, Creature self, IRng rng);
        public abstract Task ExecuteIntent(CardPlayContext ctx, Creature self, EnemyIntent intent);
    }
}
