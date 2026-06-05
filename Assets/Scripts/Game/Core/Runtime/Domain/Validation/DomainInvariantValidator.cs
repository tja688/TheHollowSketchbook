using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Inventory;

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
        public IReadOnlyList<InvariantViolation> Validate(DomainActionContext domain)
        {
            List<InvariantViolation> violations = new List<InvariantViolation>();
            if (domain == null)
            {
                violations.Add(new InvariantViolation("MissingDomain", "Domain action context is null."));
                return violations;
            }

            if (domain.Grid == null)
            {
                violations.Add(new InvariantViolation("MissingGrid", "Domain action context has no grid."));
                return violations;
            }

            ValidatePlayerCard(domain.Grid, violations);
            ValidateGridCards(domain.Grid, violations);
            ValidateRoomCardMembership(domain, violations);
            return violations;
        }

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

        private static void ValidateRoomCardMembership(DomainActionContext domain, ICollection<InvariantViolation> violations)
        {
            Dictionary<CardInstanceId, string> placements = new Dictionary<CardInstanceId, string>();
            foreach (GridCell cell in domain.Grid.Cells)
            {
                IReadOnlyList<CardInstance> stack = cell.StackView;
                for (int i = 0; i < stack.Count; i++)
                {
                    AddPlacement(placements, stack[i], CardZone.Grid, "Grid " + cell.Coord, violations);
                }
            }

            ValidateDeckMembership(domain.DungeonDeck, placements, violations);
            ValidateInventoryMembership(domain.ItemInventory, placements, violations);
            ValidateTrackedCards(domain.Grid, placements, violations);
        }

        private static void ValidateDeckMembership(DungeonDeck deck, Dictionary<CardInstanceId, string> placements, ICollection<InvariantViolation> violations)
        {
            if (deck == null)
            {
                return;
            }

            for (int i = 0; i < deck.Cards.Count; i++)
            {
                AddPlacement(placements, deck.Cards[i], CardZone.DungeonDeck, "DungeonDeck[" + i + "]", violations);
            }
        }

        private static void ValidateInventoryMembership(PlayerInventory inventory, Dictionary<CardInstanceId, string> placements, ICollection<InvariantViolation> violations)
        {
            if (inventory == null)
            {
                return;
            }

            for (int i = 0; i < inventory.Items.Count; i++)
            {
                AddPlacement(placements, inventory.Items[i], CardZone.PlayerInventory, "PlayerInventory[" + i + "]", violations);
            }
        }

        private static void ValidateTrackedCards(GridState grid, Dictionary<CardInstanceId, string> placements, ICollection<InvariantViolation> violations)
        {
            foreach (CardInstance card in grid.AllKnownCards)
            {
                if (card.Zone == CardZone.Grid && !placements.ContainsKey(card.InstanceId))
                {
                    violations.Add(new InvariantViolation("GridCardNotInCell", card.InstanceId.ToString()));
                }

                if (card.Zone == CardZone.Removed)
                {
                    if (!card.IsRemoved)
                    {
                        violations.Add(new InvariantViolation("RemovedCardFlagMismatch", card.InstanceId.ToString()));
                    }

                    if (card.Coord.HasValue || card.StackIndex != -1)
                    {
                        violations.Add(new InvariantViolation("RemovedCardHasGridPosition", card.InstanceId.ToString()));
                    }
                }
                else if (card.IsRemoved)
                {
                    violations.Add(new InvariantViolation("RemovedFlagOnActiveCard", card.InstanceId.ToString()));
                }
            }
        }

        private static void AddPlacement(Dictionary<CardInstanceId, string> placements, CardInstance card, CardZone expectedZone, string location, ICollection<InvariantViolation> violations)
        {
            if (card == null)
            {
                violations.Add(new InvariantViolation("NullCardPlacement", location));
                return;
            }

            if (!placements.TryAdd(card.InstanceId, location))
            {
                violations.Add(new InvariantViolation("CrossZoneDuplicateCard", card.InstanceId + " appears in both " + placements[card.InstanceId] + " and " + location));
            }

            if (card.Zone != expectedZone)
            {
                violations.Add(new InvariantViolation("WrongZone", card.InstanceId + " in " + location + " has zone " + card.Zone + ", expected " + expectedZone));
            }
        }
    }
}
