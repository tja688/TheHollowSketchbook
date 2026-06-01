using System;
using System.Collections.Generic;
using Game.Core.Map;
using Game.Core.Rewards;
using Game.Core.Rooms;

namespace Game.Core.Saves
{
    [Serializable]
    public sealed class RunSaveDto
    {
        public int SaveVersion;
        public int Seed;
        public int CurrentActIndex;
        public bool IsGameOver;
        public RngStateDto RngState;
        public MapCoordSaveDto CurrentMapCoord;
        public MapSaveDto Map;
        public RoomSaveDto CurrentRoom;
        public List<PlayerSaveDto> Players = new List<PlayerSaveDto>();
    }

    [Serializable]
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

    [Serializable]
    public sealed class CardSaveDto
    {
        public string ModelCategory;
        public string ModelEntry;
        public int UpgradeLevel;
        public bool ExhaustOnNextPlay;
        public Dictionary<string, string> ExtraState = new Dictionary<string, string>();
    }

    [Serializable]
    public sealed class MapSaveDto
    {
        public int ColumnCount;
        public int RowCount;
        public MapCoordSaveDto Start;
        public MapCoordSaveDto Boss;
        public List<MapPointSaveDto> Points = new List<MapPointSaveDto>();
    }

    [Serializable]
    public sealed class MapPointSaveDto
    {
        public int Column;
        public int Row;
        public MapPointType PointType;
        public bool IsVisited;
        public bool IsCompleted;
        public List<MapCoordSaveDto> Children = new List<MapCoordSaveDto>();
    }

    [Serializable]
    public sealed class MapCoordSaveDto
    {
        public int Column;
        public int Row;
    }

    [Serializable]
    public sealed class RoomSaveDto
    {
        public RoomType RoomType;
        public bool IsCompleted;
        public string EncounterCategory;
        public string EncounterEntry;
        public List<RewardSaveDto> Rewards = new List<RewardSaveDto>();
    }

    [Serializable]
    public sealed class RewardSaveDto
    {
        public RewardType RewardType;
        public bool IsResolved;
        public int GoldAmount;
        public int SelectedIndex = -1;
        public bool WasSkipped;
        public List<CardSaveDto> CardChoices = new List<CardSaveDto>();
    }

    [Serializable]
    public sealed class RngStateDto
    {
        public uint Value;
    }
}
