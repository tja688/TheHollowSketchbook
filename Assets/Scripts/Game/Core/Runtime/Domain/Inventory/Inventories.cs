using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;

namespace Game.Core.Domain.Inventory
{
    public readonly struct InventorySlot : IEquatable<InventorySlot>
    {
        public InventorySlot(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public bool IsValid
        {
            get { return Index >= 0; }
        }

        public bool Equals(InventorySlot other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is InventorySlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public override string ToString()
        {
            return IsValid ? "slot" + Index : "slot<invalid>";
        }

        public static bool operator ==(InventorySlot left, InventorySlot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InventorySlot left, InventorySlot right)
        {
            return !left.Equals(right);
        }
    }

    public sealed class PlayerInventory
    {
        private readonly List<CardInstance> _items = new List<CardInstance>();

        public IReadOnlyList<CardInstance> Items
        {
            get { return _items; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool Contains(CardInstanceId cardId)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].InstanceId == cardId)
                {
                    return true;
                }
            }

            return false;
        }

        public InventorySlot Store(CardInstance item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.CardType != CardType.Item)
            {
                throw new InvalidOperationException("Only item cards can be stored in player inventory.");
            }

            item.Zone = CardZone.PlayerInventory;
            item.Coord = null;
            item.StackIndex = -1;
            item.IsFaceUp = true;
            item.IsRemoved = false;
            _items.Add(item);
            return new InventorySlot(_items.Count - 1);
        }

        public bool TryGet(InventorySlot slot, out CardInstance item)
        {
            if (slot.Index >= 0 && slot.Index < _items.Count)
            {
                item = _items[slot.Index];
                return true;
            }

            item = null;
            return false;
        }

        public CardInstance Get(InventorySlot slot)
        {
            if (!TryGet(slot, out CardInstance item))
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            return item;
        }

        public InventorySlot FindSlot(CardInstanceId cardId)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].InstanceId == cardId)
                {
                    return new InventorySlot(i);
                }
            }

            return new InventorySlot(-1);
        }

        public CardInstance RemoveAt(InventorySlot slot)
        {
            CardInstance item = Get(slot);
            _items.RemoveAt(slot.Index);
            item.StackIndex = -1;
            return item;
        }

        public bool TryRemove(CardInstanceId cardId, out CardInstance item)
        {
            InventorySlot slot = FindSlot(cardId);
            if (!slot.IsValid)
            {
                item = null;
                return false;
            }

            item = RemoveAt(slot);
            return true;
        }

        public void Clear()
        {
            _items.Clear();
        }
    }

    public sealed class ActiveRelicSlot
    {
        public ModelId RelicId { get; private set; }
        public int MaxUsesPerRoom { get; private set; }
        public int UsesRemainingThisRoom { get; private set; }

        public bool IsEmpty
        {
            get { return RelicId.IsEmpty; }
        }

        public void Assign(ModelId relicId, int maxUsesPerRoom = 1)
        {
            if (relicId.IsEmpty)
            {
                throw new ArgumentException("Relic id cannot be empty.", nameof(relicId));
            }

            RelicId = relicId;
            MaxUsesPerRoom = Math.Max(1, maxUsesPerRoom);
            UsesRemainingThisRoom = MaxUsesPerRoom;
        }

        public void Clear()
        {
            RelicId = default;
            MaxUsesPerRoom = 0;
            UsesRemainingThisRoom = 0;
        }

        public void ResetForRoom()
        {
            if (!IsEmpty)
            {
                UsesRemainingThisRoom = MaxUsesPerRoom;
            }
        }

        public bool Contains(ModelId relicId)
        {
            return !IsEmpty && RelicId == relicId;
        }

        public bool CanActivate(ModelId relicId)
        {
            return Contains(relicId) && UsesRemainingThisRoom > 0;
        }

        public void MarkActivated(ModelId relicId)
        {
            if (!CanActivate(relicId))
            {
                throw new InvalidOperationException("Active relic cannot be activated in the current slot state.");
            }

            UsesRemainingThisRoom--;
        }

        public void SetUsesRemaining(int value)
        {
            UsesRemainingThisRoom = value;
        }
    }

    public sealed class RelicInventory
    {
        private readonly List<ModelId> _passiveRelics = new List<ModelId>();

        public IReadOnlyList<ModelId> PassiveRelics
        {
            get { return _passiveRelics; }
        }

        public ActiveRelicSlot ActiveSlot { get; } = new ActiveRelicSlot();

        public IEnumerable<ModelId> AllRelics
        {
            get
            {
                for (int i = 0; i < _passiveRelics.Count; i++)
                {
                    yield return _passiveRelics[i];
                }

                if (!ActiveSlot.IsEmpty)
                {
                    yield return ActiveSlot.RelicId;
                }
            }
        }

        public bool Contains(ModelId relicId)
        {
            if (ActiveSlot.Contains(relicId))
            {
                return true;
            }

            for (int i = 0; i < _passiveRelics.Count; i++)
            {
                if (_passiveRelics[i] == relicId)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddPassive(ModelId relicId)
        {
            if (relicId.IsEmpty)
            {
                throw new ArgumentException("Relic id cannot be empty.", nameof(relicId));
            }

            if (!Contains(relicId))
            {
                _passiveRelics.Add(relicId);
            }
        }

        public void SetActive(ModelId relicId, int maxUsesPerRoom = 1)
        {
            if (!ActiveSlot.IsEmpty && !ActiveSlot.Contains(relicId))
            {
                throw new InvalidOperationException("Active relic slot is already occupied.");
            }

            ActiveSlot.Assign(relicId, maxUsesPerRoom);
        }

        public void Add(RelicModel relic, int maxUsesPerRoom = 1)
        {
            if (relic == null)
            {
                throw new ArgumentNullException(nameof(relic));
            }

            switch (relic.Kind)
            {
                case RelicKind.Active:
                    SetActive(relic.Id, maxUsesPerRoom);
                    break;
                default:
                    AddPassive(relic.Id);
                    break;
            }
        }

        public void Clear()
        {
            _passiveRelics.Clear();
            ActiveSlot.Clear();
        }
    }
}
