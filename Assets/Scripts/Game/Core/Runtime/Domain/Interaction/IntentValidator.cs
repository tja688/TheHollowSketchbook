using System;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Inventory;

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
                return new IntentPreview(result.IsValid, intent.Kind, result.FailureCode, Array.Empty<GridCoord>(), Array.Empty<CardInstanceId>());
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

                ItemUseContext useContext = new ItemUseContext(_domain, itemCard, useItemIntent.Slot, useItemIntent, Array.Empty<Game.Core.Domain.Events.DomainEvent>());
                if (!itemModel.CanUse(useContext))
                {
                    return IntentValidationResult.Invalid("ItemCannotUse");
                }

                return IntentValidationResult.Valid();
            }

            if (intent is ChooseOptionIntent)
            {
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
    }
}
