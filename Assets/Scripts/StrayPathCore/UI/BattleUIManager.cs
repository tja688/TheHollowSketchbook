using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Deck;
using StrayPathCore.Combat;

namespace StrayPathCore.UI
{
    /// <summary>
    /// 战斗场景UI总控。
    /// 初始化并协调所有子UI模块，管理目标选择状态，处理战斗事件刷新。
    /// 纯表现层，所有状态从 GameStateManager / BattleStateMachine 读取。
    /// </summary>
    public class BattleUIManager : MonoBehaviour
    {
        public static BattleUIManager Instance { get; private set; }

        [Header("子模块引用（可为空，会自动查找）")]
        public PlayerHandDisplay HandDisplay;
        public EnergyDisplay EnergyDisplay;
        public EndTurnButton EndTurnButton;
        public HeroDisplay HeroDisplay;
        public BoostDisplay BoostDisplay;

        [Header("布局容器")]
        public RectTransform EnemyContainer;
        public RectTransform HeroContainer;
        public RectTransform HandContainer;
        public RectTransform EnergyContainer;
        public RectTransform EndTurnContainer;
        public RectTransform BoostContainer;

        private List<EnemyDisplay> _enemyDisplays = new List<EnemyDisplay>();
        private CardRuntime _pendingCard;
        private bool _isSelectingTarget;
        private GameEventBus _eventBus;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _eventBus = GameEventBus.Instance;
            EnsureEventSystem();
            EnsureContainers();
            EnsureSubModules();
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private void Start()
        {
            SubscribeEvents();
            RefreshAll();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this)
                Instance = null;
        }

        #region 事件订阅与取消订阅

        private void SubscribeEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
            _eventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            _eventBus.Subscribe<CardDiscardedEvent>(OnCardDiscarded);
            _eventBus.Subscribe<EnergyChangedEvent>(OnEnergyChanged);
            _eventBus.Subscribe<DamageTakenEvent>(OnDamageTaken);
            _eventBus.Subscribe<BlockGainedEvent>(OnBlockGained);
            _eventBus.Subscribe<HealEvent>(OnHeal);
            _eventBus.Subscribe<EnemyIntentDisplayedEvent>(OnEnemyIntentDisplayed);
            _eventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            _eventBus.Subscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
            _eventBus.Subscribe<PlayerTurnEndedEvent>(OnPlayerTurnEnded);
            _eventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            _eventBus.Subscribe<StatusEffectAppliedEvent>(OnStatusEffectChanged);
            _eventBus.Subscribe<StatusEffectRemovedEvent>(OnStatusEffectChanged);
            _eventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
        }

        private void UnsubscribeEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
            _eventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            _eventBus.Unsubscribe<CardDiscardedEvent>(OnCardDiscarded);
            _eventBus.Unsubscribe<EnergyChangedEvent>(OnEnergyChanged);
            _eventBus.Unsubscribe<DamageTakenEvent>(OnDamageTaken);
            _eventBus.Unsubscribe<BlockGainedEvent>(OnBlockGained);
            _eventBus.Unsubscribe<HealEvent>(OnHeal);
            _eventBus.Unsubscribe<EnemyIntentDisplayedEvent>(OnEnemyIntentDisplayed);
            _eventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            _eventBus.Unsubscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
            _eventBus.Unsubscribe<PlayerTurnEndedEvent>(OnPlayerTurnEnded);
            _eventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            _eventBus.Unsubscribe<StatusEffectAppliedEvent>(OnStatusEffectChanged);
            _eventBus.Unsubscribe<StatusEffectRemovedEvent>(OnStatusEffectChanged);
            _eventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        }

        #endregion

        #region 容器与子模块初始化

        private void EnsureContainers()
        {
            if (EnemyContainer == null)
                EnemyContainer = CreateContainer("EnemyContainer", new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.95f));
            if (HeroContainer == null)
                HeroContainer = CreateContainer("HeroContainer", new Vector2(0.02f, 0.05f), new Vector2(0.25f, 0.25f));
            if (HandContainer == null)
                HandContainer = CreateContainer("HandContainer", new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.2f));
            if (EnergyContainer == null)
                EnergyContainer = CreateContainer("EnergyContainer", new Vector2(0.42f, 0.22f), new Vector2(0.58f, 0.3f));
            if (EndTurnContainer == null)
                EndTurnContainer = CreateContainer("EndTurnContainer", new Vector2(0.85f, 0.05f), new Vector2(0.98f, 0.12f));
            if (BoostContainer == null)
                BoostContainer = CreateContainer("BoostContainer", new Vector2(0.02f, 0.85f), new Vector2(0.2f, 0.95f));
        }

        private RectTransform CreateContainer(string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private void EnsureSubModules()
        {
            if (HandDisplay == null)
            {
                HandDisplay = HandContainer.GetComponentInChildren<PlayerHandDisplay>();
                if (HandDisplay == null)
                {
                    var go = new GameObject("PlayerHandDisplay", typeof(RectTransform));
                    go.transform.SetParent(HandContainer, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    HandDisplay = go.AddComponent<PlayerHandDisplay>();
                }
            }
            if (EnergyDisplay == null)
            {
                EnergyDisplay = EnergyContainer.GetComponentInChildren<EnergyDisplay>();
                if (EnergyDisplay == null)
                {
                    var go = new GameObject("EnergyDisplay", typeof(RectTransform));
                    go.transform.SetParent(EnergyContainer, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    EnergyDisplay = go.AddComponent<EnergyDisplay>();
                }
            }
            if (EndTurnButton == null)
            {
                EndTurnButton = EndTurnContainer.GetComponentInChildren<EndTurnButton>();
                if (EndTurnButton == null)
                {
                    var go = new GameObject("EndTurnButton", typeof(RectTransform));
                    go.transform.SetParent(EndTurnContainer, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    EndTurnButton = go.AddComponent<EndTurnButton>();
                }
            }
            if (HeroDisplay == null)
            {
                HeroDisplay = HeroContainer.GetComponentInChildren<HeroDisplay>();
                if (HeroDisplay == null)
                {
                    var go = new GameObject("HeroDisplay", typeof(RectTransform));
                    go.transform.SetParent(HeroContainer, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    HeroDisplay = go.AddComponent<HeroDisplay>();
                }
            }
            if (BoostDisplay == null)
            {
                BoostDisplay = BoostContainer.GetComponentInChildren<BoostDisplay>();
                if (BoostDisplay == null)
                {
                    var go = new GameObject("BoostDisplay", typeof(RectTransform));
                    go.transform.SetParent(BoostContainer, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    BoostDisplay = go.AddComponent<BoostDisplay>();
                }
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 刷新所有UI模块。
        /// </summary>
        public void RefreshAll()
        {
            HandDisplay?.RefreshHand();
            EnergyDisplay?.Refresh();
            BoostDisplay?.Refresh();
            RefreshHero();
            RefreshEnemies();
        }

        /// <summary>
        /// 卡牌点击入口：判断是否需要目标，进入目标选择模式或直接出牌。
        /// </summary>
        public void OnCardClicked(CardRuntime card)
        {
            if (card == null) return;
            var data = GetCardData(card.CardID);
            bool needsTarget = data?.TargetsEnemy ?? false;

            if (needsTarget)
            {
                _pendingCard = card;
                _isSelectingTarget = true;
                foreach (var ed in _enemyDisplays)
                    ed?.SetHighlight(true);
            }
            else
            {
                DeckManager.Instance?.PlayCard(card, null);
            }
        }

        /// <summary>
        /// 敌人点击入口：若在目标选择模式中，则打出待处理的卡牌。
        /// </summary>
        public void OnEnemyClicked(string enemyUID)
        {
            if (_isSelectingTarget && _pendingCard != null)
            {
                DeckManager.Instance?.PlayCard(_pendingCard, enemyUID);
                CancelTargetSelection();
            }
        }

        /// <summary>
        /// 取消目标选择状态。
        /// </summary>
        public void CancelTargetSelection()
        {
            _isSelectingTarget = false;
            _pendingCard = null;
            foreach (var ed in _enemyDisplays)
                ed?.SetHighlight(false);
        }

        #endregion

        #region 私有刷新方法

        private void RefreshHero()
        {
            var hero = BattleStateMachine.Instance?.GetHero();
            if (hero != null)
            {
                HeroDisplay?.Bind(hero);
                HeroDisplay?.Refresh();
            }
        }

        private void RefreshEnemies()
        {
            foreach (var ed in _enemyDisplays)
            {
                if (ed != null && ed.gameObject != null)
                    Destroy(ed.gameObject);
            }
            _enemyDisplays.Clear();

            var enemies = BattleStateMachine.Instance?.GetAllEnemies();
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                CreateEnemyDisplay(enemy);
            }
        }

        private void CreateEnemyDisplay(EnemyCombatEntity enemy)
        {
            var go = new GameObject($"EnemyDisplay_{enemy.UniqueID}", typeof(RectTransform));
            go.transform.SetParent(EnemyContainer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 200);
            var display = go.AddComponent<EnemyDisplay>();
            display.Bind(enemy);
            display.OnClicked = OnEnemyClicked;
            _enemyDisplays.Add(display);
        }

        private static Dictionary<int, CardData> _cardDataCache;

        private static CardData GetCardData(int cardID)
        {
            if (_cardDataCache == null)
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
            _cardDataCache.TryGetValue(cardID, out var data);
            return data;
        }

        #endregion

        #region 事件处理

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            HandDisplay?.RefreshHand();
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            HandDisplay?.RemoveCard(evt.CardID, evt.CopyCount);
            CancelTargetSelection();
        }

        private void OnCardDiscarded(CardDiscardedEvent evt)
        {
            // 批量弃牌（回合结束）或单张弃牌
            if (evt.CardID < 0 && string.IsNullOrEmpty(evt.TargetPile) == false && evt.TargetPile.Contains("hand"))
            {
                HandDisplay?.ClearHand();
            }
            else
            {
                HandDisplay?.RemoveCard(evt.CardID, evt.CopyCount);
            }
        }

        private void OnEnergyChanged(EnergyChangedEvent evt)
        {
            EnergyDisplay?.Refresh();
        }

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            if (evt.TargetUID == "hero")
            {
                HeroDisplay?.Refresh();
            }
            else
            {
                foreach (var ed in _enemyDisplays)
                {
                    if (ed != null)
                        ed.Refresh();
                }
            }
        }

        private void OnBlockGained(BlockGainedEvent evt)
        {
            if (evt.TargetUID == "hero")
                HeroDisplay?.Refresh();
            else
            {
                foreach (var ed in _enemyDisplays)
                {
                    if (ed != null)
                        ed.Refresh();
                }
            }
        }

        private void OnHeal(HealEvent evt)
        {
            if (evt.TargetUID == "hero")
                HeroDisplay?.Refresh();
            else
            {
                foreach (var ed in _enemyDisplays)
                {
                    if (ed != null)
                        ed.Refresh();
                }
            }
        }

        private void OnEnemyIntentDisplayed(EnemyIntentDisplayedEvent evt)
        {
            foreach (var ed in _enemyDisplays)
            {
                if (ed != null)
                    ed.RefreshIntent();
            }
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            // 延迟一帧刷新，避免在事件回调中立即销毁导致的问题
            StartCoroutine(DelayedEnemyRefresh());
        }

        private System.Collections.IEnumerator DelayedEnemyRefresh()
        {
            yield return null;
            RefreshEnemies();
        }

        private void OnPlayerTurnStarted(PlayerTurnStartedEvent evt)
        {
            HandDisplay?.RefreshHand();
            EnergyDisplay?.Refresh();
            HeroDisplay?.Refresh();
            BoostDisplay?.Refresh();
            foreach (var ed in _enemyDisplays)
            {
                if (ed != null)
                    ed.Refresh();
            }
        }

        private void OnPlayerTurnEnded(PlayerTurnEndedEvent evt)
        {
            CancelTargetSelection();
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            RefreshAll();
        }

        private void OnStatusEffectChanged(StatusEffectAppliedEvent evt)
        {
            if (evt.TargetUID == "hero")
                HeroDisplay?.Refresh();
        }

        private void OnStatusEffectChanged(StatusEffectRemovedEvent evt)
        {
            if (evt.TargetUID == "hero")
                HeroDisplay?.Refresh();
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            HandDisplay?.ClearHand();
        }

        #endregion
    }
}
