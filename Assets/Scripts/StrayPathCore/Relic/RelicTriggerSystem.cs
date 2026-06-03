// 整改: 2026-06-03 修复了 FindObjectOfType/FindObjectsOfType 滥用 —— 使用 BattleStateMachine 查询接口获取英雄与敌人
using System;
using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Combat;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Deck;
using StrayPathCore.Status;
using UnityEngine;

namespace StrayPathCore.Relic
{
    /// <summary>
    /// 遗物触发系统 —— 非侵入式事件订阅，替代原硬编码侵入。
    /// </summary>
    public class RelicTriggerSystem : MonoBehaviour
    {
        public static RelicTriggerSystem Instance { get; private set; }

        // 触发时机 -> (遗物ID -> 回调)
        private Dictionary<RelicTriggerTiming, Dictionary<int, Action>> _subscribers = new Dictionary<RelicTriggerTiming, Dictionary<int, Action>>();

        // 限制机制：每场战斗/每回合限1次的遗物标记
        private HashSet<int> _oncePerBattleRelics = new HashSet<int>();
        private HashSet<int> _oncePerTurnRelics = new HashSet<int>();

        // 上下文缓存（通过事件总线收集）
        private int _lastPlayedCardID = -1;
        private int _lastPlayedCardCost = 0;
        private CardType _lastPlayedCardType = CardType.Attack;
        private bool _lastPlayedIsCombo = false;
        private bool _lastPlayedIsFinisher = false;
        private MapNodeType _lastEnteredNodeType;
        private int _lastEnergyOldValue = 0;
        private int _lastEnergyNewValue = 0;

        // 事件订阅引用（用于生命周期管理，防止内存泄漏）
        private Action<CardPlayedEvent> _cardPlayedHandler;
        private Action<NodeEnteredEvent> _nodeEnteredHandler;
        private Action<EnergyChangedEvent> _energyChangedHandler;

        // CardData 缓存
        private Dictionary<int, CardData> _cardDataCache;
        private bool _isEventBusSubscribed = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadCardDataCache();
            if (!_isEventBusSubscribed)
            {
                SubscribeToEventBus();
                _isEventBusSubscribed = true;
            }
        }

        private void LoadCardDataCache()
        {
            _cardDataCache = new Dictionary<int, CardData>();
            var all = Resources.LoadAll<CardData>("");
            if (all != null)
            {
                foreach (var cd in all)
                {
                    if (cd != null && !_cardDataCache.ContainsKey(cd.CardID))
                        _cardDataCache[cd.CardID] = cd;
                }
            }
        }

        private void SubscribeToEventBus()
        {
            // 缓存 CardPlayed 上下文
            _cardPlayedHandler = evt =>
            {
                _lastPlayedCardID = evt.CardID;
                _lastPlayedCardCost = evt.EnergyCost;
                if (_cardDataCache.TryGetValue(evt.CardID, out var data))
                {
                    _lastPlayedCardType = data.Type;
                    _lastPlayedIsCombo = data.IsCombo;
                    _lastPlayedIsFinisher = data.IsFinisher;
                }
            };
            GameEventBus.Instance?.Subscribe<CardPlayedEvent>(_cardPlayedHandler);

            // 缓存 NodeEntered 上下文
            _nodeEnteredHandler = evt =>
            {
                _lastEnteredNodeType = evt.NodeType;
            };
            GameEventBus.Instance?.Subscribe<NodeEnteredEvent>(_nodeEnteredHandler);

            // 缓存 EnergyChanged 上下文
            _energyChangedHandler = evt =>
            {
                _lastEnergyOldValue = evt.OldValue;
                _lastEnergyNewValue = evt.NewValue;
            };
            GameEventBus.Instance?.Subscribe<EnergyChangedEvent>(_energyChangedHandler);
        }

        private void OnDestroy()
        {
            if (_cardPlayedHandler != null)
                GameEventBus.Instance?.Unsubscribe<CardPlayedEvent>(_cardPlayedHandler);
            if (_nodeEnteredHandler != null)
                GameEventBus.Instance?.Unsubscribe<NodeEnteredEvent>(_nodeEnteredHandler);
            if (_energyChangedHandler != null)
                GameEventBus.Instance?.Unsubscribe<EnergyChangedEvent>(_energyChangedHandler);
        }

        public void SubscribeRelic(int relicID, RelicTriggerTiming timing, Action callback)
        {
            if (!_subscribers.TryGetValue(timing, out var dict))
            {
                dict = new Dictionary<int, Action>();
                _subscribers[timing] = dict;
            }
            dict[relicID] = callback;
        }

        public void UnsubscribeRelic(int relicID, RelicTriggerTiming timing)
        {
            if (_subscribers.TryGetValue(timing, out var dict))
            {
                dict.Remove(relicID);
            }
        }

        public void Trigger(RelicTriggerTiming timing, object context = null)
        {
            // 限制重置：确保在触发其他遗物前先重置对应限制
            if (timing == RelicTriggerTiming.BattleStart)
                ResetBattleLimits();
            if (timing == RelicTriggerTiming.PlayerTurnStart)
                ResetTurnLimits();

            if (!_subscribers.TryGetValue(timing, out var dict)) return;
            foreach (var kvp in dict.ToList())
            {
                if (RelicManager.Instance?.HasRelic(kvp.Key) ?? false)
                {
                    try { kvp.Value?.Invoke(); }
                    catch (Exception ex) { Debug.LogError($"[RelicTriggerSystem] 遗物{kvp.Key}触发异常: {ex}"); }
                }
            }
        }

        /// <summary>
        /// 每场战斗开始时重置单场限制。
        /// </summary>
        public void ResetBattleLimits()
        {
            _oncePerBattleRelics.Clear();
        }

        /// <summary>
        /// 每回合开始时重置回合限制。
        /// </summary>
        public void ResetTurnLimits()
        {
            _oncePerTurnRelics.Clear();
        }

        // ==================== 辅助方法 ====================

        private HeroCombatEntity GetHero() => BattleStateMachine.Instance?.GetHero();

        private EnemyCombatEntity[] GetAllEnemies()
        {
            var list = BattleStateMachine.Instance?.GetAllEnemies();
            if (list == null) return new EnemyCombatEntity[0];
            var result = new EnemyCombatEntity[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = list[i];
            return result;
        }

        private EnemyCombatEntity GetRandomEnemy()
        {
            var enemies = GetAllEnemies();
            if (enemies == null || enemies.Length == 0) return null;
            return enemies[UnityEngine.Random.Range(0, enemies.Length)];
        }

        private bool CanTriggerOncePerBattle(int relicID)
        {
            if (_oncePerBattleRelics.Contains(relicID)) return false;
            _oncePerBattleRelics.Add(relicID);
            return true;
        }

        private bool CanTriggerOncePerTurn(int relicID)
        {
            if (_oncePerTurnRelics.Contains(relicID)) return false;
            _oncePerTurnRelics.Add(relicID);
            return true;
        }

        private void SetHeroHP(HeroCombatEntity hero, int hp)
        {
            if (hero == null) return;
            hero.Revive(hp);
        }

        // ==================== 遗物注册 ====================

        public void InitializeRelicTriggers(RelicManager relicManager)
        {
            _subscribers.Clear();
            if (relicManager == null) return;

            // --------------------------------------------------
            // BattleStart 时机 —— 战斗开始时触发
            // --------------------------------------------------

            // 遗物1: Javelin — 战斗开始时对随机敌人造成11点伤害
            SubscribeRelic(1, RelicTriggerTiming.BattleStart, () =>
            {
                var enemy = GetRandomEnemy();
                enemy?.TakeDamage(11);
            });

            // 遗物2: Shuriken — 打出伤害牌时对随机敌人造成5点伤害（每场战斗限1次）
            SubscribeRelic(2, RelicTriggerTiming.CardPlayed, () =>
            {
                if (!CanTriggerOncePerBattle(2)) return;
                if (_lastPlayedCardType != CardType.Attack) return;
                var enemy = GetRandomEnemy();
                enemy?.TakeDamage(5);
            });

            // 遗物3: Bomb — 战斗开始时对全体敌人造成7点伤害
            SubscribeRelic(3, RelicTriggerTiming.BattleStart, () =>
            {
                foreach (var enemy in GetAllEnemies())
                    enemy?.TakeDamage(7);
            });

            // 遗物4: Buckler — 战斗开始时获得8点Block
            SubscribeRelic(4, RelicTriggerTiming.BattleStart, () =>
            {
                var hero = GetHero();
                hero?.GainBlock(8);
            });

            // 遗物5: Defense Manual — 打出防御牌时额外获得7 Block（每场战斗限1次）
            SubscribeRelic(5, RelicTriggerTiming.CardPlayed, () =>
            {
                if (!CanTriggerOncePerBattle(5)) return;
                if (_lastPlayedCardType != CardType.Defense) return;
                var hero = GetHero();
                hero?.GainBlock(7);
            });

            // 遗物7: Ring of Hope — 回合开始时额外抽1张牌（简化版）
            SubscribeRelic(7, RelicTriggerTiming.PlayerTurnStart, () =>
            {
                DeckManager.Instance?.DrawCards(1);
            });

            // 遗物8: Utility Belt — 战斗开始时抽2张牌
            SubscribeRelic(8, RelicTriggerTiming.BattleStart, () =>
            {
                DeckManager.Instance?.DrawCards(2);
            });

            // 遗物9: Boots of Speed — 战斗开始时获得1能量
            SubscribeRelic(9, RelicTriggerTiming.BattleStart, () =>
            {
                var hero = GetHero();
                hero?.GainEnergy(1);
            });

            // 遗物10: 战斗开始时获得1 Power
            SubscribeRelic(10, RelicTriggerTiming.BattleStart, () =>
            {
                StatusEffectSystem.Instance?.ApplyEffect("hero", StatusEffectType.Power, 1, StatusDurationType.StackBased);
            });

            // 遗物11: Oracle's Eye — 战斗开始时获得1 Toughness
            SubscribeRelic(11, RelicTriggerTiming.BattleStart, () =>
            {
                StatusEffectSystem.Instance?.ApplyEffect("hero", StatusEffectType.Toughness, 1, StatusDurationType.StackBased);
            });

            // 遗物12: Dragon Horn — 回合开始时获得1 Crit Charge
            SubscribeRelic(12, RelicTriggerTiming.PlayerTurnStart, () =>
            {
                StatusEffectSystem.Instance?.ApplyEffect("hero", StatusEffectType.Crit, 1, StatusDurationType.StackBased);
            });

            // 遗物13: Runic Shield — 受到伤害时获得1 Thorns
            SubscribeRelic(13, RelicTriggerTiming.DamageTaken, () =>
            {
                StatusEffectSystem.Instance?.ApplyEffect("hero", StatusEffectType.Thorns, 1, StatusDurationType.StackBased);
            });

            // 遗物14: Glass Gauntlets — 战斗开始时获得3 Power（持续到洗牌，简化版用StackBased）
            SubscribeRelic(14, RelicTriggerTiming.BattleStart, () =>
            {
                StatusEffectSystem.Instance?.ApplyEffect("hero", StatusEffectType.Power, 3, StatusDurationType.StackBased);
            });

            // 遗物15: Giant Gloves — 打出费用≥2的牌时，对全体敌人造成5伤害（每回合限1次）
            SubscribeRelic(15, RelicTriggerTiming.CardPlayed, () =>
            {
                if (!CanTriggerOncePerTurn(15)) return;
                if (_lastPlayedCardCost < 2) return;
                foreach (var enemy in GetAllEnemies())
                    enemy?.TakeDamage(5);
            });

            // 遗物16: Giant Cloak — 打出费用≥2的牌时，获得5 Block（每回合限1次）
            SubscribeRelic(16, RelicTriggerTiming.CardPlayed, () =>
            {
                if (!CanTriggerOncePerTurn(16)) return;
                if (_lastPlayedCardCost < 2) return;
                var hero = GetHero();
                hero?.GainBlock(5);
            });

            // 遗物17: Giant Belt — 打出费用≥2的牌时，抽1张牌（每回合限1次）
            SubscribeRelic(17, RelicTriggerTiming.CardPlayed, () =>
            {
                if (!CanTriggerOncePerTurn(17)) return;
                if (_lastPlayedCardCost < 2) return;
                DeckManager.Instance?.DrawCards(1);
            });

            // 遗物18: Pocket Watch — 每3回合开始时获得1 Haste
            SubscribeRelic(18, RelicTriggerTiming.PlayerTurnStart, () =>
            {
                int turn = BattleStateMachine.Instance?.PlayerTurnNumber ?? 0;
                if (turn > 0 && turn % 3 == 0)
                {
                    StatusEffectSystem.Instance?.ApplyEffect("hero", StatusEffectType.Haste, 1, StatusDurationType.StackBased);
                }
            });

            // 遗物19: Collector's Cloak — 每有2个遗物，回合开始时获得1 Block
            SubscribeRelic(19, RelicTriggerTiming.PlayerTurnStart, () =>
            {
                int relicCount = RelicManager.Instance?.PlayerRelics?.Count ?? 0;
                int block = relicCount / 2;
                if (block > 0)
                {
                    var hero = GetHero();
                    hero?.GainBlock(block);
                }
            });

            // 遗物21: Tabard of Vigor (DS) — 打出Combo牌时获得1能量
            SubscribeRelic(21, RelicTriggerTiming.CardPlayed, () =>
            {
                if (!_lastPlayedIsCombo) return;
                var hero = GetHero();
                hero?.GainEnergy(1);
            });

            // 遗物22: Tabard of Command (DS) — 打出Finisher牌时给随机敌人施加1 Weak
            SubscribeRelic(22, RelicTriggerTiming.CardPlayed, () =>
            {
                if (!_lastPlayedIsFinisher) return;
                var enemy = GetRandomEnemy();
                if (enemy != null)
                    StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 1, StatusDurationType.StackBased);
            });

            // 遗物23: Enchanted Boots — 受到伤害后下回合抽1张牌+获得1能量（每场战斗限1次）
            SubscribeRelic(23, RelicTriggerTiming.DamageTaken, () =>
            {
                if (!CanTriggerOncePerBattle(23)) return;
                var hero = GetHero();
                if (hero != null)
                {
                    hero.AddStartOfTurnEffect(() =>
                    {
                        DeckManager.Instance?.DrawCards(1);
                        hero.GainEnergy(1);
                    });
                }
            });

            // 遗物24: Adaptive Armor — 受到伤害时获得4 Block（每回合限1次）
            SubscribeRelic(24, RelicTriggerTiming.DamageTaken, () =>
            {
                if (!CanTriggerOncePerTurn(24)) return;
                var hero = GetHero();
                hero?.GainBlock(4);
            });

            // 遗物25: Luma's Grace — 受到致命伤害时以1HP存活并获得40 Block（每场战斗限1次）
            SubscribeRelic(25, RelicTriggerTiming.DamageTaken, () =>
            {
                if (!CanTriggerOncePerBattle(25)) return;
                var hero = GetHero();
                if (hero != null && hero.CurrentHP <= 0)
                {
                    SetHeroHP(hero, 1);
                    hero.GainBlock(40);
                }
            });

            // 遗物26: Runic Axe — HP<50%时受到伤害，对全体敌人造成20伤害（每场战斗限1次）
            SubscribeRelic(26, RelicTriggerTiming.DamageTaken, () =>
            {
                if (!CanTriggerOncePerBattle(26)) return;
                var hero = GetHero();
                if (hero != null && hero.CurrentHP * 100 / Mathf.Max(1, hero.MaxHP) < 50)
                {
                    foreach (var enemy in GetAllEnemies())
                        enemy?.TakeDamage(20);
                }
            });

            // --------------------------------------------------
            // EnemyKilled 时机 —— 击杀敌人时触发
            // --------------------------------------------------

            // 遗物32: Amulet of Triumph — 击杀敌人时抽1张牌+获得1能量（每场战斗限1次）
            SubscribeRelic(32, RelicTriggerTiming.EnemyKilled, () =>
            {
                if (!CanTriggerOncePerBattle(32)) return;
                DeckManager.Instance?.DrawCards(1);
                var hero = GetHero();
                hero?.GainEnergy(1);
            });

            // 遗物33: Amulet of Vampirism — 击杀敌人时恢复3HP
            SubscribeRelic(33, RelicTriggerTiming.EnemyKilled, () =>
            {
                var hero = GetHero();
                hero?.Heal(3);
            });

            // 遗物34: Iceborn Amulet (GM) — 击杀敌人时抽2张牌+获得2能量
            SubscribeRelic(34, RelicTriggerTiming.EnemyKilled, () =>
            {
                DeckManager.Instance?.DrawCards(2);
                var hero = GetHero();
                hero?.GainEnergy(2);
            });

            // --------------------------------------------------
            // NodeEntered 时机 —— 进入节点时触发
            // --------------------------------------------------

            // 遗物41: Refillable Potion — 进入商店时恢复10HP
            SubscribeRelic(41, RelicTriggerTiming.NodeEntered, () =>
            {
                if (_lastEnteredNodeType != MapNodeType.Shop) return;
                GameStateManager.Instance?.HealHP(10, "relic");
            });

            // 遗物42: Gem of Growth — 进入营地时MaxHP+1并恢复HP
            SubscribeRelic(42, RelicTriggerTiming.NodeEntered, () =>
            {
                if (_lastEnteredNodeType != MapNodeType.Campfire) return;
                var gsm = GameStateManager.Instance;
                if (gsm != null)
                {
                    gsm.SetMaxHP(gsm.CurrentRun.MaxHP + 1, false);
                    gsm.HealHP(1, "relic");
                }
            });

            // 遗物43: Bloodstone — 进入Boss战时恢复20HP
            SubscribeRelic(43, RelicTriggerTiming.NodeEntered, () =>
            {
                if (_lastEnteredNodeType != MapNodeType.Boss) return;
                GameStateManager.Instance?.HealHP(20, "relic");
            });

            // --------------------------------------------------
            // CardDrawn 时机 —— 抽牌时触发
            // --------------------------------------------------

            // 遗物50: Ring of Protection — 抽牌时获得1 Block
            SubscribeRelic(50, RelicTriggerTiming.CardDrawn, () =>
            {
                var hero = GetHero();
                hero?.GainBlock(1);
            });

            // 遗物51: Coffee Beans — 抽牌时获得1能量（简化版）
            SubscribeRelic(51, RelicTriggerTiming.CardDrawn, () =>
            {
                var hero = GetHero();
                hero?.GainEnergy(1);
            });

            // --------------------------------------------------
            // DeckShuffled 时机 —— 洗牌时触发
            // --------------------------------------------------

            // 遗物60: Winged Boots — 洗牌时获得1能量
            SubscribeRelic(60, RelicTriggerTiming.DeckShuffled, () =>
            {
                var hero = GetHero();
                hero?.GainEnergy(1);
            });

            // 遗物61: Heroic Gauntlets — 洗牌时获得2 Power（每场战斗限1次）
            SubscribeRelic(61, RelicTriggerTiming.DeckShuffled, () =>
            {
                if (!CanTriggerOncePerBattle(61)) return;
                StatusEffectSystem.Instance?.ApplyEffect("hero", StatusEffectType.Power, 2, StatusDurationType.StackBased);
            });

            // --------------------------------------------------
            // EnergyChanged 时机 —— 能量变化时触发
            // --------------------------------------------------

            // 遗物73: Prismatic Gem — 能量归零时保留1点（简化版）
            SubscribeRelic(73, RelicTriggerTiming.EnergyChanged, () =>
            {
                if (_lastEnergyOldValue > 0 && _lastEnergyNewValue == 0)
                {
                    var hero = GetHero();
                    hero?.GainEnergy(1);
                }
            });

            // --------------------------------------------------
            // TurnEnd 时机 —— 回合结束时触发
            // --------------------------------------------------

            // 遗物70: Cloak of Shadows — 回合结束时若Block=0，获得5 Block
            SubscribeRelic(70, RelicTriggerTiming.TurnEnd, () =>
            {
                var hero = GetHero();
                if (hero != null && hero.CurrentBlock == 0)
                    hero.GainBlock(5);
            });

            // 遗物71: Tabard of Devotion — 回合结束时若Bleed>0，额外恢复2HP（Bleed减速简化版）
            SubscribeRelic(71, RelicTriggerTiming.TurnEnd, () =>
            {
                var hero = GetHero();
                if (hero != null && hero.BleedStacks > 0)
                    hero.Heal(2);
            });
        }
    }
}
