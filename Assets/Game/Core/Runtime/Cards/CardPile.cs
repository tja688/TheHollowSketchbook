using System;
using System.Collections.Generic;
using Game.Core.Entities;
using Game.Core.Logging;
using Game.Core.Models;

namespace Game.Core.Cards
{
    public sealed class CardPile
    {
        public const int MaxHandSize = 10;

        private readonly List<CardModel> _cards = new List<CardModel>();

        public CardPile(PileType type)
        {
            Type = type;
        }

        public PileType Type { get; }

        public IReadOnlyList<CardModel> Cards
        {
            get { return _cards; }
        }

        public int Count
        {
            get { return _cards.Count; }
        }

        public event Action ContentsChanged;
        public event Action<CardModel> CardAdded;
        public event Action<CardModel> CardRemoved;

        public bool Contains(CardModel card)
        {
            return _cards.Contains(card);
        }

        public void Add(CardModel card, int index = -1)
        {
            if (card == null)
            {
                throw new GameException("Cannot add a null card to a pile.");
            }

            if (card.CurrentPile == this)
            {
                if (index >= 0 && index < _cards.Count)
                {
                    _cards.Remove(card);
                    _cards.Insert(index, card);
                    NotifyContentsChanged();
                }

                return;
            }

            CardPile fromPile = card.CurrentPile;
            if (fromPile != null)
            {
                _ = Game.Core.Hooks.Hook.BeforeCardMovedPile(card, fromPile, this);
                fromPile.Remove(card);
            }

            if (index < 0 || index > _cards.Count)
            {
                _cards.Add(card);
            }
            else
            {
                _cards.Insert(index, card);
            }

            card.SetCurrentPile(this);
            CardAdded?.Invoke(card);
            NotifyContentsChanged();
            if (fromPile != null)
            {
                _ = Game.Core.Hooks.Hook.AfterCardMovedPile(card, fromPile, this);
            }
        }

        public bool Remove(CardModel card)
        {
            if (card == null)
            {
                return false;
            }

            if (!_cards.Remove(card))
            {
                return false;
            }

            card.SetCurrentPile(null);
            CardRemoved?.Invoke(card);
            NotifyContentsChanged();
            return true;
        }

        public void Clear()
        {
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                Remove(_cards[i]);
            }
        }

        public CardModel DrawTop()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            CardModel card = _cards[0];
            Remove(card);
            return card;
        }

        public void MoveToTop(CardModel card)
        {
            Add(card, 0);
        }

        public void MoveToBottom(CardModel card)
        {
            Add(card);
        }

        private void NotifyContentsChanged()
        {
            ContentsChanged?.Invoke();
        }
    }
}
