using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Cards;

namespace Game.Core.Domain.Progression
{
    public enum PlayerStat
    {
        MaxHp,
        Attack,
        Defense
    }

    public enum StatModifierScope
    {
        Permanent,
        Room
    }

    public sealed class StatModifier
    {
        public StatModifier(PlayerStat stat, StatModifierScope scope, int amount, string source)
        {
            Stat = stat;
            Scope = scope;
            Amount = amount;
            Source = source ?? string.Empty;
        }

        public PlayerStat Stat { get; }
        public StatModifierScope Scope { get; }
        public int Amount { get; }
        public string Source { get; }
    }

    public sealed class PlayerRunState
    {
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        private readonly Dictionary<string, int> _permanentKeywords = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _roomKeywords = new Dictionary<string, int>();

        public PlayerRunState(int baseMaxHp, int baseAttack, int baseDefense)
        {
            BaseMaxHp = Math.Max(0, baseMaxHp);
            BaseAttack = Math.Max(0, baseAttack);
            BaseDefense = Math.Max(0, baseDefense);
        }

        public int BaseMaxHp { get; }
        public int BaseAttack { get; }
        public int BaseDefense { get; }

        public int CurrentMaxHp => Math.Max(0, BaseMaxHp + Sum(PlayerStat.MaxHp));
        public int CurrentAttack => Math.Max(0, BaseAttack + Sum(PlayerStat.Attack));
        public int CurrentDefense => Math.Max(0, BaseDefense + Sum(PlayerStat.Defense));

        public IReadOnlyList<StatModifier> Modifiers => _modifiers;
        public IReadOnlyDictionary<string, int> PermanentKeywords => _permanentKeywords;
        public IReadOnlyDictionary<string, int> RoomKeywords => _roomKeywords;

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            _modifiers.Add(modifier);
        }

        public void SetKeyword(string keyword, int value, StatModifierScope scope)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                throw new ArgumentException("Keyword cannot be empty.", nameof(keyword));
            }

            Dictionary<string, int> target = scope == StatModifierScope.Room ? _roomKeywords : _permanentKeywords;
            target[keyword] = value;
        }

        public int GetKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return 0;
            }

            if (_roomKeywords.TryGetValue(keyword, out int roomValue))
            {
                return roomValue;
            }

            return _permanentKeywords.TryGetValue(keyword, out int permanentValue) ? permanentValue : 0;
        }

        public void ClearRoomState()
        {
            _modifiers.RemoveAll(modifier => modifier.Scope == StatModifierScope.Room);
            _roomKeywords.Clear();
        }

        public void ApplyTo(CardInstance playerCard)
        {
            if (playerCard == null)
            {
                throw new ArgumentNullException(nameof(playerCard));
            }

            int currentHp = playerCard.CurrentHp;
            playerCard.ConfigureCombatStats(CurrentMaxHp, CurrentAttack, CurrentDefense, 0, 0);
            playerCard.SetCurrentHp(currentHp);
        }

        private int Sum(PlayerStat stat)
        {
            return _modifiers.Where(modifier => modifier.Stat == stat).Sum(modifier => modifier.Amount);
        }
    }
}
