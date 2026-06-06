using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Content.Runtime;
using Game.Core;
using Game.Core.Domain;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Inventory;
using Game.Core.Models;
using Game.Core.Random;
using NUnit.Framework;

namespace Game.Content.Tests
{
    public sealed class StarterContentRegistryTests
    {
        [SetUp]
        public void SetUp()
        {
            ModelDb.Clear();
        }

        [Test]
        public void RegisterAll_RegistersContentCatalogAndLegacyPrototypeModels()
        {
            RoomContentCatalog catalog = StarterContentRegistry.RegisterAll();

            Assert.IsTrue(ModelDb.Contains(StarterContentIds.PlayerHero));
            Assert.IsTrue(ModelDb.Contains(StarterContentIds.RouteGold));
            Assert.IsTrue(ModelDb.Contains(StarterContentIds.Monsters.Skeleton));
            Assert.IsTrue(ModelDb.Contains(new ModelId("Character", "PrototypeHero")));
            Assert.IsTrue(ModelDb.Contains(new ModelId("Act", "PrototypeAct")));

            Assert.IsTrue(catalog.HasCategory("gold"));
            Assert.IsTrue(catalog.HasCategory("stat"));
            Assert.IsTrue(catalog.HasCategory("chest"));
            Assert.IsTrue(catalog.HasCategory("food"));
            Assert.IsTrue(catalog.HasCategory("mentor"));
            Assert.IsTrue(catalog.HasCategory("shop-product"));
            Assert.IsTrue(catalog.HasCategory("trap"));
            Assert.IsTrue(catalog.HasCategory("item"));
            Assert.IsTrue(catalog.GetAvailableMonsters(1).Contains(StarterContentIds.Monsters.Skeleton));
            Assert.IsTrue(catalog.GetAvailableMonsters(4).Contains(StarterContentIds.Monsters.TrackerSkeleton));
            Assert.IsTrue(catalog.GetAvailable("boss").Contains(StarterContentIds.Monsters.BigSkeletonLord));
        }

        [Test]
        public void StatUpgradeAndChestChoices_ApplyConfiguredProgressionAndEvents()
        {
            Await(async () =>
            {
                StarterContentRegistry.RegisterAll();

                DomainActionContext context = NewContext();
                DomainFacade facade = new DomainFacade(context);

                CardInstance statCard = ModelDb.Get<CardModel>(StarterContentIds.RoomCards.StatUpgrade).CreateInstance(new CardInstanceId(2));
                context.Grid.AddCardToGrid(statCard, GridCoord.FromCellIndex(5), true);

                DomainEventBatch statOpened = await facade.SubmitIntentAsync(new InteractWithCardIntent(statCard.InstanceId));
                Assert.IsTrue(statOpened.Events.Any(e => e.EventType == DomainEventType.ChoiceOpened));
                string statSessionId = statOpened.Events.First(e => e.EventType == DomainEventType.ChoiceOpened).Reason;

                DomainEventBatch statResolved = await facade.SubmitIntentAsync(new ChooseOptionIntent(statSessionId, 2));
                Assert.IsTrue(statResolved.Events.Any(e => e.EventType == DomainEventType.StatChanged && e.Reason == "player:max-hp"));
                Assert.AreEqual(22, context.Grid.PlayerCard.MaxHp);
                Assert.AreEqual(22, context.Grid.PlayerCard.CurrentHp);

                context.Rng = new DeterministicRng(7);
                CardInstance chestCard = ModelDb.Get<CardModel>(StarterContentIds.RoomCards.OrdinaryChest).CreateInstance(new CardInstanceId(3));
                context.Grid.AddCardToGrid(chestCard, GridCoord.FromCellIndex(6), true);

                DomainEventBatch chestOpened = await facade.SubmitIntentAsync(new InteractWithCardIntent(chestCard.InstanceId));
                string chestSessionId = chestOpened.Events.First(e => e.EventType == DomainEventType.ChoiceOpened).Reason;
                Assert.IsTrue(context.ChoiceSessions.TryGet(chestSessionId, out ChoiceSession chestSession));
                Assert.AreEqual(3, chestSession.OptionKeys.Count);

                DomainEventBatch chestResolved = await facade.SubmitIntentAsync(new ChooseOptionIntent(chestSessionId, 0));
                Assert.IsTrue(chestResolved.Events.Any(e => e.EventType == DomainEventType.RelicAcquired));
                Assert.IsTrue(context.Relics.AllRelics.Any());
            });
        }

        [Test]
        public void FirstLayerCombatContent_UsesConfiguredTraitAndTrapPipelines()
        {
            Await(async () =>
            {
                StarterContentRegistry.RegisterAll();

                DomainActionContext bannerContext = NewContext();
                CardInstance banner = ModelDb.Get<CardModel>(StarterContentIds.Monsters.BannerSkeleton).CreateInstance(new CardInstanceId(2));
                CardInstance skeleton = ModelDb.Get<CardModel>(StarterContentIds.Monsters.Skeleton).CreateInstance(new CardInstanceId(3));
                bannerContext.Grid.AddCardToGrid(banner, GridCoord.FromCellIndex(4), true);
                bannerContext.Grid.AddCardToGrid(skeleton, GridCoord.FromCellIndex(5), true);
                await new DomainFacade(bannerContext).SubmitIntentAsync(new InteractWithCardIntent(skeleton.InstanceId));
                Assert.AreEqual(18, bannerContext.Grid.PlayerCard.CurrentHp, "Banner skeleton should increase another monster's damage by 1.");

                DomainActionContext revengeContext = NewContext();
                CardInstance revenge = ModelDb.Get<CardModel>(StarterContentIds.Monsters.RevengeSkeleton).CreateInstance(new CardInstanceId(4));
                CardInstance victim = ModelDb.Get<CardModel>(StarterContentIds.Monsters.Skeleton).CreateInstance(new CardInstanceId(5));
                revengeContext.Grid.PlayerCard.ConfigureCombatStats(20, 10, 1, 0, 0);
                revengeContext.Grid.AddCardToGrid(revenge, GridCoord.FromCellIndex(4), true);
                revengeContext.Grid.AddCardToGrid(victim, GridCoord.FromCellIndex(5), true);
                await new DomainFacade(revengeContext).SubmitIntentAsync(new InteractWithCardIntent(victim.InstanceId));
                Assert.AreEqual(6, revenge.Attack, "Revenge skeleton should gain +2 attack after another monster is removed.");

                DomainActionContext crossbowContext = NewContext();
                CardInstance crossbow = ModelDb.Get<CardModel>(StarterContentIds.Traps.Crossbow).CreateInstance(new CardInstanceId(6));
                CardInstance target = ModelDb.Get<CardModel>(StarterContentIds.Monsters.Skeleton).CreateInstance(new CardInstanceId(7));
                crossbowContext.Grid.PlayerCard.ConfigureCombatStats(20, 5, 1, 0, 0);
                crossbowContext.Grid.AddCardToGrid(target, GridCoord.FromCellIndex(2), true);
                crossbowContext.Grid.AddCardToGrid(crossbow, GridCoord.FromCellIndex(5), true);
                await new DomainFacade(crossbowContext).SubmitIntentAsync(new InteractWithCardIntent(crossbow.InstanceId));
                Assert.AreEqual(CardZone.Removed, target.Zone, "Crossbow trap should damage the revealed cards directly above it when destroyed.");

                DomainActionContext bossContext = NewContext();
                bossContext.Rng = new DeterministicRng(11);
                CardInstance boss = ModelDb.Get<CardModel>(StarterContentIds.Monsters.BigSkeletonLord).CreateInstance(new CardInstanceId(8));
                bossContext.Grid.PlayerCard.ConfigureCombatStats(20, 13, 1, 0, 0);
                bossContext.Grid.AddCardToGrid(boss, GridCoord.FromCellIndex(5), true);
                await new DomainFacade(bossContext).SubmitIntentAsync(new InteractWithCardIntent(boss.InstanceId));
                Assert.IsTrue(bossContext.Grid.AllGridCards.Count(card => card.InstanceId != boss.InstanceId && card.CardType == CardType.Monster) >= 1,
                    "Big Skeleton Lord should summon adjacent skeletons when its health crosses a 10 HP threshold.");
            });
        }

        [Test]
        public void ItemsAndActiveRelics_TargetingValidationAndEffectsWork()
        {
            Await(async () =>
            {
                StarterContentRegistry.RegisterAll();

                DomainActionContext context = NewContext();
                context.Rng = new DeterministicRng(19);
                context.Relics.Add(ModelDb.Get<RelicModel>(StarterContentIds.Relics.LawWand));
                DomainFacade facade = new DomainFacade(context);

                CardInstance movable = ModelDb.Get<CardModel>(StarterContentIds.Monsters.Skeleton).CreateInstance(new CardInstanceId(2));
                context.Grid.AddCardToGrid(movable, GridCoord.FromCellIndex(5), true);

                DomainEventBatch relicRejected = await facade.SubmitIntentAsync(new ActivateRelicIntent(StarterContentIds.Relics.LawWand));
                Assert.IsTrue(relicRejected.Events.Any(e => e.EventType == DomainEventType.IntentRejected && e.Reason == "RelicTargetMissing"));

                DomainEventBatch relicActivated = await facade.SubmitIntentAsync(new ActivateRelicIntent(
                    StarterContentIds.Relics.LawWand,
                    ItemTargetSelection.CardThenCell(movable.InstanceId, GridCoord.FromCellIndex(1))));
                Assert.IsTrue(relicActivated.Events.Any(e => e.EventType == DomainEventType.RelicActivated));
                Assert.AreEqual(GridCoord.FromCellIndex(1), movable.Coord.Value);

                CardInstance flipItem = ModelDb.Get<CardModel>(StarterContentIds.Items.FlipCard).CreateInstance(new CardInstanceId(3));
                InventorySlot flipSlot = context.ItemInventory.Store(flipItem);
                CardInstance faceUp = ModelDb.Get<CardModel>(StarterContentIds.Monsters.ArmoredSkeleton).CreateInstance(new CardInstanceId(4));
                CardInstance faceDown = ModelDb.Get<CardModel>(StarterContentIds.Items.ThrowingKnife).CreateInstance(new CardInstanceId(5));
                context.Grid.AddCardToGrid(faceUp, GridCoord.FromCellIndex(2), true);
                context.Grid.AddCardToGrid(faceDown, GridCoord.FromCellIndex(5), false);

                DomainEventBatch flipUsed = await facade.SubmitIntentAsync(new UseItemIntent(flipSlot, ItemTargetSelection.TwoCards(faceUp.InstanceId, faceDown.InstanceId)));
                Assert.IsTrue(flipUsed.Events.Any(e => e.EventType == DomainEventType.ItemUsed));
                Assert.AreEqual(GridCoord.FromCellIndex(5), faceUp.Coord.Value);
                Assert.AreEqual(GridCoord.FromCellIndex(2), faceDown.Coord.Value);
            });
        }

        private static DomainActionContext NewContext()
        {
            GridState grid = new GridState();
            CardInstance player = new CardInstance(new CardInstanceId(1), StarterContentIds.PlayerHero, CardType.Player);
            player.ConfigureCombatStats(20, 3, 1, 0, 0);
            grid.AddCardToGrid(player, GridCoord.FromCellIndex(8), true);

            DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
            context.Rng = new DeterministicRng(5);
            context.ContentCatalog = StarterContentRegistry.RegisterAll();
            return context;
        }

        private static void Await(System.Func<Task> asyncTest)
        {
            asyncTest().GetAwaiter().GetResult();
        }
    }
}
