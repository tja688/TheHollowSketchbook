using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Entities;
using Game.Presentation.Combat.Cards;
using Game.Presentation.Combat.Creatures;
using Game.Presentation.Combat.UI;
using Game.Presentation.Input;
using Game.Presentation.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Combat
{
    public sealed class CombatPrototypeController : MonoBehaviour
    {
        private CombatManager _combatManager;
        private PlayerHandView _handView;
        private CardDragController _dragController;
        private CombatInputController _inputController;
        private CombatRaycastService _raycastService;
        private EnergyPanel _energyPanel;
        private EndTurnButton _endTurnButton;
        private PileButtonsView _pileButtonsView;
        private readonly List<EnemyView> _enemyViews = new List<EnemyView>();
        private Transform _enemyRoot;
        private Transform _playArea;
        private Transform _uiRoot;

        public void Bind(CombatManager combatManager)
        {
            _combatManager = combatManager;
            GameServices.EnsureInitialized();
            BuildSceneObjects();
            BindViews();
            SubscribeEvents();
        }

        private void BuildSceneObjects()
        {
            // Camera
            if (Camera.main == null)
            {
                var camGo = new GameObject("MainCamera");
                var cam = camGo.AddComponent<Camera>();
                cam.transform.SetParent(transform, false);
                cam.transform.position = new Vector3(0f, 5f, -8f);
                cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
                cam.orthographic = false;
                cam.fieldOfView = 60f;
            }

            // Light
            var lightGo = new GameObject("DirectionalLight");
            lightGo.transform.SetParent(transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Play area (ground plane for raycast)
            _playArea = new GameObject("PlayArea").transform;
            _playArea.SetParent(transform, false);
            _playArea.position = new Vector3(0f, 0f, 2f);
            var playAreaCollider = _playArea.gameObject.AddComponent<BoxCollider>();
            playAreaCollider.size = new Vector3(20f, 0.1f, 10f);
            playAreaCollider.center = new Vector3(0f, -0.05f, 0f);
            playAreaCollider.gameObject.layer = LayerMask.NameToLayer("Default");

            // Enemy root
            _enemyRoot = new GameObject("EnemyRoot").transform;
            _enemyRoot.SetParent(transform, false);
            _enemyRoot.position = new Vector3(0f, 0f, 3f);

            // Hand anchor
            var handAnchor = new GameObject("HandAnchor").transform;
            handAnchor.SetParent(transform, false);
            handAnchor.position = new Vector3(0f, 0.5f, -2f);

            // Draw pile anchor
            var drawAnchor = new GameObject("DrawPileAnchor").transform;
            drawAnchor.SetParent(transform, false);
            drawAnchor.position = new Vector3(-4f, 0.5f, -2f);

            // Discard pile anchor
            var discardAnchor = new GameObject("DiscardPileAnchor").transform;
            discardAnchor.SetParent(transform, false);
            discardAnchor.position = new Vector3(4f, 0.5f, -2f);

            // Card prefab template
            var cardPrefab = CreateCardPrefab();

            // Card pool
            var poolGo = new GameObject("CardViewPool");
            poolGo.transform.SetParent(transform, false);
            var cardPool = poolGo.AddComponent<CardViewPool>();
            cardPool.GetType().GetField("_prefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cardPool, cardPrefab);

            // Arc layout
            var layoutGo = new GameObject("ArcHandLayout");
            layoutGo.transform.SetParent(transform, false);
            var layout = layoutGo.AddComponent<ArcHandLayout>();
            layout.Anchor = handAnchor;
            layout.Radius = 4f;
            layout.ArcAngle = 80f;
            layout.YOffset = 0.5f;

            // Hand view
            var handGo = new GameObject("PlayerHandView");
            handGo.transform.SetParent(transform, false);
            _handView = handGo.AddComponent<PlayerHandView>();
            typeof(PlayerHandView).GetField("_pool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_handView, cardPool);
            typeof(PlayerHandView).GetField("_layout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_handView, layout);
            typeof(PlayerHandView).GetField("_drawPileAnchor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_handView, drawAnchor);
            typeof(PlayerHandView).GetField("_discardPileAnchor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_handView, discardAnchor);

            // Raycast service
            var rayGo = new GameObject("CombatRaycastService");
            rayGo.transform.SetParent(transform, false);
            _raycastService = rayGo.AddComponent<CombatRaycastService>();

            // Input controller
            var inputGo = new GameObject("CombatInputController");
            inputGo.transform.SetParent(transform, false);
            _inputController = inputGo.AddComponent<CombatInputController>();

            // Drag controller
            var dragGo = new GameObject("CardDragController");
            dragGo.transform.SetParent(transform, false);
            _dragController = dragGo.AddComponent<CardDragController>();
            typeof(CardDragController).GetField("_handView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_dragController, _handView);
            typeof(CardDragController).GetField("_raycastService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_dragController, _raycastService);
            typeof(CardDragController).GetField("_inputController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_dragController, _inputController);

            // UI Overlay Canvas
            var uiGo = new GameObject("CombatUI");
            uiGo.transform.SetParent(transform, false);
            _uiRoot = uiGo.transform;
            var canvas = uiGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = uiGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            uiGo.AddComponent<GraphicRaycaster>();

            // Energy panel
            var energyGo = new GameObject("EnergyPanel", typeof(RectTransform));
            energyGo.transform.SetParent(uiGo.transform, false);
            energyGo.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            energyGo.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0f);
            energyGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(120f, 80f);
            energyGo.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 60f);
            var energyBg = energyGo.AddComponent<Image>();
            energyBg.color = new Color(0.1f, 0.1f, 0.2f, 0.8f);
            var energyTextGo = new GameObject("Text", typeof(RectTransform));
            energyTextGo.transform.SetParent(energyGo.transform, false);
            energyTextGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            energyTextGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            energyTextGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            energyTextGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var energyTmp = energyTextGo.AddComponent<TextMeshProUGUI>();
            energyTmp.alignment = TextAlignmentOptions.Center;
            energyTmp.fontSize = 32f;
            _energyPanel = energyGo.AddComponent<EnergyPanel>();
            typeof(EnergyPanel).GetField("_energyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_energyPanel, energyTmp);

            // End turn button
            var endTurnGo = new GameObject("EndTurnButton", typeof(RectTransform));
            endTurnGo.transform.SetParent(uiGo.transform, false);
            endTurnGo.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0f);
            endTurnGo.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0f);
            endTurnGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120f, 80f);
            endTurnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 60f);
            var endTurnBg = endTurnGo.AddComponent<Image>();
            endTurnBg.color = new Color(0.8f, 0.3f, 0.2f, 1f);
            var endTurnBtn = endTurnGo.AddComponent<Button>();
            var endTurnTextGo = new GameObject("Text", typeof(RectTransform));
            endTurnTextGo.transform.SetParent(endTurnGo.transform, false);
            endTurnTextGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            endTurnTextGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            endTurnTextGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            endTurnTextGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var endTurnTmp = endTurnTextGo.AddComponent<TextMeshProUGUI>();
            endTurnTmp.text = "End Turn";
            endTurnTmp.alignment = TextAlignmentOptions.Center;
            endTurnTmp.fontSize = 24f;
            _endTurnButton = endTurnGo.AddComponent<EndTurnButton>();
            typeof(EndTurnButton).GetField("_button", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_endTurnButton, endTurnBtn);
            typeof(EndTurnButton).GetField("_canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_endTurnButton, endTurnGo.AddComponent<CanvasGroup>());

            // Pile buttons
            var pilesGo = new GameObject("PileButtonsView");
            pilesGo.transform.SetParent(uiGo.transform, false);
            var pilesRt = pilesGo.AddComponent<RectTransform>();
            pilesRt.anchorMin = new Vector2(0.5f, 0f);
            pilesRt.anchorMax = new Vector2(0.5f, 0f);
            pilesRt.anchoredPosition = new Vector2(0f, 40f);
            pilesRt.sizeDelta = new Vector2(600f, 40f);
            var hlg = pilesGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            var drawLabel = CreatePileLabel(pilesGo.transform, "Draw");
            var discardLabel = CreatePileLabel(pilesGo.transform, "Discard");
            var exhaustLabel = CreatePileLabel(pilesGo.transform, "Exhaust");

            _pileButtonsView = pilesGo.AddComponent<PileButtonsView>();
            typeof(PileButtonsView).GetField("_drawPileText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_pileButtonsView, drawLabel);
            typeof(PileButtonsView).GetField("_discardPileText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_pileButtonsView, discardLabel);
            typeof(PileButtonsView).GetField("_exhaustPileText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_pileButtonsView, exhaustLabel);
        }

        private CardView CreateCardPrefab()
        {
            var go = new GameObject("CardViewPrefab");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var cardView = go.AddComponent<CardView>();
            // Awake() will auto-create visuals via EnsureVisuals/EnsureCollider
            return cardView;
        }

        private TextMeshProUGUI CreatePileLabel(Transform parent, string label)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 40f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{label}(0)";
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void BindViews()
        {
            if (_combatManager?.State == null)
            {
                return;
            }

            var state = _combatManager.State;
            Player player = state.Players.Count > 0 ? state.Players[0] : null;

            if (player != null)
            {
                _handView.Bind(player);
                _energyPanel.Bind(player);
                _pileButtonsView.Bind(player);
                _inputController.Bind(_combatManager, player);
                _endTurnButton.Bind(_combatManager, player);
            }

            // Enemies
            foreach (var enemy in state.Enemies)
            {
                var enemyView = CreateEnemyView(enemy);
                _enemyViews.Add(enemyView);
            }
        }

        private EnemyView CreateEnemyView(Creature enemy)
        {
            var go = new GameObject($"EnemyView_{enemy.EnemyModel.Name}");
            go.transform.SetParent(_enemyRoot, false);

            // Position enemies in a row
            int index = _enemyViews.Count;
            go.transform.localPosition = new Vector3((index - 0.5f) * 2.5f, 0f, 0f);

            // Visual placeholder
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);
            visualGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.2f, 0.2f);
            // Create a simple square sprite for the enemy visual
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.sprite = sprite;
            sr.transform.localScale = new Vector3(1.5f, 2.5f, 1f);

            // Enemy collider for raycast
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.center = new Vector3(0f, 1f, 0f);

            // Health bar canvas
            var hbCanvasGo = new GameObject("HealthBarCanvas");
            hbCanvasGo.transform.SetParent(go.transform, false);
            hbCanvasGo.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            var hbCanvas = hbCanvasGo.AddComponent<Canvas>();
            hbCanvas.renderMode = RenderMode.WorldSpace;
            hbCanvas.sortingOrder = 10;
            var hbRt = hbCanvasGo.GetComponent<RectTransform>();
            hbRt.sizeDelta = new Vector2(2f, 0.6f);

            // HP text
            var hpTextGo = new GameObject("HPText", typeof(RectTransform));
            hpTextGo.transform.SetParent(hbCanvasGo.transform, false);
            hpTextGo.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            hpTextGo.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            hpTextGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            hpTextGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var hpTmp = hpTextGo.AddComponent<TextMeshProUGUI>();
            hpTmp.fontSize = 0.25f;
            hpTmp.alignment = TextAlignmentOptions.Center;

            // HP fill
            var hpFillGo = new GameObject("HPFill", typeof(RectTransform));
            hpFillGo.transform.SetParent(hbCanvasGo.transform, false);
            hpFillGo.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            hpFillGo.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
            hpFillGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            hpFillGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var hpFillImg = hpFillGo.AddComponent<Image>();
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.color = Color.green;

            // Block text
            var blockGo = new GameObject("BlockText", typeof(RectTransform));
            blockGo.transform.SetParent(hbCanvasGo.transform, false);
            blockGo.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            blockGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.3f, 0.5f);
            blockGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            blockGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var blockTmp = blockGo.AddComponent<TextMeshProUGUI>();
            blockTmp.fontSize = 0.2f;
            blockTmp.alignment = TextAlignmentOptions.Center;
            blockTmp.color = Color.cyan;

            // Powers text
            var powersGo = new GameObject("PowersText", typeof(RectTransform));
            powersGo.transform.SetParent(hbCanvasGo.transform, false);
            powersGo.GetComponent<RectTransform>().anchorMin = new Vector2(0.3f, 0f);
            powersGo.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
            powersGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            powersGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var powersTmp = powersGo.AddComponent<TextMeshProUGUI>();
            powersTmp.fontSize = 0.15f;
            powersTmp.alignment = TextAlignmentOptions.Left;

            var healthBar = hbCanvasGo.AddComponent<CreatureHealthBar>();
            healthBar.GetType().GetField("_hpText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(healthBar, hpTmp);
            healthBar.GetType().GetField("_hpFillImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(healthBar, hpFillImg);
            healthBar.GetType().GetField("_blockText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(healthBar, blockTmp);
            healthBar.GetType().GetField("_blockRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(healthBar, blockGo);
            healthBar.GetType().GetField("_powersText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(healthBar, powersTmp);
            healthBar.GetType().GetField("_powersRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(healthBar, powersGo);

            // Intent view
            var intentGo = new GameObject("IntentView");
            intentGo.transform.SetParent(go.transform, false);
            intentGo.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            var intentCanvas = intentGo.AddComponent<Canvas>();
            intentCanvas.renderMode = RenderMode.WorldSpace;
            intentCanvas.worldCamera = Camera.main;
            intentCanvas.sortingOrder = 11;
            var intentRt = intentGo.GetComponent<RectTransform>();
            intentRt.sizeDelta = new Vector2(1.5f, 0.5f);
            var intentCg = intentGo.AddComponent<CanvasGroup>();

            var intentIconGo = new GameObject("Icon", typeof(RectTransform));
            intentIconGo.transform.SetParent(intentGo.transform, false);
            intentIconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0.4f, 0.4f);
            var intentImg = intentIconGo.AddComponent<Image>();
            intentImg.color = Color.red;

            var intentDescGo = new GameObject("Desc", typeof(RectTransform));
            intentDescGo.transform.SetParent(intentGo.transform, false);
            intentDescGo.GetComponent<RectTransform>().anchorMin = new Vector2(0.3f, 0f);
            intentDescGo.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            intentDescGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            intentDescGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var intentTmp = intentDescGo.AddComponent<TextMeshProUGUI>();
            intentTmp.fontSize = 0.2f;
            intentTmp.alignment = TextAlignmentOptions.Left;

            var intentView = intentGo.AddComponent<IntentView>();
            intentView.GetType().GetField("_typeIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(intentView, intentImg);
            intentView.GetType().GetField("_descriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(intentView, intentTmp);
            intentView.GetType().GetField("_canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(intentView, intentCg);

            var enemyView = go.AddComponent<EnemyView>();
            enemyView.GetType().GetField("_portraitRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemyView, sr);
            enemyView.GetType().GetField("_healthBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemyView, healthBar);
            enemyView.GetType().GetField("_intentView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemyView, intentView);
            enemyView.GetType().GetField("_canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemyView, go.AddComponent<CanvasGroup>());

            enemyView.Bind(enemy);
            return enemyView;
        }

        private void SubscribeEvents()
        {
            if (_combatManager == null)
            {
                return;
            }

            _combatManager.TurnStarted += OnTurnStarted;
            _combatManager.TurnEnded += OnTurnEnded;
            _combatManager.CombatWon += OnCombatWon;
            _combatManager.CombatEnded += OnCombatEnded;
            _combatManager.EnemyIntentRolled += OnEnemyIntentRolled;
            _combatManager.CreaturesChanged += OnCreaturesChanged;
        }

        private void OnTurnStarted(CombatState state)
        {
            _energyPanel?.Refresh();
            _pileButtonsView?.Refresh();
            _handView?.ArrangeCards();
        }

        private void OnTurnEnded(CombatState state)
        {
            _energyPanel?.Refresh();
            _pileButtonsView?.Refresh();
        }

        private void OnCreaturesChanged(CombatState state)
        {
            _energyPanel?.Refresh();
            _pileButtonsView?.Refresh();
        }

        private void OnEnemyIntentRolled(Creature enemy, EnemyIntent intent)
        {
            foreach (var view in _enemyViews)
            {
                if (view.Creature == enemy)
                {
                    view.GetComponentInChildren<IntentView>()?.ShowIntent(intent);
                    break;
                }
            }
        }

        private void OnCombatWon(CombatState state)
        {
            Debug.Log("[CombatPrototype] Combat WON!");
            ShowCombatResult("VICTORY");
        }

        private void OnCombatEnded(CombatState state)
        {
            if (!state.PlayerWon)
            {
                Debug.Log("[CombatPrototype] Combat LOST!");
                ShowCombatResult("DEFEAT");
            }
        }

        private void ShowCombatResult(string text)
        {
            var go = new GameObject("CombatResult", typeof(RectTransform));
            go.transform.SetParent(_uiRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600f, 200f);
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            var tmpGo = new GameObject("Text", typeof(RectTransform));
            tmpGo.transform.SetParent(go.transform, false);
            tmpGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            tmpGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            tmpGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            tmpGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            var tmp = tmpGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 72f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = text == "VICTORY" ? Color.yellow : Color.red;
        }

        private void OnDestroy()
        {
            if (_combatManager != null)
            {
                _combatManager.TurnStarted -= OnTurnStarted;
                _combatManager.TurnEnded -= OnTurnEnded;
                _combatManager.CombatWon -= OnCombatWon;
                _combatManager.CombatEnded -= OnCombatEnded;
                _combatManager.EnemyIntentRolled -= OnEnemyIntentRolled;
                _combatManager.CreaturesChanged -= OnCreaturesChanged;
                _combatManager.Reset();
            }
        }
    }
}
