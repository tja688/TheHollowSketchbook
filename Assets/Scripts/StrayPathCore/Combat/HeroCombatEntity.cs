// 整改: 2026-06-03 修复了 CombatEntity 抽象问题与状态双重存储问题
using System;
using System.Collections.Generic;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Status;
using UnityEngine;

namespace StrayPathCore.Combat
{
    /// <summary>
    /// 英雄战斗实体 —— 管理 HP、能量、格挡、状态效果与回合触发器。
    /// 所有持久化状态变更同步到 GameStateManager。
    /// 状态效果层数统一从 StatusEffectSystem 查询，消除状态双重存储。
    /// </summary>
    public class HeroCombatEntity : MonoBehaviour, ICombatEntity
    {
        public static HeroCombatEntity Instance { get; private set; }

        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentBlock { get; private set; }
        public int CurrentEnergy { get; private set; }
        public int MaxEnergy { get; private set; }

        public void SetMaxEnergy(int value)
        {
            MaxEnergy = value;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.BattleState.CurrentMaxEnergy = value;
        }
        public int Armor { get; private set; }
        public int Power { get; private set; }
        public int Toughness { get; private set; }
        public int Thorns { get; private set; }
        public bool IsDead => CurrentHP <= 0;

        // 兼容属性
        public string UID { get; private set; } = "hero";

        // ICombatEntity 兼容属性
        public int Block => CurrentBlock;

        // 状态效果层数 —— 统一从 StatusEffectSystem 查询，消除状态双重存储
        public int WeakStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.Weak) ?? 0;
        public int FragileStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.Fragile) ?? 0;
        public int BurnStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.Burn) ?? 0;
        public int ChillStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.Chill) ?? 0;
        public int BleedStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.Bleed) ?? 0;
        public int EnchantedArmorStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.EnchantedArmor) ?? 0;
        public int IllusionStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.Illusion) ?? 0;
        public int StatusProtectStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.StatusProtect) ?? 0;
        public int DemonicBrandStacks => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.DemonicBrand) ?? 0;
        public int CritCharges => StatusEffectSystem.Instance?.GetEffectValue("hero", StatusEffectType.Crit) ?? 0;

        private List<Action> _startOfTurnEffects = new List<Action>();
        private List<Action> _endOfTurnEffects = new List<Action>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(HeroData heroData, RunState runState)
        {
            if (runState == null) return;

            MaxHP = runState.MaxHP;
            CurrentHP = runState.CurrentHP;
            MaxEnergy = runState.MaxMP > 0 ? runState.MaxMP : (heroData != null ? heroData.BaseEnergy : 3);
            CurrentEnergy = MaxEnergy;
            CurrentBlock = 0;
            Armor = 0;
            Power = 0;
            Toughness = 0;
            Thorns = 0;

            // 清除所有旧状态效果，消除状态残留
            StatusEffectSystem.Instance?.RemoveAllEffects("hero");

            _startOfTurnEffects.Clear();
            _endOfTurnEffects.Clear();

            if (GameStateManager.Instance != null)
            {
                var bs = GameStateManager.Instance.BattleState;
                bs.CurrentHeroPower = 0;
                bs.CurrentHeroArmor = 0;
                bs.CurrentHeroToughness = 0;
                bs.CurrentHeroThorns = 0;
                bs.CurrentHeroHaste = 0;
                bs.CurrentHeroSlow = 0;
                bs.CurrentBlock = 0;
                bs.CurrentEnergy = MaxEnergy;
                bs.CurrentMaxEnergy = MaxEnergy;
                bs.BleedStacks = 0;
                bs.EnchantedArmorStacks = 0;
                bs.IllusionStacks = 0;
                bs.StatusProtectStacks = 0;
                bs.DemonicBrandStacks = 0;
                bs.CurrentHeroCrit = 0;
            }
        }

        public void TakeDamage(int baseDamage, EnemyCombatEntity source, List<float> multipliers = null)
        {
            if (IsDead || baseDamage <= 0) return;

            int damage = baseDamage;
            if (source != null)
                damage += source.Power;

            float mult = 1.0f;
            if (multipliers != null)
            {
                foreach (var m in multipliers)
                    mult *= m;
            }

            if (WeakStacks > 0) mult *= 0.7f;
            if (FragileStacks > 0) mult *= 1.3f;

            damage = Mathf.RoundToInt(damage * mult);
            damage = DamageCalculator.ApplyArmor(damage, Armor);

            int block = CurrentBlock;
            int remaining = DamageCalculator.ApplyBlock(damage, ref block, false);
            if (block != CurrentBlock)
            {
                int consumed = CurrentBlock - block;
                CurrentBlock = block;
                GameEventBus.Instance.Publish(new BlockConsumedEvent
                {
                    TargetUID = "hero",
                    Amount = consumed,
                    RemainingBlock = CurrentBlock
                });
            }

            if (remaining > 0 && EnchantedArmorStacks > 0)
            {
                bool canAbsorb = (CurrentBlock == 0 && damage <= 2) || remaining <= 3;
                if (canAbsorb)
                {
                    StatusEffectSystem.Instance?.ReduceStack("hero", StatusEffectType.EnchantedArmor, 1);
                    remaining = 0;
                    GameEventBus.Instance.Publish(new StatusEffectDecayedEvent
                    {
                        TargetUID = "hero",
                        EffectType = StatusEffectType.EnchantedArmor,
                        RemainingValue = EnchantedArmorStacks
                    });
                }
            }

            if (remaining > 0)
            {
                int oldHP = CurrentHP;
                CurrentHP = Mathf.Max(0, CurrentHP - remaining);
                GameStateManager.Instance?.SetHP(CurrentHP);
                GameEventBus.Instance.Publish(new DamageTakenEvent
                {
                    TargetUID = "hero",
                    Damage = remaining,
                    RemainingHP = CurrentHP
                });
                ApplyOnHitSideEffects(source);
            }

            CheckDeath();
        }

        /// <summary>
        /// ICombatEntity 接口实现 —— 将通用调用转发到具体签名的 TakeDamage。
        /// </summary>
        void ICombatEntity.TakeDamage(int damage, object source)
        {
            TakeDamage(damage, source as EnemyCombatEntity);
        }

        private void ApplyOnHitSideEffects(EnemyCombatEntity source)
        {
            if (source == null) return;
            if (source.HasTrait(EnemyTraitType.Lacerate))
            {
                ApplyBleedStacks(1);
            }
            // VenomousBite、FieryMark、CheapShot 等副作用通过事件由 Deck/Status 系统处理
            GameEventBus.Instance.Publish(new DamageDealtEvent
            {
                SourceUID = source.UniqueID,
                TargetUID = "hero",
                BaseDamage = 0,
                FinalDamage = 0,
                IsBlocked = false
            });
        }

        public void GainBlock(int amount, bool isUpdate = false)
        {
            if (amount <= 0) return;
            CurrentBlock += amount;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.BattleState.CurrentBlock = CurrentBlock;
            GameEventBus.Instance.Publish(new BlockGainedEvent
            {
                TargetUID = "hero",
                Amount = amount,
                TotalBlock = CurrentBlock
            });
        }

        /// <summary>
        /// ICombatEntity 接口实现。
        /// </summary>
        void ICombatEntity.GainBlock(int amount)
        {
            GainBlock(amount);
        }

        public void ResetBlock()
        {
            if (CurrentBlock > 0)
            {
                int old = CurrentBlock;
                CurrentBlock = 0;
                if (GameStateManager.Instance != null)
                    GameStateManager.Instance.BattleState.CurrentBlock = 0;
                GameEventBus.Instance.Publish(new BlockConsumedEvent
                {
                    TargetUID = "hero",
                    Amount = old,
                    RemainingBlock = 0
                });
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;
            int oldHP = CurrentHP;
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
            int healed = CurrentHP - oldHP;
            if (healed > 0)
            {
                GameStateManager.Instance?.SetHP(CurrentHP);
                GameEventBus.Instance.Publish(new HealEvent
                {
                    TargetUID = "hero",
                    Amount = healed,
                    CurrentHP = CurrentHP
                });
            }
        }

        public void ConsumeEnergy(int amount)
        {
            if (amount <= 0) return;
            int old = CurrentEnergy;
            CurrentEnergy = Mathf.Max(0, CurrentEnergy - amount);
            SyncEnergyToState();
            GameEventBus.Instance.Publish(new EnergyChangedEvent
            {
                OldValue = old,
                NewValue = CurrentEnergy,
                Reason = "consume"
            });
        }

        public void GainEnergy(int amount)
        {
            if (amount <= 0) return;
            int old = CurrentEnergy;
            CurrentEnergy = Mathf.Min(CurrentEnergy + amount, MaxEnergy);
            SyncEnergyToState();
            GameEventBus.Instance.Publish(new EnergyChangedEvent
            {
                OldValue = old,
                NewValue = CurrentEnergy,
                Reason = "gain"
            });
        }

        public void ResetEnergy()
        {
            int old = CurrentEnergy;
            CurrentEnergy = MaxEnergy;
            SyncEnergyToState();
            GameEventBus.Instance.Publish(new EnergyChangedEvent
            {
                OldValue = old,
                NewValue = CurrentEnergy,
                Reason = "reset"
            });
        }

        private void SyncEnergyToState()
        {
            if (GameStateManager.Instance == null) return;
            GameStateManager.Instance.BattleState.CurrentEnergy = CurrentEnergy;
            GameStateManager.Instance.BattleState.CurrentMaxEnergy = MaxEnergy;
            GameStateManager.Instance.CurrentRun.CurrentMP = CurrentEnergy;
            GameStateManager.Instance.CurrentRun.MaxMP = MaxEnergy;
        }

        public void AddStartOfTurnEffect(Action effect)
        {
            if (effect != null) _startOfTurnEffects.Add(effect);
        }

        public void ExecuteStartOfTurnEffects()
        {
            var effects = new List<Action>(_startOfTurnEffects);
            foreach (var e in effects)
                e?.Invoke();
        }

        public void AddEndOfTurnEffect(Action effect)
        {
            if (effect != null) _endOfTurnEffects.Add(effect);
        }

        public void ExecuteEndOfTurnEffects()
        {
            var effects = new List<Action>(_endOfTurnEffects);
            foreach (var e in effects)
                e?.Invoke();
        }

        public void ApplyBleedStacks(int stacks)
        {
            if (stacks <= 0) return;
            StatusEffectSystem.Instance?.ApplyEffect("hero", StatusEffectType.Bleed, stacks, StatusDurationType.StackBased);
        }

        public void DecayBleed()
        {
            int stacks = BleedStacks;
            if (stacks > 0)
            {
                int damage = stacks;
                // 委托 StatusEffectSystem 减少层数并同步 BattleState（内部自动发布 Decayed/Removed 事件）
                StatusEffectSystem.Instance?.ReduceStack("hero", StatusEffectType.Bleed, 1);
                if (damage > 0)
                {
                    int oldHP = CurrentHP;
                    CurrentHP = Mathf.Max(0, CurrentHP - damage);
                    GameStateManager.Instance?.SetHP(CurrentHP);
                    GameEventBus.Instance.Publish(new DamageTakenEvent
                    {
                        TargetUID = "hero",
                        Damage = damage,
                        RemainingHP = CurrentHP
                    });
                    CheckDeath();
                }
            }
        }

        public void CheckDeath()
        {
            if (CurrentHP <= 0)
            {
                CurrentHP = 0;
                GameStateManager.Instance?.SetHP(0);
                GameEventBus.Instance.Publish(new HPChangedEvent
                {
                    OldHP = 0,
                    NewHP = 0,
                    MaxHP = MaxHP
                });
            }
        }

        /// <summary>
        /// 复活/急救 —— 绕过死亡检查直接设置HP（用于遗物如 Luma's Grace 的致命伤害保护）。
        /// </summary>
        public void Revive(int hp)
        {
            CurrentHP = Mathf.Clamp(hp, 1, MaxHP);
            GameStateManager.Instance?.SetHP(CurrentHP);
            GameEventBus.Instance.Publish(new HPChangedEvent
            {
                OldHP = 0,
                NewHP = CurrentHP,
                MaxHP = MaxHP
            });
        }

        public bool HasStatusEffect(StatusEffectType type)
        {
            return StatusEffectSystem.Instance?.HasEffect("hero", type) ?? false;
        }

        public void ApplyStatusEffect(StatusEffectType type, int value)
        {
            if (value == 0) return;
            if (value > 0)
            {
                StatusEffectSystem.Instance?.ApplyEffect("hero", type, value, StatusDurationType.StackBased);
            }
            else
            {
                StatusEffectSystem.Instance?.ReduceStack("hero", type, -value);
            }
        }
    }
}
