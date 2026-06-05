using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Inventory;
using Game.Core.Models;

namespace Game.Core.Saves
{
    /// <summary>
    /// Bridges between DomainActionContext runtime state and RoomDomainStateSaveDto.
    /// BOUNDARY: Domain-layer serialization only; does not touch Presentation or RunState.
    /// </summary>
    public static class DomainSaveAdapter
    {
        public static RoomDomainStateSaveDto Capture(DomainActionContext domain)
        {
            if (domain == null)
            {
                return null;
            }

            return new RoomDomainStateSaveDto
            {
                ActionCounterValue = domain.ActionCounter.Value,
                PlayerGold = domain.PlayerGold,
                Grid = CaptureGrid(domain.Grid),
                ItemInventory = CaptureItemInventory(domain.ItemInventory),
                RelicInventory = CaptureRelicInventory(domain.Relics)
            };
        }

        public static void Restore(RoomDomainStateSaveDto dto, DomainActionContext domain)
        {
            if (dto == null || domain == null)
            {
                return;
            }

            GridState restoredGrid = RestoreGrid(dto.Grid);
            if (restoredGrid != null)
            {
                domain.Grid = restoredGrid;
            }

            domain.ActionCounter.RestoreValue(dto.ActionCounterValue);
            domain.SetPlayerGold(dto.PlayerGold);

            Dictionary<uint, CardInstance> cardLookup = BuildCardLookup(domain.Grid);
            RestoreItemInventory(dto.ItemInventory, domain.ItemInventory, cardLookup);
            RestoreRelicInventory(dto.RelicInventory, domain.Relics);
        }

        private static Dictionary<uint, CardInstance> BuildCardLookup(GridState grid)
        {
            Dictionary<uint, CardInstance> lookup = new Dictionary<uint, CardInstance>();
            if (grid == null)
            {
                return lookup;
            }

            foreach (CardInstance card in grid.AllKnownCards)
            {
                lookup[card.InstanceId.Value] = card;
            }

            return lookup;
        }

        public static GridStateSaveDto CaptureGrid(GridState grid)
        {
            if (grid == null)
            {
                return null;
            }

            GridStateSaveDto dto = new GridStateSaveDto();
            Dictionary<CardInstance, uint> idMap = new Dictionary<CardInstance, uint>();

            foreach (GridCell cell in grid.Cells)
            {
                GridCellSaveDto cellDto = new GridCellSaveDto
                {
                    CoordRow = cell.Coord.Row,
                    CoordCol = cell.Coord.Col
                };

                IReadOnlyList<CardInstance> stack = cell.StackView;
                for (int i = 0; i < stack.Count; i++)
                {
                    cellDto.CardInstanceIds.Add(stack[i].InstanceId.Value);
                }

                dto.Cells.Add(cellDto);
            }

            foreach (CardInstance card in grid.AllKnownCards)
            {
                dto.Cards.Add(CaptureCardInstance(card));
            }

            return dto;
        }

        public static CardInstanceSaveDto CaptureCardInstance(CardInstance card)
        {
            if (card == null)
            {
                return null;
            }

            CardInstanceSaveDto dto = new CardInstanceSaveDto
            {
                InstanceId = card.InstanceId.Value,
                ModelCategory = card.ModelId.Category,
                ModelEntry = card.ModelId.Entry,
                CardType = (int)card.CardType,
                Zone = (int)card.Zone,
                StackIndex = card.StackIndex,
                IsFaceUp = card.IsFaceUp,
                IsRemoved = card.IsRemoved,
                MaxHp = card.MaxHp,
                CurrentHp = card.CurrentHp,
                Attack = card.Attack,
                Defense = card.Defense,
                ContactDamageToPlayer = card.ContactDamageToPlayer,
                GoldOnRemoved = card.GoldOnRemoved,
                GoldValue = card.GoldValue
            };

            if (card.Coord.HasValue)
            {
                dto.CoordRow = card.Coord.Value.Row;
                dto.CoordCol = card.Coord.Value.Col;
            }

            foreach (KeyValuePair<string, int> entry in card.RuntimeState)
            {
                dto.RuntimeState.Add(new RuntimeStateEntry { Key = entry.Key, Value = entry.Value });
            }

            return dto;
        }

        public static PlayerInventorySaveDto CaptureItemInventory(PlayerInventory inventory)
        {
            if (inventory == null)
            {
                return null;
            }

            PlayerInventorySaveDto dto = new PlayerInventorySaveDto();
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                dto.ItemInstanceIds.Add(inventory.Items[i].InstanceId.Value);
            }

            return dto;
        }

        public static RelicInventorySaveDto CaptureRelicInventory(RelicInventory relics)
        {
            if (relics == null)
            {
                return null;
            }

            RelicInventorySaveDto dto = new RelicInventorySaveDto();
            foreach (ModelId relicId in relics.PassiveRelics)
            {
                dto.PassiveRelics.Add(new ActIdSaveDto { Category = relicId.Category, Entry = relicId.Entry });
            }

            if (!relics.ActiveSlot.IsEmpty)
            {
                dto.ActiveRelic = new ActIdSaveDto
                {
                    Category = relics.ActiveSlot.RelicId.Category,
                    Entry = relics.ActiveSlot.RelicId.Entry
                };
                dto.ActiveRelicMaxUses = relics.ActiveSlot.MaxUsesPerRoom;
                dto.ActiveRelicUsesRemaining = relics.ActiveSlot.UsesRemainingThisRoom;
            }

            return dto;
        }

        public static GridState RestoreGrid(GridStateSaveDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            GridState grid = new GridState();
            Dictionary<uint, CardInstance> cardsById = new Dictionary<uint, CardInstance>();

            // First pass: reconstruct all CardInstances
            foreach (CardInstanceSaveDto cardDto in dto.Cards)
            {
                CardInstance card = new CardInstance(
                    new CardInstanceId(cardDto.InstanceId),
                    new ModelId(cardDto.ModelCategory, cardDto.ModelEntry),
                    (CardType)cardDto.CardType)
                {
                    Zone = (CardZone)cardDto.Zone,
                    StackIndex = cardDto.StackIndex,
                    IsFaceUp = cardDto.IsFaceUp,
                    IsRemoved = cardDto.IsRemoved
                };

                card.ConfigureCombatStats(cardDto.MaxHp, cardDto.Attack, cardDto.Defense, cardDto.ContactDamageToPlayer, cardDto.GoldOnRemoved);
                card.SetCurrentHp(cardDto.CurrentHp);
                card.ConfigureGoldValue(cardDto.GoldValue);

                if (cardDto.CoordRow.HasValue && cardDto.CoordCol.HasValue)
                {
                    card.Coord = new GridCoord(cardDto.CoordRow.Value, cardDto.CoordCol.Value);
                }

                foreach (RuntimeStateEntry entry in cardDto.RuntimeState)
                {
                    card.SetState(entry.Key, entry.Value);
                }

                cardsById[cardDto.InstanceId] = card;
            }

            // Second pass: place cards into cells by stack order
            foreach (GridCellSaveDto cellDto in dto.Cells)
            {
                GridCoord coord = new GridCoord(cellDto.CoordRow, cellDto.CoordCol);
                for (int i = 0; i < cellDto.CardInstanceIds.Count; i++)
                {
                    uint id = cellDto.CardInstanceIds[i];
                    if (cardsById.TryGetValue(id, out CardInstance card))
                    {
                        grid.AddCardToGrid(card, coord, card.IsFaceUp);
                    }
                }
            }

            return grid;
        }

        public static void RestoreItemInventory(PlayerInventorySaveDto dto, PlayerInventory inventory, Dictionary<uint, CardInstance> cardLookup)
        {
            if (dto == null || inventory == null)
            {
                return;
            }

            inventory.Clear();
            for (int i = 0; i < dto.ItemInstanceIds.Count; i++)
            {
                if (cardLookup.TryGetValue(dto.ItemInstanceIds[i], out CardInstance card) && card.CardType == CardType.Item)
                {
                    inventory.Store(card);
                }
            }
        }

        public static void RestoreRelicInventory(RelicInventorySaveDto dto, RelicInventory relics)
        {
            if (dto == null || relics == null)
            {
                return;
            }

            relics.Clear();
            foreach (ActIdSaveDto passive in dto.PassiveRelics)
            {
                relics.AddPassive(new ModelId(passive.Category, passive.Entry));
            }

            if (dto.ActiveRelic != null)
            {
                relics.ActiveSlot.Assign(new ModelId(dto.ActiveRelic.Category, dto.ActiveRelic.Entry), dto.ActiveRelicMaxUses);
                relics.ActiveSlot.SetUsesRemaining(dto.ActiveRelicUsesRemaining);
            }
        }
    }
}
