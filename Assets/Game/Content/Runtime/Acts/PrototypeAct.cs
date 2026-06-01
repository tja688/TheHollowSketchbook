using Game.Core;
using Game.Core.Entities;

namespace Game.Content
{
    public sealed class PrototypeAct : ActModel
    {
        private static readonly ModelId[] EncounterIdsValue =
        {
            new ModelId("Encounter", "PrototypeCultistEncounter"),
            new ModelId("Encounter", "PrototypeSlimeEncounter")
        };

        public override ModelId Id => new ModelId("Act", "PrototypeAct");
        public override string Name => "Prototype Act";
        public override ModelId[] EncounterIds => EncounterIdsValue;
        public override int MapLength => 8;
        public override int ColumnCount => 7;
    }
}
