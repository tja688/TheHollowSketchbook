using System;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Interaction
{
    public sealed class IntentValidator
    {
        private readonly GridState _grid;

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

            return new IntentPreview(false, IntentKind.None, "UnknownIntent", Array.Empty<GridCoord>(), Array.Empty<CardInstanceId>());
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

            return IntentValidationResult.Invalid("UnsupportedIntent");
        }
    }
}
