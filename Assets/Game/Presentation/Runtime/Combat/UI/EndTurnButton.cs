using System;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Combat.UI
{
    public sealed class EndTurnButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _canvasGroup;

        private CombatManager _combatManager;
        private Player _player;

        public void Bind(CombatManager combatManager, Player player)
        {
            if (_combatManager != null)
            {
                Unsubscribe();
            }

            _combatManager = combatManager ?? throw new ArgumentNullException(nameof(combatManager));
            _player = player ?? throw new ArgumentNullException(nameof(player));

            _combatManager.PlayerActionsDisabledChanged += OnPlayerActionsDisabledChanged;

            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }

            UpdateInteractable(!_combatManager.State.IsPlayPhase || _combatManager.State.CurrentSide != CombatSide.Player);
        }

        private void OnClick()
        {
            if (_combatManager == null || _player == null)
            {
                return;
            }

            _combatManager.RequestEndTurn(_player);
        }

        private void OnPlayerActionsDisabledChanged(bool disabled)
        {
            UpdateInteractable(disabled);
        }

        private void UpdateInteractable(bool disabled)
        {
            bool interactable = !disabled;

            if (_button != null)
            {
                _button.interactable = interactable;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = interactable ? 1f : 0.5f;
            }
        }

        private void Unsubscribe()
        {
            if (_combatManager != null)
            {
                _combatManager.PlayerActionsDisabledChanged -= OnPlayerActionsDisabledChanged;
            }

            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
