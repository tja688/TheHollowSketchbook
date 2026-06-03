using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Combat;
using StrayPathCore.Status;

namespace StrayPathCore.Deck
{
    public class CardEffectDispatcher : MonoBehaviour
    {
        public static CardEffectDispatcher Instance { get; private set; }

        private Dictionary<int, Action<EnemyCombatEntity>> _enemyCardEffectDict = new Dictionary<int, Action<EnemyCombatEntity>>();
        private Dictionary<int, Action> _heroCardEffectDict = new Dictionary<int, Action>();
        private Dictionary<int, Action<CardRuntime, EnemyCombatEntity>> _advEnemyCardEffectDict = new Dictionary<int, Action<CardRuntime, EnemyCombatEntity>>();
        private Dictionary<int, Action<CardRuntime>> _advHeroCardEffectDict = new Dictionary<int, Action<CardRuntime>>();
        private Dictionary<string, int> _weakeningCardDict = new Dictionary<string, int>();
        private Dictionary<string, int> _rampageCounterDict = new Dictionary<string, int>();
        private static Dictionary<int, CardData> _cardDataCache;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        private static CardData GetCardData(int cardID)
        {
            if (_cardDataCache == null)
            {
                _cardDataCache = new Dictionary<int, CardData>();
                var all = Resources.LoadAll<CardData>("");
                foreach (var cd in all)
                {
                    if (cd != null && !_cardDataCache.ContainsKey(cd.CardID))
                        _cardDataCache[cd.CardID] = cd;
                }
            }
            _cardDataCache.TryGetValue(cardID, out var data);
            return data;
        }

        public void Initialize()
        {
            RegisterCoreEffects();
        }

        private void RegisterCoreEffects()
        {
            _enemyCardEffectDict[1] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 7;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null) bs.ComboActive = true;
            };
            _enemyCardEffectDict[1001] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 10;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };

            _heroCardEffectDict[2] = () =>
            {
                int block = 6;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
            };
            _heroCardEffectDict[1002] = () =>
            {
                int block = 9;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
            };

            _heroCardEffectDict[5] = () =>
            {
                int block = 7;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
                BattleStateMachine.Instance?.GetHero()?.AddStartOfTurnEffect(() => DeckManager.Instance?.DrawCards(1));
            };
            _heroCardEffectDict[1005] = () =>
            {
                int block = 10;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
                BattleStateMachine.Instance?.GetHero()?.AddStartOfTurnEffect(() => DeckManager.Instance?.DrawCards(1));
            };

            _advEnemyCardEffectDict[25] = (card, enemy) =>
            {
                if (enemy == null) return;
                var run = GameStateManager.Instance?.CurrentRun;
                string key = $"{card.CardID}_{card.CopyCount}";
                if (run != null)
                {
                    if (!run.InfinityCharges.ContainsKey(key))
                        run.InfinityCharges[key] = 0;
                    int charges = run.InfinityCharges[key];
                    int dmg = 8 + charges * 2;
                    run.InfinityCharges[key] = charges + 1;
                    PlayVFXEnemy("InfinityBlade", enemy);
                    enemy.TakeDamage(dmg);
                    GameEventBus.Instance.Publish(new DamageDealtEvent
                    {
                        SourceUID = "hero",
                        TargetUID = enemy.UniqueID,
                        BaseDamage = dmg,
                        FinalDamage = dmg,
                        IsBlocked = false
                    });
                }
            };
            _advEnemyCardEffectDict[1025] = (card, enemy) =>
            {
                if (enemy == null) return;
                var run = GameStateManager.Instance?.CurrentRun;
                string key = $"{card.CardID}_{card.CopyCount}";
                if (run != null)
                {
                    if (!run.InfinityCharges.ContainsKey(key))
                        run.InfinityCharges[key] = 0;
                    int charges = run.InfinityCharges[key];
                    int dmg = 12 + charges * 3;
                    run.InfinityCharges[key] = charges + 1;
                    PlayVFXEnemy("InfinityBlade", enemy);
                    enemy.TakeDamage(dmg);
                    GameEventBus.Instance.Publish(new DamageDealtEvent { SourceUID = "hero", TargetUID = enemy.UniqueID, BaseDamage = dmg, FinalDamage = dmg, IsBlocked = false });
                }
            };

            _advHeroCardEffectDict[134] = card =>
            {
                string key = $"{card.CardID}_{card.CopyCount}";
                if (!_weakeningCardDict.TryGetValue(key, out int weaken))
                    weaken = 0;
                int block = Mathf.Max(0, 10 - weaken * 2);
                _weakeningCardDict[key] = weaken + 1;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
            };
            _advHeroCardEffectDict[1134] = card =>
            {
                string key = $"{card.CardID}_{card.CopyCount}";
                if (!_weakeningCardDict.TryGetValue(key, out int weaken))
                    weaken = 0;
                int block = Mathf.Max(0, 14 - weaken * 2);
                _weakeningCardDict[key] = weaken + 1;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
            };

            // ==================== 基础攻击卡 ====================

            // ID 3: Heavy Strike — 单体12伤害，费用2
            _enemyCardEffectDict[3] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 12;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };
            _enemyCardEffectDict[1003] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 17;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };

            // ID 4: Dual Strike — 2段伤害，每段4点
            _enemyCardEffectDict[4] = enemy =>
            {
                if (enemy == null) return;
                int dmgPerHit = 4;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeMultipleDamage(dmgPerHit, 2);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmgPerHit * 2,
                    FinalDamage = dmgPerHit * 2,
                    IsBlocked = false
                });
            };
            _enemyCardEffectDict[1004] = enemy =>
            {
                if (enemy == null) return;
                int dmgPerHit = 6;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeMultipleDamage(dmgPerHit, 2);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmgPerHit * 2,
                    FinalDamage = dmgPerHit * 2,
                    IsBlocked = false
                });
            };

            // ID 13: Heavy Swing — 单体14伤害，费用2，Combo
            _enemyCardEffectDict[13] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 14;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null) bs.ComboActive = true;
            };
            _enemyCardEffectDict[1013] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 20;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null) bs.ComboActive = true;
            };

            // ID 15: Swift Strike — 单体6伤害，抽1张牌
            _enemyCardEffectDict[15] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 6;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                DeckManager.Instance?.DrawCards(1);
            };
            _enemyCardEffectDict[1015] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 9;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                DeckManager.Instance?.DrawCards(1);
            };

            // ID 16: Bash — 单体8伤害，施加1层Weak
            _enemyCardEffectDict[16] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 8;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 1, StatusDurationType.StackBased);
            };
            _enemyCardEffectDict[1016] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 12;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 2, StatusDurationType.StackBased);
            };

            // ID 20: Cleave — 全体敌人6伤害（AOE）
            _heroCardEffectDict[20] = () =>
            {
                int dmg = 6;
                var enemies = BattleStateMachine.Instance?.GetAllEnemies();
                PlayVFXAllEnemies("AttackHit");
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakeDamage(dmg);
                    GameEventBus.Instance.Publish(new DamageDealtEvent
                    {
                        SourceUID = "hero",
                        TargetUID = enemy.UniqueID,
                        BaseDamage = dmg,
                        FinalDamage = dmg,
                        IsBlocked = false
                    });
                }
            };
            _heroCardEffectDict[1020] = () =>
            {
                int dmg = 9;
                var enemies = BattleStateMachine.Instance?.GetAllEnemies();
                PlayVFXAllEnemies("AttackHit");
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakeDamage(dmg);
                    GameEventBus.Instance.Publish(new DamageDealtEvent
                    {
                        SourceUID = "hero",
                        TargetUID = enemy.UniqueID,
                        BaseDamage = dmg,
                        FinalDamage = dmg,
                        IsBlocked = false
                    });
                }
            };

            // ID 37: Whirlwind — 全体敌人10伤害，费用3
            _heroCardEffectDict[37] = () =>
            {
                int dmg = 10;
                var enemies = BattleStateMachine.Instance?.GetAllEnemies();
                PlayVFXAllEnemies("AttackHit");
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakeDamage(dmg);
                    GameEventBus.Instance.Publish(new DamageDealtEvent
                    {
                        SourceUID = "hero",
                        TargetUID = enemy.UniqueID,
                        BaseDamage = dmg,
                        FinalDamage = dmg,
                        IsBlocked = false
                    });
                }
            };
            _heroCardEffectDict[1037] = () =>
            {
                int dmg = 15;
                var enemies = BattleStateMachine.Instance?.GetAllEnemies();
                PlayVFXAllEnemies("AttackHit");
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakeDamage(dmg);
                    GameEventBus.Instance.Publish(new DamageDealtEvent
                    {
                        SourceUID = "hero",
                        TargetUID = enemy.UniqueID,
                        BaseDamage = dmg,
                        FinalDamage = dmg,
                        IsBlocked = false
                    });
                }
            };

            // ID 52: Blade Barrier — 单体15伤害，获得5格挡
            _enemyCardEffectDict[52] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 15;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                var hero = BattleStateMachine.Instance?.GetHero();
                if (hero != null)
                {
                    hero.GainBlock(5);
                    PlayVFXHero("ShieldUp");
                }
            };
            _enemyCardEffectDict[1052] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 22;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                var hero = BattleStateMachine.Instance?.GetHero();
                if (hero != null)
                {
                    hero.GainBlock(7);
                    PlayVFXHero("ShieldUp");
                }
            };

            // ==================== 基础防御卡 ====================

            // ID 6: Fortify — 获得10格挡，费用2
            _heroCardEffectDict[6] = () =>
            {
                int block = 10;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
            };
            _heroCardEffectDict[1006] = () =>
            {
                int block = 15;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
            };

            // ID 7: Shield Bash — 获得5格挡，敌人获得Fragile
            _enemyCardEffectDict[7] = enemy =>
            {
                int block = 5;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
                if (enemy != null)
                {
                    StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Fragile, 1, StatusDurationType.StackBased);
                }
            };
            _enemyCardEffectDict[1007] = enemy =>
            {
                int block = 8;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
                if (enemy != null)
                {
                    StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Fragile, 2, StatusDurationType.StackBased);
                }
            };

            // ID 8: Barricade — 获得8格挡，下回合获得4格挡
            _heroCardEffectDict[8] = () =>
            {
                int block = 8;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.AddStartOfTurnEffect(() => hero.GainBlock(4));
            };
            _heroCardEffectDict[1008] = () =>
            {
                int block = 12;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.AddStartOfTurnEffect(() => hero.GainBlock(6));
            };

            // ==================== 特殊攻击/效果卡 ====================

            // ID 21: Poisoned Strike — 单体5伤害，施加2层Burn
            _enemyCardEffectDict[21] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 5;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Burn, 2, StatusDurationType.StackBased);
            };
            _enemyCardEffectDict[1021] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 8;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Burn, 3, StatusDurationType.StackBased);
            };

            // ID 22: Piercing Strike — 单体8伤害，穿透Block
            _enemyCardEffectDict[22] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 8;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg, true);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };
            _enemyCardEffectDict[1022] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 12;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg, true);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };

            // ID 23: Execute — 单体10伤害，若敌人HP<50%则伤害翻倍
            _enemyCardEffectDict[23] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 10;
                if (enemy.CurrentHP * 2 < enemy.MaxHP) dmg *= 2;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };
            _enemyCardEffectDict[1023] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 14;
                if (enemy.CurrentHP * 2 < enemy.MaxHP) dmg *= 2;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };

            // ID 24: Rampage — 单体6伤害，每次使用+3伤害
            _advEnemyCardEffectDict[24] = (card, enemy) =>
            {
                if (enemy == null) return;
                string key = $"{card.CardID}_{card.CopyCount}";
                if (!_rampageCounterDict.TryGetValue(key, out int uses))
                    uses = 0;
                int dmg = 6 + uses * 3;
                _rampageCounterDict[key] = uses + 1;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };
            _advEnemyCardEffectDict[1024] = (card, enemy) =>
            {
                if (enemy == null) return;
                string key = $"{card.CardID}_{card.CopyCount}";
                if (!_rampageCounterDict.TryGetValue(key, out int uses))
                    uses = 0;
                int dmg = 9 + uses * 4;
                _rampageCounterDict[key] = uses + 1;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };

            // ID 26: Uppercut — 单体8伤害，获得2能量
            _enemyCardEffectDict[26] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 8;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.GainEnergy(2);
            };
            _enemyCardEffectDict[1026] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 12;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.GainEnergy(3);
            };

            // ID 27: Dropkick — 单体5伤害，若敌人有Weak则抽1张牌
            _enemyCardEffectDict[27] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 5;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                if (StatusEffectSystem.Instance?.HasEffect(enemy.UniqueID, StatusEffectType.Weak) ?? false)
                {
                    DeckManager.Instance?.DrawCards(1);
                }
            };
            _enemyCardEffectDict[1027] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 8;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                if (StatusEffectSystem.Instance?.HasEffect(enemy.UniqueID, StatusEffectType.Weak) ?? false)
                {
                    DeckManager.Instance?.DrawCards(1);
                }
            };

            // ID 28: Hemokinesis — 失去3HP，单体14伤害
            _enemyCardEffectDict[28] = enemy =>
            {
                if (enemy == null) return;
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.TakeDamage(3, null);
                int dmg = 14;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };
            _enemyCardEffectDict[1028] = enemy =>
            {
                if (enemy == null) return;
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.TakeDamage(3, null);
                int dmg = 20;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };

            // ID 33: Clothesline — 单体12伤害，施加2层Weak
            _enemyCardEffectDict[33] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 12;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 2, StatusDurationType.StackBased);
            };
            _enemyCardEffectDict[1033] = enemy =>
            {
                if (enemy == null) return;
                int dmg = 17;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
                StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 3, StatusDurationType.StackBased);
            };

            // ID 34: Thunderclap — 全体敌人4伤害，施加1层Weak
            _heroCardEffectDict[34] = () =>
            {
                int dmg = 4;
                var enemies = BattleStateMachine.Instance?.GetAllEnemies();
                PlayVFXAllEnemies("AttackHit");
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakeDamage(dmg);
                    GameEventBus.Instance.Publish(new DamageDealtEvent
                    {
                        SourceUID = "hero",
                        TargetUID = enemy.UniqueID,
                        BaseDamage = dmg,
                        FinalDamage = dmg,
                        IsBlocked = false
                    });
                    StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 1, StatusDurationType.StackBased);
                }
            };
            _heroCardEffectDict[1034] = () =>
            {
                int dmg = 6;
                var enemies = BattleStateMachine.Instance?.GetAllEnemies();
                PlayVFXAllEnemies("AttackHit");
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakeDamage(dmg);
                    GameEventBus.Instance.Publish(new DamageDealtEvent
                    {
                        SourceUID = "hero",
                        TargetUID = enemy.UniqueID,
                        BaseDamage = dmg,
                        FinalDamage = dmg,
                        IsBlocked = false
                    });
                    StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 2, StatusDurationType.StackBased);
                }
            };

            // ID 35: Perfected Strike — 单体6伤害，牌组中每有一张Strike+2伤害
            _advEnemyCardEffectDict[35] = (card, enemy) =>
            {
                if (enemy == null) return;
                int strikeCount = 0;
                var deck = DeckManager.Instance?.ListAllCardsInDeck();
                if (deck != null)
                {
                    foreach (var c in deck)
                    {
                        if (c != null && c.CardID == 1) strikeCount++;
                    }
                }
                int dmg = 6 + strikeCount * 2;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };
            _advEnemyCardEffectDict[1035] = (card, enemy) =>
            {
                if (enemy == null) return;
                int strikeCount = 0;
                var deck = DeckManager.Instance?.ListAllCardsInDeck();
                if (deck != null)
                {
                    foreach (var c in deck)
                    {
                        if (c != null && c.CardID == 1) strikeCount++;
                    }
                }
                int dmg = 9 + strikeCount * 3;
                PlayVFXEnemy("AttackHit", enemy);
                enemy.TakeDamage(dmg);
                GameEventBus.Instance.Publish(new DamageDealtEvent
                {
                    SourceUID = "hero",
                    TargetUID = enemy.UniqueID,
                    BaseDamage = dmg,
                    FinalDamage = dmg,
                    IsBlocked = false
                });
            };

            // ==================== 防御/辅助卡 ====================

            // ID 40: Shrug It Off — 获得8格挡，抽1张牌
            _heroCardEffectDict[40] = () =>
            {
                int block = 8;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
                DeckManager.Instance?.DrawCards(1);
            };
            _heroCardEffectDict[1040] = () =>
            {
                int block = 12;
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null)
                {
                    bs.CurrentBlock += block;
                    GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = block, TotalBlock = bs.CurrentBlock });
                }
                PlayVFXHero("ShieldUp");
                DeckManager.Instance?.DrawCards(1);
            };

            // ID 41: Battle Trance — 抽3张牌（简化版：仅抽牌，本回合不再抽牌的限制未实现）
            _heroCardEffectDict[41] = () =>
            {
                DeckManager.Instance?.DrawCards(3);
            };
            _heroCardEffectDict[1041] = () =>
            {
                DeckManager.Instance?.DrawCards(4);
            };

            // ID 42: Bloodletting — 失去3HP，获得2能量
            _heroCardEffectDict[42] = () =>
            {
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.TakeDamage(3, null);
                hero?.GainEnergy(2);
            };
            _heroCardEffectDict[1042] = () =>
            {
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.TakeDamage(3, null);
                hero?.GainEnergy(3);
            };

            // ID 43: Seeing Red — 获得2能量
            _heroCardEffectDict[43] = () =>
            {
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.GainEnergy(2);
            };
            _heroCardEffectDict[1043] = () =>
            {
                var hero = BattleStateMachine.Instance?.GetHero();
                hero?.GainEnergy(3);
            };

            // ID 44: Disarm — 单体敌人失去5Power（简化版：施加2层Weak，因EnemyCombatEntity暂不支持直接修改Power）
            _enemyCardEffectDict[44] = enemy =>
            {
                if (enemy == null) return;
                StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 2, StatusDurationType.StackBased);
                GameEventBus.Instance.Publish(new StatusEffectAppliedEvent
                {
                    TargetUID = enemy.UniqueID,
                    EffectType = StatusEffectType.Weak,
                    Value = 2,
                    DurationType = StatusDurationType.StackBased
                });
            };
            _enemyCardEffectDict[1044] = enemy =>
            {
                if (enemy == null) return;
                StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 3, StatusDurationType.StackBased);
                GameEventBus.Instance.Publish(new StatusEffectAppliedEvent
                {
                    TargetUID = enemy.UniqueID,
                    EffectType = StatusEffectType.Weak,
                    Value = 3,
                    DurationType = StatusDurationType.StackBased
                });
            };

            // ID 45: Shockwave — 全体敌人施加2层Weak
            _heroCardEffectDict[45] = () =>
            {
                var enemies = BattleStateMachine.Instance?.GetAllEnemies();
                PlayVFXAllEnemies("Debuff");
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 2, StatusDurationType.StackBased);
                }
            };
            _heroCardEffectDict[1045] = () =>
            {
                var enemies = BattleStateMachine.Instance?.GetAllEnemies();
                PlayVFXAllEnemies("Debuff");
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    StatusEffectSystem.Instance?.ApplyEffect(enemy.UniqueID, StatusEffectType.Weak, 3, StatusDurationType.StackBased);
                }
            };
        }

        public void RegisterEffect(int cardID, Action effect) => _heroCardEffectDict[cardID] = effect;
        public void RegisterEffect(int cardID, Action<EnemyCombatEntity> effect) => _enemyCardEffectDict[cardID] = effect;
        public void RegisterEffect(int cardID, Action<CardRuntime> effect) => _advHeroCardEffectDict[cardID] = effect;
        public void RegisterEffect(int cardID, Action<CardRuntime, EnemyCombatEntity> effect) => _advEnemyCardEffectDict[cardID] = effect;

        public void ExecuteCardEffect(int cardID, CardRuntime card, EnemyCombatEntity target = null)
        {
            if (card == null) return;
            int effectID = card.IsUpgraded ? cardID + 1000 : cardID;

            if (_advEnemyCardEffectDict.TryGetValue(effectID, out var advEnemyEffect))
            {
                advEnemyEffect.Invoke(card, target);
                return;
            }
            if (card.IsUpgraded && _advEnemyCardEffectDict.TryGetValue(cardID, out var advEnemyBase))
            {
                advEnemyBase.Invoke(card, target);
                return;
            }

            if (_advHeroCardEffectDict.TryGetValue(effectID, out var advHeroEffect))
            {
                advHeroEffect.Invoke(card);
                return;
            }
            if (card.IsUpgraded && _advHeroCardEffectDict.TryGetValue(cardID, out var advHeroBase))
            {
                advHeroBase.Invoke(card);
                return;
            }

            if (_enemyCardEffectDict.TryGetValue(effectID, out var enemyEffect))
            {
                enemyEffect.Invoke(target);
                return;
            }
            if (card.IsUpgraded && _enemyCardEffectDict.TryGetValue(cardID, out var enemyBase))
            {
                enemyBase.Invoke(target);
                return;
            }

            if (_heroCardEffectDict.TryGetValue(effectID, out var heroEffect))
            {
                heroEffect.Invoke();
                return;
            }
            if (card.IsUpgraded && _heroCardEffectDict.TryGetValue(cardID, out var heroBase))
            {
                heroBase.Invoke();
                return;
            }

            Debug.LogWarning($"[CardEffectDispatcher] 未注册卡牌效果: {cardID} (升级={card.IsUpgraded})");
        }

        public int GetCardEnergyCost(CardRuntime card)
        {
            if (card == null) return 0;
            var data = GetCardData(card.CardID);
            return data?.GetEnergyCost(card.IsUpgraded) ?? 0;
        }

        public int GetCardAttackValue(CardRuntime card)
        {
            if (card == null) return 0;
            var data = GetCardData(card.CardID);
            if (data == null) return 0;
            int baseVal = data.GetAttackValue(card.IsUpgraded);
            string key = $"{card.CardID}_{card.CopyCount}";
            if (_weakeningCardDict.TryGetValue(key, out int weaken))
                baseVal = Mathf.Max(0, baseVal - weaken);
            return baseVal;
        }

        public int GetCardDefendValue(CardRuntime card)
        {
            if (card == null) return 0;
            var data = GetCardData(card.CardID);
            if (data == null) return 0;
            int baseVal = data.GetDefendValue(card.IsUpgraded);
            string key = $"{card.CardID}_{card.CopyCount}";
            if (_weakeningCardDict.TryGetValue(key, out int weaken))
                baseVal = Mathf.Max(0, baseVal - weaken * 2);
            return baseVal;
        }

        public int GetCardBanishCharges(CardRuntime card)
        {
            if (card == null) return 0;
            var data = GetCardData(card.CardID);
            return data?.GetBanishCharges(card.IsUpgraded) ?? 0;
        }

        public void PlayVFXEnemy(string vfxName, EnemyCombatEntity enemy)
        {
            Debug.Log($"[VFX] {vfxName} on enemy {enemy?.UniqueID}");
        }

        public void PlayVFXHero(string vfxName)
        {
            Debug.Log($"[VFX] {vfxName} on hero");
        }

        public void PlayVFXAllEnemies(string vfxName)
        {
            Debug.Log($"[VFX] {vfxName} on all enemies");
        }

        public void DelayedCardDraw(int count, float delay = 0.1f)
        {
            StartCoroutine(DelayedActionCoroutine(delay, () => DeckManager.Instance?.DrawCards(count)));
        }

        public void DelayedFakeCardDraw(int cardID, bool upgraded, float delay = 0.1f)
        {
            StartCoroutine(DelayedActionCoroutine(delay, () => DeckManager.Instance?.AddFakeCardToPlayerHand(cardID, upgraded)));
        }

        public void DelayedTurnEnd(float delay = 0.1f)
        {
            StartCoroutine(DelayedActionCoroutine(delay, () =>
            {
                int turn = GameStateManager.Instance?.BattleState.PlayerTurn ?? 0;
                GameEventBus.Instance.Publish(new PlayerTurnEndedEvent { TurnNumber = turn });
            }));
        }

        private IEnumerator DelayedActionCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
