using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Inventory;
using Game.Core.Domain.Rooms;

namespace Game.Core.Domain.Interaction
{
    public sealed class IntentValidator
    {
        private readonly DomainActionContext _domain;
        private readonly GridState _grid;

        public IntentValidator(DomainActionContext domain)
        {
            _domain = domain ?? throw new ArgumentNullException(nameof(domain));
            _grid = domain.Grid;
        }

        public IntentValidator(GridState grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public IntentPreview Preview(PlayerIntent intent)
        {
            IntentValidationResult result = Validate(intent);
            if (intent is MovePlayerIntent moveIntent)
            {
                return new IntentPreview(result.IsValid, intent.Kind, result.FailureCode, new[] { moveIntent.To }, Array.Empty<CardInstanceId>());
            }

            if (intent is InteractWithCardIntent interactIntent)
            {
                return new IntentPreview(result.IsValid, intent.Kind, result.FailureCode, Array.Empty<GridCoord>(), new[] { interactIntent.Target });
            }

            if (intent is StoreItemIntent storeItemIntent)
            {
                return new IntentPreview(result.IsValid, intent.Kind, result.FailureCode, Array.Empty<GridCoord>(), new[] { storeItemIntent.ItemCard });
            }

            if (intent is UseItemIntent useItemIntent)
            {
                return new IntentPreview(result.IsValid, intent.Kind, result.FailureCode, GetTargetCells(useItemIntent.Target), GetTargetCards(useItemIntent.Target));
            }

            if (intent is ChooseOptionIntent chooseOptionIntent)
            {
                return new IntentPreview(result.IsValid, intent.Kind, result.FailureCode, Array.Empty<GridCoord>(), Array.Empty<CardInstanceId>());
            }

            return new IntentPreview(result.IsValid, intent.Kind, result.FailureCode, Array.Empty<GridCoord>(), Array.Empty<CardInstanceId>());
        }

        public IntentValidationResult Validate(PlayerIntent intent)
        {
            if (intent == null)
            {
                return IntentValidationResult.Invalid("NullIntent");
            }

            CardInstance player = _grid.PlayerCard;
            if (player == null)
            {
                return IntentValidationResult.Invalid("MissingPlayerCard");
            }

            if (intent is MovePlayerIntent moveIntent)
            {
                if (!moveIntent.To.IsValid)
                {
                    return IntentValidationResult.Invalid("InvalidCoord");
                }

                if (!_grid.IsEmpty(moveIntent.To))
                {
                    return IntentValidationResult.Invalid("TargetCellNotEmpty");
                }

                return IntentValidationResult.Valid();
            }

            if (intent is InteractWithCardIntent interactIntent)
            {
                if (!_grid.TryGetCard(interactIntent.Target, out CardInstance target))
                {
                    return IntentValidationResult.Invalid("TargetNotFound");
                }

                if (target.CardType == CardType.Player)
                {
                    return IntentValidationResult.Invalid("CannotInteractWithPlayer");
                }

                if (target.Zone != CardZone.Grid || !target.Coord.HasValue)
                {
                    return IntentValidationResult.Invalid("TargetNotOnGrid");
                }

                if (_grid.GetTopCard(target.Coord.Value) != target)
                {
                    return IntentValidationResult.Invalid("TargetNotTopCard");
                }

                if (!target.IsFaceUp)
                {
                    return IntentValidationResult.Invalid("TargetFaceDown");
                }

                // Route choice cards can only be interacted with after room is cleared
                if (target.CardType == CardType.RouteChoice && _domain != null)
                {
                    if (!_domain.RoomClearChecker.IsRoomCleared(_grid))
                    {
                        return IntentValidationResult.Invalid("RoomNotCleared");
                    }
                }

                return IntentValidationResult.Valid();
            }

            if (intent is StoreItemIntent storeItemIntent)
            {
                if (_domain == null)
                {
                    return IntentValidationResult.Invalid("StoreItemRequiresDomainContext");
                }

                if (!_grid.TryGetCard(storeItemIntent.ItemCard, out CardInstance itemCard))
                {
                    return IntentValidationResult.Invalid("TargetNotFound");
                }

                if (itemCard.CardType != CardType.Item)
                {
                    return IntentValidationResult.Invalid("TargetNotItem");
                }

                if (itemCard.Zone != CardZone.Grid || !itemCard.Coord.HasValue)
                {
                    return IntentValidationResult.Invalid("TargetNotOnGrid");
                }

                if (_grid.GetTopCard(itemCard.Coord.Value) != itemCard)
                {
                    return IntentValidationResult.Invalid("TargetNotTopCard");
                }

                if (!itemCard.IsFaceUp)
                {
                    return IntentValidationResult.Invalid("TargetFaceDown");
                }

                if (!_domain.TryResolveCardModel(itemCard, out CardModel model))
                {
                    return IntentValidationResult.Invalid("CardModelNotFound");
                }

                if (!model.CanBeStoredInInventory)
                {
                    return IntentValidationResult.Invalid("TargetCannotBeStored");
                }

                return IntentValidationResult.Valid();
            }

            if (intent is UseItemIntent useItemIntent)
            {
                if (_domain == null)
                {
                    return IntentValidationResult.Invalid("UseItemRequiresDomainContext");
                }

                if (!_domain.ItemInventory.TryGet(useItemIntent.Slot, out CardInstance itemCard))
                {
                    return IntentValidationResult.Invalid("ItemSlotEmpty");
                }

                if (itemCard.CardType != CardType.Item)
                {
                    return IntentValidationResult.Invalid("SlotNotItem");
                }

                if (!_domain.TryResolveCardModel(itemCard, out CardModel model) || model is not ItemCardModel itemModel)
                {
                    return IntentValidationResult.Invalid("ItemModelNotFound");
                }

                IntentValidationResult targetValidation = ValidateItemTarget(itemModel.TargetMode, useItemIntent.Target);
                if (!targetValidation.IsValid)
                {
                    return targetValidation;
                }

                ItemUseContext useContext = new ItemUseContext(_domain, itemCard, useItemIntent.Slot, useItemIntent, Array.Empty<Game.Core.Domain.Events.DomainEvent>());
                if (!itemModel.CanUse(useContext))
                {
                    return IntentValidationResult.Invalid("ItemCannotUse");
                }

                return IntentValidationResult.Valid();
            }

            if (intent is ChooseOptionIntent chooseOptionIntent)
            {
                if (_domain == null)
                {
                    return IntentValidationResult.Invalid("ChooseOptionRequiresDomainContext");
                }

                if (!_domain.ChoiceSessions.TryGet(chooseOptionIntent.SessionId, out ChoiceSession session))
                {
                    return IntentValidationResult.Invalid("ChoiceSessionNotFound");
                }

                if (session.IsResolved)
                {
                    return IntentValidationResult.Invalid("ChoiceAlreadyResolved");
                }

                if (!session.IsValidOption(chooseOptionIntent.OptionIndex))
                {
                    return IntentValidationResult.Invalid("ChoiceOptionOutOfRange");
                }

                return IntentValidationResult.Valid();
            }

            if (intent is ActivateRelicIntent activateRelicIntent)
            {
                if (_domain == null)
                {
                    return IntentValidationResult.Invalid("ActivateRelicRequiresDomainContext");
                }

                if (!_domain.TryResolveRelicModel(activateRelicIntent.RelicId, out RelicModel relic))
                {
                    return IntentValidationResult.Invalid("RelicModelNotFound");
                }

                if (!_domain.Relics.ActiveSlot.Contains(relic.Id))
                {
                    return IntentValidationResult.Invalid("RelicNotEquipped");
                }

                ActiveRelicContext relicContext = new ActiveRelicContext(_domain, relic, _domain.Relics.ActiveSlot, activateRelicIntent, Array.Empty<Game.Core.Domain.Events.DomainEvent>());
                if (!relic.CanActivate(relicContext))
                {
                    return IntentValidationResult.Invalid("RelicCannotActivate");
                }

                return IntentValidationResult.Valid();
            }

            return IntentValidationResult.Invalid("UnsupportedIntent");
        }

        private IntentValidationResult ValidateItemTarget(ItemTargetMode mode, ItemTargetSelection target)
        {
            switch (mode)
            {
                case ItemTargetMode.None:
                case ItemTargetMode.Player:
                    return IntentValidationResult.Valid();
                case ItemTargetMode.GridCell:
                    return ValidateGridCell(target);
                case ItemTargetMode.MonsterCard:
                    return ValidateTargetCard(target.PrimaryCard, target.HasPrimaryCard, true);
                case ItemTargetMode.CardThenDirection:
                    IntentValidationResult cardResult = ValidateTargetCard(target.PrimaryCard, target.HasPrimaryCard, false);
                    if (!cardResult.IsValid)
                    {
                        return cardResult;
                    }

                    return target.HasDirection ? IntentValidationResult.Valid() : IntentValidationResult.Invalid("ItemTargetDirectionMissing");
                case ItemTargetMode.TwoCards:
                    IntentValidationResult firstCard = ValidateTargetCard(target.PrimaryCard, target.HasPrimaryCard, false);
                    if (!firstCard.IsValid)
                    {
                        return firstCard;
                    }

                    IntentValidationResult secondCard = ValidateTargetCard(target.SecondaryCard, target.HasSecondaryCard, false);
                    if (!secondCard.IsValid)
                    {
                        return secondCard;
                    }

                    return target.PrimaryCard != target.SecondaryCard ? IntentValidationResult.Valid() : IntentValidationResult.Invalid("ItemTargetsMustDiffer");
                case ItemTargetMode.AnyCard:
                    return ValidateTargetCard(target.PrimaryCard, target.HasPrimaryCard, false);
                case ItemTargetMode.AnyCardThenAnyCell:
                    IntentValidationResult anyCard = ValidateTargetCard(target.PrimaryCard, target.HasPrimaryCard, false);
                    if (!anyCard.IsValid)
                    {
                        return anyCard;
                    }

                    return ValidateGridCell(target);
                default:
                    return IntentValidationResult.Invalid("UnsupportedItemTargetMode");
            }
        }

        private static IntentValidationResult ValidateGridCell(ItemTargetSelection target)
        {
            if (!target.HasGridCell)
            {
                return IntentValidationResult.Invalid("ItemTargetCellMissing");
            }

            return target.GridCell.IsValid ? IntentValidationResult.Valid() : IntentValidationResult.Invalid("InvalidCoord");
        }

        private IntentValidationResult ValidateTargetCard(CardInstanceId cardId, bool hasCard, bool requireMonster)
        {
            if (!hasCard || cardId.IsEmpty)
            {
                return IntentValidationResult.Invalid("ItemTargetCardMissing");
            }

            if (!_grid.TryGetCard(cardId, out CardInstance target))
            {
                return IntentValidationResult.Invalid("TargetNotFound");
            }

            if (target.Zone != CardZone.Grid || !target.Coord.HasValue)
            {
                return IntentValidationResult.Invalid("TargetNotOnGrid");
            }

            if (_grid.GetTopCard(target.Coord.Value) != target)
            {
                return IntentValidationResult.Invalid("TargetNotTopCard");
            }

            if (!target.IsFaceUp)
            {
                return IntentValidationResult.Invalid("TargetFaceDown");
            }

            if (requireMonster && target.CardType != CardType.Monster)
            {
                return IntentValidationResult.Invalid("TargetNotMonster");
            }

            return IntentValidationResult.Valid();
        }

        private static IReadOnlyList<GridCoord> GetTargetCells(ItemTargetSelection target)
        {
            List<GridCoord> cells = new List<GridCoord>(2);
            if (target.HasGridCell)
            {
                cells.Add(target.GridCell);
            }

            if (target.HasSecondaryGridCell)
            {
                cells.Add(target.SecondaryGridCell);
            }

            return cells;
        }

        private static IReadOnlyList<CardInstanceId> GetTargetCards(ItemTargetSelection target)
        {
            List<CardInstanceId> cards = new List<CardInstanceId>(2);
            if (target.HasPrimaryCard)
            {
                cards.Add(target.PrimaryCard);
            }

            if (target.HasSecondaryCard)
            {
                cards.Add(target.SecondaryCard);
            }

            return cards;
        }
    }
}
