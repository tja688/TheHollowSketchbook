using System;
using System.Threading.Tasks;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Entities;
using Game.Core.Logging;
using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace Game.Presentation.Input
{
    public sealed class CombatInputController : MonoBehaviour
    {
        private CombatManager _combatManager;
        private Player _player;

        public event Action<CardModel> CardPlayFailed;

        public void Bind(CombatManager combatManager, Player player)
        {
            _combatManager = combatManager ?? throw new ArgumentNullException(nameof(combatManager));
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public Player GetPlayer()
        {
            return _player;
        }

        public async void SubmitCardPlayRequest(CardModel card, PlayTarget target)
        {
            if (_combatManager == null || _player == null || card == null)
            {
                CardPlayFailed?.Invoke(card);
                return;
            }

            if (!_combatManager.State.IsPlayPhase)
            {
                CardPlayFailed?.Invoke(card);
                return;
            }

            if (!card.CanPlay(out _))
            {
                CardPlayFailed?.Invoke(card);
                return;
            }

            var request = new CardPlayRequest
            {
                Player = _player,
                Card = card,
                Target = target,
                IsAutoPlay = false
            };

            try
            {
                await _combatManager.SubmitCardPlayRequestAsync(request);
            }
            catch (GameException)
            {
                CardPlayFailed?.Invoke(card);
            }
        }

        public void RequestEndTurn()
        {
            if (_combatManager == null || _player == null)
            {
                return;
            }

            _combatManager.RequestEndTurn(_player);
        }

        private void Update()
        {
            if (UnityInput.GetKeyDown(KeyCode.Space))
            {
                RequestEndTurn();
            }
        }
    }
}
