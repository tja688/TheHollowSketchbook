using System;
using Game.Core.Models;

namespace Game.Core.Entities
{
    /// <summary>
    /// Player runtime entity.
    /// BOUNDARY: StS-specific systems removed: Deck, PlayerCombatState, MaxEnergy, card draw pile.
    /// Retained: Character config, Creature (HP/Block/Power), Gold.
    /// Extend this class to add: BaseAttack, BaseDefense, Keywords, Relics, Items for the grid-based system.
    /// </summary>
    public sealed class Player
    {
        public Player(CharacterModel character)
        {
            Character = character ?? throw new ArgumentNullException(nameof(character));
            Creature = new Creature(this, character.StartingMaxHp, character.StartingMaxHp);
        }

        public CharacterModel Character { get; }
        public Creature Creature { get; }

        public int Gold { get; private set; }

        public void SetGold(int amount)
        {
            Gold = Math.Max(0, amount);
        }

        public void GainGold(int amount)
        {
            SetGold(Gold + amount);
        }
    }
}
