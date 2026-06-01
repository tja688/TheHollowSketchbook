using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Content;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Entities;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Runs;
using UnityEngine;

namespace Game.Presentation.Bootstrap
{
    public sealed class DebugCombatBootstrap : MonoBehaviour
    {
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _startOnPlay = true;

        private CombatManager _combatManager;

        private void OnDestroy()
        {
            _combatManager?.Reset();
        }

        private async void Start()
        {
            if (_startOnPlay)
            {
                await StartPrototypeCombatAsync();
            }
        }

        [ContextMenu("Start Prototype Combat")]
        public void StartPrototypeCombat()
        {
            _ = StartPrototypeCombatAsync();
        }

        public async Task StartPrototypeCombatAsync()
        {
            StarterContentRegistry.RegisterAll();

            Player player = new Player(ModelDb.Get<CharacterModel>(new ModelId("Character", "PrototypeHero")));
            IRng rng = new DeterministicRng(_seed);
            RunState run = new RunState(
                _seed,
                rng,
                new[] { player },
                new[] { ModelDb.Get<ActModel>(new ModelId("Act", "PrototypeAct")) });

            EncounterModel encounter = ModelDb.Get<EncounterModel>(new ModelId("Encounter", "PrototypeCultistEncounter"));
            List<Creature> enemies = new List<Creature>(encounter.EnemyIds.Length);
            for (int i = 0; i < encounter.EnemyIds.Length; i++)
            {
                EnemyModel enemyModel = ModelDb.Get<EnemyModel>(encounter.EnemyIds[i]);
                enemies.Add(new Creature(enemyModel, enemyModel.MaxHp, enemyModel.MaxHp));
            }

            CombatState combat = new CombatState(run, encounter, new[] { player }, enemies);
            _combatManager = new CombatManager();
            _combatManager.SetUpCombat(combat);

            var controllerGo = new GameObject("CombatPrototypeController");
            var controller = controllerGo.AddComponent<Combat.CombatPrototypeController>();
            controller.Bind(_combatManager);

            await _combatManager.StartCombatAsync();
        }
    }
}
