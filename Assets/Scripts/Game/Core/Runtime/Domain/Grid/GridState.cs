using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Events;
using Game.Core.Random;

namespace Game.Core.Domain.Grid
{
    public sealed class GridState
    {
        private readonly GridCell[] _cells = new GridCell[9];
        private readonly Dictionary<CardInstanceId, CardInstance> _cardsById = new Dictionary<CardInstanceId, CardInstance>();

        public GridState()
        {
            IReadOnlyList<GridCoord> coords = GridQueries.AllCoordsRowMajor();
            for (int i = 0; i < coords.Count; i++)
            {
                _cells[i] = new GridCell(coords[i]);
            }
        }

        public IReadOnlyList<GridCell> Cells
        {
            get { return _cells; }
        }

        public CardInstance PlayerCard
        {
            get
            {
                foreach (CardInstance card in _cardsById.Values)
                {
                    if (card.CardType == CardType.Player && card.Zone == CardZone.Grid && !card.IsRemoved)
                    {
                        return card;
                    }
                }

                return null;
            }
        }

        public IEnumerable<CardInstance> AllKnownCards
        {
            get { return _cardsById.Values; }
        }

        public IEnumerable<CardInstance> AllGridCards
        {
            get
            {
                foreach (GridCell cell in _cells)
                {
                    IReadOnlyList<CardInstance> stack = cell.StackView;
                    for (int i = 0; i < stack.Count; i++)
                    {
                        yield return stack[i];
                    }
                }
            }
        }

        public IEnumerable<CardInstance> FaceUpCards
        {
            get
            {
                foreach (CardInstance card in AllGridCards)
                {
                    if (card.IsFaceUp)
                    {
                        yield return card;
                    }
                }
            }
        }

        public IEnumerable<CardInstance> MonsterCards
        {
            get
            {
                foreach (CardInstance card in AllGridCards)
                {
                    if (card.CardType == CardType.Monster)
                    {
                        yield return card;
                    }
                }
            }
        }

        public GridCell GetCell(GridCoord coord)
        {
            ValidateCoord(coord);
            return _cells[coord.Row * 3 + coord.Col];
        }

        public bool TryGetCard(CardInstanceId id, out CardInstance card)
        {
            return _cardsById.TryGetValue(id, out card);
        }

        public CardInstance GetCard(CardInstanceId id)
        {
            if (!_cardsById.TryGetValue(id, out CardInstance card))
            {
                throw new KeyNotFoundException(id.ToString());
            }

            return card;
        }

        public bool IsEmpty(GridCoord coord)
        {
            return GetCell(coord).IsEmpty;
        }

        public CardInstance GetTopCard(GridCoord coord)
        {
            return GetCell(coord).TopCard;
        }

        public IReadOnlyList<CardInstance> GetStack(GridCoord coord)
        {
            return GetCell(coord).StackView;
        }

        public GridOperationResult AddCardToGrid(CardInstance card, GridCoord coord, bool faceUp)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            GridCell cell = GetCell(coord);
            if (card.Zone == CardZone.Grid && card.Coord.HasValue)
            {
                RemoveFromCurrentCell(card);
            }

            Register(card);
            card.IsFaceUp = faceUp;
            cell.PushTop(card);
            return GridOperationResult.Success(new[]
            {
                new DomainEvent(DomainEventType.CardAddedToGrid)
                {
                    CardId = card.InstanceId,
                    ToCoord = coord,
                    Reason = faceUp ? "FaceUp" : "FaceDown"
                }
            });
        }

        public GridOperationResult MoveCardToEmptyCell(CardInstance card, GridCoord to)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!to.IsValid)
            {
                return GridOperationResult.Failure("InvalidCoord");
            }

            if (!IsEmpty(to))
            {
                return GridOperationResult.Failure("TargetCellNotEmpty");
            }

            if (card.Zone != CardZone.Grid || !card.Coord.HasValue)
            {
                return GridOperationResult.Failure("CardNotOnGrid");
            }

            GridCoord from = card.Coord.Value;
            if (GetCell(from).TopCard != card)
            {
                return GridOperationResult.Failure("OnlyTopCardCanMove");
            }

            GetCell(from).PopTop();
            GetCell(to).PushTop(card);
            return GridOperationResult.Success(new[]
            {
                new DomainEvent(DomainEventType.CardMoved)
                {
                    CardId = card.InstanceId,
                    FromCoord = from,
                    ToCoord = to,
                    Reason = MoveReason.PlayerMove.ToString()
                }
            });
        }

        public GridOperationResult MoveTopCardToTop(CardInstance card, GridCoord to)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!to.IsValid)
            {
                return GridOperationResult.Failure("InvalidCoord");
            }

            if (card.Zone != CardZone.Grid || !card.Coord.HasValue)
            {
                return GridOperationResult.Failure("CardNotOnGrid");
            }

            GridCoord from = card.Coord.Value;
            if (GetCell(from).TopCard != card)
            {
                return GridOperationResult.Failure("OnlyTopCardCanMove");
            }

            GetCell(from).PopTop();
            GetCell(to).PushTop(card);
            return GridOperationResult.Success(new[]
            {
                new DomainEvent(DomainEventType.CardMoved)
                {
                    CardId = card.InstanceId,
                    FromCoord = from,
                    ToCoord = to,
                    Reason = MoveReason.Scripted.ToString()
                }
            });
        }

        public GridOperationResult SwapTopCards(CardInstance a, CardInstance b)
        {
            if (a == null || b == null)
            {
                throw new ArgumentNullException(a == null ? nameof(a) : nameof(b));
            }

            if (a.Zone != CardZone.Grid || b.Zone != CardZone.Grid || !a.Coord.HasValue || !b.Coord.HasValue)
            {
                return GridOperationResult.Failure("CardNotOnGrid");
            }

            GridCoord aCoord = a.Coord.Value;
            GridCoord bCoord = b.Coord.Value;
            GridCell aCell = GetCell(aCoord);
            GridCell bCell = GetCell(bCoord);
            if (aCell.TopCard != a || bCell.TopCard != b)
            {
                return GridOperationResult.Failure("OnlyTopCardCanSwap");
            }

            aCell.PopTop();
            bCell.PopTop();
            aCell.PushTop(b);
            bCell.PushTop(a);
            return GridOperationResult.Success(new[]
            {
                new DomainEvent(DomainEventType.CardMoved) { CardId = a.InstanceId, FromCoord = aCoord, ToCoord = bCoord, Reason = "Swap" },
                new DomainEvent(DomainEventType.CardMoved) { CardId = b.InstanceId, FromCoord = bCoord, ToCoord = aCoord, Reason = "Swap" }
            });
        }

        public GridOperationResult CoverCellWithCard(CardInstance card, GridCoord coord, bool faceUp)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!coord.IsValid)
            {
                return GridOperationResult.Failure("InvalidCoord");
            }

            CardInstance top = GetTopCard(coord);
            if (top != null)
            {
                top.IsFaceUp = false;
            }

            if (card.Zone == CardZone.Grid && card.Coord.HasValue)
            {
                RemoveFromCurrentCell(card);
            }

            Register(card);
            card.IsFaceUp = faceUp;
            GetCell(coord).PushTop(card);
            return GridOperationResult.Success(new[]
            {
                new DomainEvent(DomainEventType.CardCovered)
                {
                    CardId = card.InstanceId,
                    TargetCardId = top != null ? top.InstanceId : default,
                    ToCoord = coord,
                    Reason = faceUp ? "FaceUp" : "FaceDown"
                }
            });
        }

        public GridOperationResult RemoveCard(CardInstance card, RemoveReason reason)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (card.Zone != CardZone.Grid || !card.Coord.HasValue)
            {
                return GridOperationResult.Failure("CardNotOnGrid");
            }

            GridCoord from = card.Coord.Value;
            GridCell cell = GetCell(from);
            if (!cell.Remove(card))
            {
                return GridOperationResult.Failure("CardNotInCell");
            }

            card.Zone = CardZone.Removed;
            card.IsRemoved = true;
            card.IsFaceUp = false;
            List<DomainEvent> events = new List<DomainEvent>
            {
                new DomainEvent(DomainEventType.CardRemoved)
                {
                    CardId = card.InstanceId,
                    FromCoord = from,
                    Reason = reason.ToString()
                }
            };

            CardInstance newTop = cell.TopCard;
            if (newTop != null && !newTop.IsFaceUp)
            {
                newTop.IsFaceUp = true;
                events.Add(new DomainEvent(DomainEventType.CardFlipped)
                {
                    CardId = newTop.InstanceId,
                    ToCoord = from,
                    Reason = FlipReason.RevealAfterTopRemoved.ToString()
                });
            }

            return GridOperationResult.Success(events);
        }

        public GridOperationResult FlipTopCard(GridCoord coord, FlipReason reason)
        {
            CardInstance topCard = GetTopCard(coord);
            if (topCard == null)
            {
                return GridOperationResult.Failure("CellEmpty");
            }

            return FlipCard(topCard, reason);
        }

        public GridOperationResult FlipCard(CardInstance card, FlipReason reason)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (card.Zone != CardZone.Grid || !card.Coord.HasValue)
            {
                return GridOperationResult.Failure("CardNotOnGrid");
            }

            if (GetCell(card.Coord.Value).TopCard != card)
            {
                return GridOperationResult.Failure("OnlyTopCardCanFlip");
            }

            if (card.IsFaceUp)
            {
                return GridOperationResult.Success(Array.Empty<DomainEvent>());
            }

            card.IsFaceUp = true;
            return GridOperationResult.Success(new[]
            {
                new DomainEvent(DomainEventType.CardFlipped)
                {
                    CardId = card.InstanceId,
                    ToCoord = card.Coord.Value,
                    Reason = reason.ToString()
                }
            });
        }

        public GridOperationResult RevealAround(GridCoord center, FlipReason reason)
        {
            if (!center.IsValid)
            {
                return GridOperationResult.Failure("InvalidCoord");
            }

            List<DomainEvent> events = new List<DomainEvent>();
            IReadOnlyList<GridCoord> neighbors = GridQueries.OrthogonalNeighbors(center);
            for (int i = 0; i < neighbors.Count; i++)
            {
                CardInstance topCard = GetTopCard(neighbors[i]);
                if (topCard != null && !topCard.IsFaceUp)
                {
                    topCard.IsFaceUp = true;
                    events.Add(new DomainEvent(DomainEventType.CardFlipped)
                    {
                        CardId = topCard.InstanceId,
                        ToCoord = neighbors[i],
                        Reason = reason.ToString()
                    });
                }
            }

            return GridOperationResult.Success(events);
        }

        public GridOperationResult ShuffleNonPlayerGridCardsIntoDeck(DungeonDeck deck, IRng rng)
        {
            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            List<DomainEvent> events = new List<DomainEvent>();
            List<CardInstance> moved = new List<CardInstance>();
            foreach (GridCell cell in _cells)
            {
                for (int i = cell.StackView.Count - 1; i >= 0; i--)
                {
                    CardInstance card = cell.StackView[i];
                    if (card.CardType == CardType.Player)
                    {
                        continue;
                    }

                    cell.Remove(card);
                    card.IsFaceUp = false;
                    deck.AddToTopFromGrid(card);
                    moved.Add(card);
                    events.Add(new DomainEvent(DomainEventType.CardZoneChanged)
                    {
                        CardId = card.InstanceId,
                        FromCoord = cell.Coord,
                        Reason = CardZone.DungeonDeck.ToString()
                    });
                }
            }

            if (rng != null)
            {
                deck.Shuffle(rng);
            }

            return GridOperationResult.Success(events);
        }

        public GridOperationResult RedistributeDeck(DungeonDeck deck, GridCoord excludedCoord, IRng rng)
        {
            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            List<GridCoord> targets = new List<GridCoord>();
            IReadOnlyList<GridCoord> coords = GridQueries.AllCoordsRowMajor();
            for (int i = 0; i < coords.Count; i++)
            {
                if (coords[i] != excludedCoord)
                {
                    targets.Add(coords[i]);
                }
            }

            rng?.Shuffle(targets);
            List<DomainEvent> events = new List<DomainEvent>();
            int targetIndex = 0;
            while (deck.Count > 0)
            {
                CardInstance card = deck.DrawTop();
                GridCoord coord = targets[targetIndex % targets.Count];
                targetIndex++;
                card.IsFaceUp = false;
                GetCell(coord).PushTop(card);
                events.Add(new DomainEvent(DomainEventType.CardAddedToGrid)
                {
                    CardId = card.InstanceId,
                    ToCoord = coord,
                    Reason = "RedistributeDeck"
                });
            }

            return GridOperationResult.Success(events);
        }

        private void Register(CardInstance card)
        {
            if (!_cardsById.ContainsKey(card.InstanceId))
            {
                _cardsById.Add(card.InstanceId, card);
            }
        }

        private void RemoveFromCurrentCell(CardInstance card)
        {
            if (!card.Coord.HasValue)
            {
                return;
            }

            GetCell(card.Coord.Value).Remove(card);
        }

        private static void ValidateCoord(GridCoord coord)
        {
            if (!coord.IsValid)
            {
                throw new ArgumentOutOfRangeException(nameof(coord));
            }
        }
    }
}
