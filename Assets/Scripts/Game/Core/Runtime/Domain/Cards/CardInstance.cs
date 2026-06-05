using System;
using System.Collections.Generic;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Cards
{
    public sealed class CardInstance
    {
        private readonly Dictionary<string, int> _runtimeState = new Dictionary<string, int>();

        public CardInstance(CardInstanceId instanceId, ModelId modelId, CardType cardType)
        {
            if (instanceId.IsEmpty)
            {
                throw new ArgumentException("Card instance id cannot be empty.", nameof(instanceId));
            }

            InstanceId = instanceId;
            ModelId = modelId;
            CardType = cardType;
            Zone = CardZone.None;
            StackIndex = -1;
            GoldOnRemoved = cardType == CardType.Monster ? 10 : 0;
            GoldValue = cardType == CardType.Gold ? 20 : 0;
        }

        public CardInstanceId InstanceId { get; }
        public ModelId ModelId { get; }
        public CardType CardType { get; }

        public CardZone Zone { get; internal set; }
        public GridCoord? Coord { get; internal set; }
        public int StackIndex { get; internal set; }
        public bool IsFaceUp { get; internal set; }
        public bool IsRemoved { get; internal set; }

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }
        public int ContactDamageToPlayer { get; private set; }
        public int GoldOnRemoved { get; private set; }
        public int GoldValue { get; private set; }

        public IReadOnlyDictionary<string, int> RuntimeState
        {
            get { return _runtimeState; }
        }

        public bool HasHitPoints
        {
            get { return MaxHp > 0; }
        }

        public bool IsAlive
        {
            get { return !HasHitPoints || CurrentHp > 0; }
        }

        public void ConfigureCombatStats(int maxHp, int attack, int defense, int contactDamageToPlayer = 0, int goldOnRemoved = 10)
        {
            MaxHp = Math.Max(0, maxHp);
            CurrentHp = MaxHp;
            Attack = Math.Max(0, attack);
            Defense = Math.Max(0, defense);
            ContactDamageToPlayer = Math.Max(0, contactDamageToPlayer);
            GoldOnRemoved = Math.Max(0, goldOnRemoved);
        }

        public void ConfigureGoldValue(int goldValue)
        {
            GoldValue = Math.Max(0, goldValue);
        }

        public void SetAttack(int value)
        {
            Attack = Math.Max(0, value);
        }

        public void SetDefense(int value)
        {
            Defense = Math.Max(0, value);
        }

        public void SetCurrentHp(int value)
        {
            if (MaxHp <= 0)
            {
                MaxHp = Math.Max(0, value);
            }

            CurrentHp = Math.Max(0, Math.Min(MaxHp, value));
        }

        public int GetState(string key, int defaultValue = 0)
        {
            return _runtimeState.TryGetValue(key, out int value) ? value : defaultValue;
        }

        public void SetState(string key, int value)
        {
            _runtimeState[key] = value;
        }

        public bool RemoveState(string key)
        {
            return _runtimeState.Remove(key);
        }

        internal int ApplyHpLoss(int amount)
        {
            int hpLoss = Math.Max(0, amount);
            int oldHp = CurrentHp;
            SetCurrentHp(CurrentHp - hpLoss);
            return oldHp - CurrentHp;
        }
    }
}
