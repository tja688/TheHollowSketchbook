using System.Collections.Generic;
using Game.Core.Cards;
using UnityEngine;

namespace Game.Presentation.Combat.Cards
{
    public sealed class CardViewPool : MonoBehaviour
    {
        [SerializeField] private CardView _prefab;

        private readonly List<CardView> _available = new List<CardView>();
        private readonly List<CardView> _inUse = new List<CardView>();

        public CardView GetOrCreate(CardModel model)
        {
            CardView view = null;

            if (_available.Count > 0)
            {
                view = _available[_available.Count - 1];
                _available.RemoveAt(_available.Count - 1);
            }
            else if (_prefab != null)
            {
                view = Instantiate(_prefab, transform);
            }
            else
            {
                GameObject go = new GameObject("CardView");
                view = go.AddComponent<CardView>();
                go.transform.SetParent(transform, false);
            }

            if (view != null)
            {
                _inUse.Add(view);
                view.gameObject.SetActive(true);
            }

            return view;
        }

        public void Recycle(CardView view)
        {
            if (view == null)
                return;

            _inUse.Remove(view);
            if (!_available.Contains(view))
            {
                _available.Add(view);
            }

            view.gameObject.SetActive(false);
            view.transform.SetParent(transform, false);
        }
    }
}
