using System.Collections.Generic;

namespace Game.Core.Saves
{
    public sealed class RunSaveDto
    {
        public int SaveVersion;
        public int Seed;
        public int CurrentActIndex;
        public List<PlayerSaveDto> Players = new List<PlayerSaveDto>();
    }

    public sealed class PlayerSaveDto
    {
        public string CharacterCategory;
        public string CharacterEntry;
        public int CurrentHp;
        public int MaxHp;
        public int Gold;
        public int MaxEnergy;
        public List<CardSaveDto> Deck = new List<CardSaveDto>();
    }

    public sealed class CardSaveDto
    {
        public string ModelCategory;
        public string ModelEntry;
        public int UpgradeLevel;
        public bool ExhaustOnNextPlay;
        public Dictionary<string, string> ExtraState = new Dictionary<string, string>();
    }
}
