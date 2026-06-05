using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Inventory;

namespace Game.Core.Saves
{
    [Serializable]
    public sealed class CardInstanceSaveDto
    {
        public uint InstanceId;
        public string ModelCategory;
        public string ModelEntry;
        public int CardType;
        public int Zone;
        public int? CoordRow;
        public int? CoordCol;
        public int StackIndex;
        public bool IsFaceUp;
        public bool IsRemoved;
        public int MaxHp;
        public int CurrentHp;
        public int Attack;
        public int Defense;
        public int ContactDamageToPlayer;
        public int GoldOnRemoved;
        public int GoldValue;
        public List<RuntimeStateEntry> RuntimeState = new List<RuntimeStateEntry>();
    }

    [Serializable]
    public sealed class RuntimeStateEntry
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public sealed class GridCellSaveDto
    {
        public int CoordRow;
        public int CoordCol;
        public List<uint> CardInstanceIds = new List<uint>();
    }

    [Serializable]
    public sealed class GridStateSaveDto
    {
        public List<GridCellSaveDto> Cells = new List<GridCellSaveDto>();
        public List<CardInstanceSaveDto> Cards = new List<CardInstanceSaveDto>();
    }

    [Serializable]
    public sealed class PlayerInventorySaveDto
    {
        public List<uint> ItemInstanceIds = new List<uint>();
    }

    [Serializable]
    public sealed class RelicInventorySaveDto
    {
        public List<ActIdSaveDto> PassiveRelics = new List<ActIdSaveDto>();
        public ActIdSaveDto ActiveRelic;
        public int ActiveRelicMaxUses;
        public int ActiveRelicUsesRemaining;
    }

    [Serializable]
    public sealed class RoomDomainStateSaveDto
    {
        public int ActionCounterValue;
        public int PlayerGold;
        public GridStateSaveDto Grid;
        public PlayerInventorySaveDto ItemInventory;
        public RelicInventorySaveDto RelicInventory;
    }
}
