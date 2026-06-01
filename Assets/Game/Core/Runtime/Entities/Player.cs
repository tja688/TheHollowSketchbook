using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Models;

namespace Game.Core.Entities
{
    public sealed class Player
    {
        public Player(CharacterModel character)
        {
            Character = character ?? throw new ArgumentNullException(nameof(character));
            MaxEnergy = character.StartingMaxEnergy;
            Creature = new Creature(this, character.StartingMaxHp, character.StartingMaxHp);
            Deck = new CardPile(PileType.Deck);
            PlayerCombatState = new PlayerCombatState(this, MaxEnergy);
            PopulateStarterDeck();
        }

        public CharacterModel Character { get; }
        public Creature Creature { get; }
        public CardPile Deck { get; }
        public PlayerCombatState PlayerCombatState { get; private set; }

        public int Gold { get; private set; }
        public int MaxEnergy { get; set; }

        public void SetGold(int amount)
        {
            Gold = Math.Max(0, amount);
        }

        public void GainGold(int amount)
        {
            SetGold(Gold + amount);
        }

        public void ResetCombatState()
        {
            PlayerCombatState = new PlayerCombatState(this, MaxEnergy);
        }

        public void PopulateCombatDrawPileFromDeck()
        {
            PlayerCombatState.ClearPiles();
            IReadOnlyList<CardModel> cards = Deck.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                CardModel clone = cards[i].CloneMutable<CardModel>();
                clone.SetOwner(this);
                PlayerCombatState.DrawPile.Add(clone);
            }

            PlayerCombatState.ResetEnergy(MaxEnergy);
        }

        public void AddCardToDeck(CardModel card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            card.SetOwner(this);
            Deck.Add(card);
        }

        private void PopulateStarterDeck()
        {
            ModelId[] starterDeck = Character.StarterDeck;
            for (int i = 0; i < starterDeck.Length; i++)
            {
                CardModel card = ModelDb.CreateMutable<CardModel>(starterDeck[i]);
                AddCardToDeck(card);
            }
        }
    }
}
