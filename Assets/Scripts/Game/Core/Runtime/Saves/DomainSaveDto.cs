using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Inventory;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Progression;
using Game.Core.Domain.Rooms;

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
    public sealed class DungeonDeckSaveDto
    {
        public List<uint> CardInstanceIds = new List<uint>();
    }

    [Serializable]
    public sealed class ChoiceSessionSaveDto
    {
        public string SessionId;
        public int OptionCount;
        public string ChoiceKind;
        public bool IsResolved;
        public int SelectedOptionIndex = -1;
    }

    [Serializable]
    public sealed class RunProgressionSaveDto
    {
        public int LayerIndex;
        public int NodeIndex;
        public int CurrentRoomType;
        public List<int> PendingChoiceRoomTypes = new List<int>();
    }

    [Serializable]
    public sealed class StatModifierSaveDto
    {
        public int Stat;
        public int Scope;
        public int Amount;
        public string Source;
    }

    [Serializable]
    public sealed class KeywordStateSaveDto
    {
        public string Keyword;
        public int Scope;
        public int Value;
    }

    [Serializable]
    public sealed class PlayerRunStateSaveDto
    {
        public int BaseMaxHp;
        public int BaseAttack;
        public int BaseDefense;
        public List<StatModifierSaveDto> Modifiers = new List<StatModifierSaveDto>();
        public List<KeywordStateSaveDto> Keywords = new List<KeywordStateSaveDto>();
    }

    [Serializable]
    public sealed class PendingTriggerSaveDto
    {
        public uint CardInstanceId;
        public int Timing;
        public int DueActionIndex;
        public string TriggerKey;
    }

    [Serializable]
    public sealed class RoomDomainStateSaveDto
    {
        public int RoomType;
        public int LayerIndex;
        public int NodeIndex;
        public int ActionCounterValue;
        public int PlayerGold;
        public uint? RngState;
        public GridStateSaveDto Grid;
        public DungeonDeckSaveDto DungeonDeck;
        public PlayerInventorySaveDto ItemInventory;
        public RelicInventorySaveDto RelicInventory;
        public PlayerRunStateSaveDto PlayerRunState;
        public List<ChoiceSessionSaveDto> ActiveChoices = new List<ChoiceSessionSaveDto>();
        public List<uint> PendingTriggerCardInstanceIds = new List<uint>();
        public List<PendingTriggerSaveDto> PendingTriggers = new List<PendingTriggerSaveDto>();
        public List<int> RouteChoiceRoomTypes = new List<int>();
    }
}
