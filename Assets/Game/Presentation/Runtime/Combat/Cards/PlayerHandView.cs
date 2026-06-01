using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Cards;
using Game.Core.Entities;
using Game.Presentation.Services;
using UnityEngine;

namespace Game.Presentation.Combat.Cards
{
    public sealed class PlayerHandView : MonoBehaviour
    {
        [SerializeField] private ArcHandLayout _layout;
        [SerializeField] private CardViewPool _pool;
        [SerializeField] private Transform _drawPileAnchor;
        [SerializeField] private Transform _discardPileAnchor;
        [SerializeField] private float _layoutDuration = 0.25f;
        [SerializeField] private float _exitDuration = 0.25f;

        private Player _player;
        private readonly List<CardView> _cardViews = new List<CardView>();

        public Player Player => _player;
        public IReadOnlyList<CardView> CardViews => _cardViews;

        private void Awake()
        {
            if (_layout == null)
                _layout = GetComponentInChildren<ArcHandLayout>();
            if (_pool == null)
                _pool = GetComponentInChildren<CardViewPool>();
        }

        public void Bind(Player player)
        {
            if (_player == player)
                return;

            Unbind();
            _player = player;

            if (_player != null)
            {
                Subscribe();
                SyncFromHand();
            }
        }

        private void Unbind()
        {
            if (_player == null)
                return;

            Unsubscribe();

            foreach (CardView view in _cardViews)
            {
                if (_pool != null)
                    _pool.Recycle(view);
                else
                    Destroy(view.gameObject);
            }
            _cardViews.Clear();

            _player = null;
        }

        private void Subscribe()
        {
            CardPile hand = _player.PlayerCombatState.Hand;
            hand.CardAdded += OnCardAdded;
            hand.CardRemoved += OnCardRemoved;
            hand.ContentsChanged += OnContentsChanged;
        }

        private void Unsubscribe()
        {
            if (_player == null)
                return;

            CardPile hand = _player.PlayerCombatState.Hand;
            hand.CardAdded -= OnCardAdded;
            hand.CardRemoved -= OnCardRemoved;
            hand.ContentsChanged -= OnContentsChanged;
        }

        private void SyncFromHand()
        {
            CardPile hand = _player.PlayerCombatState.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                CardModel card = hand.Cards[i];
                CardView view = _pool != null ? _pool.GetOrCreate(card) : CreateFallbackView(card);
                view.Bind(card);
                view.transform.SetParent(transform, false);
                view.gameObject.SetActive(true);
                _cardViews.Add(view);
            }
            ApplyLayout();
        }

        private void OnCardAdded(CardModel card)
        {
            CardView view = _pool != null ? _pool.GetOrCreate(card) : CreateFallbackView(card);
            view.Bind(card);
            view.transform.SetParent(transform, false);
            view.transform.position = GetEntryPosition();
            view.transform.rotation = Quaternion.identity;
            view.gameObject.SetActive(true);
            _cardViews.Add(view);
        }

        private void OnCardRemoved(CardModel card)
        {
            CardView view = FindView(card);
            if (view == null)
                return;

            _cardViews.Remove(view);
            AnimateExitAndRecycle(view);
        }

        private void OnContentsChanged()
        {
            ReorderToMatchHand();
            ApplyLayout();
        }

        private void ReorderToMatchHand()
        {
            CardPile hand = _player.PlayerCombatState.Hand;
            List<CardView> ordered = new List<CardView>(_cardViews.Count);

            for (int i = 0; i < hand.Count; i++)
            {
                CardView view = FindView(hand.Cards[i]);
                if (view != null)
                {
                    ordered.Add(view);
                }
            }

            _cardViews.Clear();
            _cardViews.AddRange(ordered);
        }

        private void ApplyLayout()
        {
            if (_layout == null)
                return;

            for (int i = 0; i < _cardViews.Count; i++)
            {
                HandPose pose = _layout.GetPose(i, _cardViews.Count);
                CardView view = _cardViews[i];
                view.SetBaseScale(pose.Scale, _layoutDuration);
                view.PlayMoveTo(pose.Position, pose.Rotation, _layoutDuration);
            }
        }

        private async void AnimateExitAndRecycle(CardView view)
        {
            Vector3 exitPos = GetExitPosition();

            if (GameServices.Tween != null)
            {
                await GameServices.Tween.MoveTo(view.transform, exitPos, _exitDuration);
                await GameServices.Tween.RotateTo(view.transform, Quaternion.identity, _exitDuration * 0.5f);
            }
            else
            {
                view.transform.position = exitPos;
            }

            if (_pool != null)
            {
                _pool.Recycle(view);
            }
            else
            {
                Destroy(view.gameObject);
            }
        }

        public void ArrangeCards()
        {
            if (_layout == null)
                return;

            int count = _cardViews.Count;
            for (int i = 0; i < count; i++)
            {
                CardView view = _cardViews[i];
                if (view == null || view.IsDragging)
                    continue;

                var pose = _layout.GetPose(i, count);
                view.PlayMoveTo(pose.Position, pose.Rotation, _layoutDuration);
                view.SetBaseScale(pose.Scale, _layoutDuration);
            }
        }

        private CardView FindView(CardModel card)
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (_cardViews[i].Model == card)
                    return _cardViews[i];
            }
            return null;
        }

        private Vector3 GetEntryPosition()
        {
            if (_drawPileAnchor != null)
                return _drawPileAnchor.position;

            if (_layout != null && _layout.Anchor != null)
                return _layout.Anchor.position + Vector3.down * 3f;

            return transform.position + Vector3.down * 3f;
        }

        private Vector3 GetExitPosition()
        {
            if (_discardPileAnchor != null)
                return _discardPileAnchor.position;

            if (_layout != null && _layout.Anchor != null)
                return _layout.Anchor.position + Vector3.up * 2f + Vector3.right * 4f;

            return transform.position + Vector3.up * 2f + Vector3.right * 4f;
        }

        private CardView CreateFallbackView(CardModel card)
        {
            GameObject go = new GameObject("CardView");
            go.transform.SetParent(transform, false);
            return go.AddComponent<CardView>();
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
