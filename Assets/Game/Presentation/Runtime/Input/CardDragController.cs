using System.Threading.Tasks;
using Game.Core;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Entities;
using Game.Presentation.Combat.Cards;
using Game.Presentation.Combat.Creatures;
using Game.Presentation.Services;
using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace Game.Presentation.Input
{
    public sealed class CardDragController : MonoBehaviour
    {
        [SerializeField] private CombatRaycastService _raycastService;
        [SerializeField] private CombatInputController _inputController;
        [SerializeField] private float _dragLiftY = 0.2f;
        [SerializeField] private float _dragScale = 1.1f;
        [SerializeField] private float _snapBackDuration = 0.2f;

        private DragState _state = DragState.None;
        private CardView _hoveredCard;
        private CardView _draggedCard;
        private EnemyView _hoveredEnemy;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;
        private Plane _dragPlane;

        private CardView _pendingSnapBackCard;
        private Vector3 _pendingSnapBackPosition;
        private Quaternion _pendingSnapBackRotation;
        private Vector3 _pendingSnapBackScale;

        private enum DragState
        {
            None,
            HoveringCard,
            DraggingCard,
            SelectingTarget
        }

        private void Awake()
        {
            if (_raycastService == null)
            {
                _raycastService = FindObjectOfType<CombatRaycastService>();
            }

            if (_inputController == null)
            {
                _inputController = FindObjectOfType<CombatInputController>();
            }
        }

        private void OnEnable()
        {
            if (_inputController != null)
            {
                _inputController.CardPlayFailed += OnCardPlayFailed;
            }
        }

        private void OnDisable()
        {
            if (_inputController != null)
            {
                _inputController.CardPlayFailed -= OnCardPlayFailed;
            }

            ForceReset();
        }

        private void Update()
        {
            if (_raycastService == null || _inputController == null)
            {
                return;
            }

            switch (_state)
            {
                case DragState.None:
                    UpdateNone();
                    break;
                case DragState.HoveringCard:
                    UpdateHovering();
                    break;
                case DragState.DraggingCard:
                case DragState.SelectingTarget:
                    UpdateDragging();
                    break;
            }
        }

        private void UpdateNone()
        {
            if (UnityInput.GetMouseButton(0))
            {
                return;
            }

            CardView card = _raycastService.RaycastCard();
            if (card != null && card.Model != null && card.Model.CanPlay(out _))
            {
                BeginHover(card);
            }
        }

        private void UpdateHovering()
        {
            CardView card = _raycastService.RaycastCard();
            if (card != _hoveredCard)
            {
                EndHover();
                if (card != null && card.Model != null && card.Model.CanPlay(out _))
                {
                    BeginHover(card);
                }
                return;
            }

            if (UnityInput.GetMouseButtonDown(0))
            {
                BeginDrag(_hoveredCard);
            }
        }

        private void UpdateDragging()
        {
            if (_draggedCard == null)
            {
                TransitionTo(DragState.None);
                return;
            }

            if (UnityInput.GetMouseButton(0))
            {
                UpdateDrag();

                EnemyView enemy = _raycastService.RaycastEnemy();
                bool playArea = _raycastService.RaycastPlayArea(out _);

                if (_hoveredEnemy != null && _hoveredEnemy != enemy)
                {
                    _hoveredEnemy.SetHighlight(false);
                    _hoveredEnemy = null;
                }

                CardTargeting targeting = _draggedCard.Model.Targeting;

                if (targeting == CardTargeting.SingleEnemy)
                {
                    if (enemy != null && enemy.Creature != null && enemy.Creature.IsAlive)
                    {
                        enemy.SetHighlight(true);
                        _hoveredEnemy = enemy;
                        _state = DragState.SelectingTarget;
                    }
                    else
                    {
                        _state = DragState.DraggingCard;
                    }
                }
                else if (targeting == CardTargeting.AllEnemies)
                {
                    if (enemy != null && enemy.Creature != null && enemy.Creature.IsAlive)
                    {
                        enemy.SetHighlight(true);
                        _hoveredEnemy = enemy;
                        _state = DragState.SelectingTarget;
                    }
                    else if (playArea)
                    {
                        _state = DragState.SelectingTarget;
                    }
                    else
                    {
                        _state = DragState.DraggingCard;
                    }
                }
                else if (targeting == CardTargeting.None || targeting == CardTargeting.Self)
                {
                    if (playArea)
                    {
                        _state = DragState.SelectingTarget;
                    }
                    else
                    {
                        _state = DragState.DraggingCard;
                    }
                }
            }
            else if (UnityInput.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }

        public void BeginDrag(CardView cardView)
        {
            if (cardView == null || cardView.Model == null)
            {
                return;
            }

            if (!cardView.Model.CanPlay(out _))
            {
                return;
            }

            EndHover();

            _draggedCard = cardView;
            _draggedCard.IsDragging = true;

            Transform t = _draggedCard.transform;
            _originalPosition = t.position;
            _originalRotation = t.rotation;
            _originalScale = t.localScale;

            _dragPlane = new Plane(Vector3.up, _originalPosition);

            t.localScale = _originalScale * _dragScale;

            TransitionTo(DragState.DraggingCard);
        }

        public void UpdateDrag()
        {
            if (_draggedCard == null || Camera.main == null)
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(UnityInput.mousePosition);
            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPos = ray.GetPoint(enter);
                targetPos.y = _originalPosition.y + _dragLiftY;
                _draggedCard.transform.position = targetPos;
            }
        }

        public void EndDrag()
        {
            if (_draggedCard == null)
            {
                TransitionTo(DragState.None);
                return;
            }

            CardModel card = _draggedCard.Model;
            bool valid = false;
            PlayTarget target = PlayTarget.None;

            EnemyView enemy = _raycastService.RaycastEnemy();
            bool playArea = _raycastService.RaycastPlayArea(out _);

            switch (card.Targeting)
            {
                case CardTargeting.None:
                    valid = playArea;
                    target = PlayTarget.None;
                    break;
                case CardTargeting.Self:
                    valid = playArea;
                    Player player = _inputController.GetPlayer();
                    if (player != null)
                    {
                        target = PlayTarget.ForCreature(player.Creature);
                    }
                    break;
                case CardTargeting.SingleEnemy:
                    valid = enemy != null && enemy.Creature != null && enemy.Creature.IsAlive;
                    target = valid ? PlayTarget.ForCreature(enemy.Creature) : PlayTarget.None;
                    break;
                case CardTargeting.AllEnemies:
                    valid = enemy != null || playArea;
                    target = PlayTarget.None;
                    break;
            }

            CardView cardView = _draggedCard;
            _draggedCard.IsDragging = false;
            _draggedCard = null;

            if (_hoveredEnemy != null)
            {
                _hoveredEnemy.SetHighlight(false);
                _hoveredEnemy = null;
            }

            if (valid)
            {
                _pendingSnapBackCard = cardView;
                _pendingSnapBackPosition = _originalPosition;
                _pendingSnapBackRotation = _originalRotation;
                _pendingSnapBackScale = _originalScale;

                _inputController.SubmitCardPlayRequest(card, target);
                TransitionTo(DragState.None);
            }
            else
            {
                _ = SnapBackAsync(cardView, _originalPosition, _originalRotation, _originalScale);
                TransitionTo(DragState.None);
            }
        }

        private void OnCardPlayFailed(CardModel card)
        {
            if (_pendingSnapBackCard != null && _pendingSnapBackCard.Model == card)
            {
                _ = SnapBackAsync(_pendingSnapBackCard, _pendingSnapBackPosition, _pendingSnapBackRotation, _pendingSnapBackScale);
                _pendingSnapBackCard = null;
            }
        }

        private async Task SnapBackAsync(CardView cardView, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (cardView == null)
            {
                return;
            }

            GameServices.EnsureInitialized();

            Task moveTask = GameServices.Tween.MoveTo(cardView.transform, position, _snapBackDuration, EaseType.OutQuad);
            Task rotateTask = GameServices.Tween.RotateTo(cardView.transform, rotation, _snapBackDuration, EaseType.OutQuad);
            Task scaleTask = GameServices.Tween.ScaleTo(cardView.transform, scale, _snapBackDuration, EaseType.OutQuad);

            await moveTask;
            await rotateTask;
            await scaleTask;
        }

        private void BeginHover(CardView card)
        {
            _hoveredCard = card;
            _hoveredCard.PlayHover(true);
            TransitionTo(DragState.HoveringCard);
        }

        private void EndHover()
        {
            if (_hoveredCard != null)
            {
                _hoveredCard.PlayHover(false);
                _hoveredCard = null;
            }
        }

        private void TransitionTo(DragState newState)
        {
            _state = newState;
        }

        private void ForceReset()
        {
            EndHover();

            if (_hoveredEnemy != null)
            {
                _hoveredEnemy.SetHighlight(false);
                _hoveredEnemy = null;
            }

            if (_draggedCard != null)
            {
                _draggedCard.IsDragging = false;
                _draggedCard.transform.position = _originalPosition;
                _draggedCard.transform.rotation = _originalRotation;
                _draggedCard.transform.localScale = _originalScale;
                _draggedCard = null;
            }

            _pendingSnapBackCard = null;
            _state = DragState.None;
        }
    }
}
