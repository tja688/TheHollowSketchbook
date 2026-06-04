using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Entities;
using Game.Core.Logging;
using Game.Core.Hooks;

namespace Game.Core.Combat
{
    /// <summary>
    /// Combat manager skeleton. StS turn-based cycle (player turn / enemy turn / end turn / draw / energy)
    /// has been removed. Retained: ActionQueue, event system, win/loss check, creature lifecycle.
    /// 
    /// BOUNDARY: This is a placeholder skeleton. A new combat manager for the grid-based
    /// action-driven system should be built on top of or replace this class.
    /// </summary>
    public sealed class CombatManager
    {
        private readonly ActionQueueSet _actions = new ActionQueueSet();
        private readonly ActionExecutor _executor;
        private bool _isProcessingActions;

        public CombatManager()
        {
            _executor = new ActionExecutor(_actions);
        }

        public CombatState State { get; private set; }
        public bool IsInProgress => State != null && State.IsInProgress && !State.IsCombatEnded;

        public event Action<CombatState> CombatSetUp;
        public event Action<CombatState> CombatWon;
        public event Action<CombatState> CombatEnded;
        public event Action<bool> PlayerActionsDisabledChanged;
        public event Action<CombatState> CreaturesChanged;

        public void SetUpCombat(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            State = state;
            State.IsCombatEnded = false;
            State.PlayerWon = false;
            State.IsInProgress = true;
            State.ActionCount = 0;
            _actions.Clear();

            SubscribeCreatures(State);

            for (int i = 0; i < State.Enemies.Count; i++)
            {
                Creature enemy = State.Enemies[i];
                enemy.SetBlock(0);
            }

            CombatSetUp?.Invoke(State);
            CreaturesChanged?.Invoke(State);
        }

        public async Task StartCombatAsync()
        {
            EnsureState();
            await Hook.BeforeCombatStart(State);
            PlayerActionsDisabledChanged?.Invoke(false);
        }

        public void EnqueueAction(GameAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            _actions.Enqueue(action);
        }

        public async Task ProcessActionsAsync()
        {
            if (_isProcessingActions)
            {
                return;
            }

            _isProcessingActions = true;
            try
            {
                await _executor.ExecuteAllAsync();
            }
            finally
            {
                _isProcessingActions = false;
            }
        }

        public async Task<bool> CheckWinConditionAsync()
        {
            EnsureState();
            if (State.IsCombatEnded)
            {
                return true;
            }

            if (State.ArePlayersDefeated)
            {
                State.IsCombatEnded = true;
                State.IsInProgress = false;
                State.PlayerWon = false;
                PlayerActionsDisabledChanged?.Invoke(true);
                CombatEnded?.Invoke(State);
                await Hook.AfterCombatEnd(State);
                return true;
            }

            if (State.AreEnemiesDefeated)
            {
                State.IsCombatEnded = true;
                State.IsInProgress = false;
                State.PlayerWon = true;
                PlayerActionsDisabledChanged?.Invoke(true);
                CombatWon?.Invoke(State);
                CombatEnded?.Invoke(State);
                await Hook.AfterCombatEnd(State);
                return true;
            }

            return false;
        }

        public void Reset()
        {
            if (State != null)
            {
                UnsubscribeCreatures(State);
            }

            _actions.Clear();
            State = null;
            _isProcessingActions = false;
            PlayerActionsDisabledChanged?.Invoke(true);
        }

        private void SubscribeCreatures(CombatState combat)
        {
            for (int i = 0; i < combat.Creatures.Count(); i++)
            {
                Creature creature = combat.Creatures.ElementAt(i);
                creature.HpChanged += OnCreatureStateChanged;
                creature.BlockChanged += OnCreatureStateChanged;
                creature.PowerApplied += OnCreaturePowerChanged;
                creature.PowerRemoved += OnCreaturePowerChanged;
                creature.Died += OnCreatureDied;
            }
        }

        private void UnsubscribeCreatures(CombatState combat)
        {
            for (int i = 0; i < combat.Creatures.Count(); i++)
            {
                Creature creature = combat.Creatures.ElementAt(i);
                creature.HpChanged -= OnCreatureStateChanged;
                creature.BlockChanged -= OnCreatureStateChanged;
                creature.PowerApplied -= OnCreaturePowerChanged;
                creature.PowerRemoved -= OnCreaturePowerChanged;
                creature.Died -= OnCreatureDied;
            }
        }

        private void OnCreatureStateChanged(int _, int __)
        {
            if (State != null)
            {
                CreaturesChanged?.Invoke(State);
            }
        }

        private void OnCreaturePowerChanged(Game.Core.Powers.PowerModel _)
        {
            if (State != null)
            {
                CreaturesChanged?.Invoke(State);
            }
        }

        private void OnCreatureDied(Creature _)
        {
            if (State != null)
            {
                CreaturesChanged?.Invoke(State);
            }
        }

        private void EnsureState()
        {
            if (State == null)
            {
                throw new GameException("Combat has not been set up.");
            }
        }
    }
}
