// 整改: 2026-06-03 修复了状态双重存储问题 —— 添加 ReduceStack 公共方法，统一状态衰减入口；
// 修复 ProcessBurnDamage/ProcessBleedDamage 绕过封装直接修改内部字典的问题。
using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Combat;
using StrayPathCore.Core;
using UnityEngine;

namespace StrayPathCore.Status
{
    /// <summary>
    /// 状态效果系统 —— 统一管理所有 Buff/Debuff 的增删改查与生命周期。
    /// 作为唯一状态源，所有状态层数变更必须经过此系统。
    /// 跨模块通信通过 GameEventBus，状态读写通过 GameStateManager。
    /// </summary>
    public class StatusEffectSystem : MonoBehaviour
    {
        public static StatusEffectSystem Instance { get; private set; }

        private Dictionary<string, List<StatusEffect>> _entityEffects = new Dictionary<string, List<StatusEffect>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ==================== 核心增删改查 ====================

        public void ApplyEffect(string targetUID, StatusEffectType type, int value,
            StatusDurationType durationType, int turnValue = 0,
            string turnType = "playerturn", string sourceUID = "")
        {
            if (string.IsNullOrEmpty(targetUID) || value <= 0) return;

            if (!_entityEffects.TryGetValue(targetUID, out var list))
            {
                list = new List<StatusEffect>();
                _entityEffects[targetUID] = list;
            }

            var existing = list.Find(e => e.Type == type);
            if (existing != null)
            {
                // 同类型叠加规则
                switch (durationType)
                {
                    case StatusDurationType.Continuous:
                        existing.Value = Mathf.Max(existing.Value, value);
                        break;
                    case StatusDurationType.TurnBased:
                        existing.Value += value;
                        existing.TurnValue = Mathf.Max(existing.TurnValue, turnValue);
                        break;
                    case StatusDurationType.ChargeBased:
                        existing.Value += value;
                        break;
                    case StatusDurationType.StackBased:
                        existing.Value += value;
                        break;
                }
            }
            else
            {
                var effect = new StatusEffect(type, value, durationType, turnValue, turnType, sourceUID);
                list.Add(effect);
            }

            GameEventBus.Instance.Publish(new StatusEffectAppliedEvent
            {
                TargetUID = targetUID,
                EffectType = type,
                Value = value,
                DurationType = durationType
            });

            SyncToBattleState(targetUID, type, GetEffectValue(targetUID, type));
        }

        public void RemoveEffect(string targetUID, StatusEffectType type)
        {
            if (!_entityEffects.TryGetValue(targetUID, out var list)) return;

            var existing = list.Find(e => e.Type == type);
            if (existing != null)
            {
                list.Remove(existing);
                GameEventBus.Instance.Publish(new StatusEffectRemovedEvent
                {
                    TargetUID = targetUID,
                    EffectType = type
                });
                SyncToBattleState(targetUID, type, 0);
            }

            if (list.Count == 0)
                _entityEffects.Remove(targetUID);
        }

        public void RemoveAllEffects(string targetUID)
        {
            if (!_entityEffects.TryGetValue(targetUID, out var list)) return;

            var copy = new List<StatusEffect>(list);
            foreach (var effect in copy)
            {
                list.Remove(effect);
                GameEventBus.Instance.Publish(new StatusEffectRemovedEvent
                {
                    TargetUID = targetUID,
                    EffectType = effect.Type
                });
                SyncToBattleState(targetUID, effect.Type, 0);
            }

            _entityEffects.Remove(targetUID);
        }

        public int GetEffectValue(string targetUID, StatusEffectType type)
        {
            if (!_entityEffects.TryGetValue(targetUID, out var list)) return 0;
            var effect = list.Find(e => e.Type == type);
            return effect?.Value ?? 0;
        }

        public bool HasEffect(string targetUID, StatusEffectType type)
        {
            return GetEffectValue(targetUID, type) > 0;
        }

        public List<StatusEffect> GetAllEffects(string targetUID)
        {
            if (!_entityEffects.TryGetValue(targetUID, out var list)) return new List<StatusEffect>();
            return list.Select(e => e.Clone()).ToList();
        }

        // ==================== 层数减少（统一入口，消除双重存储） ====================

        /// <summary>
        /// 减少指定目标的 StackBased 或 TurnBased 状态层数。
        /// 自动处理归零移除、事件发布与 BattleTransientState 同步。
        /// </summary>
        public void ReduceStack(string targetUID, StatusEffectType type, int amount)
        {
            if (amount <= 0) return;
            if (!_entityEffects.TryGetValue(targetUID, out var list)) return;

            var existing = list.Find(e => e.Type == type);
            if (existing == null) return;

            existing.Value = Mathf.Max(0, existing.Value - amount);
            if (existing.Value <= 0)
            {
                list.Remove(existing);
                GameEventBus.Instance.Publish(new StatusEffectRemovedEvent
                {
                    TargetUID = targetUID,
                    EffectType = type
                });
                SyncToBattleState(targetUID, type, 0);
            }
            else
            {
                GameEventBus.Instance.Publish(new StatusEffectDecayedEvent
                {
                    TargetUID = targetUID,
                    EffectType = type,
                    RemainingValue = existing.Value
                });
                SyncToBattleState(targetUID, type, existing.Value);
            }

            if (list.Count == 0)
                _entityEffects.Remove(targetUID);
        }

        // ==================== 衰减与处理 ====================

        public void DecayTurnBasedEffects(string targetUID, string currentTurnType)
        {
            if (!_entityEffects.TryGetValue(targetUID, out var list)) return;

            var toRemove = new List<StatusEffect>();
            foreach (var effect in list)
            {
                if (effect.DurationType != StatusDurationType.TurnBased) continue;
                if (effect.TurnType != currentTurnType) continue;

                effect.Value--;
                if (effect.Value <= 0)
                {
                    toRemove.Add(effect);
                }
                else
                {
                    GameEventBus.Instance.Publish(new StatusEffectDecayedEvent
                    {
                        TargetUID = targetUID,
                        EffectType = effect.Type,
                        RemainingValue = effect.Value
                    });
                }
                SyncToBattleState(targetUID, effect.Type, effect.Value);
            }

            foreach (var effect in toRemove)
            {
                list.Remove(effect);
                GameEventBus.Instance.Publish(new StatusEffectRemovedEvent
                {
                    TargetUID = targetUID,
                    EffectType = effect.Type
                });
                SyncToBattleState(targetUID, effect.Type, 0);
            }

            if (list.Count == 0)
                _entityEffects.Remove(targetUID);
        }

        public void ConsumeCharge(string targetUID, StatusEffectType type)
        {
            if (!_entityEffects.TryGetValue(targetUID, out var list)) return;

            var effect = list.Find(e => e.Type == type && e.DurationType == StatusDurationType.ChargeBased);
            if (effect == null) return;

            effect.Value--;
            if (effect.Value <= 0)
            {
                list.Remove(effect);
                GameEventBus.Instance.Publish(new StatusEffectRemovedEvent
                {
                    TargetUID = targetUID,
                    EffectType = type
                });
                SyncToBattleState(targetUID, type, 0);
            }
            else
            {
                GameEventBus.Instance.Publish(new StatusEffectDecayedEvent
                {
                    TargetUID = targetUID,
                    EffectType = type,
                    RemainingValue = effect.Value
                });
                SyncToBattleState(targetUID, type, effect.Value);
            }

            if (list.Count == 0)
                _entityEffects.Remove(targetUID);
        }

        public void ProcessStartOfTurn(string targetUID, string turnType)
        {
            if (!_entityEffects.TryGetValue(targetUID, out var list)) return;

            // 处理持续效果标志同步
            foreach (var effect in list)
            {
                if (effect.DurationType == StatusDurationType.Continuous)
                {
                    SyncFlagToBattleState(targetUID, effect.Type, true);
                }
            }
        }

        public void ProcessEndOfTurn(string targetUID, string turnType)
        {
            // 回合结束先处理 Burn/Bleed/DemonicBrand 伤害，再衰减 TurnBased
            ProcessBurnDamage(targetUID);
            ProcessBleedDamage(targetUID);
            ProcessDemonicBrandDamage(targetUID);
            DecayTurnBasedEffects(targetUID, turnType);
        }

        public void ProcessBurnDamage(string targetUID)
        {
            int burnStacks = GetEffectValue(targetUID, StatusEffectType.Burn);
            if (burnStacks <= 0) return;

            // Burn 每层造成2点伤害
            int damage = burnStacks * 2;
            GameEventBus.Instance.Publish(new DamageTakenEvent
            {
                TargetUID = targetUID,
                Damage = damage,
                RemainingHP = 0 // 由 DamageSystem 计算后更新
            });

            // 使用公共 ReduceStack 统一入口，消除绕过封装直接修改内部字典的问题
            ReduceStack(targetUID, StatusEffectType.Burn, 1);
        }

        public void ProcessBleedDamage(string targetUID)
        {
            int bleedStacks = GetEffectValue(targetUID, StatusEffectType.Bleed);
            if (bleedStacks <= 0) return;

            // Bleed 每层造成3点伤害
            int damage = bleedStacks * 3;
            GameEventBus.Instance.Publish(new DamageTakenEvent
            {
                TargetUID = targetUID,
                Damage = damage,
                RemainingHP = 0
            });

            // 使用公共 ReduceStack 统一入口
            ReduceStack(targetUID, StatusEffectType.Bleed, 1);
        }

        public void ProcessDemonicBrandDamage(string targetUID)
        {
            int brandStacks = GetEffectValue(targetUID, StatusEffectType.DemonicBrand);
            if (brandStacks <= 0) return;

            // DemonicBrand 每层造成5点伤害
            int damage = brandStacks * 5;
            GameEventBus.Instance.Publish(new DamageTakenEvent
            {
                TargetUID = targetUID,
                Damage = damage,
                RemainingHP = 0
            });
        }

        public void ClearAllBattleEffects()
        {
            _entityEffects.Clear();
        }

        // ==================== 与 BattleTransientState 同步 ====================

        private void SyncToBattleState(string targetUID, StatusEffectType type, int value)
        {
            var bs = GameStateManager.Instance?.BattleState;
            if (bs == null) return;

            if (targetUID == "hero")
            {
                switch (type)
                {
                    case StatusEffectType.Power: bs.CurrentHeroPower = value; break;
                    case StatusEffectType.Toughness: bs.CurrentHeroToughness = value; break;
                    case StatusEffectType.Armor: bs.CurrentHeroArmor = value; break;
                    case StatusEffectType.Thorns: bs.CurrentHeroThorns = value; break;
                    case StatusEffectType.Crit: bs.CurrentHeroCrit = value; break;
                    case StatusEffectType.Haste: bs.CurrentHeroHaste = value; break;
                    case StatusEffectType.Slow: bs.CurrentHeroSlow = value; break;
                    case StatusEffectType.Bleed: bs.BleedStacks = value; break;
                    case StatusEffectType.DemonicBrand: bs.DemonicBrandStacks = value; break;
                    case StatusEffectType.EnchantedArmor: bs.EnchantedArmorStacks = value; break;
                    case StatusEffectType.Illusion: bs.IllusionStacks = value; break;
                    case StatusEffectType.StatusProtect: bs.StatusProtectStacks = value; break;
                }
            }
        }

        private void SyncFlagToBattleState(string targetUID, StatusEffectType type, bool active)
        {
            var bs = GameStateManager.Instance?.BattleState;
            if (bs == null || targetUID != "hero") return;

            switch (type)
            {
                case StatusEffectType.SensoryOverload: bs.SensoryOverload = active; break;
                case StatusEffectType.SweepingStrikes: bs.SweepingStrikes = active; break;
                case StatusEffectType.Juggernaut: bs.Juggernaut = active; break;
                case StatusEffectType.GrowingPower: bs.GrowingPower = active; break;
                case StatusEffectType.Motivation: bs.Motivation = active; break;
                case StatusEffectType.AdrenalineRush: bs.AdrenalineRush = active; break;
                case StatusEffectType.FireRadiance: bs.FireRadiance = active; break;
                case StatusEffectType.Panic: bs.Panic = active; break;
                case StatusEffectType.Berserk: bs.Berserk = active; break;
                case StatusEffectType.TemporalStasis: bs.TemporalStasisActive = active; break;
            }
        }
    }
}
