using Game.Core;
using Game.Core.Entities;

namespace Game.Content
{
    public sealed class PrototypeSlimeEncounter : EncounterModel
    {
        private static readonly ModelId[] EnemyIdsValue =
        {
            new ModelId("Enemy", "DebugSlime")
        };

        public override ModelId Id => new ModelId("Encounter", "PrototypeSlimeEncounter");
        public override string Name => "Prototype Slime Encounter";
        public override ModelId[] EnemyIds => EnemyIdsValue;
    }
}
