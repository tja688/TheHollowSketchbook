using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Commands;
using Game.Core.Entities;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Runs;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class CoreLogicTests
    {
        [SetUp]
        public void SetUp()
        {
            ModelDb.Clear();
            Game.Content.StarterContentRegistry.RegisterAll(force: true);
        }

        [Test]
        public void ModelDb_RegisterAndCloneMutable()
        {
            Assert.That(ModelDb.Contains(new ModelId("Card", "Strike")), Is.True);
            CardModel strike = ModelDb.CreateMutable<CardModel>(new ModelId("Card", "Strike"));
            Assert.That(strike.IsCanonical, Is.False);
            Assert.That(strike.Id, Is.EqualTo(new ModelId("Card", "Strike")));
        }

        [Test]
        public void CardPile_Draw_ShuffleDiscardWhenDrawEmpty()
        {
            Player player = CreatePlayer();
            player.ResetCombatState();
            CardModel strike = ModelDb.CreateMutable<CardModel>(new ModelId("Card", "Strike"));
            CardModel defend = ModelDb.CreateMutable<CardModel>(new ModelId("Card", "Defend"));
            strike.SetOwner(player);
            defend.SetOwner(player);
            player.PlayerCombatState.DiscardPile.Add(strike);
            player.PlayerCombatState.DiscardPile.Add(defend);

            int drawn = CardPileCmd.Draw(player, 2, new DeterministicRng(123));

            Assert.That(drawn, Is.EqualTo(2));
            Assert.That(player.PlayerCombatState.Hand.Count, Is.EqualTo(2));
            Assert.That(player.PlayerCombatState.DiscardPile.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreatureCmd_Damage_BlockAbsorbsBeforeHp()
        {
            Player player = CreatePlayer();
            Creature enemy = CreateEnemy("DebugCultist");
            CombatState combat = CreateCombat(player, enemy);
            CardPlayContext ctx = new CardPlayContext(combat, new DeterministicRng(123));
            player.Creature.SetBlock(5);

            DamageResult result = CreatureCmd.DealDamage(ctx, enemy, player.Creature, 8).GetAwaiter().GetResult();

            Assert.That(player.Creature.Block, Is.EqualTo(0));
            Assert.That(player.Creature.CurrentHp, Is.EqualTo(player.Creature.MaxHp - 3));
            Assert.That(result.BlockedAmount, Is.EqualTo(5));
            Assert.That(result.HpLoss, Is.EqualTo(3));
        }

        [Test]
        public void CreatureCmd_StrengthModifiesAttackDamage()
        {
            Player player = CreatePlayer();
            Creature enemy = CreateEnemy("DebugCultist");
            CombatState combat = CreateCombat(player, enemy);
            CardPlayContext ctx = new CardPlayContext(combat, new DeterministicRng(123));
            CreatureCmd.ApplyPower(ctx, player.Creature, ModelDb.CreateMutable<Game.Core.Powers.PowerModel>(new ModelId("Power", "Strength")), 2).GetAwaiter().GetResult();

            DamageResult result = CreatureCmd.DealDamage(ctx, player.Creature, enemy, 6).GetAwaiter().GetResult();

            Assert.That(result.ModifiedAmount, Is.EqualTo(8));
            Assert.That(enemy.CurrentHp, Is.EqualTo(enemy.MaxHp - 8));
        }

        [Test]
        public void Card_Strike_DealsDamage()
        {
            Player player = CreatePlayer();
            Creature enemy = CreateEnemy("DebugCultist");
            CombatState combat = CreateCombat(player, enemy);
            CardPlayContext ctx = new CardPlayContext(combat, new DeterministicRng(123));
            CardModel strike = ModelDb.CreateMutable<CardModel>(new ModelId("Card", "Strike"));
            strike.SetOwner(player);

            strike.OnPlayWrapper(ctx, new CardPlay
            {
                Card = strike,
                Target = PlayTarget.ForCreature(enemy),
                Resources = new ResourceInfo(1)
            }).GetAwaiter().GetResult();

            Assert.That(enemy.CurrentHp, Is.EqualTo(enemy.MaxHp - 6));
        }

        [Test]
        public void Card_Defend_GainsBlock()
        {
            Player player = CreatePlayer();
            Creature enemy = CreateEnemy("DebugCultist");
            CombatState combat = CreateCombat(player, enemy);
            CardPlayContext ctx = new CardPlayContext(combat, new DeterministicRng(123));
            CardModel defend = ModelDb.CreateMutable<CardModel>(new ModelId("Card", "Defend"));
            defend.SetOwner(player);

            defend.OnPlayWrapper(ctx, new CardPlay
            {
                Card = defend,
                Target = PlayTarget.ForCreature(player.Creature),
                Resources = new ResourceInfo(1)
            }).GetAwaiter().GetResult();

            Assert.That(player.Creature.Block, Is.EqualTo(5));
        }

        [Test]
        public void ActionQueue_ExecutesInOrder()
        {
            List<int> order = new List<int>();
            ActionQueueSet queue = new ActionQueueSet();
            ActionExecutor executor = new ActionExecutor(queue);
            queue.Enqueue(new TestAction(order, 1));
            queue.Enqueue(new TestAction(order, 2));
            queue.Enqueue(new TestAction(order, 3));

            executor.ExecuteAllAsync().GetAwaiter().GetResult();

            Assert.That(order, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        private static Player CreatePlayer()
        {
            return new Player(ModelDb.Get<CharacterModel>(new ModelId("Character", "PrototypeHero")));
        }

        private static Creature CreateEnemy(string id)
        {
            EnemyModel enemyModel = ModelDb.Get<EnemyModel>(new ModelId("Enemy", id));
            return new Creature(enemyModel, enemyModel.MaxHp, enemyModel.MaxHp);
        }

        private static CombatState CreateCombat(Player player, Creature enemy)
        {
            player.ResetCombatState();
            RunState run = new RunState(123, new DeterministicRng(123), new[] { player }, new[] { ModelDb.Get<ActModel>(new ModelId("Act", "PrototypeAct")) });
            return new CombatState(run, ModelDb.Get<EncounterModel>(new ModelId("Encounter", "PrototypeCultistEncounter")), new[] { player }, new[] { enemy });
        }

        private sealed class TestAction : GameAction
        {
            private readonly List<int> _order;
            private readonly int _value;

            public TestAction(List<int> order, int value)
            {
                _order = order;
                _value = value;
            }

            protected override Task ExecuteActionAsync(GameActionExecutionContext ctx)
            {
                _order.Add(_value);
                return Task.CompletedTask;
            }
        }
    }
}
