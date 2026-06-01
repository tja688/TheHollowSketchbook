using Game.Core;
using Game.Core.Entities;

namespace Game.Content
{
    public sealed class PrototypeEncounter : EncounterModel
    {
        private static readonly ModelId[] EnemyIdsValue =
        {
            new ModelId("Enemy", "DebugCultist")
        };

        public override ModelId Id => new ModelId("Encounter", "PrototypeCultistEncounter");
        public override string Name => "Prototype Cultist Encounter";
        public override ModelId[] EnemyIds => EnemyIdsValue;
    }
}
