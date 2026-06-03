// 整改: 2026-06-03 修复了 FindObjectOfType 滥用 —— 添加公开查询接口，使用单例引用替代运行时查找
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Utils;
using StrayPathCore.Deck;
using UnityEngine;

namespace StrayPathCore.Combat
{
    /// <summary>
    /// 战斗阶段状态机 —— 严格回合制，驱动整个战斗流程。
    /// </summary>
    public enum BattlePhase
    {
        BattleStart,
        PlayerTurnStart,
        PlayerTurn,
        PlayerTurnEnd,
        EnemyTurnStart,
        EnemyTurn,
        EnemyTurnEnd,
        BattleEnd
    }

    public class BattleStateMachine : MonoBehaviour
    {
        public static BattleStateMachine Instance { get; private set; }

        public BattlePhase CurrentPhase { get; private set; }
        public int PlayerTurnNumber { get; private set; }
        public int EnemyTurnNumber { get; private set; }

        private HeroCombatEntity _hero;
        private List<EnemyCombatEntity> _enemies = new List<EnemyCombatEntity>();
        private Dictionary<string, EnemyAbilityData> _selectedEnemyActions = new Dictionary<string, EnemyAbilityData>();
        private BoostSystem _boostSystem;
        private CombatRewardSystem _rewardSystem;
        private GameEventBus _eventBus;
        private int _actID;
        private int _battleType;
        private bool? _pendingVictory;
        private int _dsAttackCounter; // DS被动：每第3张攻击牌计数
        private static Dictionary<int, StrayPathCore.Data.CardData> _cardDataCache;

        // ==================== 公开查询接口（替代 FindObjectOfType 滥用） ====================

        /// <summary>获取当前战斗中的英雄实体。</summary>
        public HeroCombatEntity GetHero() => _hero;

        /// <summary>根据唯一标识符获取敌人实体。</summary>
        public EnemyCombatEntity GetEnemyByUID(string uid) => _enemies.Find(e => e?.UniqueID == uid);

        /// <summary>获取所有存活敌人的只读列表。</summary>
        public IReadOnlyList<EnemyCombatEntity> GetAllEnemies() => _enemies.AsReadOnly();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _eventBus = GameEventBus.Instance;
            _eventBus.Subscribe<CardPlayedEvent>(OnCardPlayedForDSPassive);
        }

        private void OnDestroy()
        {
            if (_eventBus != null)
                _eventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayedForDSPassive);
        }

        public void StartBattle(int actID, int battleType)
        {
            if (CurrentPhase != BattlePhase.BattleEnd && CurrentPhase != default(BattlePhase))
            {
                Debug.LogWarning("[BattleStateMachine] 战斗已在进行中，无法重复启动");
                return;
            }
            _actID = actID;
            _battleType = battleType;
            EnterPhase(BattlePhase.BattleStart);
        }

        public void EndPlayerTurn()
        {
            if (CurrentPhase != BattlePhase.PlayerTurn) return;
            EnterPhase(BattlePhase.PlayerTurnEnd);
        }

        public void StartPlayerTurn()
        {
            if (CurrentPhase != BattlePhase.EnemyTurnEnd && CurrentPhase != BattlePhase.BattleStart) return;
            EnterPhase(BattlePhase.PlayerTurnStart);
        }

        public void StartEnemyTurn()
        {
            if (CurrentPhase != BattlePhase.PlayerTurnEnd) return;
            EnterPhase(BattlePhase.EnemyTurnStart);
        }

        public void EndEnemyTurn()
        {
            if (CurrentPhase != BattlePhase.EnemyTurn) return;
            EnterPhase(BattlePhase.EnemyTurnEnd);
        }

        public void EndBattle(bool playerVictory)
        {
            if (CurrentPhase == BattlePhase.BattleEnd) return;
            _pendingVictory = playerVictory;
            EnterPhase(BattlePhase.BattleEnd);
        }

        private void EnterPhase(BattlePhase phase)
        {
            CurrentPhase = phase;
            switch (phase)
            {
                case BattlePhase.BattleStart: ExecuteBattleStart(); break;
                case BattlePhase.PlayerTurnStart: ExecutePlayerTurnStart(); break;
                case BattlePhase.PlayerTurn: OnPlayerTurnEntered(); break;
                case BattlePhase.PlayerTurnEnd: ExecutePlayerTurnEnd(); break;
                case BattlePhase.EnemyTurnStart: ExecuteEnemyTurnStart(); break;
                case BattlePhase.EnemyTurn: ExecuteEnemyTurn(); break;
                case BattlePhase.EnemyTurnEnd: ExecuteEnemyTurnEnd(); break;
                case BattlePhase.BattleEnd:
                    bool victory = _pendingVictory ?? (_hero != null && !_hero.IsDead && _enemies.TrueForAll(e => e == null || e.IsDead));
                    _pendingVictory = null;
                    ExecuteBattleEnd(victory);
                    break;
            }
        }

        // ==================== 各阶段执行 ====================

        private void ExecuteBattleStart()
        {
            _eventBus.Publish(new BattleStartedEvent { ActID = _actID, BattleType = _battleType });

            GameStateManager.Instance?.ResetBattleState();
            PlayerTurnNumber = 0;
            EnemyTurnNumber = 0;
            _selectedEnemyActions.Clear();
            _enemies.RemoveAll(e => e == null);
            _dsAttackCounter = 0;

            _boostSystem = BoostSystem.Instance;
            if (_boostSystem == null)
            {
                var go = new GameObject("BoostSystem");
                _boostSystem = go.AddComponent<BoostSystem>();
            }

            _rewardSystem = CombatRewardSystem.Instance;
            if (_rewardSystem == null)
            {
                var go = new GameObject("CombatRewardSystem");
                _rewardSystem = go.AddComponent<CombatRewardSystem>();
            }

            var heroData = LoadHeroData(GameStateManager.Instance?.SelectedHeroID);
            _hero = HeroCombatEntity.Instance;
            if (_hero == null)
            {
                var go = new GameObject("HeroCombatEntity");
                _hero = go.AddComponent<HeroCombatEntity>();
            }
            _hero.Initialize(heroData, GameStateManager.Instance?.CurrentRun);

            SpawnEnemies(_actID, _battleType);

            _eventBus.Publish(new DeckShuffledEvent { SourcePile = "draw" });
            _eventBus.Publish(new RelicTriggeredEvent { RelicID = 0, Timing = RelicTriggerTiming.BattleStart });

            EnterPhase(BattlePhase.PlayerTurnStart);
        }

        private void ExecutePlayerTurnStart()
        {
            // 偏移逻辑：首回合设为 1，之后递增
            if (PlayerTurnNumber == 0)
                PlayerTurnNumber = 1;
            else
                PlayerTurnNumber++;

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.BattleState.PlayerTurn = PlayerTurnNumber;
                GameStateManager.Instance.BattleState.IsPlayerTurn = true;
            }

            // 清空回合标记
            if (GameStateManager.Instance != null)
            {
                var bs = GameStateManager.Instance.BattleState;
                bs.CardsPlayedThisTurn = 0;
                bs.AttackCardsPlayedThisTurn = 0;
                bs.DefenseCardsPlayedThisTurn = 0;
                bs.ZeroCostCardsPlayedThisTurn = 0;
                bs.CardsDiscardedThisTurn = 0;
                bs.CardsBanishedThisTurn = 0;
                bs.CardsExhaustedThisTurn = 0;
                bs.SpellCastThisTurn = false;
            }

            // 选择敌人行动
            SelectEnemyActions();

            // 规则制定者特殊逻辑
            ProcessRulemakerLogic();

            // 刷新能量（基础 3，遗物可能 4）
            int baseEnergy = 3;
            if (GameStateManager.Instance?.HasRelic(66) ?? false) baseEnergy = 4;
            _hero.SetMaxEnergy(baseEnergy);
            _hero.ResetEnergy();

            // 抽牌（基础 5，受 Debuff 影响）
            int drawCount = 5;
            if (_hero.HasStatusEffect(StatusEffectType.Amnesia)) drawCount -= 1;
            DrawCards(drawCount);

            // 英雄特性回调
            CallHeroPassiveLogic();

            // 执行回合开始效果
            _hero.ExecuteStartOfTurnEffects();

            // Bleed 结算
            _hero.DecayBleed();

            _eventBus.Publish(new PlayerTurnStartedEvent { TurnNumber = PlayerTurnNumber });
            _eventBus.Publish(new RelicTriggeredEvent { RelicID = 0, Timing = RelicTriggerTiming.PlayerTurnStart });

            EnterPhase(BattlePhase.PlayerTurn);
        }

        private void OnPlayerTurnEntered()
        {
            // 玩家可操作阶段，等待外部输入
        }

        private void ExecutePlayerTurnEnd()
        {
            _eventBus.Publish(new PlayerTurnEndedEvent { TurnNumber = PlayerTurnNumber });
            _eventBus.Publish(new RelicTriggeredEvent { RelicID = 0, Timing = RelicTriggerTiming.PlayerTurnEnd });

            // 弃掉未打出的手牌
            _eventBus.Publish(new CardDiscardedEvent { CardID = -1, CopyCount = 0, TargetPile = "discard_hand" });

            _hero.ExecuteEndOfTurnEffects();
            _boostSystem?.SetBoostOff();

            if (CheckBattleEnd()) return;
            EnterPhase(BattlePhase.EnemyTurnStart);
        }

        private void ExecuteEnemyTurnStart()
        {
            // 偏移逻辑
            if (EnemyTurnNumber == 0)
                EnemyTurnNumber = 1;
            else
                EnemyTurnNumber++;

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.BattleState.EnemyTurn = EnemyTurnNumber;
                GameStateManager.Instance.BattleState.IsPlayerTurn = false;
            }

            // 重置敌人 Block（Golem / Rocksolid 除外）
            foreach (var enemy in _enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                if (!enemy.HasTrait(EnemyTraitType.Rocksolid))
                    enemy.ResetBlock();
            }

            _eventBus.Publish(new EnemyTurnStartedEvent { TurnNumber = EnemyTurnNumber });
            _eventBus.Publish(new RelicTriggeredEvent { RelicID = 0, Timing = RelicTriggerTiming.EnemyTurnStart });

            EnterPhase(BattlePhase.EnemyTurn);
        }

        private void ExecuteEnemyTurn()
        {
            StartCoroutine(EnemyTurnCoroutine());
        }

        private IEnumerator EnemyTurnCoroutine()
        {
            foreach (var enemy in _enemies)
            {
                if (enemy == null || enemy.IsDead) continue;

                if (_selectedEnemyActions.TryGetValue(enemy.UniqueID, out var ability))
                {
                    enemy.ExecuteAbility(ability);
                    yield return new WaitForSeconds(1f);
                }

                if (_hero == null || _hero.IsDead)
                {
                    EndBattle(false);
                    yield break;
                }
            }

            EndEnemyTurn();
        }

        private void ExecuteEnemyTurnEnd()
        {
            foreach (var enemy in _enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                enemy.DecayChill();
                enemy.ClearBarrier();
            }

            _eventBus.Publish(new EnemyTurnEndedEvent { TurnNumber = EnemyTurnNumber });
            _eventBus.Publish(new RelicTriggeredEvent { RelicID = 0, Timing = RelicTriggerTiming.EnemyTurnEnd });

            if (CheckBattleEnd()) return;
            EnterPhase(BattlePhase.PlayerTurnStart);
        }

        private void ExecuteBattleEnd(bool playerVictory)
        {
            if (playerVictory)
            {
                bool isElite = _battleType == 2;
                bool isBoss = _battleType == 3;

                int gold = _rewardSystem.CalculateGoldReward(_actID, isElite, isBoss);
                GameStateManager.Instance?.AddGold(gold, "battle_reward");
                _rewardSystem.GenerateBattleRewards(_actID, isElite, isBoss);

                if (isBoss)
                {
                    var boss = _enemies.FirstOrDefault(e => e != null && e.Data != null && e.Data.IsBoss);
                    if (boss != null)
                    {
                        var spells = _rewardSystem.GenerateBossSpellRewards(boss.Data.EnemyID);
                        foreach (var spell in spells)
                            GameStateManager.Instance?.AddSpell(spell);
                    }
                }

                _eventBus.Publish(new BattleEndedEvent { PlayerVictory = true, RewardGold = gold });
                _boostSystem?.RechargeBoostAfterBattle();
            }
            else
            {
                _eventBus.Publish(new BattleEndedEvent { PlayerVictory = false, RewardGold = 0 });
                SceneTransitionManager.Instance?.TransitionTo("Scoreboard");
            }
        }

        // ==================== 辅助方法 ====================

        private bool CheckBattleEnd()
        {
            if (_hero != null && _hero.IsDead)
            {
                EndBattle(false);
                return true;
            }
            if (_enemies.Count > 0 && _enemies.All(e => e == null || e.IsDead))
            {
                EndBattle(true);
                return true;
            }
            return false;
        }

        private void SelectEnemyActions()
        {
            _selectedEnemyActions.Clear();
            foreach (var enemy in _enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                var ability = SelectAbilityForEnemy(enemy);
                if (ability != null)
                {
                    _selectedEnemyActions[enemy.UniqueID] = ability;
                    int preview = 0;
                    if (ability.BaseDamage > 0 && _hero != null)
                        preview = DamageCalculator.PreviewEnemyDamageToHero(ability.BaseDamage, _hero, enemy);
                    enemy.SetIntent(ability, preview);
                }
            }
        }

        private EnemyAbilityData SelectAbilityForEnemy(EnemyCombatEntity enemy)
        {
            if (enemy?.Data?.AIProfile?.Abilities == null || enemy.Data.AIProfile.Abilities.Count == 0)
                return null;

            var candidates = new List<EnemyAbilityData>();
            foreach (var ab in enemy.Data.AIProfile.Abilities)
            {
                if (CheckAbilityConditions(enemy, ab))
                    candidates.Add(ab);
            }

            if (candidates.Count == 0)
                return enemy.Data.AIProfile.Abilities[0];

            int totalWeight = 0;
            foreach (var c in candidates) totalWeight += c.BaseWeight;
            int roll = Random.Range(0, totalWeight);
            int current = 0;
            foreach (var c in candidates)
            {
                current += c.BaseWeight;
                if (roll < current) return c;
            }
            return candidates[candidates.Count - 1];
        }

        private bool CheckAbilityConditions(EnemyCombatEntity enemy, EnemyAbilityData ability)
        {
            if (ability.Conditions == null || ability.Conditions.Count == 0) return true;
            foreach (var cond in ability.Conditions)
            {
                bool pass = true;
                switch (cond.Type)
                {
                    case ConditionType.TurnNumber:
                        pass = EvaluateComparison(EnemyTurnNumber, cond.Value, cond.Comparison);
                        break;
                    case ConditionType.EnemyHPPercent:
                        pass = EvaluateComparison(enemy.CurrentHP * 100 / Mathf.Max(1, enemy.MaxHP), cond.Value, cond.Comparison);
                        break;
                    case ConditionType.PlayerHPPercent:
                        pass = _hero != null && EvaluateComparison(_hero.CurrentHP * 100 / Mathf.Max(1, _hero.MaxHP), cond.Value, cond.Comparison);
                        break;
                    case ConditionType.AllyCount:
                        pass = EvaluateComparison(_enemies.Count(e => e != null && !e.IsDead), cond.Value, cond.Comparison);
                        break;
                    case ConditionType.RandomChance:
                        pass = Random.value < (cond.Value / 100f);
                        break;
                    default:
                        pass = true;
                        break;
                }
                if (!pass) return false;
            }
            return true;
        }

        private bool EvaluateComparison(int left, int right, string op)
        {
            if (string.IsNullOrEmpty(op)) return left == right;
            switch (op)
            {
                case "==": return left == right;
                case "!=": return left != right;
                case ">": return left > right;
                case ">=": return left >= right;
                case "<": return left < right;
                case "<=": return left <= right;
                default: return left == right;
            }
        }

        private void ProcessRulemakerLogic()
        {
            bool hasRulemaker = _enemies.Any(e => e != null && !e.IsDead && e.Data != null && e.Data.EnemyID == 38);
            if (hasRulemaker && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.BattleState.CurrentRulemakerRule = "no_attack";
            }
        }

        private void CallHeroPassiveLogic()
        {
            string heroID = GameStateManager.Instance?.SelectedHeroID;
            if (string.IsNullOrEmpty(heroID)) return;

            if (heroID == "DS" || heroID == "DragonSlayer")
            {
                // DS被动通过 CardPlayedEvent 处理，见 OnCardPlayedForDSPassive
            }
            else if (heroID == "GM" || heroID == "GrandMage")
            {
                // GM被动：回合开始时获得1 Block（简化版）
                _hero?.GainBlock(1);
            }
            else if (heroID == "PG" || heroID == "PossessedGunslinger")
            {
                // PG被动：回合开始时抽1张牌
                DeckManager.Instance?.DrawCards(1);
            }
        }

        /// <summary>
        /// DS被动：每第3张打出的攻击牌获得1能量。
        /// </summary>
        private void OnCardPlayedForDSPassive(CardPlayedEvent evt)
        {
            string heroID = GameStateManager.Instance?.SelectedHeroID;
            if (heroID != "DS" && heroID != "DragonSlayer") return;

            var cardData = GetCardDataCached(evt.CardID);
            if (cardData != null && cardData.Type == StrayPathCore.Data.CardType.Attack)
            {
                _dsAttackCounter++;
                if (_dsAttackCounter >= 3)
                {
                    _dsAttackCounter = 0;
                    _hero?.GainEnergy(1);
                }
            }
        }

        private static StrayPathCore.Data.CardData GetCardDataCached(int cardID)
        {
            if (_cardDataCache == null)
            {
                _cardDataCache = new Dictionary<int, StrayPathCore.Data.CardData>();
                var all = Resources.LoadAll<StrayPathCore.Data.CardData>("");
                if (all != null)
                {
                    foreach (var cd in all)
                    {
                        if (cd != null && !_cardDataCache.ContainsKey(cd.CardID))
                            _cardDataCache[cd.CardID] = cd;
                    }
                }
            }
            _cardDataCache.TryGetValue(cardID, out var data);
            return data;
        }

        private void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _eventBus.Publish(new CardDrawnEvent { CardID = 0, CopyCount = 0, SourcePile = "draw" });
            }
        }

        private void SpawnEnemies(int actID, int battleType)
        {
            _enemies.RemoveAll(e => e == null);

            // 尝试从遭遇配置表中选择
            var encounter = SelectEncounterFromConfig(actID, battleType);
            if (encounter != null && encounter.Enemies != null && encounter.Enemies.Count > 0)
            {
                for (int i = 0; i < encounter.Enemies.Count; i++)
                {
                    var entry = encounter.Enemies[i];
                    if (entry?.EnemyData == null) continue;
                    var go = new GameObject($"Enemy_{entry.EnemyData.EnemyName}_{i}");
                    var enemy = go.AddComponent<EnemyCombatEntity>();
                    enemy.Initialize(entry.EnemyData, actID, battleType, false);
                    _enemies.Add(enemy);
                }
                return;
            }

            // 降级：从所有敌人数据中随机选择（遗留兼容）
            var allEnemies = Resources.LoadAll<EnemyData>("StrayPath/Data/Enemies");
            if (allEnemies == null || allEnemies.Length == 0)
            {
                Debug.LogWarning("[BattleStateMachine] 未找到敌人数据，无法生成敌人");
                return;
            }

            int enemyCount = battleType == 3 ? 1 : (battleType == 2 ? 1 : Random.Range(1, 3));
            for (int i = 0; i < enemyCount; i++)
            {
                var data = allEnemies[Random.Range(0, allEnemies.Length)];
                var go = new GameObject($"Enemy_{data.EnemyName}_{i}");
                var enemy = go.AddComponent<EnemyCombatEntity>();
                enemy.Initialize(data, actID, battleType, false);
                _enemies.Add(enemy);
            }
        }

        /// <summary>
        /// 从遭遇配置表中按Act/BattleType加权随机选择一组敌人。
        /// </summary>
        private StrayPathCore.Data.EnemyEncounterData SelectEncounterFromConfig(int actID, int battleType)
        {
            var allEncounters = Resources.LoadAll<StrayPathCore.Data.EnemyEncounterData>("StrayPath/Data/Encounters");
            if (allEncounters == null || allEncounters.Length == 0) return null;

            var candidates = new System.Collections.Generic.List<StrayPathCore.Data.EnemyEncounterData>();
            foreach (var enc in allEncounters)
            {
                if (enc == null) continue;
                if (enc.ActID != 0 && enc.ActID != actID) continue;
                if (enc.BattleType != battleType) continue;
                candidates.Add(enc);
            }

            if (candidates.Count == 0) return null;

            int totalWeight = 0;
            foreach (var c in candidates) totalWeight += c.SpawnWeight;
            int roll = Random.Range(0, totalWeight);
            int current = 0;
            foreach (var c in candidates)
            {
                current += c.SpawnWeight;
                if (roll < current) return c;
            }
            return candidates[candidates.Count - 1];
        }

        private HeroData LoadHeroData(string heroID)
        {
            if (string.IsNullOrEmpty(heroID)) return null;
            var all = Resources.LoadAll<HeroData>("StrayPath/Data/Heroes");
            if (all != null)
            {
                foreach (var h in all)
                {
                    if (h.HeroCode == heroID || h.ID.ToString() == heroID)
                        return h;
                }
            }
            return null;
        }
    }
}
