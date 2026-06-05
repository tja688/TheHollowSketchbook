using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Validation
{
    public sealed class InvariantViolation
    {
        public InvariantViolation(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }

        public override string ToString()
        {
            return Code + ": " + Message;
        }
    }

    public sealed class DomainInvariantValidator
    {
        public IReadOnlyList<InvariantViolation> Validate(GridState grid)
        {
            List<InvariantViolation> violations = new List<InvariantViolation>();
            ValidatePlayerCard(grid, violations);
            ValidateGridCards(grid, violations);
            return violations;
        }

        private static void ValidatePlayerCard(GridState grid, ICollection<InvariantViolation> violations)
        {
            int playerCount = grid.AllGridCards.Count(card => card.CardType == CardType.Player && !card.IsRemoved);
            if (playerCount != 1)
            {
                violations.Add(new InvariantViolation("PlayerCount", "Grid must contain exactly one player card, actual: " + playerCount));
            }
        }

        private static void ValidateGridCards(GridState grid, ICollection<InvariantViolation> violations)
        {
            HashSet<CardInstanceId> seen = new HashSet<CardInstanceId>();
            foreach (GridCell cell in grid.Cells)
            {
                IReadOnlyList<CardInstance> stack = cell.StackView;
                for (int i = 0; i < stack.Count; i++)
                {
                    CardInstance card = stack[i];
                    if (!seen.Add(card.InstanceId))
                    {
                        violations.Add(new InvariantViolation("DuplicateGridCard", card.InstanceId.ToString()));
                    }

                    if (card.Zone != CardZone.Grid)
                    {
                        violations.Add(new InvariantViolation("WrongZone", card.InstanceId + " zone is " + card.Zone));
                    }

                    if (!card.Coord.HasValue || card.Coord.Value != cell.Coord)
                    {
                        violations.Add(new InvariantViolation("WrongCoord", card.InstanceId + " coord does not match cell " + cell.Coord));
                    }

                    if (card.StackIndex != i)
                    {
                        violations.Add(new InvariantViolation("WrongStackIndex", card.InstanceId + " stack index is " + card.StackIndex + ", expected " + i));
                    }

                    if (card.IsRemoved)
                    {
                        violations.Add(new InvariantViolation("RemovedCardOnGrid", card.InstanceId.ToString()));
                    }
                }
            }
        }
    }
}
