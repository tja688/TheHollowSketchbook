using System;
using Game.Core.Entities;
using TMPro;
using UnityEngine;

namespace Game.Presentation.Combat.UI
{
    public sealed class EnergyPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _energyText;

        private Player _player;

        public void Bind(Player player)
        {
            if (_player != null)
            {
                Unsubscribe();
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            Refresh();
        }

        public void Refresh()
        {
            if (_player == null || _player.PlayerCombatState == null)
            {
                return;
            }

            int current = _player.PlayerCombatState.Energy;
            int max = _player.PlayerCombatState.MaxEnergy;

            if (_energyText != null)
            {
                _energyText.text = $"{current} / {max}";
                _energyText.color = current <= 0 ? Color.red : Color.white;
            }
        }

        private void Unsubscribe()
        {
            // PlayerCombatState is recreated each combat, so we don't subscribe to it directly.
            // If future energy change events are added, subscribe here.
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
