using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Combat;
using StrayPathCore.Core;
using StrayPathCore.Data;
using UnityEngine;

namespace StrayPathCore.Relic
{
    /// <summary>
    /// 遗物管理器 —— 维护遗物数据库、玩家遗物、效果执行与掉落逻辑。
    /// </summary>
    public class RelicManager : MonoBehaviour
    {
        public static RelicManager Instance { get; private set; }

        public List<RelicData> AllRelics { get; private set; } = new List<RelicData>();
        public List<RelicRuntime> PlayerRelics => GameStateManager.Instance.CurrentRun.Relics ?? new List<RelicRuntime>();

        [Header("Database")]
        [SerializeField] private List<RelicData> relicDatabase = new List<RelicData>();

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
            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            AllRelics = new List<RelicData>(relicDatabase);
        }

        // ==================== 玩家遗物管理 ====================

        public bool GiveRelicToHero(int relicID)
        {
            var data = AllRelics.Find(r => r.RelicID == relicID);
            if (data == null) return false;
            return GiveRelicToHero(data);
        }

        public bool GiveRelicToHero(RelicData data)
        {
            if (data == null) return false;
            // GenieRelicCurse: 拒绝获得遗物
            if (GameStateManager.Instance.CurrentRun.GenieRelicCurse)
                return false;
            // 检查是否已拥有
            if (HasRelic(data.RelicID)) return false;
            // 检查英雄等级锁
            if (!string.IsNullOrEmpty(data.RequiredHeroID))
            {
                if (GameStateManager.Instance.SelectedHeroID != data.RequiredHeroID)
                    return false;
            }
            int heroLevel = GameStateManager.Instance.GetHeroLevel(GameStateManager.Instance.SelectedHeroID);
            if (heroLevel < data.RequiredHeroLevel) return false;

            var relic = new RelicRuntime
            {
                RelicID = data.RelicID,
                IsActive = true,
                CurrentCharges = data.MaxCharges
            };

            // 特殊遗物计数器初始化
            if (data.RelicID == 77) relic.CurrentCharges = 2;
            if (data.RelicID == 79) relic.CurrentCharges = 2;
            if (data.RelicID == 301) relic.CurrentCharges = 3;

            GameStateManager.Instance.AddRelic(relic);
            return true;
        }

        public bool RemoveRelic(int relicID)
        {
            return GameStateManager.Instance.RemoveRelic(relicID);
        }

        public RelicRuntime GetPlayerRelic(int relicID)
        {
            return GameStateManager.Instance.GetRelic(relicID);
        }

        public bool HasRelic(int relicID)
        {
            return GameStateManager.Instance.HasRelic(relicID);
        }

        public void SetRelicActive(int relicID, bool active)
        {
            var relic = GetPlayerRelic(relicID);
            if (relic != null) relic.IsActive = active;
        }

        // ==================== 效果执行 ====================

        public void ExecuteRelicEffect(int relicID)
        {
            var relic = GetPlayerRelic(relicID);
            if (relic == null || !relic.IsActive) return;

            // 触发遗物效果事件
            GameEventBus.Instance.Publish(new RelicTriggeredEvent
            {
                RelicID = relicID,
                Timing = RelicTriggerTiming.CardPlayed
            });

            // 特殊遗物: 单场战斗一次
            var data = AllRelics.Find(r => r.RelicID == relicID);
            if (data != null && data.IsSingleUse)
            {
                SetRelicActive(relicID, false);
            }

            // 消耗Charges
            if (relic.CurrentCharges > 0)
            {
                relic.CurrentCharges--;
                if (relic.CurrentCharges <= 0)
                    SetRelicActive(relicID, false);
            }
        }

        // ==================== 掉落与生成 ====================

        public List<RelicData> Return3UniqueShopRelics(int actID)
        {
            var result = new List<RelicData>();
            var pool = GetAvailableRelicsForReward(actID, false);

            // 先尝试放入1个Rare
            float rareChance = actID * 0.05f;
            if (HasRelic(47)) rareChance += 0.3f; // Lucky Clover
            if (Random.value < rareChance)
            {
                var rares = pool.Where(r => r.Rarity == RelicRarity.Rare).ToList();
                if (rares.Count > 0)
                {
                    var pick = rares[Random.Range(0, rares.Count)];
                    result.Add(pick);
                    pool.Remove(pick);
                }
            }

            // 填充剩余位置
            while (result.Count < 3 && pool.Count > 0)
            {
                var pick = pool[Random.Range(0, pool.Count)];
                result.Add(pick);
                pool.Remove(pick);
            }

            return result;
        }

        public RelicData ReturnTreasureRelic()
        {
            var pool = GetAvailableRelicsForReward(GameStateManager.Instance.CurrentRun.Act, false);
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        public List<RelicData> GetAvailableRelicsForReward(int actID, bool isElite)
        {
            var owned = PlayerRelics.Select(r => r.RelicID).ToHashSet();
            return AllRelics.Where(r =>
                !owned.Contains(r.RelicID) &&
                r.Category != RelicCategory.TaintedGift &&
                (string.IsNullOrEmpty(r.RequiredHeroID) || r.RequiredHeroID == GameStateManager.Instance.SelectedHeroID) &&
                GameStateManager.Instance.GetHeroLevel(GameStateManager.Instance.SelectedHeroID) >= r.RequiredHeroLevel
            ).ToList();
        }

        public int GetRelicPrice(RelicData relic)
        {
            if (relic == null) return 0;
            int price = relic.BasePrice;
            // 浮动
            switch (relic.Rarity)
            {
                case RelicRarity.Common: price += Random.Range(-10, 6); break;
                case RelicRarity.Uncommon: price += Random.Range(-5, 11); break;
                case RelicRarity.Rare: price += Random.Range(-12, 13); break;
            }
            // 诅咒3: 价格上涨10%
            var curse = CurseSystem.Instance;
            if (curse != null)
                price = Mathf.RoundToInt(price * curse.GetShopPriceMultiplier());
            // 遗物81: 折扣35%
            if (HasRelic(81)) price = Mathf.RoundToInt(price * 0.65f);
            return Mathf.Max(1, price);
        }

        // ==================== 世界状态触发 ====================

        public void ExecuteWorldStateRelics(MapNodeType nodeType)
        {
            foreach (var relic in PlayerRelics.Where(r => r.IsActive))
            {
                var data = AllRelics.Find(r => r.RelicID == relic.RelicID);
                if (data == null) continue;
                foreach (var trigger in data.Triggers)
                {
                    if (trigger.Timing == RelicTriggerTiming.NodeEntered)
                    {
                        ExecuteRelicEffect(relic.RelicID);
                    }
                }
            }
        }

        // ==================== 战斗事件触发 ====================

        public void OnBattleStarted()
        {
            // 重置单场战斗一次的遗物
            foreach (var relic in PlayerRelics)
            {
                var data = AllRelics.Find(r => r.RelicID == relic.RelicID);
                if (data != null && data.IsSingleUse)
                    relic.IsActive = true;
            }
            TriggerAll(RelicTriggerTiming.BattleStart);
        }

        public void OnTurnStarted(string turnType)
        {
            var timing = turnType == "player" ? RelicTriggerTiming.PlayerTurnStart : RelicTriggerTiming.EnemyTurnStart;
            TriggerAll(timing);
        }

        public void OnTurnEnded(string turnType)
        {
            var timing = turnType == "player" ? RelicTriggerTiming.PlayerTurnEnd : RelicTriggerTiming.EnemyTurnEnd;
            TriggerAll(timing);
        }

        public void OnCardPlayed(CardRuntime card)
        {
            TriggerAll(RelicTriggerTiming.CardPlayed);
        }

        public void OnDamageTaken(int damage)
        {
            TriggerAll(RelicTriggerTiming.DamageTaken);
        }

        public void OnEnemyKilled(EnemyCombatEntity enemy)
        {
            TriggerAll(RelicTriggerTiming.EnemyKilled);
        }

        public void OnNodeEntered(MapNodeType nodeType)
        {
            TriggerAll(RelicTriggerTiming.NodeEntered);
        }

        public void OnCardDrawn(CardRuntime card)
        {
            TriggerAll(RelicTriggerTiming.CardDrawn);
        }

        public void OnEnergyChanged(int oldValue, int newValue)
        {
            TriggerAll(RelicTriggerTiming.EnergyChanged);
        }

        public void OnDeckShuffled()
        {
            TriggerAll(RelicTriggerTiming.DeckShuffled);
        }

        private void TriggerAll(RelicTriggerTiming timing)
        {
            foreach (var relic in PlayerRelics.Where(r => r.IsActive))
            {
                var data = AllRelics.Find(r => r.RelicID == relic.RelicID);
                if (data == null) continue;
                if (data.Triggers.Any(t => t.Timing == timing))
                {
                    ExecuteRelicEffect(relic.RelicID);
                }
            }
        }
    }
}
