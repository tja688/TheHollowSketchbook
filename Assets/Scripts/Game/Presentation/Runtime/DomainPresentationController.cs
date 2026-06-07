using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Game.Core;
using Game.Core.Domain;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Inventory;
using Game.Core.Domain.Progression;
using Game.Core.Domain.Rooms;
using Game.Core.Models;
using Game.Core.Rooms;
using Game.Presentation.Runtime.Playtest;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.Runtime
{
    public sealed class DomainPresentationController : MonoBehaviour
    {
        [Header("Effect Panel (Odin)")]
        [SerializeField] private PresentationEffectPanel _effectPanel;

        [Header("Boot")]
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _startOnEnable = true;

        [Header("View Roots")]
        [SerializeField] private Transform _uiRoot;
        [SerializeField] private Transform _gridRoot;
        [SerializeField] private Canvas _detailPanel;
        [SerializeField] private Canvas _playerPanel;
        [SerializeField] private Transform _relicRoot;
        [SerializeField] private Transform _choiceRoot;
        [SerializeField] private Canvas _simpleUiCanvas;
        [SerializeField] private Canvas _activeRelicSlotCanvas;
        [SerializeField] private Canvas _passiveRelicSlotCanvas;
        [SerializeField] private TextMeshProUGUI _detailTitleText;
        [SerializeField] private TextMeshProUGUI _detailBodyText;
        [SerializeField] private TextMeshProUGUI _playerRoleText;
        [SerializeField] private TextMeshProUGUI _playerHealthText;
        [SerializeField] private TextMeshProUGUI _playerGoldText;

        [Header("Prefabs")]
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private Button _simpleActionButtonPrefab;

        [Header("Visual Tuning")]
        [SerializeField] private float _moveDuration = 0.38f;
        [SerializeField] private float _flipDuration = 0.34f;
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private float _hitPunchDuration = 0.22f;
        [SerializeField] private float _hitPunchStrength = 0.18f;
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private Color _faceDownColor = new Color(0.22f, 0.18f, 0.16f, 0.92f);
        [SerializeField] private Color _playerColor = new Color(0.90f, 0.95f, 0.78f, 0.95f);
        [SerializeField] private Color _monsterColor = new Color(0.76f, 0.34f, 0.30f, 0.95f);
        [SerializeField] private Color _trapColor = new Color(0.52f, 0.32f, 0.22f, 0.95f);
        [SerializeField] private Color _itemColor = new Color(0.42f, 0.60f, 0.86f, 0.95f);
        [SerializeField] private Color _goldColor = new Color(0.92f, 0.78f, 0.26f, 0.95f);
        [SerializeField] private Color _choiceColor = new Color(0.48f, 0.76f, 0.58f, 0.95f);
        [SerializeField] private Color _specialColor = new Color(0.74f, 0.58f, 0.88f, 0.95f);
        [SerializeField] private Color _relicColor = new Color(0.90f, 0.68f, 0.36f, 0.95f);
        [SerializeField] private Color _previewValidColor = new Color(0.58f, 0.88f, 0.58f, 1f);
        [SerializeField] private Color _previewInvalidColor = new Color(0.95f, 0.34f, 0.34f, 1f);
        [SerializeField] private Color _idleOutlineColor = new Color(0f, 0f, 0f, 0.35f);

        private const float HiddenZ = -0.012f;
        private const float StackZStep = -0.00055f;

        /// <summary>
        /// Delegate that creates a DomainActionContext from a seed.
        /// Must be assigned before StartPresentation is called.
        /// Typically wired by a Content-layer bootstrap that calls StarterContentRegistry.StartNewRun(seed).
        /// </summary>
        public Func<int, DomainActionContext> CreateRunContext { get; set; }

        private PresentationPlaytestConfig _config;
        private DomainActionContext _context;
        private DomainFacade _facade;
        private readonly Dictionary<int, Canvas> _gridCanvasesByCell = new Dictionary<int, Canvas>(9);
        private readonly Dictionary<int, Vector3> _gridWorldPositionsByCell = new Dictionary<int, Vector3>(9);
        private readonly Dictionary<CardInstanceId, CardView> _cardViews = new Dictionary<CardInstanceId, CardView>();
        private readonly List<CardView> _routeChoiceViews = new List<CardView>();
        private readonly List<Canvas> _choiceSlotCanvases = new List<Canvas>(3);
        private readonly List<CardView> _choiceCardViews = new List<CardView>(3);
        private readonly Queue<DomainEventBatch> _pendingBatches = new Queue<DomainEventBatch>();
        private readonly List<ChoiceButtonView> _choiceButtons = new List<ChoiceButtonView>();
        private readonly Dictionary<CardView, VirtualDetail> _virtualDetails = new Dictionary<CardView, VirtualDetail>();

        private bool _isReady;
        private bool _isAnimating;
        private bool _isChoosingTarget;
        private bool _isChoosingRelicTarget;
        private bool _isChoosingChoice;
        private bool _pendingAutoStart;
        private bool _preferCombatCamera;
        private InventorySlot _pendingItemSlot;
        private ModelId _pendingRelicId;
        private ItemTargetMode _pendingTargetMode;
        private CardInstanceId? _pendingPrimaryCard;
        private string _pendingChoiceSessionId;
        private int _lastConsumedBatchCount;
        private int _passiveRelicDisplayIndex;
        private CardView _hoveredView;
        private Button _restartRunButton;
        private Button _cancelSelectionButton;
        private TextMeshProUGUI _restartRunLabel;
        private TextMeshProUGUI _cancelSelectionLabel;
        private Component _globalCamera;
        private Component _combatCamera;
        private Component _choiceCamera;
        private Component _relicTargetCamera;
        private LineRenderer _dragLine;
        private bool _isDraggingPlayerCard;
        private bool _dragThresholdExceeded;
        private CardView _draggingCardView;
        private CardInstance _draggingCard;
        private int _dragHoveredCellIndex;
        private static TMP_FontAsset s_runtimeChineseFallback;

        public bool IsRunning => _isReady;

        private void OnEnable()
        {
            LoadConfig();
            ApplyConfig();
            if (_startOnEnable)
            {
                TryStartPresentationOrDefer();
            }
        }

        [ContextMenu("Start Presentation")]
        public void StartPresentation()
        {
            LoadConfig();
            ApplyConfig();
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            EnsureBindings();
            EnsureRuntimeFontFallbacks();
            BuildGridLookup();
            EnsureCellClickProxies();
            EnsureChoiceSlots();
            EnsureSimpleActionButtons();
            EnsureCameraBindings();
            ClearAllRuntimeViews();

            if (CreateRunContext == null)
            {
                _pendingAutoStart = true;
                Debug.LogWarning("[DomainPresentationController] CreateRunContext delegate is not assigned yet. Waiting for bootstrap.");
                return;
            }

            _pendingAutoStart = false;
            _context = CreateRunContext(_seed);
            _facade = new DomainFacade(_context);
            _lastConsumedBatchCount = _context.Batches.Count;
            _isReady = true;
            _isAnimating = false;
            _pendingBatches.Clear();
            CancelDrag();
            CancelSelectionModes();

            EnsureInitialReveal();
            FullRefreshImmediate();
            EnqueuePendingBatches();
            PlayQueuedBatchesIfIdle();
            RefreshSimpleUiState();
            HideDragLine();
            ApplyCameraMode();
        }

        private void Update()
        {
            if (!_isReady)
            {
                if (_pendingAutoStart && CreateRunContext != null)
                {
                    StartPresentation();
                }

                RefreshSimpleUiState();
                return;
            }

            EnqueuePendingBatches();
            if (!_isAnimating)
            {
                PlayQueuedBatchesIfIdle();
            }

            UpdateHoverDetail();
            HandleScrollShortcuts();
            HandleDragUpdate();
            RefreshSimpleUiState();
            ApplyCameraMode();
        }

        private void LoadConfig()
        {
            _config = Resources.Load<PresentationPlaytestConfig>(PresentationPlaytestConfig.ResourcesPath);
            if (_effectPanel == null)
            {
                _effectPanel = Resources.Load<PresentationEffectPanel>(PresentationEffectPanel.ResourcesPath);
            }
        }

        private void ApplyConfig()
        {
            if (_config != null)
            {
                _startOnEnable = _config.autoStartOnEnable;
                _seed = _config.seed;
                _moveDuration = _config.moveDuration;
                _flipDuration = _config.flipDuration;
                _fadeDuration = _config.fadeDuration;
                _hitPunchDuration = _config.hitPunchDuration;
                _hitPunchStrength = _config.hitPunchStrength;
                _hoverScale = _config.hoverScale;
                _faceDownColor = _config.faceDownColor;
                _playerColor = _config.playerColor;
                _monsterColor = _config.monsterColor;
                _trapColor = _config.trapColor;
                _itemColor = _config.itemColor;
                _goldColor = _config.goldColor;
                _choiceColor = _config.routeColor;
                _specialColor = _config.specialColor;
                _relicColor = _config.relicColor;
                _previewValidColor = _config.previewValidColor;
                _previewInvalidColor = _config.previewInvalidColor;
                _idleOutlineColor = _config.outlineIdleColor;
            }

            // Effect Panel overrides basic config when present.
            if (_effectPanel != null)
            {
                _startOnEnable = _effectPanel.autoStartOnEnable;
                _seed = _effectPanel.seed;
                _moveDuration = _effectPanel.moveDuration;
                _flipDuration = _effectPanel.flipDuration;
                _fadeDuration = _effectPanel.fadeDuration;
                _hitPunchDuration = _effectPanel.hitPunchDuration;
                _hitPunchStrength = _effectPanel.hitPunchStrength;
                _hoverScale = _effectPanel.hoverScale;
                _faceDownColor = _effectPanel.faceDownColor;
                _playerColor = _effectPanel.playerColor;
                _monsterColor = _effectPanel.monsterColor;
                _trapColor = _effectPanel.trapColor;
                _itemColor = _effectPanel.itemColor;
                _goldColor = _effectPanel.goldColor;
                _choiceColor = _effectPanel.routeColor;
                _specialColor = _effectPanel.specialColor;
                _relicColor = _effectPanel.relicColor;
                _previewValidColor = _effectPanel.previewValidColor;
                _previewInvalidColor = _effectPanel.previewInvalidColor;
                _idleOutlineColor = _effectPanel.outlineIdleColor;
                _effectPanel.LinkController(this);
            }
        }

        private void EnsureBindings()
        {
            if (_uiRoot == null)
            {
                _uiRoot = FindSceneTransformExact("UI");
            }

            if (_gridRoot == null)
            {
                _gridRoot = FindDescendantByNameContains(_uiRoot, "九宫场地格");
            }

            if (_detailPanel == null)
            {
                _detailPanel = FindCanvasByNameContains(_uiRoot, "具体信息面板");
                _detailPanel ??= FindCanvasByNameContains(_uiRoot, "详情面板");
            }

            if (_playerPanel == null)
            {
                _playerPanel = FindCanvasByNameContains(_uiRoot, "玩家信息面板");
            }

            if (_relicRoot == null)
            {
                _relicRoot = FindDescendantByNameContains(_uiRoot, "遗物格");
            }

            if (_choiceRoot == null)
            {
                _choiceRoot = FindDescendantByNameContains(_uiRoot, "三选一遗物格");
            }

            if (_simpleUiCanvas == null)
            {
                _simpleUiCanvas = FindCanvasByNameContains(_uiRoot, "开始、重开、结束游戏");
                _simpleUiCanvas ??= FindCanvasByNameContains(_uiRoot, "简易UI");
                _simpleUiCanvas ??= FindCanvasByNameContains(_uiRoot, "结束游戏锚点");
            }

            if (_activeRelicSlotCanvas == null && _relicRoot != null)
            {
                _activeRelicSlotCanvas = FindCanvasByNameContains(_relicRoot, "主动遗物格");
            }

            if (_passiveRelicSlotCanvas == null && _relicRoot != null)
            {
                _passiveRelicSlotCanvas = FindCanvasByNameContains(_relicRoot, "被动遗物格");
            }

            if (_detailTitleText == null && _detailPanel != null)
            {
                _detailTitleText = FindTextByNameContains(_detailPanel.transform, "标题");
            }

            if (_detailBodyText == null && _detailPanel != null)
            {
                _detailBodyText = FindTextByNameContains(_detailPanel.transform, "内容");
            }

            if (_playerRoleText == null && _playerPanel != null)
            {
                _playerRoleText = FindTextByNameContains(_playerPanel.transform, "角色");
            }

            if (_playerHealthText == null && _playerPanel != null)
            {
                _playerHealthText = FindTextByNameContains(_playerPanel.transform, "生命值");
            }

            if (_playerGoldText == null && _playerPanel != null)
            {
                _playerGoldText = FindTextByNameContains(_playerPanel.transform, "金币");
            }

            if (_cardPrefab == null && _gridRoot != null)
            {
                Transform sampleCard = FindDescendantByNameContains(_gridRoot, "标准卡面模板");
                sampleCard ??= FindDescendantByNameContains(_gridRoot, "标准卡牌模板");
                if (sampleCard != null)
                {
                    _cardPrefab = sampleCard.gameObject;
                }
            }

            if (_simpleActionButtonPrefab == null && _simpleUiCanvas != null)
            {
                _simpleActionButtonPrefab = FindButtonByNameContains(_simpleUiCanvas.transform, "标准Button");
                _simpleActionButtonPrefab ??= FindButtonByNameContains(_simpleUiCanvas.transform, "标准按钮");
            }

            if (_restartRunButton == null && _simpleUiCanvas != null)
            {
                _restartRunButton = FindButtonByNameContains(_simpleUiCanvas.transform, "开始演出按钮");
            }

            if (_cancelSelectionButton == null && _simpleUiCanvas != null)
            {
                _cancelSelectionButton = FindButtonByNameContains(_simpleUiCanvas.transform, "取消选择按钮");
            }

            if (_restartRunLabel == null && _restartRunButton != null)
            {
                _restartRunLabel = FindButtonLabel(_restartRunButton.transform);
            }

            if (_cancelSelectionLabel == null && _cancelSelectionButton != null)
            {
                _cancelSelectionLabel = FindButtonLabel(_cancelSelectionButton.transform);
            }

            if (_dragLine == null)
            {
                Transform lrTransform = FindDescendantByNameContains(_uiRoot, "Line Renderer");
                if (lrTransform != null)
                {
                    _dragLine = lrTransform.GetComponent<LineRenderer>();
                }
            }
        }

        private void EnsureChoiceSlots()
        {
            _choiceSlotCanvases.Clear();
            if (_choiceRoot == null)
            {
                return;
            }

            TryAddChoiceSlot("一号位");
            TryAddChoiceSlot("二号位");
            TryAddChoiceSlot("三号位");
        }

        private void TryAddChoiceSlot(string nameFragment)
        {
            Canvas canvas = FindCanvasByNameContains(_choiceRoot, nameFragment);
            if (canvas != null)
            {
                _choiceSlotCanvases.Add(canvas);
            }
        }

        private void EnsureRuntimeFontFallbacks()
        {
            if (s_runtimeChineseFallback == null)
            {
                TMP_FontAsset sourceFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/MaShanZheng SDF");
                if (sourceFontAsset == null && _cardPrefab != null)
                {
                    sourceFontAsset = _cardPrefab.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
                }

                Font sourceFont = sourceFontAsset != null ? sourceFontAsset.sourceFontFile : null;
                if (sourceFont != null)
                {
                    s_runtimeChineseFallback = TMP_FontAsset.CreateFontAsset(
                        sourceFont,
                        90,
                        9,
                        UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                        1024,
                        1024,
                        AtlasPopulationMode.Dynamic,
                        true);
                    s_runtimeChineseFallback.name = "[Runtime] Chinese Fallback";
                    s_runtimeChineseFallback.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    if (s_runtimeChineseFallback.material != null)
                    {
                        s_runtimeChineseFallback.material.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    }

                    if (s_runtimeChineseFallback.atlasTextures != null)
                    {
                        for (int i = 0; i < s_runtimeChineseFallback.atlasTextures.Length; i++)
                        {
                            if (s_runtimeChineseFallback.atlasTextures[i] != null)
                            {
                                s_runtimeChineseFallback.atlasTextures[i].hideFlags = HideFlags.DontUnloadUnusedAsset;
                            }
                        }
                    }
                }
            }

            if (s_runtimeChineseFallback == null)
            {
                return;
            }

            AddFallbackFont(TMP_Settings.defaultFontAsset, s_runtimeChineseFallback);
            ApplyFallbackToTexts(_uiRoot, s_runtimeChineseFallback);
            if (_cardPrefab != null)
            {
                ApplyFallbackToTexts(_cardPrefab.transform, s_runtimeChineseFallback);
            }
        }

        private static void ApplyFallbackToTexts(Transform root, TMP_FontAsset fallback)
        {
            if (root == null || fallback == null)
            {
                return;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                AddFallbackFont(text.font, fallback);
                text.SetAllDirty();
            }
        }

        private static void AddFallbackFont(TMP_FontAsset target, TMP_FontAsset fallback)
        {
            if (target == null || fallback == null || target == fallback)
            {
                return;
            }

            target.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (target.fallbackFontAssetTable.Contains(fallback))
            {
                return;
            }

            target.fallbackFontAssetTable.Add(fallback);
        }

        private void EnsureSimpleActionButtons()
        {
            if (_simpleUiCanvas == null)
            {
                return;
            }

            if (_restartRunButton == null)
            {
                _restartRunButton = CreateSimpleActionButton(_simpleUiCanvas.transform, "开始演出按钮（运行时兜底）", new Vector2(-150f, -72f), out _restartRunLabel);
            }
            else
            {
                _restartRunLabel ??= FindButtonLabel(_restartRunButton.transform);
            }

            _restartRunButton.onClick.RemoveAllListeners();
            _restartRunButton.onClick.AddListener(StartPresentation);

            if (_cancelSelectionButton == null)
            {
                _cancelSelectionButton = CreateSimpleActionButton(_simpleUiCanvas.transform, "取消选择按钮（运行时兜底）", new Vector2(150f, -72f), out _cancelSelectionLabel);
            }
            else
            {
                _cancelSelectionLabel ??= FindButtonLabel(_cancelSelectionButton.transform);
            }

            if (_cancelSelectionLabel != null)
            {
                _cancelSelectionLabel.text = "取消选择";
            }

            _cancelSelectionButton.onClick.RemoveAllListeners();
            _cancelSelectionButton.onClick.AddListener(() =>
            {
                CancelSelectionModes();
                RefreshDetailPanelDefault();
                ApplyCameraMode();
            });
        }

        private void EnsureCameraBindings()
        {
            _globalCamera = FindCameraComponentByBestMatch("玩家位置全局视角（默认）", "全局视角（默认）", "玩家位置全局视角");
            _combatCamera = FindCameraComponentByBestMatch("专注战斗场地的竖向视角（可跟全局视角切换）", "专注战斗场地", "竖向视角");
            _choiceCamera = FindCameraComponentByBestMatch("三选一的时候专属摄视角", "三选一");
            _relicTargetCamera = FindCameraComponentByBestMatch("主动遗物时斜向风格专属视角", "主动遗物");
        }

        private void TryStartPresentationOrDefer()
        {
            if (CreateRunContext != null)
            {
                StartPresentation();
                return;
            }

            _pendingAutoStart = true;
        }

        private Transform FindSceneTransformExact(string name)
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.name == name)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform FindDescendantByNameContains(Transform root, string nameFragment)
        {
            if (root == null || string.IsNullOrWhiteSpace(nameFragment))
            {
                return null;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.Contains(nameFragment, StringComparison.Ordinal))
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static Canvas FindCanvasByNameContains(Transform root, string nameFragment)
        {
            Transform found = FindDescendantByNameContains(root, nameFragment);
            return found != null ? found.GetComponent<Canvas>() : null;
        }

        private static TextMeshProUGUI FindTextByNameContains(Transform root, string nameFragment)
        {
            Transform found = FindDescendantByNameContains(root, nameFragment);
            return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Button FindButtonByNameContains(Transform root, string nameFragment)
        {
            Transform found = FindDescendantByNameContains(root, nameFragment);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static TextMeshProUGUI FindButtonLabel(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            return labels.Length > 0 ? labels[0] : null;
        }

        private static Component FindCameraComponentByBestMatch(params string[] preferredNames)
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int nameIndex = 0; nameIndex < preferredNames.Length; nameIndex++)
            {
                string preferredName = preferredNames[nameIndex];
                if (string.IsNullOrWhiteSpace(preferredName))
                {
                    continue;
                }

                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform candidate = transforms[i];
                    if (candidate == null || !string.Equals(candidate.name, preferredName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Component exactComponent = FindVirtualCameraComponent(candidate);
                    if (exactComponent != null)
                    {
                        return exactComponent;
                    }
                }
            }

            for (int nameIndex = 0; nameIndex < preferredNames.Length; nameIndex++)
            {
                string preferredName = preferredNames[nameIndex];
                if (string.IsNullOrWhiteSpace(preferredName))
                {
                    continue;
                }

                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform candidate = transforms[i];
                    if (candidate == null || !candidate.name.Contains(preferredName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Component fuzzyComponent = FindVirtualCameraComponent(candidate);
                    if (fuzzyComponent != null)
                    {
                        return fuzzyComponent;
                    }
                }
            }

            return null;
        }

        private static Component FindVirtualCameraComponent(Transform candidate)
        {
            Component[] components = candidate.GetComponents<Component>();
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                Component component = components[componentIndex];
                if (component != null && component.GetType().FullName == "Cinemachine.CinemachineVirtualCamera")
                {
                    return component;
                }
            }

            return null;
        }

        private void BuildGridLookup()
        {
            _gridCanvasesByCell.Clear();
            _gridWorldPositionsByCell.Clear();
            if (_gridRoot == null)
            {
                return;
            }

            for (int i = 1; i <= 9; i++)
            {
                Transform child = _gridRoot.Find("格" + i);
                if (child == null)
                {
                    continue;
                }

                Canvas canvas = child.GetComponent<Canvas>();
                if (canvas == null)
                {
                    continue;
                }

                _gridCanvasesByCell[i] = canvas;
                _gridWorldPositionsByCell[i] = canvas.transform.position;
            }
        }

        private void EnsureCellClickProxies()
        {
            if (_gridRoot == null)
            {
                return;
            }

            for (int i = 1; i <= 9; i++)
            {
                Transform child = _gridRoot.Find("格" + i);
                if (child == null)
                {
                    continue;
                }

                Image bgImage = child.GetComponent<Image>();
                if (bgImage == null)
                {
                    bgImage = child.gameObject.AddComponent<Image>();
                    bgImage.color = new Color(0f, 0f, 0f, 0.01f);
                    bgImage.raycastTarget = true;
                }

                GridCellClickProxy proxy = child.GetComponent<GridCellClickProxy>();
                if (proxy == null)
                {
                    proxy = child.gameObject.AddComponent<GridCellClickProxy>();
                }

                proxy.Initialize(this, i);
            }
        }

        private void EnsureInitialReveal()
        {
            if (_context == null || _context.Grid == null)
            {
                return;
            }

            GridCoord playerCoord = GridCoord.FromCellIndex(8);
            GridOperationResult reveal = _context.Grid.RevealAround(playerCoord, FlipReason.PlayerAdjacentReveal);
            if (reveal != null && reveal.Succeeded && reveal.Events.Count > 0)
            {
                DomainEventBatch batch = new DomainEventBatch(0, null);
                batch.AddRange(reveal.Events);
                _context.Batches.Add(batch);
            }
        }

        private void FullRefreshImmediate()
        {
            RebuildGridViewsImmediate();
            RefreshAllCardLabels();
            RefreshHudTexts();
            RefreshDetailPanelDefault();
            RebuildChoiceButtons();
            RefreshRelicViews();
            RefreshSimpleUiState();
            ApplyCameraMode();
        }

        private void ClearAllRuntimeViews()
        {
            foreach (CardView view in _cardViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            foreach (ChoiceButtonView button in _choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            foreach (CardView view in _choiceCardViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            _cardViews.Clear();
            _choiceButtons.Clear();
            _choiceCardViews.Clear();
            _virtualDetails.Clear();
            _routeChoiceViews.Clear();

            if (_activeRelicSlotCanvas != null)
            {
                ClearChildrenExceptCardViews(_activeRelicSlotCanvas.transform);
            }

            if (_passiveRelicSlotCanvas != null)
            {
                ClearChildrenExceptCardViews(_passiveRelicSlotCanvas.transform);
            }

            for (int i = 0; i < _choiceSlotCanvases.Count; i++)
            {
                if (_choiceSlotCanvases[i] != null)
                {
                    ClearChildrenExceptCardViews(_choiceSlotCanvases[i].transform);
                }
            }
        }

        private void EnqueuePendingBatches()
        {
            if (_context == null)
            {
                return;
            }

            while (_lastConsumedBatchCount < _context.Batches.Count)
            {
                DomainEventBatch batch = _context.Batches[_lastConsumedBatchCount];
                _pendingBatches.Enqueue(batch);
                _lastConsumedBatchCount++;
            }
        }

        private void PlayQueuedBatchesIfIdle()
        {
            if (_isAnimating || _pendingBatches.Count == 0)
            {
                return;
            }

            DomainEventBatch batch = _pendingBatches.Dequeue();
            _ = PlayBatchAsync(batch);
        }

        private async Task PlayBatchAsync(DomainEventBatch batch)
        {
            _isAnimating = true;
            try
            {
                await ProcessBatchAsync(batch);
            }
            finally
            {
                _isAnimating = false;
                RebuildChoiceButtons();
                RefreshRelicViews();
                RefreshHudTexts();
                if (_pendingBatches.Count > 0)
                {
                    PlayQueuedBatchesIfIdle();
                }
            }
        }

        private async Task ProcessBatchAsync(DomainEventBatch batch)
        {
            if (batch == null)
            {
                return;
            }

            for (int i = 0; i < batch.Events.Count; i++)
            {
                await PlayEventAsync(batch.Events[i]);
            }

            RefreshAllCardLabels();
            RebuildGridViewsImmediate();
            RefreshHudTexts();
            RefreshDetailPanelDefault();
        }

        private async Task PlayEventAsync(DomainEvent domainEvent)
        {
            if (domainEvent == null)
            {
                return;
            }

            switch (domainEvent.EventType)
            {
                case DomainEventType.CardAddedToGrid:
                case DomainEventType.CardCovered:
                    await HandleCardAddedOrCoveredAsync(domainEvent);
                    break;
                case DomainEventType.CardMoved:
                    await HandleCardMovedAsync(domainEvent);
                    break;
                case DomainEventType.CardFlipped:
                    await HandleCardFlippedAsync(domainEvent);
                    break;
                case DomainEventType.CardRemoved:
                    await HandleCardRemovedAsync(domainEvent);
                    break;
                case DomainEventType.CardZoneChanged:
                    await HandleCardZoneChangedAsync(domainEvent);
                    break;
                case DomainEventType.DamageApplied:
                    await HandleDamageAppliedAsync(domainEvent);
                    break;
                case DomainEventType.HealingApplied:
                    await HandleHealingAppliedAsync(domainEvent);
                    break;
                case DomainEventType.GoldChanged:
                    await HandleGoldChangedAsync(domainEvent);
                    break;
                case DomainEventType.StatChanged:
                    await HandleStatChangedAsync(domainEvent);
                    break;
                case DomainEventType.ItemStored:
                    await HandleItemStoredAsync(domainEvent);
                    break;
                case DomainEventType.ItemUsed:
                    await HandleItemUsedAsync(domainEvent);
                    break;
                case DomainEventType.RelicAcquired:
                case DomainEventType.RelicActivated:
                    await HandleRelicFeedbackAsync(domainEvent);
                    break;
                case DomainEventType.RouteChoicesGenerated:
                    await HandleRouteChoicesGeneratedAsync(domainEvent);
                    break;
                case DomainEventType.RouteChoiceSelected:
                    await HandleRouteChoiceSelectedAsync(domainEvent);
                    break;
                case DomainEventType.ChoiceOpened:
                case DomainEventType.ChoiceResolved:
                case DomainEventType.RoomEntered:
                case DomainEventType.RoomCleared:
                case DomainEventType.TrapTriggered:
                case DomainEventType.MonsterDefeated:
                case DomainEventType.IntentRejected:
                case DomainEventType.RunEnded:
                    await HandleInformationalEventAsync(domainEvent);
                    break;
            }
        }

        private async Task HandleCardAddedOrCoveredAsync(DomainEvent domainEvent)
        {
            if (!_context.Grid.TryGetCard(domainEvent.CardId, out CardInstance card))
            {
                return;
            }

            CardView view = GetOrCreateCardView(card);
            PlaceViewInstant(view, card);
            view.SyncFromCard(card, ResolveCardTitle(card), ResolveCardDescription(card), ResolveCardColor(card, false));
            if (card.IsFaceUp)
            {
                view.SetVisibleImmediate(true);
                view.ApplyFaceUpImmediate();
            }
            else
            {
                view.SetVisibleImmediate(true);
                view.ApplyFaceDownImmediate(_faceDownColor);
            }

            view.transform.DOPunchScale(Vector3.one * 0.06f, _flipDuration * 0.7f, 1, 0.2f).SetEase(Ease.OutQuart);
            await WaitSeconds(Mathf.Min(0.18f, _flipDuration));
        }

        private async Task HandleCardMovedAsync(DomainEvent domainEvent)
        {
            if (!_cardViews.TryGetValue(domainEvent.CardId, out CardView view))
            {
                if (_context.Grid.TryGetCard(domainEvent.CardId, out CardInstance currentCard))
                {
                    view = GetOrCreateCardView(currentCard);
                    PlaceViewInstant(view, currentCard);
                }
                else
                {
                    return;
                }
            }

            Vector3 targetPosition = ResolveWorldPosition(domainEvent.ToCoord, _context.Grid.TryGetCard(domainEvent.CardId, out CardInstance card) ? card.StackIndex : 0);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(view.transform.DOMove(targetPosition, _moveDuration).SetEase(Ease.OutQuart));
            sequence.Join(view.transform.DOPunchScale(Vector3.one * 0.05f, _moveDuration, 1, 0.2f).SetEase(Ease.OutQuart));
            await AwaitTween(sequence);
            if (card != null)
            {
                view.SyncFromCard(card, ResolveCardTitle(card), ResolveCardDescription(card), ResolveCardColor(card, false));
            }
        }

        private async Task HandleCardFlippedAsync(DomainEvent domainEvent)
        {
            if (!_context.Grid.TryGetCard(domainEvent.CardId, out CardInstance card))
            {
                return;
            }

            CardView view = GetOrCreateCardView(card);
            view.SetVisibleImmediate(true);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(view.RectTransform.DOScaleX(0.03f, _flipDuration * 0.45f).SetEase(Ease.OutQuart));
            sequence.AppendCallback(() =>
            {
                view.SyncFromCard(card, ResolveCardTitle(card), ResolveCardDescription(card), ResolveCardColor(card, false));
                view.ApplyFaceState(card.IsFaceUp, _faceDownColor);
            });
            sequence.Append(view.RectTransform.DOScaleX(1f, _flipDuration * 0.55f).SetEase(Ease.OutQuart));
            await AwaitTween(sequence);
        }

        private async Task HandleCardRemovedAsync(DomainEvent domainEvent)
        {
            if (!_cardViews.TryGetValue(domainEvent.CardId, out CardView view))
            {
                return;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Append(FadeCanvasGroupAlpha(view.CanvasGroup, 0f, _fadeDuration).SetEase(Ease.OutQuart));
            sequence.Join(view.RectTransform.DOScale(0.78f, _fadeDuration).SetEase(Ease.OutQuart));
            await AwaitTween(sequence);
            _cardViews.Remove(domainEvent.CardId);
            _routeChoiceViews.Remove(view);
            Destroy(view.gameObject);
        }

        private async Task HandleCardZoneChangedAsync(DomainEvent domainEvent)
        {
            if (!_context.Grid.TryGetCard(domainEvent.CardId, out CardInstance card))
            {
                if (_cardViews.TryGetValue(domainEvent.CardId, out CardView removedView))
                {
                    await AwaitTween(FadeCanvasGroupAlpha(removedView.CanvasGroup, 0f, _fadeDuration * 0.6f).SetEase(Ease.OutQuart));
                }

                return;
            }

            CardView view = GetOrCreateCardView(card);
            PlaceViewInstant(view, card);
            view.SyncFromCard(card, ResolveCardTitle(card), ResolveCardDescription(card), ResolveCardColor(card, false));
            view.transform.DOPunchScale(Vector3.one * 0.04f, _moveDuration * 0.8f, 1, 0.2f).SetEase(Ease.OutQuart);
            await WaitSeconds(0.16f);
        }

        private async Task HandleDamageAppliedAsync(DomainEvent domainEvent)
        {
            if (domainEvent.TargetCardId.IsEmpty)
            {
                return;
            }

            if (_cardViews.TryGetValue(domainEvent.TargetCardId, out CardView view))
            {
                Sequence sequence = DOTween.Sequence();
                sequence.Append(view.transform.DOPunchScale(Vector3.one * _hitPunchStrength, _hitPunchDuration, 1, 0.2f).SetEase(Ease.OutQuart));
                await AwaitTween(sequence);
            }
            else
            {
                await WaitSeconds(0.05f);
            }
        }

        private async Task HandleHealingAppliedAsync(DomainEvent domainEvent)
        {
            if (_context.Grid.PlayerCard != null && _cardViews.TryGetValue(_context.Grid.PlayerCard.InstanceId, out CardView playerView))
            {
                playerView.transform.DOPunchScale(Vector3.one * 0.12f, _hitPunchDuration, 1, 0.2f).SetEase(Ease.OutQuart);
            }

            await WaitSeconds(0.14f);
        }

        private async Task HandleGoldChangedAsync(DomainEvent domainEvent)
        {
            if (_playerGoldText != null)
            {
                _playerGoldText.transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 1, 0.2f).SetEase(Ease.OutQuart);
            }

            await WaitSeconds(0.12f);
        }

        private async Task HandleStatChangedAsync(DomainEvent domainEvent)
        {
            if (_playerHealthText != null)
            {
                _playerHealthText.transform.DOPunchScale(Vector3.one * 0.06f, 0.18f, 1, 0.2f).SetEase(Ease.OutQuart);
            }

            await WaitSeconds(0.1f);
        }

        private async Task HandleItemStoredAsync(DomainEvent domainEvent)
        {
            RefreshHudTexts();
            await WaitSeconds(0.12f);
        }

        private async Task HandleItemUsedAsync(DomainEvent domainEvent)
        {
            RefreshHudTexts();
            await WaitSeconds(0.12f);
        }

        private async Task HandleRelicFeedbackAsync(DomainEvent domainEvent)
        {
            RefreshRelicViews();
            await WaitSeconds(0.15f);
        }

        private async Task HandleRouteChoicesGeneratedAsync(DomainEvent domainEvent)
        {
            RebuildGridViewsImmediate();
            await WaitSeconds(0.16f);
        }

        private async Task HandleRouteChoiceSelectedAsync(DomainEvent domainEvent)
        {
            await WaitSeconds(0.12f);
            RebuildGridViewsImmediate();
        }

        private async Task HandleInformationalEventAsync(DomainEvent domainEvent)
        {
            if (domainEvent.EventType == DomainEventType.IntentRejected)
            {
                string reason = string.IsNullOrWhiteSpace(domainEvent.Reason) ? "该操作现在不合法" : domainEvent.Reason;
                SetDetailText("操作被拒绝", TranslateFailureReason(reason));
                await WaitSeconds(0.16f);
                return;
            }

            if (domainEvent.EventType == DomainEventType.RoomEntered)
            {
                SetDetailText("进入房间", DescribeRoomType(_context.Progression.CurrentRoomType));
            }
            else if (domainEvent.EventType == DomainEventType.RoomCleared)
            {
                SetDetailText("房间已清理", "现在可以选择路线卡前往下一房间。旧卡牌会保留在桌面上。");
            }
            else if (domainEvent.EventType == DomainEventType.ChoiceOpened)
            {
                RebuildChoiceButtons();
            }
            else if (domainEvent.EventType == DomainEventType.RunEnded)
            {
                SetDetailText("本局结束", domainEvent.Reason == "Victory" ? "你已完成当前三层设计体量。" : "玩家生命归零，本局结束。");
            }
            else if (domainEvent.EventType == DomainEventType.TrapTriggered)
            {
                if (_context.Grid.TryGetCard(domainEvent.CardId, out CardInstance trapCard))
                {
                    SetDetailText("机关触发", ResolveCardTitle(trapCard));
                }
            }

            await WaitSeconds(0.08f);
        }

        private CardView GetOrCreateCardView(CardInstance card)
        {
            if (_cardViews.TryGetValue(card.InstanceId, out CardView existing) && existing != null)
            {
                return existing;
            }

            if (_cardPrefab == null)
            {
                Canvas fallbackCanvas = _gridCanvasesByCell.TryGetValue(card.Coord.HasValue ? card.Coord.Value.CellIndex : 8, out Canvas canvas) ? canvas : null;
                if (fallbackCanvas != null && fallbackCanvas.transform.childCount > 0)
                {
                    _cardPrefab = fallbackCanvas.transform.GetChild(0).gameObject;
                }
            }

            Canvas parentCanvas = ResolveParentCanvas(card);
            if (parentCanvas == null)
            {
                throw new InvalidOperationException("Missing parent canvas for card view.");
            }

            GameObject cardObject = Instantiate(_cardPrefab, parentCanvas.transform, false);
            cardObject.name = BuildCardObjectName(card);
            CardView view = cardObject.GetComponent<CardView>();
            if (view == null)
            {
                view = cardObject.AddComponent<CardView>();
            }

            view.Initialize(this);
            _cardViews[card.InstanceId] = view;
            return view;
        }

        private string BuildCardObjectName(CardInstance card)
        {
            return ResolveCardTitle(card) + "_" + card.InstanceId.Value;
        }

        private Canvas ResolveParentCanvas(CardInstance card)
        {
            if (card.Zone == CardZone.Grid && card.Coord.HasValue && _gridCanvasesByCell.TryGetValue(card.Coord.Value.CellIndex, out Canvas gridCanvas))
            {
                return gridCanvas;
            }

            if (card.Zone == CardZone.PlayerInventory)
            {
                int slotIndex = _context.ItemInventory.FindSlot(card.InstanceId).Index;
                return ResolveInventoryCanvas(slotIndex);
            }

            if (card.Zone == CardZone.RelicInventory)
            {
                return _activeRelicSlotCanvas;
            }

            return _detailPanel;
        }

        private Canvas ResolveInventoryCanvas(int slotIndex)
        {
            if (_gridRoot == null)
            {
                return null;
            }

            return slotIndex switch
            {
                0 => _gridRoot.Find("格7")?.GetComponent<Canvas>(),
                1 => _gridRoot.Find("格9")?.GetComponent<Canvas>(),
                _ => _gridRoot.Find("格7")?.GetComponent<Canvas>()
            };
        }

        private void PlaceViewInstant(CardView view, CardInstance card)
        {
            Canvas targetCanvas = ResolveParentCanvas(card);
            if (targetCanvas == null)
            {
                return;
            }

            Transform t = view.transform;
            if (t.parent != targetCanvas.transform)
            {
                t.SetParent(targetCanvas.transform, false);
            }

            view.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            view.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            view.RectTransform.anchoredPosition = Vector2.zero;
            view.RectTransform.localPosition = new Vector3(0f, 0f, ComputeLocalZ(card));
            view.RectTransform.localRotation = Quaternion.identity;
            view.RectTransform.localScale = Vector3.one;
            view.CanvasGroup.alpha = 1f;
            view.ApplyFaceState(card.IsFaceUp, _faceDownColor);
        }

        private float ComputeLocalZ(CardInstance card)
        {
            return card.Zone switch
            {
                CardZone.Grid => HiddenZ + (card.StackIndex * StackZStep),
                CardZone.PlayerInventory => HiddenZ,
                CardZone.RelicInventory => HiddenZ,
                _ => HiddenZ
            };
        }

        private Vector3 ResolveWorldPosition(GridCoord? coord, int stackIndex)
        {
            if (!coord.HasValue || !_gridWorldPositionsByCell.TryGetValue(coord.Value.CellIndex, out Vector3 basePosition))
            {
                return Vector3.zero;
            }

            return basePosition + new Vector3(0f, 0f, stackIndex * StackZStep);
        }

        private void RebuildGridViewsImmediate()
        {
            if (_context == null || _context.Grid == null)
            {
                return;
            }

            HashSet<CardInstanceId> aliveIds = new HashSet<CardInstanceId>();
            foreach (CardInstance card in _context.Grid.AllKnownCards)
            {
                if (card == null || card.IsRemoved)
                {
                    continue;
                }

                if (card.Zone != CardZone.Grid && card.Zone != CardZone.PlayerInventory && card.Zone != CardZone.RelicInventory)
                {
                    continue;
                }

                aliveIds.Add(card.InstanceId);
                CardView view = GetOrCreateCardView(card);
                PlaceViewInstant(view, card);
                view.SyncFromCard(card, ResolveCardTitle(card), ResolveCardDescription(card), ResolveCardColor(card, false));
            }

            List<CardInstanceId> removeIds = new List<CardInstanceId>();
            foreach (KeyValuePair<CardInstanceId, CardView> pair in _cardViews)
            {
                if (!aliveIds.Contains(pair.Key) && pair.Value != null)
                {
                    removeIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeIds.Count; i++)
            {
                CardView removedView = _cardViews[removeIds[i]];
                _cardViews.Remove(removeIds[i]);
                _routeChoiceViews.Remove(removedView);
                Destroy(removedView.gameObject);
            }
        }

        private void RefreshAllCardLabels()
        {
            foreach (KeyValuePair<CardInstanceId, CardView> pair in _cardViews)
            {
                if (_context.Grid.TryGetCard(pair.Key, out CardInstance card))
                {
                    pair.Value.SyncFromCard(card, ResolveCardTitle(card), ResolveCardDescription(card), ResolveCardColor(card, false));
                }
            }
        }

        private void RefreshHudTexts()
        {
            if (_context == null || _context.Grid == null)
            {
                return;
            }

            CardInstance player = _context.Grid.PlayerCard;
            if (player != null)
            {
                if (_playerRoleText != null)
                {
                    _playerRoleText.text = "兵大哥";
                }

                if (_playerHealthText != null)
                {
                    _playerHealthText.text = $"生命 {player.CurrentHp}/{player.MaxHp}  攻击 {player.Attack}  防御 {player.Defense}";
                }
            }

            if (_playerGoldText != null)
            {
                _playerGoldText.text = $"金币 {_context.PlayerGold}  道具 {_context.ItemInventory.Count}/{_context.ItemInventory.Capacity}";
            }
        }

        private void RefreshRelicViews()
        {
            if (_context == null)
            {
                return;
            }

            PruneVirtualDetails();
            RefreshActiveRelicView();
            RefreshPassiveRelicView();
        }

        private void RefreshActiveRelicView()
        {
            if (_activeRelicSlotCanvas == null)
            {
                return;
            }

            ClearChildrenExceptCardViews(_activeRelicSlotCanvas.transform);
            if (_context.Relics.ActiveSlot.IsEmpty)
            {
                return;
            }

            GameObject cardObject = Instantiate(_cardPrefab, _activeRelicSlotCanvas.transform, false);
            CardView view = cardObject.GetComponent<CardView>();
            if (view == null)
            {
                view = cardObject.AddComponent<CardView>();
            }

            view.Initialize(this);
            view.RectTransform.localPosition = new Vector3(0f, 0f, HiddenZ);
            view.RectTransform.localRotation = Quaternion.identity;
            view.RectTransform.localScale = Vector3.one;
            string title = ResolveRelicTitle(_context.Relics.ActiveSlot.RelicId);
            string desc = ResolveRelicDescription(_context.Relics.ActiveSlot.RelicId, true);
            view.SyncVirtual(title, desc, string.Empty, string.Empty, _relicColor, true);
            view.SetOverlayText($"{_context.Relics.ActiveSlot.UsesRemainingThisRoom}/{_context.Relics.ActiveSlot.MaxUsesPerRoom}");
            view.SetPassive(false);
            view.SetOnClick(OnActiveRelicClicked);
            RememberVirtualDetail(view, title, desc);
        }

        private void RefreshPassiveRelicView()
        {
            if (_passiveRelicSlotCanvas == null)
            {
                return;
            }

            ClearChildrenExceptCardViews(_passiveRelicSlotCanvas.transform);
            if (_context.Relics.PassiveRelics.Count == 0)
            {
                _passiveRelicDisplayIndex = 0;
                return;
            }

            _passiveRelicDisplayIndex = Mathf.Clamp(_passiveRelicDisplayIndex, 0, _context.Relics.PassiveRelics.Count - 1);
            ModelId relicId = _context.Relics.PassiveRelics[_passiveRelicDisplayIndex];
            GameObject cardObject = Instantiate(_cardPrefab, _passiveRelicSlotCanvas.transform, false);
            CardView view = cardObject.GetComponent<CardView>();
            if (view == null)
            {
                view = cardObject.AddComponent<CardView>();
            }

            view.Initialize(this);
            view.RectTransform.localPosition = new Vector3(0f, 0f, HiddenZ);
            view.RectTransform.localRotation = Quaternion.identity;
            view.RectTransform.localScale = Vector3.one;
            string title = ResolveRelicTitle(relicId);
            string desc = ResolveRelicDescription(relicId, false);
            view.SyncVirtual(title, desc, string.Empty, string.Empty, _relicColor, true);
            view.SetPassive(true);
            if (_context.Relics.PassiveRelics.Count > 1)
            {
                view.SetOverlayText($"{_passiveRelicDisplayIndex + 1}/{_context.Relics.PassiveRelics.Count}");
            }

            view.SetOnClick(() => SetDetailText(title, desc));
            RememberVirtualDetail(view, title, desc);
        }

        private void ClearChildrenExceptCardViews(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponent<CardView>() != null || child.GetComponent<ChoiceButtonView>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void RebuildChoiceButtons()
        {
            foreach (ChoiceButtonView choice in _choiceButtons)
            {
                if (choice != null)
                {
                    Destroy(choice.gameObject);
                }
            }
            _choiceButtons.Clear();

            foreach (CardView choiceView in _choiceCardViews)
            {
                if (choiceView != null)
                {
                    Destroy(choiceView.gameObject);
                }
            }
            _choiceCardViews.Clear();
            PruneVirtualDetails();
            _isChoosingChoice = false;
            _pendingChoiceSessionId = string.Empty;

            if (_context == null || _detailPanel == null)
            {
                if (_choiceRoot != null)
                {
                    _choiceRoot.gameObject.SetActive(false);
                }
                return;
            }

            foreach (ChoiceSession session in _context.ChoiceSessions.Sessions)
            {
                if (session == null || session.IsResolved)
                {
                    continue;
                }

                _isChoosingChoice = true;
                _pendingChoiceSessionId = session.SessionId;
                SetDetailText(ResolveChoiceTitle(session), ResolveChoiceDescription(session));
                if (_choiceSlotCanvases.Count >= session.OptionCount && session.OptionCount <= 3)
                {
                    for (int i = 0; i < session.OptionCount; i++)
                    {
                        CreateChoiceCard(session, i);
                    }
                }
                else
                {
                    for (int i = 0; i < session.OptionCount; i++)
                    {
                        CreateChoiceButton(session, i);
                    }
                }
                break;
            }

            if (_choiceRoot != null)
            {
                _choiceRoot.gameObject.SetActive(_isChoosingChoice);
            }
        }

        private void CreateChoiceButton(ChoiceSession session, int optionIndex)
        {
            GameObject buttonObject = new GameObject($"Choice_{optionIndex}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ChoiceButtonView));
            buttonObject.transform.SetParent(_detailPanel.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 100f);
            rect.anchoredPosition = new Vector2(0f, -180f - (optionIndex * 112f));

            Image image = buttonObject.GetComponent<Image>();
            image.color = _choiceColor;

            GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(buttonObject.transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(320f, 36f);
            titleRect.anchoredPosition = new Vector2(0f, -26f);
            TextMeshProUGUI titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 22;
            titleText.color = Color.black;

            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(buttonObject.transform, false);
            RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0f);
            bodyRect.anchorMax = new Vector2(0.5f, 0f);
            bodyRect.sizeDelta = new Vector2(320f, 42f);
            bodyRect.anchoredPosition = new Vector2(0f, 18f);
            TextMeshProUGUI bodyText = bodyObject.GetComponent<TextMeshProUGUI>();
            bodyText.alignment = TextAlignmentOptions.Midline;
            bodyText.fontSize = 16;
            bodyText.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

            ChoiceButtonView view = buttonObject.GetComponent<ChoiceButtonView>();
            view.Initialize(titleText, bodyText);
            view.Bind(ResolveChoiceOptionTitle(session, optionIndex), ResolveChoiceOptionBody(session, optionIndex), () => OnChoiceSelected(session, optionIndex));
            _choiceButtons.Add(view);
        }

        private void CreateChoiceCard(ChoiceSession session, int optionIndex)
        {
            Canvas slotCanvas = _choiceSlotCanvases[optionIndex];
            if (slotCanvas == null)
            {
                return;
            }

            ClearChildrenExceptCardViews(slotCanvas.transform);
            GameObject cardObject = Instantiate(_cardPrefab, slotCanvas.transform, false);
            CardView view = cardObject.GetComponent<CardView>();
            if (view == null)
            {
                view = cardObject.AddComponent<CardView>();
            }

            string title = ResolveChoiceOptionTitle(session, optionIndex);
            string body = ResolveChoiceOptionBody(session, optionIndex);
            view.Initialize(this);
            view.RectTransform.localPosition = new Vector3(0f, 0f, HiddenZ);
            view.RectTransform.localRotation = Quaternion.identity;
            view.RectTransform.localScale = Vector3.one * 0.96f;
            view.SyncVirtual(title, body, string.Empty, string.Empty, ResolveChoiceOptionColor(session), true);
            view.SetOnClick(() => OnChoiceSelected(session, optionIndex));
            view.RectTransform.DOScale(1f, 0.24f).SetEase(Ease.OutQuart);
            _choiceCardViews.Add(view);
            RememberVirtualDetail(view, title, body);
        }

        private async void OnChoiceSelected(ChoiceSession session, int optionIndex)
        {
            if (session == null || _facade == null || _isAnimating)
            {
                return;
            }

            DomainEventBatch batch = await _facade.SubmitIntentAsync(new ChooseOptionIntent(session.SessionId, optionIndex));
            EnqueuePendingBatches();
            if (!_isAnimating)
            {
                PlayQueuedBatchesIfIdle();
            }
        }

        public async void OnCardClicked(CardView view)
        {
            if (!_isReady || _isAnimating || view == null)
            {
                return;
            }

            if (!TryGetBackingCard(view, out CardInstance card))
            {
                return;
            }

            if (_isChoosingChoice)
            {
                return;
            }

            if (_isChoosingTarget)
            {
                await HandleTargetSelectionClickAsync(card);
                return;
            }

            if (_isChoosingRelicTarget)
            {
                await HandleRelicTargetSelectionClickAsync(card);
                return;
            }

            switch (card.Zone)
            {
                case CardZone.Grid:
                    await HandleGridCardClickAsync(card);
                    break;
                case CardZone.PlayerInventory:
                    await HandleInventoryCardClickAsync(card);
                    break;
            }
        }

        private async Task HandleGridCardClickAsync(CardInstance card)
        {
            if (card.CardType == CardType.Player)
            {
                SetDetailText(ResolveCardTitle(card), ResolveCardDescription(card));
                return;
            }

            if (card.CardType == CardType.Item)
            {
                DomainEventBatch batch = await _facade.SubmitIntentAsync(new StoreItemIntent(card.InstanceId));
                HandleSubmittedBatch(batch);
                return;
            }

            DomainEventBatch interactBatch = await _facade.SubmitIntentAsync(new InteractWithCardIntent(card.InstanceId));
            HandleSubmittedBatch(interactBatch);
        }

        private async Task HandleInventoryCardClickAsync(CardInstance card)
        {
            InventorySlot slot = _context.ItemInventory.FindSlot(card.InstanceId);
            if (!slot.IsValid)
            {
                return;
            }

            if (!_context.TryResolveCardModel(card, out CardModel rawModel) || rawModel is not Game.Core.Domain.ContentContracts.ItemCardModel itemModel)
            {
                return;
            }

            ItemTargetMode mode = itemModel.TargetMode;
            if (mode == ItemTargetMode.None || mode == ItemTargetMode.Player)
            {
                DomainEventBatch batch = await _facade.SubmitIntentAsync(new UseItemIntent(slot));
                HandleSubmittedBatch(batch);
                return;
            }

            BeginItemTargetSelection(slot, mode, card);
        }

        private void OnActiveRelicClicked()
        {
            if (!_isReady || _isAnimating || _context.Relics.ActiveSlot.IsEmpty)
            {
                return;
            }

            ModelId relicId = _context.Relics.ActiveSlot.RelicId;
            if (!_context.TryResolveRelicModel(relicId, out Game.Core.Domain.ContentContracts.RelicModel relic))
            {
                return;
            }

            if (relic.TargetMode == ItemTargetMode.None || relic.TargetMode == ItemTargetMode.Player)
            {
                _ = SubmitRelicIntentAsync(relicId, ItemTargetSelection.None);
                return;
            }

            BeginRelicTargetSelection(relicId, relic.TargetMode);
        }

        private void BeginItemTargetSelection(InventorySlot slot, ItemTargetMode mode, CardInstance itemCard)
        {
            CancelSelectionModes();
            _isChoosingTarget = true;
            _pendingItemSlot = slot;
            _pendingTargetMode = mode;
            _pendingPrimaryCard = null;
            SetDetailText(ResolveCardTitle(itemCard), DescribeTargetMode(mode, false));
            ApplyPreviewForTargetMode(mode, false);
        }

        private void BeginRelicTargetSelection(ModelId relicId, ItemTargetMode mode)
        {
            CancelSelectionModes();
            _isChoosingRelicTarget = true;
            _pendingRelicId = relicId;
            _pendingTargetMode = mode;
            _pendingPrimaryCard = null;
            SetDetailText(ResolveRelicTitle(relicId), DescribeTargetMode(mode, true));
            ApplyPreviewForTargetMode(mode, true);
        }

        private async Task HandleTargetSelectionClickAsync(CardInstance clickedCard)
        {
            if (_pendingTargetMode == ItemTargetMode.AnyCard || _pendingTargetMode == ItemTargetMode.MonsterCard)
            {
                await SubmitItemIntentAsync(new UseItemIntent(_pendingItemSlot, ItemTargetSelection.CardTarget(clickedCard.InstanceId)));
                return;
            }

            if (_pendingTargetMode == ItemTargetMode.CardThenDirection)
            {
                if (!_pendingPrimaryCard.HasValue)
                {
                    _pendingPrimaryCard = clickedCard.InstanceId;
                    SetDetailText(ResolveCardTitle(clickedCard), "再点击相邻空格方向：目标格会决定勾绳方向。");
                    ApplyDirectionPreview(clickedCard);
                    return;
                }
            }

            if (_pendingTargetMode == ItemTargetMode.TwoCards)
            {
                if (!_pendingPrimaryCard.HasValue)
                {
                    _pendingPrimaryCard = clickedCard.InstanceId;
                    SetDetailText(ResolveCardTitle(clickedCard), "已选择第一张牌，再点第二张牌。");
                    HighlightCard(clickedCard.InstanceId, _previewValidColor);
                    return;
                }

                await SubmitItemIntentAsync(new UseItemIntent(_pendingItemSlot, ItemTargetSelection.TwoCards(_pendingPrimaryCard.Value, clickedCard.InstanceId)));
                return;
            }
        }

        private Task HandleRelicTargetSelectionClickAsync(CardInstance clickedCard)
        {
            if (_pendingTargetMode == ItemTargetMode.AnyCardThenAnyCell)
            {
                if (!_pendingPrimaryCard.HasValue)
                {
                    _pendingPrimaryCard = clickedCard.InstanceId;
                    SetDetailText(ResolveCardTitle(clickedCard), "已选择卡牌，再点击任意格子放置。");
                    HighlightCard(clickedCard.InstanceId, _previewValidColor);
                    HighlightAllCells(_previewValidColor);
                }
            }

            return Task.CompletedTask;
        }

        public async void OnCellClicked(int cellIndex)
        {
            if (!_isReady || _isAnimating)
            {
                return;
            }

            GridCoord coord = GridCoord.FromCellIndex(cellIndex);

            if (_isChoosingTarget)
            {
                await HandleItemCellSelectionAsync(coord);
                return;
            }

            if (_isChoosingRelicTarget)
            {
                await HandleRelicCellSelectionAsync(coord);
                return;
            }

            DomainEventBatch moveBatch = await _facade.SubmitIntentAsync(new MovePlayerIntent(coord));
            HandleSubmittedBatch(moveBatch);
        }

        private async Task HandleItemCellSelectionAsync(GridCoord coord)
        {
            if (_pendingTargetMode == ItemTargetMode.GridCell)
            {
                await SubmitItemIntentAsync(new UseItemIntent(_pendingItemSlot, ItemTargetSelection.GridCellTarget(coord)));
                return;
            }

            if (_pendingTargetMode == ItemTargetMode.CardThenDirection && _pendingPrimaryCard.HasValue)
            {
                if (!_context.Grid.TryGetCard(_pendingPrimaryCard.Value, out CardInstance target) || !target.Coord.HasValue)
                {
                    CancelSelectionModes();
                    return;
                }

                GridDirection? direction = GridQueries.DirectionFromTo(target.Coord.Value, coord);
                if (!direction.HasValue)
                {
                    SetDetailText("方向无效", "勾绳第二步必须点击目标牌正交相邻的一格。");
                    return;
                }

                await SubmitItemIntentAsync(new UseItemIntent(_pendingItemSlot, ItemTargetSelection.CardThenDirection(_pendingPrimaryCard.Value, direction.Value)));
            }
        }

        private async Task HandleRelicCellSelectionAsync(GridCoord coord)
        {
            if (_pendingTargetMode == ItemTargetMode.AnyCardThenAnyCell && _pendingPrimaryCard.HasValue)
            {
                await SubmitRelicIntentAsync(_pendingRelicId, ItemTargetSelection.CardThenCell(_pendingPrimaryCard.Value, coord));
            }
        }

        private async Task SubmitItemIntentAsync(UseItemIntent intent)
        {
            CancelSelectionModes();
            DomainEventBatch batch = await _facade.SubmitIntentAsync(intent);
            HandleSubmittedBatch(batch);
        }

        private async Task SubmitRelicIntentAsync(ModelId relicId, ItemTargetSelection selection)
        {
            CancelSelectionModes();
            DomainEventBatch batch = await _facade.SubmitIntentAsync(new ActivateRelicIntent(relicId, selection));
            HandleSubmittedBatch(batch);
        }

        private void HandleSubmittedBatch(DomainEventBatch batch)
        {
            EnqueuePendingBatches();
            if (batch == null)
            {
                return;
            }

            if (!_isAnimating)
            {
                PlayQueuedBatchesIfIdle();
            }
        }

        private void CancelSelectionModes()
        {
            _isChoosingTarget = false;
            _isChoosingRelicTarget = false;
            _pendingItemSlot = new InventorySlot(-1);
            _pendingRelicId = default;
            _pendingTargetMode = ItemTargetMode.None;
            _pendingPrimaryCard = null;
            CancelDrag();
            ClearAllHighlights();
        }

        private void ApplyPreviewForTargetMode(ItemTargetMode mode, bool forRelic)
        {
            ClearAllHighlights();
            switch (mode)
            {
                case ItemTargetMode.GridCell:
                    HighlightAllCells(_previewValidColor);
                    break;
                case ItemTargetMode.MonsterCard:
                    HighlightCardsByPredicate(card => card.Zone == CardZone.Grid && card.IsFaceUp && card.CardType == CardType.Monster, _previewValidColor);
                    break;
                case ItemTargetMode.AnyCard:
                case ItemTargetMode.CardThenDirection:
                case ItemTargetMode.TwoCards:
                    HighlightCardsByPredicate(card => card.Zone == CardZone.Grid && card.IsFaceUp && card.CardType != CardType.Player, _previewValidColor);
                    break;
                case ItemTargetMode.AnyCardThenAnyCell:
                    HighlightCardsByPredicate(card => card.Zone == CardZone.Grid && card.CardType != CardType.Player, _previewValidColor);
                    break;
            }
        }

        private void ApplyDirectionPreview(CardInstance target)
        {
            ClearAllHighlights();
            HighlightCard(target.InstanceId, _previewValidColor);
            if (!target.Coord.HasValue)
            {
                return;
            }

            foreach (GridCoord neighbor in GridQueries.OrthogonalNeighbors(target.Coord.Value))
            {
                HighlightCell(neighbor.CellIndex, _previewValidColor);
            }
        }

        private void HighlightCardsByPredicate(Func<CardInstance, bool> predicate, Color color)
        {
            foreach (KeyValuePair<CardInstanceId, CardView> pair in _cardViews)
            {
                if (_context.Grid.TryGetCard(pair.Key, out CardInstance card) && predicate(card))
                {
                    pair.Value.SetOutline(color);
                }
            }
        }

        private void HighlightAllCells(Color color)
        {
            for (int i = 1; i <= 9; i++)
            {
                HighlightCell(i, color);
            }
        }

        private void HighlightCell(int cellIndex, Color color)
        {
            if (_gridCanvasesByCell.TryGetValue(cellIndex, out Canvas canvas))
            {
                Image outline = canvas.GetComponentInChildren<Image>();
                if (outline != null)
                {
                    outline.color = color;
                }
            }
        }

        private void HighlightCard(CardInstanceId cardId, Color color)
        {
            if (_cardViews.TryGetValue(cardId, out CardView view))
            {
                view.SetOutline(color);
            }
        }

        private void ClearAllHighlights()
        {
            foreach (KeyValuePair<CardInstanceId, CardView> pair in _cardViews)
            {
                pair.Value.SetOutline(_idleOutlineColor);
            }

            foreach (Canvas canvas in _gridCanvasesByCell.Values)
            {
                Image outline = canvas.GetComponentInChildren<Image>();
                if (outline != null)
                {
                    outline.color = _idleOutlineColor;
                }
            }
        }

        private void UpdateHoverDetail()
        {
            if (_isChoosingChoice || _isAnimating)
            {
                return;
            }

            if (_hoveredView != null && _virtualDetails.TryGetValue(_hoveredView, out VirtualDetail virtualDetail))
            {
                SetDetailText(virtualDetail.Title, virtualDetail.Body);
                return;
            }

            if (_hoveredView != null && TryGetBackingCard(_hoveredView, out CardInstance card))
            {
                SetDetailText(ResolveCardTitle(card), ResolveCardDescription(card));
                return;
            }

            RefreshDetailPanelDefault();
        }

        private void RefreshDetailPanelDefault()
        {
            if (_isChoosingChoice || _isChoosingTarget || _isChoosingRelicTarget)
            {
                return;
            }

            if (_context == null || _context.Progression == null)
            {
                return;
            }

            SetDetailText($"第{_context.Progression.LayerIndex}层 第{_context.Progression.NodeIndex}节点", DescribeRoomType(_context.Progression.CurrentRoomType));
        }

        private bool TryGetBackingCard(CardView view, out CardInstance card)
        {
            foreach (KeyValuePair<CardInstanceId, CardView> pair in _cardViews)
            {
                if (pair.Value == view)
                {
                    return _context.Grid.TryGetCard(pair.Key, out card);
                }
            }

            card = null;
            return false;
        }

        private void SetDetailText(string title, string body)
        {
            if (_detailTitleText != null)
            {
                _detailTitleText.text = title;
            }

            if (_detailBodyText != null)
            {
                _detailBodyText.text = body;
            }
        }

        private string ResolveCardTitle(CardInstance card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            if (card.CardType == CardType.Player)
            {
                return "兵大哥";
            }

            if (card.CardType == CardType.RouteChoice)
            {
                return DescribeRoomType(ParseRouteRoomType(card.ModelId.Entry));
            }

            return card.ModelId.Entry switch
            {
                "skeleton" => "骷髅",
                "armored-skeleton" => "带甲骷髅",
                "banner-skeleton" => "旗兵骷髅",
                "revenge-skeleton" => "复仇骷髅",
                "tracker-skeleton" => "追踪者骷髅",
                "ambusher-skeleton" => "伏击者骷髅",
                "war-skeleton" => "武装骷髅",
                "big-skeleton-lord" => "大骷髅老爷",
                "crossbow" => "弩箭机关",
                "spike" => "尖刺机关",
                "teleport" => "传送机关",
                "hook-rope" => "勾绳",
                "healing-potion" => "恢复药水",
                "throwing-knife" => "飞刀",
                "protection-spell" => "庇佑魔法卡",
                "flip-card" => "翻转卡",
                "light-card" => "照明卡",
                "violence-card" => "暴力卡",
                "first-strike-card" => "先攻卡",
                "gold" => "金币卡",
                "stat-upgrade" => "属性提升卡",
                "food" => "食品卡",
                "ordinary-chest" => "普通宝箱卡",
                "blue-chest" => "蓝色宝箱卡",
                "gold-chest" => "金色宝箱卡",
                "mentor-thorn-skin" => "导师卡·荆甲",
                "mentor-iron-skin" => "导师卡·铁肤",
                "mentor-veteran" => "导师卡·老兵",
                "shop-attack" => "商品卡·攻击",
                "shop-defense" => "商品卡·防御",
                "shop-max-hp" => "商品卡·生命",
                "shop-random-item" => "商品卡·随机道具",
                "shop-ordinary-chest" => "商品卡·普通宝箱",
                "pickup-law-wand" => "主动遗物掉落·法则魔杖",
                "pickup-endless-water-bag" => "主动遗物掉落·无尽水袋",
                "pickup-blood-shield" => "主动遗物掉落·血盾",
                _ => card.ModelId.Entry
            };
        }

        private string ResolveCardDescription(CardInstance card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            if (!card.IsFaceUp)
            {
                return "背面朝下。除非被翻开，否则暂时不存在。";
            }

            return card.ModelId.Entry switch
            {
                "skeleton" => "血6 攻2 防0。基础怪物。",
                "armored-skeleton" => "血8 攻3 防1。",
                "banner-skeleton" => "血8 攻3 防2。鼓舞周围怪物。",
                "revenge-skeleton" => "血7 攻4 防0。其他怪物死亡会涨攻。",
                "tracker-skeleton" => "血10 攻5 防1。每3次玩家行动会追击。",
                "ambusher-skeleton" => "血8 攻4 防1。翻开且贴近玩家会立刻攻击。",
                "war-skeleton" => "血10 攻4 防3。若伤到玩家，会削减玩家防御。",
                "big-skeleton-lord" => "血50 攻4 防3。掉血过阈值会召唤骷髅。",
                "crossbow" => "血2。摧毁后对同列上方所有翻开的牌造成6伤害。",
                "spike" => "血4。翻开后等待下一次玩家行动结束，刺伤正交相邻翻开的牌。",
                "teleport" => "血1。摧毁后将其他牌洗回牌组并随机重发。",
                "hook-rope" => "点道具后：先选一张非玩家牌，再点相邻格决定拉动方向。",
                "healing-potion" => "恢复10点生命。",
                "throwing-knife" => "对任意翻开的牌造成6伤害。",
                "protection-spell" => "下一次玩家受到的可防伤害变为0。",
                "flip-card" => "选择一张正面牌与一张背面牌交换。",
                "light-card" => "点一个格子，翻开其正交相邻顶牌。",
                "violence-card" => "本房间下一次玩家与怪物互动前，玩家攻击翻倍。",
                "first-strike-card" => "本房间获得先攻。",
                "gold" => "互动后获得50金币并移除。",
                "stat-upgrade" => "互动后在攻击、防御、生命中三选一。",
                "food" => "互动后回满生命。",
                "ordinary-chest" => "互动后从三件遗物中选一件。",
                "blue-chest" => "更高概率给蓝色遗物。",
                "gold-chest" => "高品质遗物宝箱。",
                "mentor-thorn-skin" => "获得荆甲词条。",
                "mentor-iron-skin" => "获得铁肤词条并+10最大生命。",
                "mentor-veteran" => "获得老兵词条。",
                "shop-attack" => "花80金币，攻击+1。",
                "shop-defense" => "花80金币，防御+1。",
                "shop-max-hp" => "花80金币，生命上限+2并回复2。",
                "shop-random-item" => "花30金币，获得随机道具。",
                "shop-ordinary-chest" => "花160金币，开普通宝箱。",
                "pickup-law-wand" => "拾取并装备主动遗物：法则魔杖。",
                "pickup-endless-water-bag" => "拾取并装备主动遗物：无尽水袋。",
                "pickup-blood-shield" => "拾取并装备主动遗物：血盾。",
                _ when card.CardType == CardType.Player => "玩家卡。点击空格移动，点击翻开的牌互动。",
                _ when card.CardType == CardType.RouteChoice => "清场后出现的路线卡。点击进入下一房间。",
                _ => $"类型：{card.CardType}  模型：{card.ModelId}"
            };
        }

        private Color ResolveCardColor(CardInstance card, bool hovered)
        {
            if (card == null)
            {
                return Color.white;
            }

            Color baseColor = card.CardType switch
            {
                CardType.Player => _playerColor,
                CardType.Monster => _monsterColor,
                CardType.Trap => _trapColor,
                CardType.Item => _itemColor,
                CardType.Gold => _goldColor,
                CardType.RouteChoice => _choiceColor,
                CardType.Special => _specialColor,
                CardType.Chest => new Color(0.82f, 0.54f, 0.22f, 0.95f),
                CardType.StatUpgrade => new Color(0.54f, 0.86f, 0.70f, 0.95f),
                CardType.Food => new Color(0.64f, 0.88f, 0.60f, 0.95f),
                CardType.Mentor => new Color(0.72f, 0.64f, 0.88f, 0.95f),
                CardType.ShopProduct => new Color(0.52f, 0.80f, 0.92f, 0.95f),
                _ => Color.white
            };

            return hovered ? Color.Lerp(baseColor, Color.white, 0.15f) : baseColor;
        }

        private string ResolveRelicTitle(ModelId relicId)
        {
            return relicId.Entry switch
            {
                "living-flesh" => "活着的肉",
                "wood-shield" => "木盾",
                "wood-sword" => "木剑",
                "law-wand" => "法则魔杖",
                "endless-water-bag" => "无尽水袋",
                "item-stockpile" => "道具储备",
                "blood-shield" => "血盾",
                "village-good-sword" => "村好剑",
                _ => relicId.Entry
            };
        }

        private string ResolveRelicDescription(ModelId relicId, bool active)
        {
            return relicId.Entry switch
            {
                "living-flesh" => "攻击+1；每进入三个房间再+1攻击。",
                "wood-shield" => "防御+2。",
                "wood-sword" => "攻击+2。",
                "law-wand" => active ? "点击后：选择一张非玩家牌，再选择任意格子移动它。" : "攻击+1；主动：移动任意非玩家牌。",
                "endless-water-bag" => active ? "点击后恢复6点生命。" : "生命+4；主动恢复6生命。",
                "item-stockpile" => "进入非餐厅房间时，额外往地城牌组加两张道具卡。",
                "blood-shield" => active ? "点击后本房间防御+2；每击败一只怪刷新次数。" : "防御+2；主动可叠加本房间防御。",
                "village-good-sword" => "攻击+1；击败精英或层主时永久+2攻击。",
                _ => relicId.ToString()
            };
        }

        private string ResolveChoiceTitle(ChoiceSession session)
        {
            return session.ChoiceKind switch
            {
                "StatUpgrade" => "属性提升",
                "RelicChoice" => "选择遗物",
                _ => "做出选择"
            };
        }

        private string ResolveChoiceDescription(ChoiceSession session)
        {
            return session.ChoiceKind switch
            {
                "StatUpgrade" => "点击下面一张标准卡片完成选择。",
                "RelicChoice" => "从三张遗物卡中选一张。",
                _ => "当前规则要求进行一次选择。"
            };
        }

        private string ResolveChoiceOptionTitle(ChoiceSession session, int index)
        {
            string key = session.GetOptionKey(index);
            return session.ChoiceKind switch
            {
                "StatUpgrade" when key == "attack" => "攻击 +1",
                "StatUpgrade" when key == "defense" => "防御 +1",
                "StatUpgrade" when key == "max-hp" => "生命上限 +2",
                "RelicChoice" => ResolveRelicTitle(DecodeModelId(key)),
                _ => key
            };
        }

        private string ResolveChoiceOptionBody(ChoiceSession session, int index)
        {
            string key = session.GetOptionKey(index);
            return session.ChoiceKind switch
            {
                "StatUpgrade" when key == "attack" => "提升输出能力。",
                "StatUpgrade" when key == "defense" => "提升承伤能力。",
                "StatUpgrade" when key == "max-hp" => "增加生命并回复2点。",
                "RelicChoice" => ResolveRelicDescription(DecodeModelId(key), false),
                _ => string.Empty
            };
        }

        private static ModelId DecodeModelId(string value)
        {
            int separator = value.IndexOf(':');
            return separator >= 0
                ? new ModelId(value.Substring(0, separator), value.Substring(separator + 1))
                : default;
        }

        private string DescribeRoomType(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Reward => "奖励房：普通战斗基础上加入宝箱与属性卡。",
                RoomType.Combat => "普通战斗房。清理所有怪物后出现路线。",
                RoomType.Gold => "金币房：额外加入金币卡。",
                RoomType.Chest => "宝箱房：额外加入宝箱卡。",
                RoomType.StatUpgrade => "属性房：额外加入属性提升卡。",
                RoomType.Shop => "商店房：加入4~6张商品卡。",
                RoomType.EliteCombat => "精英战斗房。",
                RoomType.BossCombat => "层主战斗房。",
                RoomType.Restaurant => "餐厅：食品卡与3张导师卡，无怪物。",
                _ => roomType.ToString()
            };
        }

        private RoomType ParseRouteRoomType(string entry)
        {
            return entry switch
            {
                "combat" => RoomType.Combat,
                "gold" => RoomType.Gold,
                "chest" => RoomType.Chest,
                "statupgrade" => RoomType.StatUpgrade,
                "shop" => RoomType.Shop,
                "elitecombat" => RoomType.EliteCombat,
                "bosscombat" => RoomType.BossCombat,
                "reward" => RoomType.Reward,
                "restaurant" => RoomType.Restaurant,
                _ => RoomType.Combat
            };
        }

        private string DescribeTargetMode(ItemTargetMode mode, bool relic)
        {
            return mode switch
            {
                ItemTargetMode.GridCell => relic ? "点击一个格子作为主动遗物目标。" : "点击一个格子使用该道具。",
                ItemTargetMode.MonsterCard => "点击一张翻开的怪物牌作为目标。",
                ItemTargetMode.AnyCard => "点击一张翻开的牌作为目标。",
                ItemTargetMode.CardThenDirection => "先点目标牌，再点它正交相邻的一格决定方向。",
                ItemTargetMode.TwoCards => "先点第一张牌，再点第二张牌。",
                ItemTargetMode.AnyCardThenAnyCell => "先点一张非玩家牌，再点一个格子放置。",
                _ => "点击以完成目标选择。"
            };
        }

        private void RefreshSimpleUiState()
        {
            if (_restartRunLabel != null)
            {
                _restartRunLabel.text = _isReady ? "重开本局" : "开始演出";
            }

            if (_cancelSelectionButton != null)
            {
                _cancelSelectionButton.gameObject.SetActive(_isChoosingTarget || _isChoosingRelicTarget || _isChoosingChoice);
            }
        }

        private const float DragThreshold = 12f;

        private void HandleDragUpdate()
        {
            if (!_isReady || _isAnimating || _isChoosingChoice || _isChoosingTarget || _isChoosingRelicTarget)
            {
                if (_isDraggingPlayerCard)
                {
                    CancelDrag();
                }

                return;
            }

            if (_draggingCardView != null && _draggingCardView.IsPointerDown)
            {
                float distance = Vector2.Distance(
                    (Vector2)UnityEngine.Input.mousePosition,
                    _draggingCardView.PointerDownScreenPosition);
                if (distance > DragThreshold && !_dragThresholdExceeded)
                {
                    _dragThresholdExceeded = true;
                    _isDraggingPlayerCard = true;
                    if (_hoveredView == _draggingCardView)
                    {
                        NotifyHoverExit(_draggingCardView);
                    }
                }
            }
            else if (_isDraggingPlayerCard)
            {
                OnPlayerCardDragReleased();
                return;
            }

            if (!_isDraggingPlayerCard || _draggingCardView == null)
            {
                return;
            }

            UpdateDragLine();
            int newHoveredCell = GetCellIndexAtScreenPosition(UnityEngine.Input.mousePosition);
            if (newHoveredCell != _dragHoveredCellIndex)
            {
                if (_dragHoveredCellIndex > 0)
                {
                    HighlightCell(_dragHoveredCellIndex, _idleOutlineColor);
                }

                _dragHoveredCellIndex = newHoveredCell;
                if (_dragHoveredCellIndex > 0)
                {
                    if (IsCellValidDragTarget(_dragHoveredCellIndex))
                    {
                        HighlightCell(_dragHoveredCellIndex, _previewValidColor);
                    }
                    else
                    {
                        HighlightCell(_dragHoveredCellIndex, _previewInvalidColor);
                    }
                }
            }
        }

        private void UpdateDragLine()
        {
            if (_dragLine == null || _draggingCardView == null)
            {
                return;
            }

            _dragLine.gameObject.SetActive(true);
            _dragLine.positionCount = 2;
            _dragLine.SetPosition(0, _draggingCardView.transform.position);

            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                Plane cardPlane = new Plane(Vector3.up, _draggingCardView.transform.position);
                if (cardPlane.Raycast(ray, out float enter))
                {
                    _dragLine.SetPosition(1, ray.GetPoint(enter));
                }
                else
                {
                    _dragLine.SetPosition(1, ray.GetPoint(5f));
                }
            }
        }

        private void HideDragLine()
        {
            if (_dragLine != null)
            {
                _dragLine.gameObject.SetActive(false);
            }
        }

        private bool IsCellValidDragTarget(int cellIndex)
        {
            if (_context == null || _context.Grid == null)
            {
                return false;
            }

            GridCoord coord = GridCoord.FromCellIndex(cellIndex);
            CardInstance topCard = _context.Grid.GetTopCard(coord);
            if (topCard == null)
            {
                return true;
            }

            if (topCard.CardType == CardType.Player)
            {
                return false;
            }

            return topCard.IsFaceUp;
        }

        private async void OnPlayerCardDragReleased()
        {
            CardView dragView = _draggingCardView;
            int targetCell = _dragHoveredCellIndex;
            CancelDrag();

            if (targetCell < 1 || _facade == null)
            {
                return;
            }

            if (dragView != null)
            {
                dragView.MarkDragHandled();
            }

            GridCoord coord = GridCoord.FromCellIndex(targetCell);
            CardInstance topCard = _context.Grid.GetTopCard(coord);

            if (topCard != null && topCard.CardType != CardType.Player && topCard.IsFaceUp)
            {
                if (topCard.CardType == CardType.Item)
                {
                    DomainEventBatch storeBatch = await _facade.SubmitIntentAsync(new StoreItemIntent(topCard.InstanceId));
                    HandleSubmittedBatch(storeBatch);
                }
                else
                {
                    DomainEventBatch interactBatch = await _facade.SubmitIntentAsync(new InteractWithCardIntent(topCard.InstanceId));
                    HandleSubmittedBatch(interactBatch);
                }

                return;
            }

            if (topCard == null)
            {
                DomainEventBatch moveBatch = await _facade.SubmitIntentAsync(new MovePlayerIntent(coord));
                HandleSubmittedBatch(moveBatch);
            }
        }

        private void CancelDrag()
        {
            _isDraggingPlayerCard = false;
            _dragThresholdExceeded = false;
            _draggingCardView = null;
            _draggingCard = null;
            HideDragLine();
            if (_dragHoveredCellIndex > 0)
            {
                HighlightCell(_dragHoveredCellIndex, _idleOutlineColor);
            }

            _dragHoveredCellIndex = 0;
            if (!_isChoosingTarget && !_isChoosingRelicTarget)
            {
                ClearAllHighlights();
            }
        }

        private int GetCellIndexAtScreenPosition(Vector2 screenPos)
        {
            if (Camera.main == null)
            {
                return 0;
            }

            foreach (KeyValuePair<int, Canvas> pair in _gridCanvasesByCell)
            {
                RectTransform rect = pair.Value.transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                Camera eventCamera = pair.Value.worldCamera != null ? pair.Value.worldCamera : Camera.main;
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, eventCamera))
                {
                    return pair.Key;
                }
            }

            return 0;
        }

        public void NotifyPointerDownOnCard(CardView view)
        {
            if (!_isReady || _isAnimating || _isChoosingChoice || _isChoosingTarget || _isChoosingRelicTarget || view == null)
            {
                return;
            }

            if (!TryGetBackingCard(view, out CardInstance card))
            {
                return;
            }

            if (card.CardType == CardType.Player && card.Zone == CardZone.Grid)
            {
                _draggingCardView = view;
                _draggingCard = card;
                _dragThresholdExceeded = false;
            }
        }

        private void HandleScrollShortcuts()
        {
            float delta = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(delta) < 0.01f)
            {
                return;
            }

            if (IsPointerOverCanvasRect(_passiveRelicSlotCanvas))
            {
                CyclePassiveRelicDisplay(delta > 0f ? -1 : 1);
                return;
            }

            if (IsPointerOverCanvasRect(_activeRelicSlotCanvas))
            {
                return;
            }

            if (_isChoosingChoice)
            {
                return;
            }

            if (_isChoosingRelicTarget)
            {
                _preferCombatCamera = !_preferCombatCamera;
                ApplyCameraMode();
                return;
            }

            _preferCombatCamera = !_preferCombatCamera;
            ApplyCameraMode();
        }

        private void CyclePassiveRelicDisplay(int delta)
        {
            if (_context == null || _context.Relics.PassiveRelics.Count <= 1)
            {
                return;
            }

            int count = _context.Relics.PassiveRelics.Count;
            _passiveRelicDisplayIndex = (_passiveRelicDisplayIndex + delta + count) % count;
            RefreshPassiveRelicView();
            RefreshDetailPanelDefault();
        }

        private bool IsPointerOverCanvasRect(Canvas canvas)
        {
            if (canvas == null)
            {
                return false;
            }

            RectTransform rect = canvas.transform as RectTransform;
            if (rect == null)
            {
                return false;
            }

            Camera eventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, UnityEngine.Input.mousePosition, eventCamera);
        }

        private void ApplyCameraMode()
        {
            EnsureCameraBindings();
            SetCameraPriority(_globalCamera, (!_isChoosingChoice && !_isChoosingRelicTarget && !_preferCombatCamera) ? 30 : 0);
            SetCameraPriority(_combatCamera, (!_isChoosingChoice && !_isChoosingRelicTarget && _preferCombatCamera) ? 30 : 0);
            SetCameraPriority(_choiceCamera, _isChoosingChoice ? 40 : 0);
            SetCameraPriority(_relicTargetCamera, _isChoosingRelicTarget ? 50 : 0);
        }

        private static void SetCameraPriority(Component cameraComponent, int priority)
        {
            if (cameraComponent == null)
            {
                return;
            }

            System.Reflection.PropertyInfo property = cameraComponent.GetType().GetProperty("Priority");
            if (property != null && property.CanWrite)
            {
                property.SetValue(cameraComponent, priority);
            }
        }

        private Button CreateSimpleActionButton(Transform parent, string objectName, Vector2 anchoredPosition, out TextMeshProUGUI label)
        {
            Button templateButton = _simpleActionButtonPrefab;
            templateButton ??= FindButtonByNameContains(parent, "标准Button");
            templateButton ??= FindButtonByNameContains(parent, "标准按钮");
            if (templateButton != null)
            {
                Button clonedButton = Instantiate(templateButton, parent, false);
                clonedButton.name = objectName;
                RectTransform clonedRect = clonedButton.GetComponent<RectTransform>();
                if (clonedRect != null)
                {
                    clonedRect.anchorMin = new Vector2(0.5f, 0.5f);
                    clonedRect.anchorMax = new Vector2(0.5f, 0.5f);
                    clonedRect.anchoredPosition = anchoredPosition;
                }

                label = FindButtonLabel(clonedButton.transform);
                if (label != null && s_runtimeChineseFallback != null)
                {
                    label.font = s_runtimeChineseFallback;
                }

                return clonedButton;
            }

            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 74f);
            rect.anchoredPosition = anchoredPosition;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.92f, 0.88f, 0.78f, 0.94f);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 12f);
            textRect.offsetMax = new Vector2(-12f, -12f);
            label = textObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.color = new Color(0.12f, 0.10f, 0.08f, 1f);
            if (s_runtimeChineseFallback != null)
            {
                label.font = s_runtimeChineseFallback;
            }

            return buttonObject.GetComponent<Button>();
        }

        private void RememberVirtualDetail(CardView view, string title, string body)
        {
            if (view == null)
            {
                return;
            }

            _virtualDetails[view] = new VirtualDetail(title, body);
        }

        private void PruneVirtualDetails()
        {
            List<CardView> remove = null;
            foreach (KeyValuePair<CardView, VirtualDetail> pair in _virtualDetails)
            {
                if (pair.Key != null)
                {
                    continue;
                }

                remove ??= new List<CardView>();
                remove.Add(pair.Key);
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                _virtualDetails.Remove(remove[i]);
            }
        }

        private Color ResolveChoiceOptionColor(ChoiceSession session)
        {
            return session.ChoiceKind switch
            {
                "RelicChoice" => _relicColor,
                "StatUpgrade" => new Color(0.58f, 0.84f, 0.64f, 0.96f),
                _ => _choiceColor
            };
        }

        private async Task WaitSeconds(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(duration));
        }

        /// <summary>
        /// Awaits a DOTween Tween/Sequence completion asynchronously.
        /// Replacement for DOTween's AsyncWaitForCompletion which lives in Assembly-CSharp modules.
        /// </summary>
        private static Task AwaitTween(Tween tween)
        {
            if (tween == null || !tween.IsActive() || tween.IsComplete())
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            tween.OnComplete(() => tcs.TrySetResult(true));
            return tcs.Task;
        }

        /// <summary>
        /// Tweens a CanvasGroup's alpha using DOTween's DOFloat on the alpha property.
        /// Replacement for CanvasGroup.DOFade which lives in DOTweenModuleUI (Assembly-CSharp).
        /// </summary>
        private static Tweener FadeCanvasGroupAlpha(CanvasGroup canvasGroup, float endValue, float duration)
        {
            return DOTween.To(
                () => canvasGroup.alpha,
                a => canvasGroup.alpha = a,
                endValue,
                duration);
        }

        private static string TranslateFailureReason(string reason)
        {
            return reason switch
            {
                "NotAdjacent" => "只能移动到正交相邻的格子。",
                "CellOccupied" => "目标格子被占据。",
                "TargetCellNotEmpty" => "目标格子不是空位。",
                "CardNotOnGrid" => "该卡牌不在桌面上。",
                "NotPlayerAction" => "现在不是玩家行动回合。",
                "InvalidTarget" => "无效的目标。",
                "InventoryFull" => "道具栏已满。",
                "ItemSlotEmpty" => "该道具栏位是空的。",
                "NotEnoughGold" => "金币不足。",
                "ItemNotFound" => "找不到该道具。",
                "ItemCannotUse" => "当前还不能这样使用该道具。",
                "TargetFaceDown" => "背面朝下的牌暂时不能作为目标。",
                "RelicNotReady" => "遗物尚未就绪。",
                "RelicCannotActivate" => "当前还不能发动这个主动遗物。",
                "RoomNotCleared" => "房间尚未清理，无法选择路线。",
                "NoRouteChoices" => "当前没有可选路线。",
                "ChoiceAlreadyResolved" => "该选择已经做出。",
                "InteractionRejectedByModel" => "这张卡现在不能这样互动。",
                "RunAlreadyEnded" => "本局已经结束。",
                _ => reason
            };
        }

        public void NotifyHoverEnter(CardView view)
        {
            _hoveredView = view;
            if (view != null)
            {
                view.RectTransform.DOScale(_hoverScale, 0.18f).SetEase(Ease.OutQuart);
            }
        }

        public void NotifyHoverExit(CardView view)
        {
            if (_hoveredView == view)
            {
                _hoveredView = null;
            }

            if (view != null)
            {
                view.RectTransform.DOScale(1f, 0.18f).SetEase(Ease.OutQuart);
            }
        }

        private readonly struct VirtualDetail
        {
            public VirtualDetail(string title, string body)
            {
                Title = title ?? string.Empty;
                Body = body ?? string.Empty;
            }

            public string Title { get; }
            public string Body { get; }
        }
    }

    public sealed class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private TextMeshProUGUI _attackText;
        [SerializeField] private TextMeshProUGUI _defenseText;
        [SerializeField] private TextMeshProUGUI _overlayText;

        private DomainPresentationController _controller;
        private Action _onClick;

        private bool _isPointerDown;
        private Vector2 _pointerDownScreenPosition;
        private bool _dragWasHandled;

        public RectTransform RectTransform { get; private set; }
        public CanvasGroup CanvasGroup { get; private set; }
        public bool IsPointerDown => _isPointerDown;
        public Vector2 PointerDownScreenPosition => _pointerDownScreenPosition;

        public void Initialize(DomainPresentationController controller)
        {
            _controller = controller;
            RectTransform = GetComponent<RectTransform>();
            CanvasGroup = GetComponent<CanvasGroup>();
            if (CanvasGroup == null)
            {
                CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_background == null)
            {
                _background = GetComponent<Image>();
            }

            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                switch (texts[i].name)
                {
                    case "名字":
                        _titleText = texts[i];
                        break;
                    case "词条/简要描述":
                        _bodyText = texts[i];
                        break;
                    case "攻击":
                        _attackText = texts[i];
                        break;
                    case "防御":
                        _defenseText = texts[i];
                        break;
                }
            }

            if (_background != null && !_background.TryGetComponent<Button>(out _))
            {
                _background.gameObject.AddComponent<Button>();
            }

            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != _background)
                {
                    continue;
                }

                continue;
            }

            if (_overlayText == null)
            {
                GameObject overlayGo = new GameObject("Overlay", typeof(RectTransform), typeof(TextMeshProUGUI));
                overlayGo.transform.SetParent(transform, false);
                RectTransform overlayRect = overlayGo.GetComponent<RectTransform>();
                overlayRect.anchorMin = new Vector2(1f, 1f);
                overlayRect.anchorMax = new Vector2(1f, 1f);
                overlayRect.pivot = new Vector2(1f, 1f);
                overlayRect.sizeDelta = new Vector2(40f, 24f);
                overlayRect.anchoredPosition = new Vector2(-8f, -8f);
                _overlayText = overlayGo.GetComponent<TextMeshProUGUI>();
                _overlayText.alignment = TextAlignmentOptions.TopRight;
                _overlayText.fontSize = 16f;
                _overlayText.color = Color.black;
            }
        }

        public void SyncFromCard(CardInstance card, string title, string body, Color color)
        {
            if (card == null)
            {
                return;
            }

            SyncVirtual(title, body, card.HasHitPoints ? $"攻 {card.Attack}" : string.Empty, card.HasHitPoints ? $"防 {card.Defense}" : string.Empty, color, card.IsFaceUp);
            if (_attackText != null)
            {
                _attackText.gameObject.SetActive(card.HasHitPoints && card.CardType != CardType.Player ? card.Attack > 0 : card.CardType == CardType.Player);
                _attackText.text = card.HasHitPoints ? $"攻 {card.Attack}" : string.Empty;
            }

            if (_defenseText != null)
            {
                _defenseText.gameObject.SetActive(card.HasHitPoints && (card.Defense > 0 || card.CardType == CardType.Player));
                _defenseText.text = card.HasHitPoints ? $"防 {card.Defense}" : string.Empty;
            }

            if (_bodyText != null && card.HasHitPoints)
            {
                _bodyText.text = body + $"\n血 {card.CurrentHp}/{card.MaxHp}";
            }
        }

        public void SyncVirtual(string title, string body, string attack, string defense, Color color, bool isFaceUp)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }
            if (_bodyText != null)
            {
                _bodyText.text = body;
            }
            if (_attackText != null)
            {
                _attackText.text = attack;
                _attackText.gameObject.SetActive(!string.IsNullOrEmpty(attack));
            }
            if (_defenseText != null)
            {
                _defenseText.text = defense;
                _defenseText.gameObject.SetActive(!string.IsNullOrEmpty(defense));
            }
            if (_background != null)
            {
                _background.color = isFaceUp ? color : color * 0.4f;
            }
        }

        public void ApplyFaceState(bool isFaceUp, Color faceDownColor)
        {
            if (_background == null)
            {
                return;
            }

            if (isFaceUp)
            {
                _titleText?.gameObject.SetActive(true);
                _bodyText?.gameObject.SetActive(true);
                _attackText?.gameObject.SetActive(!string.IsNullOrEmpty(_attackText.text));
                _defenseText?.gameObject.SetActive(!string.IsNullOrEmpty(_defenseText.text));
                return;
            }

            ApplyFaceDownImmediate(faceDownColor);
        }

        public void ApplyFaceUpImmediate()
        {
            _titleText?.gameObject.SetActive(true);
            _bodyText?.gameObject.SetActive(true);
            _attackText?.gameObject.SetActive(!string.IsNullOrEmpty(_attackText != null ? _attackText.text : string.Empty));
            _defenseText?.gameObject.SetActive(!string.IsNullOrEmpty(_defenseText != null ? _defenseText.text : string.Empty));
        }

        public void ApplyFaceDownImmediate(Color faceDownColor)
        {
            if (_background != null)
            {
                _background.color = faceDownColor;
            }
            if (_titleText != null)
            {
                _titleText.text = "未翻开";
                _titleText.gameObject.SetActive(true);
            }
            if (_bodyText != null)
            {
                _bodyText.text = "等待翻开";
                _bodyText.gameObject.SetActive(true);
            }
            _attackText?.gameObject.SetActive(false);
            _defenseText?.gameObject.SetActive(false);
        }

        public void SetVisibleImmediate(bool visible)
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = visible ? 1f : 0f;
            }
        }

        public void SetOutline(Color color)
        {
            if (_background != null)
            {
                _background.raycastTarget = true;
                _background.material = null;
                _background.color = Color.Lerp(_background.color, color, 0.15f);
            }
        }

        public void SetOverlayText(string value)
        {
            if (_overlayText != null)
            {
                _overlayText.text = value;
                _overlayText.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        public void SetPassive(bool passive)
        {
            if (_overlayText != null && passive && string.IsNullOrEmpty(_overlayText.text))
            {
                _overlayText.text = "被";
                _overlayText.gameObject.SetActive(true);
            }
        }

        public void SetOnClick(Action onClick)
        {
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dragWasHandled)
            {
                _dragWasHandled = false;
                return;
            }

            if (_onClick != null)
            {
                _onClick();
                return;
            }

            _controller?.OnCardClicked(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _isPointerDown = true;
                _pointerDownScreenPosition = eventData.position;
                _dragWasHandled = false;
                _controller?.NotifyPointerDownOnCard(this);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _isPointerDown = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _controller?.NotifyHoverEnter(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _controller?.NotifyHoverExit(this);
        }

        public void MarkDragHandled()
        {
            _dragWasHandled = true;
        }
    }

    public sealed class ChoiceButtonView : MonoBehaviour
    {
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private Button _button;

        public void Initialize(TextMeshProUGUI title, TextMeshProUGUI body)
        {
            _title = title;
            _body = body;
            _button = GetComponent<Button>();
        }

        public void Bind(string title, string body, Action onClick)
        {
            if (_title != null)
            {
                _title.text = title;
            }

            if (_body != null)
            {
                _body.text = body;
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onClick?.Invoke());
            }
        }
    }

    public sealed class GridCellClickProxy : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private DomainPresentationController _controller;
        [SerializeField] private int _cellIndex;

        public void Initialize(DomainPresentationController controller, int cellIndex)
        {
            _controller = controller;
            _cellIndex = cellIndex;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _controller?.OnCellClicked(_cellIndex);
        }
    }
}
