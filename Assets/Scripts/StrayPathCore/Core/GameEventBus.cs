// 整改: 2026-06-03 修复了 GameEventBus 内存泄漏风险 —— 实现 WeakReference 订阅模式
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrayPathCore.Core
{
    /// <summary>
    /// 全局事件总线 —— 替代 Godot 的 signal/connect/emit 系统。
    /// 所有跨模块通信均通过此总线，彻底解耦系统间的直接引用。
    /// 使用 ScriptableObject 单例，支持编辑器调试与跨场景持久。
    /// 提供强引用 Subscribe（默认，性能最优）与弱引用 SubscribeWeak（推荐 MonoBehaviour 订阅，防内存泄漏）两种模式。
    /// </summary>
    [CreateAssetMenu(fileName = "GameEventBus", menuName = "StrayPath/Core/GameEventBus")]
    public class GameEventBus : ScriptableObject
    {
        private static GameEventBus _instance;
        public static GameEventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameEventBus>("StrayPath/Core/GameEventBus");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GameEventBus>();
                        Debug.LogWarning("[GameEventBus] 未在 Resources 中找到预设，创建了临时实例。请在 Resources/StrayPath/Core/ 下创建 GameEventBus.asset");
                    }
                }
                return _instance;
            }
        }

        // 核心事件字典：Type -> Delegate（强引用，默认模式）
        private readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();
        // 弱引用字典：Type -> List&lt;WeakReference&gt;（推荐 MonoBehaviour 使用）
        private readonly Dictionary<Type, List<WeakReference>> _weakSubscribers = new Dictionary<Type, List<WeakReference>>();
        private readonly object _lock = new object();

        // ==================== 订阅 / 取消订阅（强引用，默认） ====================

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            lock (_lock)
            {
                Type type = typeof(T);
                if (_subscribers.TryGetValue(type, out var existing))
                {
                    _subscribers[type] = Delegate.Combine(existing, handler);
                }
                else
                {
                    _subscribers[type] = handler;
                }
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            lock (_lock)
            {
                Type type = typeof(T);
                if (_subscribers.TryGetValue(type, out var existing))
                {
                    var updated = Delegate.Remove(existing, handler);
                    if (updated == null)
                        _subscribers.Remove(type);
                    else
                        _subscribers[type] = updated;
                }
            }
        }

        // ==================== 弱引用订阅（推荐 MonoBehaviour 使用） ====================

        /// <summary>
        /// 弱引用订阅 —— 即使忘记取消订阅，MonoBehaviour 被销毁后委托也会被 GC 回收，防止内存泄漏。
        /// 在 Publish 时自动清理已失效的引用。
        /// </summary>
        public void SubscribeWeak<T>(Action<T> handler) where T : struct
        {
            lock (_lock)
            {
                Type type = typeof(T);
                if (!_weakSubscribers.TryGetValue(type, out var list))
                {
                    list = new List<WeakReference>();
                    _weakSubscribers[type] = list;
                }
                list.Add(new WeakReference(handler));
            }
        }

        /// <summary>
        /// 手动清理指定事件类型下所有已失效的弱引用订阅者。
        /// </summary>
        public void PruneDeadSubscribers<T>() where T : struct
        {
            lock (_lock)
            {
                Type type = typeof(T);
                if (_weakSubscribers.TryGetValue(type, out var list))
                {
                    list.RemoveAll(w => !w.IsAlive || w.Target == null);
                }
            }
        }

        // ==================== 发布事件 ====================

        public void Publish<T>(T eventData) where T : struct
        {
            lock (_lock)
            {
                Type type = typeof(T);

                // 强引用订阅者
                if (_subscribers.TryGetValue(type, out var del))
                {
                    var handlers = del.GetInvocationList();
                    foreach (var h in handlers)
                    {
                        try
                        {
                            ((Action<T>)h)?.Invoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[GameEventBus] 事件 {type.Name} 处理异常: {ex}");
                        }
                    }
                }

                // 弱引用订阅者（自动清理已 GC 的引用）
                if (_weakSubscribers.TryGetValue(type, out var weakList))
                {
                    var deadRefs = new List<WeakReference>();
                    foreach (var weakRef in weakList)
                    {
                        if (!weakRef.IsAlive || weakRef.Target == null)
                        {
                            deadRefs.Add(weakRef);
                            continue;
                        }
                        try
                        {
                            var handler = weakRef.Target as Action<T>;
                            handler?.Invoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[GameEventBus] 弱引用事件 {type.Name} 处理异常: {ex}");
                        }
                    }
                    foreach (var dead in deadRefs)
                        weakList.Remove(dead);
                }
            }
        }

        // ==================== 一次性订阅 ====================

        public void SubscribeOnce<T>(Action<T> handler) where T : struct
        {
            Action<T> wrapper = null;
            wrapper = evt =>
            {
                Unsubscribe(wrapper);
                handler(evt);
            };
            Subscribe(wrapper);
        }

        // ==================== 清理 ====================

        public void ClearAllSubscriptions()
        {
            lock (_lock)
            {
                _subscribers.Clear();
                _weakSubscribers.Clear();
            }
        }

        public void Clear<T>() where T : struct
        {
            lock (_lock)
            {
                _subscribers.Remove(typeof(T));
                _weakSubscribers.Remove(typeof(T));
            }
        }

        private void OnEnable()
        {
            _instance = this;
        }
    }

    // ==================== 常用战斗事件定义 ====================

    public struct BattleStartedEvent { public int ActID; public int BattleType; }
    public struct BattleEndedEvent { public bool PlayerVictory; public int RewardGold; }
    public struct PlayerTurnStartedEvent { public int TurnNumber; }
    public struct PlayerTurnEndedEvent { public int TurnNumber; }
    public struct EnemyTurnStartedEvent { public int TurnNumber; }
    public struct EnemyTurnEndedEvent { public int TurnNumber; }
    public struct CardPlayedEvent { public int CardID; public int CopyCount; public int EnergyCost; public bool IsUpgraded; public string TargetEnemyUID; }
    public struct CardDrawnEvent { public int CardID; public int CopyCount; public string SourcePile; }
    public struct CardDiscardedEvent { public int CardID; public int CopyCount; public string TargetPile; }
    public struct DeckShuffledEvent { public string SourcePile; }
    public struct EnergyChangedEvent { public int OldValue; public int NewValue; public string Reason; }
    public struct DamageDealtEvent { public string SourceUID; public string TargetUID; public int BaseDamage; public int FinalDamage; public bool IsBlocked; }
    public struct DamageTakenEvent { public string TargetUID; public int Damage; public int RemainingHP; }
    public struct BlockGainedEvent { public string TargetUID; public int Amount; public int TotalBlock; }
    public struct BlockConsumedEvent { public string TargetUID; public int Amount; public int RemainingBlock; }
    public struct HealEvent { public string TargetUID; public int Amount; public int CurrentHP; }
    public struct EnemyDiedEvent { public string EnemyUID; public int EnemyID; }
    public struct EnemyIntentDisplayedEvent { public string EnemyUID; public int AbilityIndex; public int PreviewDamage; }
    public struct BoostActivatedEvent { }
    public struct BoostDeactivatedEvent { }
    public struct SpellCastEvent { public int SpellID; public string TargetEnemyUID; }
    public struct RelicTriggeredEvent { public int RelicID; public RelicTriggerTiming Timing; }
    public struct StatusEffectAppliedEvent { public string TargetUID; public StatusEffectType EffectType; public int Value; public StatusDurationType DurationType; }
    public struct StatusEffectRemovedEvent { public string TargetUID; public StatusEffectType EffectType; }
    public struct StatusEffectDecayedEvent { public string TargetUID; public StatusEffectType EffectType; public int RemainingValue; }
    public struct GoldChangedEvent { public int OldAmount; public int NewAmount; public string Reason; }
    public struct HPChangedEvent { public int OldHP; public int NewHP; public int MaxHP; }
    public struct NodeEnteredEvent { public MapNodeType NodeType; public int NodeID; public int PathGroup; }
    public struct ActCompletedEvent { public int CompletedAct; }
    public struct RunCompletedEvent { public bool Victory; public int FinalScore; }

    public enum RelicTriggerTiming
    {
        BattleStart, TurnStart, TurnEnd,
        CardPlayed, DamageTaken, EnemyKilled,
        NodeEntered, CardDrawn, EnergyChanged, DeckShuffled,
        PlayerTurnStart, PlayerTurnEnd, EnemyTurnStart, EnemyTurnEnd
    }

    public enum StatusEffectType
    {
        // Buffs
        Power, Toughness, Armor, Thorns, Haste, Crit,
        StatusProtect, EnchantedArmor, Illusion, Barrier,
        // Debuffs
        Weak, Fragile, BrokenGuard, Amnesia, Bleed, Slow,
        // Special
        Burn, Chill, Combustion, Inferno, DemonicBrand,
        // Continuous flags
        SensoryOverload, SweepingStrikes, Juggernaut, GrowingPower,
        Motivation, AdrenalineRush, RegenerationPotion, FireRadiance,
        IceAge, TemperatureShock, Berserk, Panic,
        // Enemy-only
        HobgoblinFury, SpectralForm, TemporalStasis, Rocksolid
    }

    public enum StatusDurationType
    {
        Continuous,   // ∞ 无限持续
        TurnBased,    // 按回合衰减
        ChargeBased,  // 按次数消耗
        StackBased    // 层数叠加
    }

    public enum MapNodeType
    {
        Battle, Elite, Shop, Mystery, Campfire, Treasure, Boss
    }
}
