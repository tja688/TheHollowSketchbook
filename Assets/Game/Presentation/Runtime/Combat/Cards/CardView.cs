using System.Threading.Tasks;
using Game.Core;
using Game.Core.Cards;
using Game.Presentation.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Combat.Cards
{
    public sealed class CardView : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Settings")]
        [SerializeField] private Vector2 _cardSize = new Vector2(1.4f, 2.0f);
        [SerializeField] private float _hoverLift = 0.3f;
        [SerializeField] private float _hoverScale = 1.2f;
        [SerializeField] private float _hoverDuration = 0.15f;

        private CardModel _model;
        private Vector3 _basePosition;
        private Vector3 _baseScale = Vector3.one;
        private bool _isHovered;
        private BoxCollider _collider;

        public CardModel Model => _model;
        public bool IsInteractable { get; private set; }
        public bool IsDragging { get; set; }

        private void Awake()
        {
            EnsureVisuals();
            EnsureCollider();

            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.WorldSpace;
                if (Camera.main != null)
                    _canvas.worldCamera = Camera.main;
                _canvas.sortingOrder = 10;
            }
        }

        public void Bind(CardModel model)
        {
            _model = model;
            Refresh();
        }

        public void Refresh()
        {
            if (_model == null)
                return;

            if (_nameText != null)
                _nameText.text = _model.Name;

            if (_costText != null)
                _costText.text = _model.EnergyCost.ToString();

            if (_descriptionText != null)
                _descriptionText.text = _model.Description;

            if (_backgroundImage != null)
                _backgroundImage.color = GetTypeColor(_model.Type);
        }

        public void SetInteractable(bool value)
        {
            IsInteractable = value;
            if (_backgroundImage != null)
            {
                _backgroundImage.color = value ? GetTypeColor(_model.Type) : Color.gray;
            }
        }

        public void PlayHover(bool hovered)
        {
            if (_isHovered == hovered)
                return;

            _isHovered = hovered;

            if (IsDragging)
                return;

            Vector3 targetScale = hovered ? _baseScale * _hoverScale : _baseScale;
            Vector3 targetPos = hovered ? _basePosition + new Vector3(0f, _hoverLift, 0f) : _basePosition;

            if (GameServices.Tween != null)
            {
                _ = GameServices.Tween.ScaleTo(transform, targetScale, _hoverDuration);
                _ = GameServices.Tween.MoveTo(transform, targetPos, _hoverDuration);
            }
            else
            {
                transform.localScale = targetScale;
                transform.position = targetPos;
            }
        }

        public void PlayMoveTo(Vector3 position, Quaternion rotation, float duration)
        {
            _basePosition = position;

            Vector3 targetPos = _isHovered && !IsDragging
                ? position + new Vector3(0f, _hoverLift, 0f)
                : position;

            if (GameServices.Tween != null)
            {
                _ = GameServices.Tween.MoveTo(transform, targetPos, duration);
                _ = GameServices.Tween.RotateTo(transform, rotation, duration);
            }
            else
            {
                transform.position = targetPos;
                transform.rotation = rotation;
            }
        }

        public void SetBaseScale(Vector3 scale, float duration = 0f)
        {
            _baseScale = scale;
            Vector3 targetScale = _isHovered && !IsDragging ? scale * _hoverScale : scale;

            if (GameServices.Tween != null && duration > 0f)
            {
                _ = GameServices.Tween.ScaleTo(transform, targetScale, duration);
            }
            else
            {
                transform.localScale = targetScale;
            }
        }

        private void EnsureVisuals()
        {
            if (_canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas");
                canvasGo.transform.SetParent(transform, false);
                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.WorldSpace;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (_backgroundImage == null)
            {
                GameObject bgGo = new GameObject("Background");
                bgGo.transform.SetParent(_canvas.transform, false);
                _backgroundImage = bgGo.AddComponent<Image>();
                RectTransform bgRect = bgGo.GetComponent<RectTransform>();
                bgRect.sizeDelta = _cardSize;
                bgRect.anchoredPosition = Vector2.zero;
            }

            if (_nameText == null)
            {
                GameObject nameGo = new GameObject("NameText");
                nameGo.transform.SetParent(_canvas.transform, false);
                _nameText = nameGo.AddComponent<TextMeshProUGUI>();
                _nameText.alignment = TextAlignmentOptions.Center;
                _nameText.fontSize = 0.24f;
                RectTransform nameRect = nameGo.GetComponent<RectTransform>();
                nameRect.sizeDelta = new Vector2(_cardSize.x * 0.9f, 0.4f);
                nameRect.anchoredPosition = new Vector2(0f, _cardSize.y * 0.35f);
            }

            if (_costText == null)
            {
                GameObject costGo = new GameObject("CostText");
                costGo.transform.SetParent(_canvas.transform, false);
                _costText = costGo.AddComponent<TextMeshProUGUI>();
                _costText.alignment = TextAlignmentOptions.Center;
                _costText.fontSize = 0.28f;
                RectTransform costRect = costGo.GetComponent<RectTransform>();
                costRect.sizeDelta = new Vector2(0.4f, 0.4f);
                costRect.anchoredPosition = new Vector2(-_cardSize.x * 0.4f, _cardSize.y * 0.4f);
            }

            if (_descriptionText == null)
            {
                GameObject descGo = new GameObject("DescriptionText");
                descGo.transform.SetParent(_canvas.transform, false);
                _descriptionText = descGo.AddComponent<TextMeshProUGUI>();
                _descriptionText.alignment = TextAlignmentOptions.Center;
                _descriptionText.fontSize = 0.18f;
                RectTransform descRect = descGo.GetComponent<RectTransform>();
                descRect.sizeDelta = new Vector2(_cardSize.x * 0.85f, _cardSize.y * 0.5f);
                descRect.anchoredPosition = new Vector2(0f, -_cardSize.y * 0.1f);
            }
        }

        private void EnsureCollider()
        {
            _collider = GetComponent<BoxCollider>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }
            _collider.size = new Vector3(_cardSize.x, _cardSize.y, 0.1f);
        }

        private static Color GetTypeColor(CardType type)
        {
            switch (type)
            {
                case CardType.Attack:
                    return new Color(0.85f, 0.25f, 0.25f);
                case CardType.Skill:
                    return new Color(0.25f, 0.5f, 0.9f);
                case CardType.Power:
                    return new Color(0.3f, 0.75f, 0.35f);
                case CardType.Status:
                    return new Color(0.6f, 0.6f, 0.6f);
                case CardType.Curse:
                    return new Color(0.5f, 0.25f, 0.6f);
                default:
                    return Color.white;
            }
        }
    }
}
