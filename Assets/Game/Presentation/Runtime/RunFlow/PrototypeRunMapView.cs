using System;
using System.Collections.Generic;
using Game.Core.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.RunFlow
{
    public sealed class PrototypeRunMapView : MonoBehaviour
    {
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private Button _nodeButtonPrefab;
        [SerializeField] private Text _legendText;

        private readonly List<Button> _buttons = new List<Button>();

        public event Action<MapCoord> NodeSelected;

        public void ShowMap(Game.Core.Runs.RunState run, Func<MapPoint, bool> canSelect)
        {
            gameObject.SetActive(true);
            ClearButtons();
            if (run?.Map == null)
            {
                return;
            }

            _legendText.text = "Map: click a reachable node.";
            const float spacingX = 90f;
            const float spacingY = 70f;
            foreach (MapPoint point in run.Map.Points)
            {
                Button button = Instantiate(_nodeButtonPrefab, _contentRoot);
                button.gameObject.SetActive(true);
                RectTransform rt = button.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2((point.Coord.Column - (run.Map.ColumnCount - 1) * 0.5f) * spacingX, point.Coord.Row * spacingY);
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = ShortLabel(point);
                }

                bool interactable = canSelect != null && canSelect(point);
                button.interactable = interactable;
                MapCoord coord = point.Coord;
                button.onClick.AddListener(() => NodeSelected?.Invoke(coord));
                _buttons.Add(button);
            }
        }

        public void Hide()
        {
            ClearButtons();
            gameObject.SetActive(false);
        }

        private static string ShortLabel(MapPoint point)
        {
            return point.PointType switch
            {
                MapPointType.Start => "S",
                MapPointType.Monster => "M",
                MapPointType.Event => "?",
                MapPointType.Treasure => "T",
                MapPointType.Shop => "$",
                MapPointType.Elite => "E",
                MapPointType.Rest => "R",
                MapPointType.Boss => "B",
                _ => point.PointType.ToString()
            };
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null)
                {
                    Destroy(_buttons[i].gameObject);
                }
            }

            _buttons.Clear();
        }
    }
}
