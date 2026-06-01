using Game.Core;
using Game.Core.Entities;

namespace Game.Content
{
    public sealed class PrototypeEliteEncounter : EncounterModel
    {
        private static readonly ModelId[] EnemyIdsValue =
        {
            new ModelId("Enemy", "DebugElite")
        };

        public override ModelId Id => new ModelId("Encounter", "PrototypeEliteEncounter");
        public override string Name => "Prototype Elite Encounter";
        public override ModelId[] EnemyIds => EnemyIdsValue;
    }
}
