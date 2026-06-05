using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;

namespace Game.Core.Domain.Grid
{
    public sealed class GridCell
    {
        private readonly List<CardInstance> _cards = new List<CardInstance>();

        public GridCell(GridCoord coord)
        {
            if (!coord.IsValid)
            {
                throw new ArgumentException("Grid cell coord must be valid.", nameof(coord));
            }

            Coord = coord;
        }

        public GridCoord Coord { get; }

        public bool IsEmpty
        {
            get { return _cards.Count == 0; }
        }

        public int Count
        {
            get { return _cards.Count; }
        }

        public CardInstance TopCard
        {
            get { return _cards.Count == 0 ? null : _cards[_cards.Count - 1]; }
        }

        public IReadOnlyList<CardInstance> StackView
        {
            get { return _cards; }
        }

        internal void PushTop(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _cards.Add(card);
            RefreshStackData();
        }

        internal CardInstance PopTop()
        {
            if (_cards.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop from an empty grid cell.");
            }

            CardInstance card = _cards[_cards.Count - 1];
            _cards.RemoveAt(_cards.Count - 1);
            RefreshStackData();
            card.Coord = null;
            card.StackIndex = -1;
            return card;
        }

        internal bool Remove(CardInstance card)
        {
            int index = _cards.IndexOf(card);
            if (index < 0)
            {
                return false;
            }

            _cards.RemoveAt(index);
            RefreshStackData();
            card.Coord = null;
            card.StackIndex = -1;
            return true;
        }

        internal void InsertAt(int index, CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _cards.Insert(index, card);
            RefreshStackData();
        }

        private void RefreshStackData()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                CardInstance card = _cards[i];
                card.Zone = CardZone.Grid;
                card.Coord = Coord;
                card.StackIndex = i;
                card.IsRemoved = false;
            }
        }
    }
}
