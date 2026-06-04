using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Entities;
using Game.Core.Runs;

namespace Game.Core.Combat
{
    /// <summary>
    /// Combat state container. Decoupled from StS turn-based concepts.
    /// Retained as a pure data context for a combat encounter.
    /// New systems (Grid, ActionCount, etc.) should be added here by extending this class.
    /// </summary>
    public sealed class CombatState
    {
        public CombatState(RunState runState, IReadOnlyList<Player> players, IReadOnlyList<Creature> enemies)
        {
            RunState = runState ?? throw new ArgumentNullException(nameof(runState));
            Players = players ?? throw new ArgumentNullException(nameof(players));
            Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
        }

        public RunState RunState { get; }
        public IReadOnlyList<Player> Players { get; }
        public IReadOnlyList<Creature> Enemies { get; }

        public bool IsInProgress { get; internal set; }
        public bool IsCombatEnded { get; internal set; }
        public bool PlayerWon { get; internal set; }

        // Replaced StS RoundNumber/CurrentSide with action-driven fields.
        // Extend this class to add: ActionCount, BattleGrid, etc.
        public int ActionCount { get; set; }

        public IEnumerable<Creature> PlayerCreatures
        {
            get { return Players.Select(player => player.Creature); }
        }

        public IEnumerable<Creature> Creatures
        {
            get { return PlayerCreatures.Concat(Enemies); }
        }

        public bool ArePlayersDefeated
        {
            get { return !Players.Any(player => player.Creature.IsAlive); }
        }

        public bool AreEnemiesDefeated
        {
            get { return !Enemies.Any(enemy => enemy.IsAlive); }
        }
    }
}
