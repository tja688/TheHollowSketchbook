using System;
using Game.Core.Cards;
using Game.Core.Entities;
using TMPro;
using UnityEngine;

namespace Game.Presentation.Combat.UI
{
    public sealed class PileButtonsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _drawPileText;
        [SerializeField] private TextMeshProUGUI _discardPileText;
        [SerializeField] private TextMeshProUGUI _exhaustPileText;

        private Player _player;

        public void Bind(Player player)
        {
            if (_player != null)
            {
                Unsubscribe();
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));

            if (_player.PlayerCombatState != null)
            {
                _player.PlayerCombatState.DrawPile.ContentsChanged += OnDrawPileChanged;
                _player.PlayerCombatState.DiscardPile.ContentsChanged += OnDiscardPileChanged;
                _player.PlayerCombatState.ExhaustPile.ContentsChanged += OnExhaustPileChanged;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_player == null || _player.PlayerCombatState == null)
            {
                return;
            }

            if (_drawPileText != null)
            {
                _drawPileText.text = $"Draw({_player.PlayerCombatState.DrawPile.Count})";
            }

            if (_discardPileText != null)
            {
                _discardPileText.text = $"Discard({_player.PlayerCombatState.DiscardPile.Count})";
            }

            if (_exhaustPileText != null)
            {
                _exhaustPileText.text = $"Exhaust({_player.PlayerCombatState.ExhaustPile.Count})";
            }
        }

        private void OnDrawPileChanged()
        {
            Refresh();
        }

        private void OnDiscardPileChanged()
        {
            Refresh();
        }

        private void OnExhaustPileChanged()
        {
            Refresh();
        }

        private void Unsubscribe()
        {
            if (_player?.PlayerCombatState == null)
            {
                return;
            }

            _player.PlayerCombatState.DrawPile.ContentsChanged -= OnDrawPileChanged;
            _player.PlayerCombatState.DiscardPile.ContentsChanged -= OnDiscardPileChanged;
            _player.PlayerCombatState.ExhaustPile.ContentsChanged -= OnExhaustPileChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
