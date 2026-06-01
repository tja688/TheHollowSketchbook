using System;
using System.Collections.Generic;
using Game.Core.Powers;

namespace Game.Core.Entities
{
    public sealed class Creature
    {
        private readonly List<PowerModel> _powers = new List<PowerModel>();
        private readonly Dictionary<string, int> _state = new Dictionary<string, int>();

        public Creature(Player player, int currentHp, int maxHp)
        {
            Player = player;
            Side = CombatSide.Player;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }

        public Creature(EnemyModel enemyModel, int currentHp, int maxHp)
        {
            EnemyModel = enemyModel ?? throw new ArgumentNullException(nameof(enemyModel));
            Side = CombatSide.Enemy;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }

        public Player Player { get; }
        public EnemyModel EnemyModel { get; }
        public CombatSide Side { get; }

        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public int Block { get; private set; }

        public bool IsAlive
        {
            get { return CurrentHp > 0; }
        }

        public IReadOnlyList<PowerModel> Powers
        {
            get { return _powers; }
        }

        public event Action<int, int> HpChanged;
        public event Action<int, int> BlockChanged;
        public event Action<PowerModel> PowerApplied;
        public event Action<PowerModel> PowerRemoved;
        public event Action<Creature> Died;

        public int GetState(string key, int defaultValue = 0)
        {
            return _state.TryGetValue(key, out int value) ? value : defaultValue;
        }

        public void SetState(string key, int value)
        {
            _state[key] = value;
        }

        public void SetBlock(int value)
        {
            int oldValue = Block;
            Block = Math.Max(0, value);
            if (oldValue != Block)
            {
                BlockChanged?.Invoke(oldValue, Block);
            }
        }

        public void SetCurrentHp(int value)
        {
            int oldValue = CurrentHp;
            CurrentHp = Math.Clamp(value, 0, MaxHp);
            if (oldValue != CurrentHp)
            {
                HpChanged?.Invoke(oldValue, CurrentHp);
                if (oldValue > 0 && CurrentHp <= 0)
                {
                    _ = Game.Core.Hooks.Hook.BeforeCreatureDied(this);
                    Died?.Invoke(this);
                    _ = Game.Core.Hooks.Hook.AfterCreatureDied(this);
                }
            }
        }

        public void SetMaxHp(int value)
        {
            MaxHp = Math.Max(1, value);
            if (CurrentHp > MaxHp)
            {
                SetCurrentHp(MaxHp);
            }
        }

        public void AddPower(PowerModel power)
        {
            _powers.Add(power);
            PowerApplied?.Invoke(power);
        }

        public bool RemovePower(PowerModel power)
        {
            if (_powers.Remove(power))
            {
                PowerRemoved?.Invoke(power);
                return true;
            }

            return false;
        }
    }
}
