using System;
using System.Threading.Tasks;
using Game.Core.Entities;
using Game.Presentation.Services;
using UnityEngine;

namespace Game.Presentation.Combat.Creatures
{
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _portraitRenderer;
        [SerializeField] private SpriteRenderer _highlightRenderer;
        [SerializeField] private CreatureHealthBar _healthBar;
        [SerializeField] private IntentView _intentView;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Creature _creature;
        private Color _originalPortraitColor;
        private bool _isHighlighted;

        public Creature Creature => _creature;

        public void Bind(Creature creature)
        {
            if (_creature != null)
            {
                Unsubscribe();
            }

            _creature = creature ?? throw new ArgumentNullException(nameof(creature));
            _originalPortraitColor = _portraitRenderer != null ? _portraitRenderer.color : Color.white;

            _creature.HpChanged += OnHpChanged;
            _creature.BlockChanged += OnBlockChanged;
            _creature.PowerApplied += OnPowerApplied;
            _creature.PowerRemoved += OnPowerRemoved;
            _creature.Died += OnDied;

            if (_healthBar != null)
            {
                _healthBar.Bind(creature);
            }

            Refresh();
        }

        public void PlayHitReaction()
        {
            if (_portraitRenderer == null)
            {
                return;
            }

            GameServices.EnsureInitialized();
            _ = PlayHitReactionAsync();
        }

        public void PlayDeathFade()
        {
            GameServices.EnsureInitialized();
            _ = PlayDeathFadeAsync();
        }

        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;

            if (_highlightRenderer != null)
            {
                _highlightRenderer.gameObject.SetActive(highlighted);
            }
            else if (_portraitRenderer != null)
            {
                _portraitRenderer.color = highlighted ? Color.yellow : _originalPortraitColor;
            }
        }

        private async Task PlayHitReactionAsync()
        {
            if (GameServices.Tween == null)
            {
                return;
            }

            Color flashColor = Color.red;
            _portraitRenderer.color = flashColor;

            await GameServices.Tween.PunchScale(transform, new Vector3(-0.3f, -0.3f, 0f), 0.2f);

            if (_portraitRenderer != null)
            {
                _portraitRenderer.color = _isHighlighted ? Color.yellow : _originalPortraitColor;
            }
        }

        private async Task PlayDeathFadeAsync()
        {
            if (GameServices.Tween == null)
            {
                Destroy(gameObject);
                return;
            }

            if (_canvasGroup != null)
            {
                await GameServices.Tween.FadeCanvasGroup(_canvasGroup, 0f, 0.5f);
            }
            else
            {
                await GameServices.Tween.ScaleTo(transform, Vector3.zero, 0.5f);
            }

            Destroy(gameObject);
        }

        private void OnHpChanged(int oldValue, int newValue)
        {
            if (newValue < oldValue)
            {
                PlayHitReaction();
            }
            Refresh();
        }

        private void OnBlockChanged(int oldValue, int newValue)
        {
            Refresh();
        }

        private void OnPowerApplied(Game.Core.Powers.PowerModel power)
        {
            Refresh();
        }

        private void OnPowerRemoved(Game.Core.Powers.PowerModel power)
        {
            Refresh();
        }

        private void OnDied(Creature creature)
        {
            PlayDeathFade();
        }

        private void Refresh()
        {
            if (_healthBar != null)
            {
                _healthBar.Refresh();
            }
        }

        private void Unsubscribe()
        {
            if (_creature == null)
            {
                return;
            }

            _creature.HpChanged -= OnHpChanged;
            _creature.BlockChanged -= OnBlockChanged;
            _creature.PowerApplied -= OnPowerApplied;
            _creature.PowerRemoved -= OnPowerRemoved;
            _creature.Died -= OnDied;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
