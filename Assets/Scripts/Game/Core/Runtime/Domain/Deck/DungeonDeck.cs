using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Random;

namespace Game.Core.Domain.Deck
{
    public sealed class DungeonDeck
    {
        private readonly List<CardInstance> _cards = new List<CardInstance>();
        private readonly IReadOnlyList<CardInstance> _cardView;

        public DungeonDeck()
        {
            _cardView = _cards.AsReadOnly();
        }

        public IReadOnlyList<CardInstance> Cards
        {
            get { return _cardView; }
        }

        public int Count
        {
            get { return _cards.Count; }
        }

        public void AddToTop(CardInstance card)
        {
            AddAt(_cards.Count, card);
        }

        public void AddToBottom(CardInstance card)
        {
            AddAt(0, card);
        }

        public void AddRange(IEnumerable<CardInstance> cards)
        {
            foreach (CardInstance card in cards)
            {
                AddToTop(card);
            }
        }

        public CardInstance DrawTop()
        {
            if (_cards.Count == 0)
            {
                throw new InvalidOperationException("Cannot draw from an empty dungeon deck.");
            }

            CardInstance card = _cards[_cards.Count - 1];
            _cards.RemoveAt(_cards.Count - 1);
            card.Zone = CardZone.None;
            return card;
        }

        public void Shuffle(IRng rng)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            rng.Shuffle(_cards);
        }

        public IReadOnlyList<CardInstance> RemoveAll(Predicate<CardInstance> predicate)
        {
            List<CardInstance> removed = new List<CardInstance>();
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                CardInstance card = _cards[i];
                if (predicate(card))
                {
                    _cards.RemoveAt(i);
                    card.Zone = CardZone.None;
                    removed.Add(card);
                }
            }

            removed.Reverse();
            return removed;
        }

        internal void AddToTopFromGrid(CardInstance card)
        {
            AddAt(_cards.Count, card);
        }

        private void AddAt(int index, CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            card.Zone = CardZone.DungeonDeck;
            card.Coord = null;
            card.StackIndex = -1;
            card.IsRemoved = false;
            _cards.Insert(index, card);
        }
    }
}
