using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
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

    public sealed class PlayerTraitState
    {
        public PlayerTraitState(ModelId traitId, StatModifierScope scope, string source)
        {
            TraitId = traitId;
            Scope = scope;
            Source = source ?? string.Empty;
        }

        public ModelId TraitId { get; }
        public StatModifierScope Scope { get; }
        public string Source { get; }
    }

    public sealed class PlayerRunState
    {
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        private readonly List<PlayerTraitState> _traits = new List<PlayerTraitState>();
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
        public IReadOnlyList<PlayerTraitState> Traits => _traits;
        public IReadOnlyDictionary<string, int> PermanentKeywords => _permanentKeywords;
        public IReadOnlyDictionary<string, int> RoomKeywords => _roomKeywords;
        public IEnumerable<PlayerTraitState> PermanentTraits => _traits.Where(trait => trait.Scope == StatModifierScope.Permanent);
        public IEnumerable<PlayerTraitState> RoomTraits => _traits.Where(trait => trait.Scope == StatModifierScope.Room);

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

        public void RemoveKeyword(string keyword, StatModifierScope scope)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return;
            }

            Dictionary<string, int> target = scope == StatModifierScope.Room ? _roomKeywords : _permanentKeywords;
            target.Remove(keyword);
        }

        public void AddTrait(ModelId traitId, StatModifierScope scope, string source)
        {
            if (traitId.IsEmpty)
            {
                throw new ArgumentException("Trait id cannot be empty.", nameof(traitId));
            }

            if (_traits.Any(trait => trait.Scope == scope && trait.TraitId == traitId))
            {
                return;
            }

            _traits.Add(new PlayerTraitState(traitId, scope, source));
        }

        public void RemoveTraitsBySource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            _traits.RemoveAll(trait => string.Equals(trait.Source, source, StringComparison.Ordinal));
        }

        public void RemoveTrait(ModelId traitId, StatModifierScope? scope = null)
        {
            _traits.RemoveAll(trait => trait.TraitId == traitId && (!scope.HasValue || trait.Scope == scope.Value));
        }

        public void RemoveModifiersBySource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            _modifiers.RemoveAll(modifier => string.Equals(modifier.Source, source, StringComparison.Ordinal));
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
            _traits.RemoveAll(trait => trait.Scope == StatModifierScope.Room);
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
