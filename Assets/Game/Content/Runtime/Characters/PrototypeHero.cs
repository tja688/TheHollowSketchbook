using Game.Core;
using Game.Core.Entities;

namespace Game.Content
{
    public sealed class PrototypeHero : CharacterModel
    {
        private static readonly ModelId[] StarterDeckIds =
        {
            new ModelId("Card", "Strike"),
            new ModelId("Card", "Strike"),
            new ModelId("Card", "Strike"),
            new ModelId("Card", "Strike"),
            new ModelId("Card", "Strike"),
            new ModelId("Card", "Defend"),
            new ModelId("Card", "Defend"),
            new ModelId("Card", "Defend"),
            new ModelId("Card", "Defend"),
            new ModelId("Card", "Defend"),
            new ModelId("Card", "Bash")
        };

        public override ModelId Id => new ModelId("Character", "PrototypeHero");
        public override string Name => "Prototype Hero";
        public override int StartingMaxHp => 80;
        public override int StartingMaxEnergy => 3;
        public override ModelId[] StarterDeck => StarterDeckIds;
    }
}
