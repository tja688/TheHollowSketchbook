using System.Collections.Generic;
using Game.Core;
using Game.Core.Entities;
using Game.Core.Map;
using Game.Core.Models;
using Game.Core.Rewards;
using Game.Core.Rooms;
using Game.Core.Runs;
using Game.Core.Saves;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.RunFlow
{
    /// <summary>
    /// Run flow controller. StS combat launch logic removed.
    /// BOUNDARY: This is a skeleton. CombatManager, CombatPrototypeController, and StarterContentRegistry
    /// references removed. A new grid-based combat launch should be added here.
    /// </summary>
    public sealed class PrototypeRunController : MonoBehaviour
    {
        [SerializeField] private int _seed = 12345;

        private RunManager _runManager;
        private SaveManager _saveManager;
        private PrototypeRunMapView _mapView;
        private PrototypeRunRoomPanel _roomPanel;
        private PrototypeRewardPanel _rewardPanel;
        private Text _statusText;

        public void StartPrototypeRun(int seed)
        {
            StartPrototypeRun(seed, false);
        }

        public void StartPrototypeRun(int seed, bool continueSavedRunIfPresent)
        {
            _seed = seed;
            BuildUi();

            _saveManager = new SaveManager(Application.persistentDataPath);
            _runManager = new RunManager(saveManager: _saveManager);
            _runManager.RoomEntered += OnRoomEntered;
            _runManager.RoomCompleted += OnRoomCompleted;
            _runManager.MapChanged += OnMapChanged;
            _runManager.RunEnded += OnRunEnded;

            if (continueSavedRunIfPresent)
            {
                RunState loaded = _runManager.LoadRun();
                if (loaded != null)
                {
                    return;
                }
            }

            // BOUNDARY: Character and Act models must be registered in ModelDb by the new content system.
            CharacterModel character = ModelDb.Get<CharacterModel>(new ModelId("Character", "PrototypeHero"));
            IReadOnlyList<ActModel> acts = new[] { ModelDb.Get<ActModel>(new ModelId("Act", "PrototypeAct")) };
            _runManager.StartNewRun(character, seed, acts);
        }

        private void BuildUi()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("RunCanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            _statusText = CreateText(canvas.transform, "StatusText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 40f), 24, TextAnchor.MiddleCenter);
            _mapView = CreateMapView(canvas.transform);
            _roomPanel = CreateRoomPanel(canvas.transform);
            _rewardPanel = CreateRewardPanel(canvas.transform);

            _mapView.NodeSelected += OnMapNodeSelected;
            _roomPanel.PrimaryClicked += OnRoomPrimaryClicked;
            _roomPanel.SecondaryClicked += OnRoomSecondaryClicked;
            _rewardPanel.RewardSelected += OnRewardSelected;
            _rewardPanel.ChoiceSelected += OnChoiceSelected;
            _rewardPanel.ChoiceSkipped += OnChoiceSkipped;
            _rewardPanel.ContinueClicked += OnRewardContinueClicked;
        }

        private void OnMapChanged(RunState run)
        {
            if (run.CurrentRoom != null)
            {
                return;
            }

            _statusText.text = run.IsGameOver ? "Run Finished" : "Select next node";
            _rewardPanel.Hide();
            _roomPanel.SetVisible(false);
            _mapView.ShowMap(run, CanSelectPoint);
        }

        private void OnMapNodeSelected(MapCoord coord)
        {
            _mapView.Hide();
            _runManager.EnterMapCoord(coord);
        }

        private void OnRoomEntered(AbstractRoom room)
        {
            _statusText.text = "Entered: " + room.RoomType;

            // BOUNDARY: StS combat launch removed. Add grid-based combat launch here.
            // if (room is CombatRoom combatRoom) { StartGridCombat(combatRoom); return; }

            _roomPanel.Configure(room);
        }

        private void OnRoomCompleted(AbstractRoom room)
        {
            _statusText.text = room.RoomType + " completed";
            if (room.Rewards.Count > 0)
            {
                _rewardPanel.ShowRewards(room.Rewards);
            }
            else
            {
                _runManager.ProceedToMap();
            }
        }

        private void OnRoomPrimaryClicked()
        {
            AbstractRoom room = _runManager.State.CurrentRoom;
            if (room is EventRoomPlaceholder eventRoom)
            {
                eventRoom.TakeRisk(_runManager.State.Players[0]);
                _runManager.CompleteCurrentRoom();
            }
            else if (room is RestSiteRoomPlaceholder restRoom)
            {
                restRoom.Rest(_runManager.State.Players[0]);
                _runManager.CompleteCurrentRoom();
            }
            else
            {
                _runManager.CompleteCurrentRoom();
            }
        }

        private void OnRoomSecondaryClicked()
        {
            _runManager.CompleteCurrentRoom();
        }

        private void OnRewardSelected(Reward reward)
        {
            reward.Resolve(_runManager.State, _runManager.State.Players[0]);
            _runManager.SaveRun();
            _rewardPanel.ShowRewards(_runManager.State.CurrentRoom.Rewards);
        }

        private void OnChoiceSelected(ChoiceReward reward, int index)
        {
            reward.Select(index);
            reward.Resolve(_runManager.State, _runManager.State.Players[0]);
            _runManager.SaveRun();
            _rewardPanel.ShowRewards(_runManager.State.CurrentRoom.Rewards);
        }

        private void OnChoiceSkipped(ChoiceReward reward)
        {
            reward.Skip();
            reward.Resolve(_runManager.State, _runManager.State.Players[0]);
            _runManager.SaveRun();
            _rewardPanel.ShowRewards(_runManager.State.CurrentRoom.Rewards);
        }

        private void OnRewardContinueClicked()
        {
            if (_runManager.State.CurrentRoom != null && !_runManager.State.CurrentRoom.HasPendingRewards)
            {
                _rewardPanel.Hide();
                _runManager.ProceedToMap();
            }
        }

        private void OnRunEnded(RunState run)
        {
            _statusText.text = "Boss defeated. Run finished.";
            _mapView.Hide();
            _roomPanel.SetVisible(false);
            _rewardPanel.Hide();
        }

        private bool CanSelectPoint(MapPoint point)
        {
            if (_runManager?.State?.Map == null || point == null)
            {
                return false;
            }

            if (_runManager.State.CurrentRoom != null)
            {
                return false;
            }

            if (_runManager.State.CurrentMapCoord == null)
            {
                foreach (MapPoint child in _runManager.State.Map.StartingMapPoint.Children)
                {
                    if (ReferenceEquals(child, point))
                    {
                        return true;
                    }
                }

                return false;
            }

            MapPoint current = _runManager.State.Map.GetPoint(_runManager.State.CurrentMapCoord.Value);
            foreach (MapPoint child in current.Children)
            {
                if (ReferenceEquals(child, point))
                {
                    return true;
                }
            }

            return false;
        }

        private static PrototypeRunMapView CreateMapView(Transform parent)
        {
            GameObject root = new GameObject("PrototypeRunMapView", typeof(RectTransform), typeof(Image), typeof(PrototypeRunMapView));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta = new Vector2(900f, 650f);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            GameObject legendGo = new GameObject("Legend", typeof(RectTransform), typeof(Text));
            legendGo.transform.SetParent(root.transform, false);
            RectTransform legendRt = legendGo.GetComponent<RectTransform>();
            legendRt.anchorMin = new Vector2(0.5f, 1f);
            legendRt.anchorMax = new Vector2(0.5f, 1f);
            legendRt.anchoredPosition = new Vector2(0f, -30f);
            legendRt.sizeDelta = new Vector2(500f, 30f);
            Text legend = legendGo.GetComponent<Text>();
            legend.alignment = TextAnchor.MiddleCenter;
            legend.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            legend.color = Color.white;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(root.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.anchoredPosition = new Vector2(0f, -10f);
            contentRt.sizeDelta = new Vector2(760f, 500f);

            Button prefab = CreateButtonPrefab("MapNodeButton", root.transform, new Vector2(70f, 40f));
            prefab.gameObject.SetActive(false);

            PrototypeRunMapView view = root.GetComponent<PrototypeRunMapView>();
            typeof(PrototypeRunMapView).GetField("_contentRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(view, contentRt);
            typeof(PrototypeRunMapView).GetField("_nodeButtonPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(view, prefab);
            typeof(PrototypeRunMapView).GetField("_legendText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(view, legend);
            return view;
        }

        private static PrototypeRunRoomPanel CreateRoomPanel(Transform parent)
        {
            GameObject root = new GameObject("PrototypeRunRoomPanel", typeof(RectTransform), typeof(Image), typeof(PrototypeRunRoomPanel));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(560f, 300f);
            root.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            Text title = CreateText(root.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(420f, 40f), 28, TextAnchor.MiddleCenter);
            Text body = CreateText(root.transform, "Body", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(460f, 80f), 20, TextAnchor.MiddleCenter);
            Button primary = CreateButtonPrefab("Primary", root.transform, new Vector2(220f, 44f));
            primary.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -60f);
            Text primaryText = primary.GetComponentInChildren<Text>();
            Button secondary = CreateButtonPrefab("Secondary", root.transform, new Vector2(220f, 44f));
            secondary.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -115f);
            Text secondaryText = secondary.GetComponentInChildren<Text>();

            PrototypeRunRoomPanel panel = root.GetComponent<PrototypeRunRoomPanel>();
            typeof(PrototypeRunRoomPanel).GetField("_titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, title);
            typeof(PrototypeRunRoomPanel).GetField("_bodyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, body);
            typeof(PrototypeRunRoomPanel).GetField("_primaryButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, primary);
            typeof(PrototypeRunRoomPanel).GetField("_primaryLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, primaryText);
            typeof(PrototypeRunRoomPanel).GetField("_secondaryButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, secondary);
            typeof(PrototypeRunRoomPanel).GetField("_secondaryLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, secondaryText);
            panel.SetVisible(false);
            return panel;
        }

        private static PrototypeRewardPanel CreateRewardPanel(Transform parent)
        {
            GameObject root = new GameObject("PrototypeRewardPanel", typeof(RectTransform), typeof(Image), typeof(PrototypeRewardPanel));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(620f, 420f);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

            Text title = CreateText(root.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(420f, 36f), 28, TextAnchor.MiddleCenter);
            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(root.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.anchoredPosition = new Vector2(0f, -20f);
            contentRt.sizeDelta = new Vector2(500f, 300f);
            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            Button prefab = CreateButtonPrefab("RewardButton", root.transform, new Vector2(500f, 42f));
            prefab.gameObject.SetActive(false);

            PrototypeRewardPanel panel = root.GetComponent<PrototypeRewardPanel>();
            typeof(PrototypeRewardPanel).GetField("_contentRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, contentRt);
            typeof(PrototypeRewardPanel).GetField("_buttonPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, prefab);
            typeof(PrototypeRewardPanel).GetField("_titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(panel, title);
            panel.Hide();
            return panel;
        }

        private static Button CreateButtonPrefab(string name, Transform parent, Vector2 size)
        {
            GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.26f, 0.95f);
            Button button = buttonGo.GetComponent<Button>();

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 20;
            text.text = name;
            return button;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }
    }
}
