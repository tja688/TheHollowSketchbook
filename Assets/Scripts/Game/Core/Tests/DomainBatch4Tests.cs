using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Domain;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Inventory;
using Game.Core.Domain.Progression;
using Game.Core.Models;
using Game.Core.Saves;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class DomainBatch4Tests
    {
        [SetUp]
        public void SetUp()
        {
            ModelDb.Clear();
        }

        [Test]
        public void ChoiceSession_ResolvesThroughSourceCardModelCallback()
        {
            Await(async () =>
            {
                ModelId choiceCardId = new ModelId("test", "choice-card");
                ModelDb.Register(new TestChoiceCardModel(choiceCardId));

                GridState grid = NewGridWithPlayer();
                CardInstance choiceCard = ModelDb.Get<CardModel>(choiceCardId).CreateInstance(new CardInstanceId(2));
                grid.AddCardToGrid(choiceCard, GridCoord.FromCellIndex(5), true);

                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                DomainFacade facade = new DomainFacade(context);

                DomainEventBatch opened = await facade.SubmitIntentAsync(new InteractWithCardIntent(choiceCard.InstanceId));

                Assert.IsTrue(opened.Events.Any(e => e.EventType == DomainEventType.ChoiceOpened && e.Reason == "test-choice:2"));
                Assert.IsTrue(context.ChoiceSessions.TryGet("test-choice:2", out ChoiceSession session));
                CollectionAssert.AreEqual(new[] { "3", "7" }, session.OptionKeys);

                DomainEventBatch resolved = await facade.SubmitIntentAsync(new ChooseOptionIntent("test-choice:2", 1));

                Assert.IsTrue(resolved.Events.Any(e => e.EventType == DomainEventType.ChoiceResolved && e.Reason == "test-choice:2"));
                Assert.AreEqual(7, context.PlayerGold);
                Assert.AreEqual(CardZone.Removed, choiceCard.Zone);
            });
        }

        [Test]
        public void PlayerTraitHooks_PersistThroughSaveRestoreAndDispatchOnRoomClear()
        {
            Await(async () =>
            {
                ModelId traitId = new ModelId("test", "room-clear-trait");
                TestPlayerRoomClearTraitModel trait = new TestPlayerRoomClearTraitModel(traitId);
                ModelDb.Register(trait);

                GridState grid = NewGridWithPlayer();
                CardInstance monster = NewCard(2, CardType.Monster, hp: 1, attack: 0, defense: 0);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);

                DomainActionContext original = new DomainActionContext(grid, new PlayerActionCounter());
                original.PlayerRunState.AddTrait(traitId, StatModifierScope.Permanent, "trait:test");

                RoomDomainStateSaveDto dto = DomainSaveAdapter.Capture(original);
                DomainActionContext restored = new DomainActionContext(NewGridWithPlayer(), new PlayerActionCounter());
                DomainSaveAdapter.Restore(dto, restored);

                Assert.IsTrue(restored.PlayerRunState.PermanentTraits.Any(t => t.TraitId == traitId));

                CardInstance restoredMonster = restored.Grid.AllGridCards.First(card => card.CardType == CardType.Monster);
                DomainFacade facade = new DomainFacade(restored);
                await facade.SubmitIntentAsync(new InteractWithCardIntent(restoredMonster.InstanceId));

                Assert.AreEqual(1, trait.RoomClearCount);
            });
        }

        [Test]
        public void ActivateRelicIntent_TargetSelectionFlowsIntoActiveRelicModel()
        {
            Await(async () =>
            {
                ModelId relicId = new ModelId("test", "target-relic");
                ModelDb.Register(new TestTargetedRelicModel(relicId));

                GridState grid = NewGridWithPlayer();
                CardInstance target = NewCard(2, CardType.Item);
                grid.AddCardToGrid(target, GridCoord.FromCellIndex(5), true);

                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                context.Relics.SetActive(relicId);
                DomainFacade facade = new DomainFacade(context);

                DomainEventBatch rejected = await facade.SubmitIntentAsync(new ActivateRelicIntent(relicId));
                Assert.IsTrue(rejected.Events.Any(e => e.EventType == DomainEventType.IntentRejected && e.Reason == "RelicTargetMissing"));

                DomainEventBatch activated = await facade.SubmitIntentAsync(new ActivateRelicIntent(
                    relicId,
                    ItemTargetSelection.CardThenCell(target.InstanceId, GridCoord.FromCellIndex(1))));

                Assert.IsTrue(activated.Events.Any(e => e.EventType == DomainEventType.RelicActivated));
                Assert.AreEqual(GridCoord.FromCellIndex(1), target.Coord.Value);
            });
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
            if (!ModelDb.Contains(modelId))
            {
                if (type == CardType.Monster)
                {
                    ModelDb.Register(new TestMonsterModel(modelId, hp, attack, defense));
                }
                else if (type == CardType.Trap)
                {
                    ModelDb.Register(new TestTrapModel(modelId, hp, defense, contactDamage));
                }
                else
                {
                    ModelDb.Register(new TestBasicCardModel(modelId, type, hp, attack, defense, contactDamage));
                }
            }

            return card;
        }

        private sealed class TestBasicCardModel : CardModel
        {
            private readonly CardType _cardType;
            private readonly int _maxHp;
            private readonly int _attack;
            private readonly int _defense;
            private readonly int _contactDamage;

            public TestBasicCardModel(ModelId id, CardType cardType, int maxHp, int attack, int defense, int contactDamage)
            {
                Id = id;
                _cardType = cardType;
                _maxHp = maxHp;
                _attack = attack;
                _defense = defense;
                _contactDamage = contactDamage;
            }

            public override ModelId Id { get; }
            public override CardType CardType => _cardType;

            protected override void ConfigureCreatedInstance(CardInstance instance)
            {
                instance.ConfigureCombatStats(_maxHp, _attack, _defense, _contactDamage, _cardType == CardType.Monster ? 10 : 0);
            }
        }

        private sealed class TestMonsterModel : MonsterCardModel
        {
            private readonly int _maxHp;
            private readonly int _attack;
            private readonly int _defense;

            public TestMonsterModel(ModelId id, int maxHp, int attack, int defense)
            {
                Id = id;
                _maxHp = maxHp;
                _attack = attack;
                _defense = defense;
            }

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

            public TestTrapModel(ModelId id, int maxHp, int defense, int contactDamage)
            {
                Id = id;
                _maxHp = maxHp;
                _defense = defense;
                _contactDamage = contactDamage;
            }

            public override ModelId Id { get; }
            public override int MaxHp => _maxHp;
            public override int Defense => _defense;
            public override int ContactDamageToPlayer => _contactDamage;
        }

        private sealed class TestChoiceCardModel : CardModel
        {
            public TestChoiceCardModel(ModelId id)
            {
                Id = id;
            }

            public override ModelId Id { get; }
            public override CardType CardType => CardType.Special;

            public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
            {
                ctx.Domain.OpenChoiceSession(
                    sessionId: "test-choice:" + ctx.TargetCard.InstanceId.Value,
                    sourceCard: ctx.TargetCard,
                    choiceKind: "TestChoice",
                    optionKeys: new[] { "3", "7" },
                    events: ctx.Events);
                return Task.CompletedTask;
            }

            public override Task OnChoiceResolvedAsync(ChoiceResolutionContext ctx)
            {
                ctx.Domain.GainGold(int.Parse(ctx.SelectedOptionKey), ctx.Events, "TestChoiceReward");
                ctx.AddResult(ctx.Domain.Grid.RemoveCard(ctx.SourceCard, RemoveReason.Consumed));
                return Task.CompletedTask;
            }
        }

        private sealed class TestPlayerRoomClearTraitModel : TraitModel
        {
            public TestPlayerRoomClearTraitModel(ModelId id)
            {
                Id = id;
            }

            public override ModelId Id { get; }
            public int RoomClearCount { get; private set; }

            public override Task OnRoomClearedAsync(RoomLifecycleContext ctx)
            {
                RoomClearCount++;
                return Task.CompletedTask;
            }
        }

        private sealed class TestTargetedRelicModel : RelicModel
        {
            public TestTargetedRelicModel(ModelId id)
            {
                Id = id;
            }

            public override ModelId Id { get; }
            public override RelicRarity Rarity => RelicRarity.Common;
            public override RelicKind Kind => RelicKind.Active;
            public override ItemTargetMode TargetMode => ItemTargetMode.AnyCardThenAnyCell;

            public override Task ActivateAsync(ActiveRelicContext ctx)
            {
                ActivateRelicIntent intent = (ActivateRelicIntent)ctx.SourceIntent;
                CardInstance target = ctx.Domain.Grid.GetCard(intent.Target.PrimaryCard);
                GridCoord destination = intent.Target.GridCell;
                GridOperationResult result = ctx.Domain.Grid.IsEmpty(destination)
                    ? ctx.Domain.Grid.MoveTopCardToTop(target, destination)
                    : ctx.Domain.Grid.CoverCellWithCard(target, destination, target.IsFaceUp);
                ctx.AddResult(result);
                return Task.CompletedTask;
            }
        }

        private static void Await(System.Func<Task> asyncTest)
        {
            asyncTest().GetAwaiter().GetResult();
        }
    }
}
