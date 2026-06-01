using Game.Core;
using Game.Core.Entities;

namespace Game.Content
{
    public sealed class PrototypeAct : ActModel
    {
        private static readonly ModelId[] EncounterIdsValue =
        {
            new ModelId("Encounter", "PrototypeCultistEncounter")
        };

        public override ModelId Id => new ModelId("Act", "PrototypeAct");
        public override string Name => "Prototype Act";
        public override ModelId[] EncounterIds => EncounterIdsValue;
    }
}
