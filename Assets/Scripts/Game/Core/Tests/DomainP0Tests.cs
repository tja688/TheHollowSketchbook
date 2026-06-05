using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain;
using Game.Core.Domain.Actions;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Inventory;
using Game.Core.Domain.Rooms;
using Game.Core.Domain.Validation;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class DomainP0Tests
    {
        [SetUp]
        public void SetUp()
        {
            ModelDb.Clear();
        }

        [Test]
        public void GridCoord_ConvertsCellIndexAndNeighbors()
        {
            Assert.AreEqual(new GridCoord(0, 0), GridCoord.FromCellIndex(1));
            Assert.AreEqual(new GridCoord(1, 1), GridCoord.FromCellIndex(5));
            Assert.AreEqual(new GridCoord(2, 1), GridCoord.FromCellIndex(8));
            Assert.AreEqual(8, new GridCoord(2, 1).CellIndex);
            Assert.IsTrue(GridCoord.FromCellIndex(5).IsOrthogonalNeighborOf(GridCoord.FromCellIndex(2)));
            Assert.IsFalse(GridCoord.FromCellIndex(5).IsOrthogonalNeighborOf(GridCoord.FromCellIndex(1)));
            Assert.AreEqual(new[] { 5, 2 }, GridQueries.CoordsAboveSameColumn(GridCoord.FromCellIndex(8)).Select(coord => coord.CellIndex).ToArray());
        }

        [Test]
        public void GridState_AddsStacksAndTracksTopCard()
        {
            GridState grid = new GridState();
            CardInstance bottom = NewCard(1, CardType.Monster, hp: 6);
            CardInstance top = NewCard(2, CardType.Trap, hp: 2);

            grid.AddCardToGrid(bottom, GridCoord.FromCellIndex(1), false);
            grid.AddCardToGrid(top, GridCoord.FromCellIndex(1), false);

            Assert.AreEqual(top, grid.GetTopCard(GridCoord.FromCellIndex(1)));
            Assert.AreEqual(0, bottom.StackIndex);
            Assert.AreEqual(1, top.StackIndex);
            Assert.AreEqual(CardZone.Grid, bottom.Zone);
        }

        [Test]
        public void PlayerMove_ToEmptyCellCountsActionAndRevealsAdjacentTopCards()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance faceDown = NewCard(2, CardType.Monster, hp: 6);
                grid.AddCardToGrid(faceDown, GridCoord.FromCellIndex(6), false);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                DomainFacade facade = new DomainFacade(context);

                DomainEventBatch batch = await facade.SubmitIntentAsync(new MovePlayerIntent(GridCoord.FromCellIndex(9)));

                Assert.AreEqual(1, context.ActionCounter.Value);
                Assert.AreEqual(GridCoord.FromCellIndex(9), grid.PlayerCard.Coord.Value);
                Assert.IsTrue(faceDown.IsFaceUp);
                Assert.IsTrue(batch.Events.Any(evt => evt.EventType == DomainEventType.PlayerActionCommitted));
                Assert.IsTrue(batch.Events.Any(evt => evt.EventType == DomainEventType.CardFlipped && evt.CardId == faceDown.InstanceId));
            });
        }

        [Test]
        public void InteractWithMonster_PlayerPositionStaysAndDeadMonsterRemovedWithGold()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance player = grid.PlayerCard;
                player.ConfigureCombatStats(20, 5, 1, 0, 0);
                CardInstance monster = NewCard(2, CardType.Monster, hp: 4, attack: 3, defense: 0);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                DomainFacade facade = new DomainFacade(context);

                await facade.SubmitIntentAsync(new InteractWithCardIntent(monster.InstanceId));

                Assert.AreEqual(GridCoord.FromCellIndex(8), player.Coord.Value);
                Assert.AreEqual(1, context.ActionCounter.Value);
                Assert.AreEqual(CardZone.Removed, monster.Zone);
                Assert.AreEqual(10, context.PlayerGold);
                Assert.IsTrue(context.Batches.Last().Events.Any(evt => evt.EventType == DomainEventType.MonsterDefeated));
                Assert.IsTrue(context.Batches.Last().Events.Any(evt => evt.EventType == DomainEventType.RoomCleared));
            });
        }

        [Test]
        public void InvalidInteractWithFaceDownCardIsRejectedAndDoesNotCountAction()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance monster = NewCard(2, CardType.Monster, hp: 4);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), false);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                DomainFacade facade = new DomainFacade(context);

                DomainEventBatch batch = await facade.SubmitIntentAsync(new InteractWithCardIntent(monster.InstanceId));

                Assert.AreEqual(0, context.ActionCounter.Value);
                Assert.IsTrue(batch.Events.Any(evt => evt.EventType == DomainEventType.IntentRejected && evt.Reason == "TargetFaceDown"));
            });
        }

        [Test]
        public void RemoveTopCard_RevealsUnderlyingTopCard()
        {
            GridState grid = NewGridWithPlayer();
            CardInstance bottom = NewCard(2, CardType.Monster, hp: 6);
            CardInstance top = NewCard(3, CardType.Trap, hp: 2);
            grid.AddCardToGrid(bottom, GridCoord.FromCellIndex(1), false);
            grid.AddCardToGrid(top, GridCoord.FromCellIndex(1), true);

            GridOperationResult result = grid.RemoveCard(top, RemoveReason.Destroyed);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(CardZone.Removed, top.Zone);
            Assert.IsTrue(bottom.IsFaceUp);
            Assert.IsTrue(result.Events.Any(evt => evt.EventType == DomainEventType.CardFlipped && evt.CardId == bottom.InstanceId));
        }

        [Test]
        public void CombatResolution_AppliesDefenseAndTrapIgnoresPlayerDefense()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance player = grid.PlayerCard;
                player.ConfigureCombatStats(20, 5, 3, 0, 0);
                CardInstance trap = NewCard(2, CardType.Trap, hp: 6, attack: 0, defense: 2, contactDamage: 4);
                grid.AddCardToGrid(trap, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                int oldPlayerHp = player.CurrentHp;

                await context.Combat.ResolvePlayerVsTrapAsync(player, trap, null);

                Assert.AreEqual(3, trap.CurrentHp);
                Assert.AreEqual(oldPlayerHp - 4, player.CurrentHp);
            });
        }

        [Test]
        public void InvariantValidator_CatchesMissingPlayerAndAcceptsValidGrid()
        {
            DomainInvariantValidator validator = new DomainInvariantValidator();
            Assert.AreEqual("PlayerCount", validator.Validate(new GridState()).Single().Code);

            GridState grid = NewGridWithPlayer();
            Assert.AreEqual(0, validator.Validate(grid).Count);
        }

        [Test]
        public void CombatResolution_FirstStrike_MonsterAttacksFirst()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance player = grid.PlayerCard;
                player.ConfigureCombatStats(20, 5, 0, 0, 0);
                CardInstance monster = NewCard(2, CardType.Monster, hp: 10, attack: 20, defense: 0);
                monster.SetState("firstStrike", 1);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());

                await context.Combat.ResolvePlayerVsMonsterAsync(player, monster, null);

                // Monster has first strike, so it attacks first and kills player.
                // Player should NOT get a counter-attack because player is dead.
                Assert.AreEqual(0, player.CurrentHp);
                Assert.AreEqual(10, monster.CurrentHp); // Player never got to attack
            });
        }

        [Test]
        public void CombatResolution_SimultaneousDeath_BothDie()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance player = grid.PlayerCard;
                player.ConfigureCombatStats(5, 5, 0, 0, 0);
                CardInstance monster = NewCard(2, CardType.Monster, hp: 5, attack: 5, defense: 0);
                // Both have first strike => simultaneous attack
                player.SetState("firstStrike", 1);
                monster.SetState("firstStrike", 1);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                List<DomainEvent> events = new List<DomainEvent>();

                await context.Combat.ResolvePlayerVsMonsterAsync(player, monster, events);

                // Both should die because damage is applied simultaneously
                Assert.AreEqual(0, player.CurrentHp);
                Assert.AreEqual(0, monster.CurrentHp);
                Assert.IsTrue(events.Any(e => e.EventType == DomainEventType.DamageApplied && e.Reason.Contains("PlayerAttackMonster")));
                Assert.IsTrue(events.Any(e => e.EventType == DomainEventType.DamageApplied && e.Reason.Contains("MonsterCounterAttack")));
            });
        }

        [Test]
        public void CombatResolution_DamagePrevention_BlocksDamage()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance player = grid.PlayerCard;
                player.ConfigureCombatStats(20, 5, 0, 0, 0);
                CardInstance monster = NewCard(2, CardType.Monster, hp: 10, attack: 3, defense: 0);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                List<DomainEvent> events = new List<DomainEvent>();

                // Give player one damage immunity
                player.SetState("damageImmunity", 1);
                int oldHp = player.CurrentHp;

                DamageInfo info = new DamageInfo(
                    DamageSource.FromCard(monster.InstanceId),
                    DamageTarget.Card(player.InstanceId),
                    3,
                    DamageKind.Attack,
                    false,
                    "TestDamage");

                DamageResult result = await context.Combat.ApplyDamageAsync(info, events);

                Assert.AreEqual(0, result.HpLoss);
                Assert.IsTrue(result.Prevented);
                Assert.AreEqual(oldHp, player.CurrentHp);
                Assert.AreEqual(0, player.GetState("damageImmunity"));
            });
        }

        [Test]
        public void PlayerDefeated_TriggersRunEnded()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance player = grid.PlayerCard;
                player.ConfigureCombatStats(5, 5, 0, 0, 0);
                CardInstance monster = NewCard(2, CardType.Monster, hp: 10, attack: 20, defense: 0);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                DomainFacade facade = new DomainFacade(context);

                await facade.SubmitIntentAsync(new InteractWithCardIntent(monster.InstanceId));

                Assert.AreEqual(0, player.CurrentHp);
                Assert.IsTrue(context.Batches.Any(b => b.Events.Any(e => e.EventType == DomainEventType.RunEnded && e.Reason == "PlayerDefeated")));
            });
        }

        [Test]
        public void ApplyDamage_NonCreatureTarget_DoesNothing()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance gold = NewCard(2, CardType.Gold, hp: 0, attack: 0, defense: 0);
                grid.AddCardToGrid(gold, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                List<DomainEvent> events = new List<DomainEvent>();

                DamageResult result = await context.Combat.ApplyDamageAsync(new DamageInfo(
                    DamageSource.FromCard(grid.PlayerCard.InstanceId),
                    DamageTarget.Card(gold.InstanceId),
                    5,
                    DamageKind.Attack,
                    false,
                    "Test"), events);

                Assert.AreEqual(0, result.HpLoss);
                Assert.IsFalse(result.Killed);
            });
        }

        [Test]
        public void StoreItemIntent_MovesItemToInventory()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance item = NewCard(2, CardType.Item, hp: 0, attack: 0, defense: 0);
                grid.AddCardToGrid(item, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                DomainFacade facade = new DomainFacade(context);

                DomainEventBatch batch = await facade.SubmitIntentAsync(new StoreItemIntent(item.InstanceId));

                Assert.AreEqual(CardZone.PlayerInventory, item.Zone);
                Assert.AreEqual(1, context.ItemInventory.Count);
                Assert.IsTrue(batch.Events.Any(e => e.EventType == DomainEventType.ItemStored));
            });
        }

        [Test]
        public void UseItemIntent_CarriesTargetSelectionToItemUseContext()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance monster = NewCard(2, CardType.Monster, hp: 6);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                ModelId itemModelId = new ModelId("test", "targeted-item");
                TestItemModel itemModel = new TestItemModel(itemModelId, ItemTargetMode.CardThenDirection);
                ModelDb.Register(itemModel);
                CardInstance item = new CardInstance(new CardInstanceId(3), itemModelId, CardType.Item);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                InventorySlot slot = context.ItemInventory.Store(item);
                DomainFacade facade = new DomainFacade(context);
                UseItemIntent intent = new UseItemIntent(slot, ItemTargetSelection.CardThenDirection(monster.InstanceId, GridDirection.Up));

                IntentPreview preview = facade.PreviewIntent(intent);
                DomainEventBatch batch = await facade.SubmitIntentAsync(intent);

                Assert.IsTrue(preview.IsValid);
                CollectionAssert.Contains(preview.HighlightCards, monster.InstanceId);
                Assert.AreEqual(monster.InstanceId, itemModel.LastUseIntent.Target.PrimaryCard);
                Assert.AreEqual(GridDirection.Up, itemModel.LastUseIntent.Target.Direction);
                Assert.IsTrue(batch.Events.Any(e => e.EventType == DomainEventType.ItemUsed));
            });
        }

        [Test]
        public void ChooseOptionIntent_RequiresOpenSessionAndResolvesOnce()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                DomainFacade facade = new DomainFacade(context);

                DomainEventBatch missingSession = await facade.SubmitIntentAsync(new ChooseOptionIntent("route", 0));
                context.ChoiceSessions.Open("route", 2, "RouteChoice");
                DomainEventBatch resolved = await facade.SubmitIntentAsync(new ChooseOptionIntent("route", 1));
                DomainEventBatch duplicate = await facade.SubmitIntentAsync(new ChooseOptionIntent("route", 1));

                Assert.IsTrue(missingSession.Events.Any(e => e.EventType == DomainEventType.IntentRejected && e.Reason == "ChoiceSessionNotFound"));
                Assert.IsTrue(resolved.Events.Any(e => e.EventType == DomainEventType.ChoiceResolved && e.Reason == "route" && e.Amount == 1));
                Assert.IsTrue(context.ChoiceSessions.TryGet("route", out ChoiceSession session));
                Assert.IsTrue(session.IsResolved);
                Assert.AreEqual(1, session.SelectedOptionIndex);
                Assert.IsTrue(duplicate.Events.Any(e => e.EventType == DomainEventType.IntentRejected && e.Reason == "ChoiceAlreadyResolved"));
            });
        }

        [Test]
        public void SaveRestore_RoundTripPreservesGridAndActionCounter()
        {
            GridState grid = NewGridWithPlayer();
            CardInstance player = grid.PlayerCard;
            player.ConfigureCombatStats(20, 5, 1, 0, 0);
            CardInstance monster = NewCard(2, CardType.Monster, hp: 6, attack: 2, defense: 0);
            monster.SetState("firstStrike", 1);
            grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), false);
            CardInstance trap = NewCard(3, CardType.Trap, hp: 4, attack: 0, defense: 1, contactDamage: 3);
            grid.AddCardToGrid(trap, GridCoord.FromCellIndex(2), true);

            DomainActionContext original = new DomainActionContext(grid, new PlayerActionCounter());
            original.ActionCounter.Increment(new MovePlayerIntent(GridCoord.FromCellIndex(9)));
            original.SetPlayerGold(42);

            // Capture
            Game.Core.Saves.RoomDomainStateSaveDto dto = Game.Core.Saves.DomainSaveAdapter.Capture(original);

            // Restore into fresh context
            GridState freshGrid = NewGridWithPlayer(); // dummy grid to be replaced
            DomainActionContext restored = new DomainActionContext(freshGrid, new PlayerActionCounter());
            Game.Core.Saves.DomainSaveAdapter.Restore(dto, restored);

            // Verify Grid restored
            Assert.IsNotNull(restored.Grid);
            Assert.AreEqual(3, restored.Grid.AllKnownCards.Count());
            Assert.AreEqual(42, restored.PlayerGold);
            Assert.AreEqual(1, restored.ActionCounter.Value);

            // Verify monster restored with state
            CardInstance restoredMonster = restored.Grid.AllKnownCards.First(c => c.CardType == CardType.Monster);
            Assert.AreEqual(6, restoredMonster.MaxHp);
            Assert.AreEqual(2, restoredMonster.Attack);
            Assert.AreEqual(1, restoredMonster.GetState("firstStrike"));
            Assert.AreEqual(GridCoord.FromCellIndex(5), restoredMonster.Coord.Value);
            Assert.IsFalse(restoredMonster.IsFaceUp);

            // Verify trap restored
            CardInstance restoredTrap = restored.Grid.AllKnownCards.First(c => c.CardType == CardType.Trap);
            Assert.AreEqual(4, restoredTrap.MaxHp);
            Assert.AreEqual(1, restoredTrap.Defense);
            Assert.AreEqual(3, restoredTrap.ContactDamageToPlayer);
            Assert.IsTrue(restoredTrap.IsFaceUp);

            // Verify player restored
            CardInstance restoredPlayer = restored.Grid.PlayerCard;
            Assert.AreEqual(20, restoredPlayer.MaxHp);
            Assert.AreEqual(5, restoredPlayer.Attack);
            Assert.AreEqual(GridCoord.FromCellIndex(8), restoredPlayer.Coord.Value);
        }

        [Test]
        public void Hook_RelicModifiesDamageTaken()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance player = grid.PlayerCard;
                player.ConfigureCombatStats(20, 5, 0, 0, 0);
                CardInstance monster = NewCard(2, CardType.Monster, hp: 10, attack: 5, defense: 0);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());

                // Register a test relic that reduces damage taken by 2
                TestRelicModel thornRelic = new TestRelicModel(
                    new ModelId("test", "thorn"),
                    modifyDamageTaken: (ctx, current) => current - 2);
                ModelDb.Register(thornRelic);
                context.Relics.AddPassive(thornRelic.Id);

                List<DomainEvent> events = new List<DomainEvent>();
                DamageInfo info = new DamageInfo(
                    DamageSource.FromCard(monster.InstanceId),
                    DamageTarget.Card(player.InstanceId),
                    5,
                    DamageKind.Attack,
                    false,
                    "Test");

                DamageResult result = await context.Combat.ApplyDamageAsync(info, events);

                // Base 5 damage, relic reduces by 2 => 3 actual HP loss
                Assert.AreEqual(3, result.HpLoss);
                Assert.AreEqual(20 - 3, player.CurrentHp);
            });
        }

        [Test]
        public void Hook_RelicModifiesDamageDealt()
        {
            Await(async () =>
            {
                GridState grid = NewGridWithPlayer();
                CardInstance player = grid.PlayerCard;
                player.ConfigureCombatStats(20, 5, 0, 0, 0);
                CardInstance monster = NewCard(2, CardType.Monster, hp: 10, attack: 0, defense: 0);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());

                // Register a test relic that increases damage dealt by 3
                TestRelicModel powerRelic = new TestRelicModel(
                    new ModelId("test", "power"),
                    modifyDamageDealt: (ctx, current) => current + 3);
                ModelDb.Register(powerRelic);
                context.Relics.AddPassive(powerRelic.Id);

                List<DomainEvent> events = new List<DomainEvent>();
                DamageInfo info = new DamageInfo(
                    DamageSource.FromCard(player.InstanceId),
                    DamageTarget.Card(monster.InstanceId),
                    5,
                    DamageKind.Attack,
                    false,
                    "Test");

                DamageResult result = await context.Combat.ApplyDamageAsync(info, events);

                // Base 5 damage, relic increases by 3 => 8 actual HP loss
                Assert.AreEqual(8, result.HpLoss);
                Assert.AreEqual(10 - 8, monster.CurrentHp);
            });
        }

        [Test]
        public void DungeonMapGenerator_CreatesDesignNineNodeLayer()
        {
            DungeonMapGenerator generator = new DungeonMapGenerator();
            IReadOnlyList<RoomPlan> plans = generator.GenerateLayerPlans(1);

            Assert.AreEqual(9, plans.Count);
            Assert.AreEqual(RoomType.Reward, plans[0].RoomType);
            Assert.AreEqual(RoomType.Restaurant, plans[7].RoomType);
            Assert.AreEqual(RoomType.BossCombat, plans[8].RoomType);
            CollectionAssert.AreEquivalent(new[] { RoomType.Gold, RoomType.Chest, RoomType.StatUpgrade }, generator.GetChoicePoolAfterNode(1));
            CollectionAssert.Contains(generator.GetChoicePoolAfterNode(4), RoomType.Shop);
            CollectionAssert.Contains(generator.GetChoicePoolAfterNode(4), RoomType.EliteCombat);
            Assert.AreEqual(RoomType.Restaurant, generator.GetForcedNextRoomAfterNode(7));
        }

        [Test]
        public void DungeonDeckBuilder_BuildsCountsByRoomTypeAndLayer()
        {
            DungeonDeckBuilder builder = new DungeonDeckBuilder();
            DungeonDeck rewardDeck = builder.Build(new RoomPlan(RoomType.Reward, 1, 1, false, false, new RngState(7)), null, new DeterministicRng(7));

            Assert.AreEqual(10, rewardDeck.Cards.Count(card => card.CardType == CardType.Monster));
            Assert.AreEqual(1, rewardDeck.Cards.Count(card => card.CardType == CardType.Chest));
            Assert.AreEqual(1, rewardDeck.Cards.Count(card => card.CardType == CardType.StatUpgrade));
            Assert.That(rewardDeck.Cards.Count(card => card.CardType == CardType.Trap), Is.InRange(2, 4));
            Assert.That(rewardDeck.Cards.Count(card => card.CardType == CardType.Item), Is.InRange(4, 6));

            DungeonDeck restaurantDeck = builder.Build(new RoomPlan(RoomType.Restaurant, 1, 8, false, false, new RngState(8)), null, new DeterministicRng(8));
            Assert.AreEqual(1, restaurantDeck.Cards.Count(card => card.CardType == CardType.Food));
            Assert.AreEqual(3, restaurantDeck.Cards.Count(card => card.CardType == CardType.Mentor));
            Assert.AreEqual(0, restaurantDeck.Cards.Count(card => card.CardType == CardType.Trap));
            Assert.AreEqual(0, restaurantDeck.Cards.Count(card => card.CardType == CardType.Item));
        }

        [Test]
        public void MonsterAllocationRule_StrictlyMatchesLayerTargetAfterCorrection()
        {
            MonsterAllocationRule rule = new MonsterAllocationRule(6, new[]
            {
                new MonsterTierRange(1, 0, 1),
                new MonsterTierRange(2, 2, 3),
                new MonsterTierRange(3, 3, 4),
                new MonsterTierRange(4, 1, 4)
            });

            IReadOnlyDictionary<int, int> counts = rule.AllocateCounts(3, new DeterministicRng(19));

            Assert.AreEqual(12, counts.Values.Sum());
            Assert.IsTrue(counts.Keys.All(level => level >= 1 && level <= 4));
        }

        [Test]
        public void GridDealer_DealsCombatCoverageAndRestaurantException()
        {
            DungeonDeckBuilder builder = new DungeonDeckBuilder();
            GridDealer dealer = new GridDealer();

            GridState combatGrid = NewGridWithPlayer();
            DungeonDeck combatDeck = builder.Build(new RoomPlan(RoomType.Combat, 1, 2, false, false, new RngState(11)), null, new DeterministicRng(11));
            dealer.Deal(combatGrid, combatDeck, DealPolicy.CombatDefault(), new DeterministicRng(12));
            Assert.IsTrue(GridQueries.AllCoordsRowMajor().Where(coord => coord.CellIndex != 8).All(coord => combatGrid.GetStack(coord).Count >= 1));
            Assert.IsTrue(combatGrid.AllGridCards.Where(card => card.CardType != CardType.Player).All(card => !card.IsFaceUp));

            GridState restaurantGrid = NewGridWithPlayer();
            DungeonDeck restaurantDeck = builder.Build(new RoomPlan(RoomType.Restaurant, 1, 8, false, false, new RngState(13)), null, new DeterministicRng(13));
            dealer.Deal(restaurantGrid, restaurantDeck, DealPolicy.RestaurantDefault(), new DeterministicRng(14));
            Assert.AreEqual(4, restaurantGrid.AllGridCards.Count(card => card.CardType != CardType.Player));
        }

        private sealed class TestRelicModel : Game.Core.Domain.ContentContracts.RelicModel
        {
            private readonly System.Func<DamageContext, int, int> _modifyDealt;
            private readonly System.Func<DamageContext, int, int> _modifyTaken;

            public TestRelicModel(ModelId id, System.Func<DamageContext, int, int> modifyDamageDealt = null, System.Func<DamageContext, int, int> modifyDamageTaken = null)
            {
                Id = id;
                _modifyDealt = modifyDamageDealt;
                _modifyTaken = modifyDamageTaken;
            }

            public override ModelId Id { get; }
            public override Game.Core.Domain.ContentContracts.RelicRarity Rarity => Game.Core.Domain.ContentContracts.RelicRarity.Common;
            public override Game.Core.Domain.ContentContracts.RelicKind Kind => Game.Core.Domain.ContentContracts.RelicKind.Passive;

            public override int ModifyDamageDealt(DamageContext ctx, int current)
            {
                return _modifyDealt != null ? _modifyDealt(ctx, current) : current;
            }

            public override int ModifyDamageTaken(DamageContext ctx, int current)
            {
                return _modifyTaken != null ? _modifyTaken(ctx, current) : current;
            }
        }

        private static GridState NewGridWithPlayer()
        {
            GridState grid = new GridState();
            CardInstance player = NewCard(1, CardType.Player, hp: 20, attack: 3, defense: 1);
            grid.AddCardToGrid(player, GridCoord.FromCellIndex(8), true);
            return grid;
        }

        private static CardInstance NewCard(uint id, CardType type, int hp = 0, int attack = 0, int defense = 0, int contactDamage = 0)
        {
            ModelId modelId = new ModelId("test", id.ToString());
            CardInstance card = new CardInstance(new CardInstanceId(id), modelId, type);
            card.ConfigureCombatStats(hp, attack, defense, contactDamage, type == CardType.Monster ? 10 : 0);
            RegisterTestModelIfNeeded(modelId, type, hp, attack, defense, contactDamage);
            return card;
        }

        private static void RegisterTestModelIfNeeded(ModelId modelId, CardType type, int hp, int attack, int defense, int contactDamage)
        {
            if (ModelDb.TryGet(modelId, out CardModel _))
            {
                return;
            }

            switch (type)
            {
                case CardType.Monster:
                    ModelDb.Register(new TestMonsterModel(modelId, hp, attack, defense));
                    break;
                case CardType.Trap:
                    ModelDb.Register(new TestTrapModel(modelId, hp, defense, contactDamage));
                    break;
                case CardType.Item:
                    ModelDb.Register(new TestItemModel(modelId));
                    break;
                default:
                    ModelDb.Register(new TestGenericModel(modelId, type));
                    break;
            }
        }

        private sealed class TestGenericModel : CardModel
        {
            private readonly CardType _type;
            public TestGenericModel(ModelId id, CardType type) { Id = id; _type = type; }
            public override ModelId Id { get; }
            public override CardType CardType => _type;
        }

        private sealed class TestMonsterModel : MonsterCardModel
        {
            private readonly int _maxHp;
            private readonly int _attack;
            private readonly int _defense;
            public TestMonsterModel(ModelId id, int maxHp, int attack, int defense) { Id = id; _maxHp = maxHp; _attack = attack; _defense = defense; }
            public override ModelId Id { get; }
            public override int Level => 1;
            public override int MaxHp => _maxHp;
            public override int Attack => _attack;
            public override int Defense => _defense;
        }

        private sealed class TestTrapModel : TrapCardModel
        {
            private readonly int _maxHp;
            private readonly int _defense;
            private readonly int _contactDamage;
            public TestTrapModel(ModelId id, int maxHp, int defense, int contactDamage) { Id = id; _maxHp = maxHp; _defense = defense; _contactDamage = contactDamage; }
            public override ModelId Id { get; }
            public override int MaxHp => _maxHp;
            public override int Defense => _defense;
            public override int ContactDamageToPlayer => _contactDamage;
        }

        private sealed class TestItemModel : ItemCardModel
        {
            private readonly ItemTargetMode _targetMode;

            public TestItemModel(ModelId id, ItemTargetMode targetMode = ItemTargetMode.None)
            {
                Id = id;
                _targetMode = targetMode;
            }

            public override ModelId Id { get; }
            public override ItemTargetMode TargetMode => _targetMode;
            public UseItemIntent LastUseIntent { get; private set; }

            public override Task UseAsync(ItemUseContext ctx)
            {
                LastUseIntent = (UseItemIntent)ctx.SourceIntent;
                return Task.CompletedTask;
            }
        }

        private static void Await(System.Func<Task> asyncTest)
        {
            asyncTest().GetAwaiter().GetResult();
        }
    }
}
