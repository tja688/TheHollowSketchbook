using Game.Core;
using Game.Core.Entities;

namespace Game.Content
{
    public sealed class PrototypeBossEncounter : EncounterModel
    {
        private static readonly ModelId[] EnemyIdsValue =
        {
            new ModelId("Enemy", "DebugBoss")
        };

        public override ModelId Id => new ModelId("Encounter", "PrototypeBossEncounter");
        public override string Name => "Prototype Boss Encounter";
        public override ModelId[] EnemyIds => EnemyIdsValue;
    }
}
