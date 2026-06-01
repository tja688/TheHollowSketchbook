using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Entities;
using Game.Core.Runs;

namespace Game.Core.Combat
{
    public sealed class CombatState
    {
        private readonly Dictionary<Creature, EnemyIntent> _enemyIntents = new Dictionary<Creature, EnemyIntent>();

        public CombatState(RunState runState, EncounterModel encounter, IReadOnlyList<Player> players, IReadOnlyList<Creature> enemies)
        {
            RunState = runState ?? throw new ArgumentNullException(nameof(runState));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            Players = players ?? throw new ArgumentNullException(nameof(players));
            Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
        }

        public RunState RunState { get; }
        public EncounterModel Encounter { get; }
        public IReadOnlyList<Player> Players { get; }
        public IReadOnlyList<Creature> Enemies { get; }

        public bool IsInProgress { get; internal set; }
        public bool IsCombatEnded { get; internal set; }
        public bool PlayerWon { get; internal set; }
        public bool IsPlayPhase { get; internal set; }

        public int RoundNumber { get; set; } = 1;
        public CombatSide CurrentSide { get; set; } = CombatSide.Player;

        public IEnumerable<Creature> PlayerCreatures
        {
            get { return Players.Select(player => player.Creature); }
        }

        public IEnumerable<Creature> Creatures
        {
            get { return PlayerCreatures.Concat(Enemies); }
        }

        public IReadOnlyDictionary<Creature, EnemyIntent> EnemyIntents
        {
            get { return _enemyIntents; }
        }

        public bool ArePlayersDefeated
        {
            get { return !Players.Any(player => player.Creature.IsAlive); }
        }

        public bool AreEnemiesDefeated
        {
            get { return !Enemies.Any(enemy => enemy.IsAlive); }
        }

        public void SetEnemyIntent(Creature enemy, EnemyIntent intent)
        {
            if (enemy == null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            if (intent == null)
            {
                throw new ArgumentNullException(nameof(intent));
            }

            _enemyIntents[enemy] = intent;
        }

        public bool TryGetEnemyIntent(Creature enemy, out EnemyIntent intent)
        {
            return _enemyIntents.TryGetValue(enemy, out intent);
        }

        public EnemyIntent GetEnemyIntent(Creature enemy)
        {
            return _enemyIntents[enemy];
        }

        public void ClearEnemyIntents()
        {
            _enemyIntents.Clear();
        }
    }
}
