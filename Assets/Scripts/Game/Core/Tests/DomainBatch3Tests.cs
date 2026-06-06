using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Domain;
using Game.Core.Domain.Actions;
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
    public sealed class DomainBatch3Tests
    {
        [SetUp]
        public void SetUp()
        {
            ModelDb.Clear();
        }

        [Test]
        public void PlayerRunState_CombinesPermanentAndRoomStatsAndOverridesRoomKeywords()
        {
            PlayerRunState state = new PlayerRunState(20, 3, 1);

            state.AddModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Permanent, 2, "mentor"));
            state.AddModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Room, 4, "rage-room"));
            state.SetKeyword("firstStrike", 1, StatModifierScope.Permanent);
            state.SetKeyword("firstStrike", 0, StatModifierScope.Room);

            Assert.AreEqual(9, state.CurrentAttack);
            Assert.AreEqual(0, state.GetKeyword("firstStrike"));

            state.ClearRoomState();

            Assert.AreEqual(5, state.CurrentAttack);
            Assert.AreEqual(1, state.GetKeyword("firstStrike"));
        }

        [Test]
        public void PlayerRunState_ApplyToPlayerCardUpdatesCombatStatsAndPreservesCurrentHp()
        {
            CardInstance player = NewGridWithPlayer().PlayerCard;
            player.SetCurrentHp(12);
            PlayerRunState state = new PlayerRunState(20, 3, 1);
            state.AddModifier(new StatModifier(PlayerStat.MaxHp, StatModifierScope.Permanent, 5, "relic"));
            state.AddModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Permanent, 2, "mentor"));
            state.AddModifier(new StatModifier(PlayerStat.Defense, StatModifierScope.Room, 3, "room"));

            state.ApplyTo(player);

            Assert.AreEqual(25, player.MaxHp);
            Assert.AreEqual(12, player.CurrentHp);
            Assert.AreEqual(5, player.Attack);
            Assert.AreEqual(4, player.Defense);
        }

        [Test]
        public void SaveRestore_PreservesPlayerRunStateAndPendingTriggerQueue()
        {
            DomainActionContext original = NewContext();
            original.PlayerRunState = new PlayerRunState(30, 5, 2);
            original.PlayerRunState.AddModifier(new StatModifier(PlayerStat.MaxHp, StatModifierScope.Permanent, 3, "relic"));
            original.PlayerRunState.AddModifier(new StatModifier(PlayerStat.Defense, StatModifierScope.Room, 2, "room"));
            original.PlayerRunState.SetKeyword("ambush", 1, StatModifierScope.Room);
            original.PendingTriggers.Enqueue(new PendingTrigger(
                new CardInstanceId(2),
                PendingTriggerTiming.AfterPlayerAction,
                original.ActionCounter.Value + 1,
                "spike"));

            RoomDomainStateSaveDto dto = DomainSaveAdapter.Capture(original);
            DomainActionContext restored = NewContext();
            DomainSaveAdapter.Restore(dto, restored);

            Assert.AreEqual(33, restored.PlayerRunState.CurrentMaxHp);
            Assert.AreEqual(4, restored.PlayerRunState.CurrentDefense);
            Assert.AreEqual(1, restored.PlayerRunState.GetKeyword("ambush"));
            Assert.AreEqual(1, restored.PendingTriggers.Count);
            Assert.AreEqual("spike", restored.PendingTriggers.Peek().TriggerKey);
        }

        [Test]
        public void PendingTriggerQueue_DequeueDueAfterPlayerActionOnlyOnce()
        {
            PendingTriggerQueue queue = new PendingTriggerQueue();
            queue.Enqueue(new PendingTrigger(new CardInstanceId(2), PendingTriggerTiming.AfterPlayerAction, 2, "spike"));

            Assert.AreEqual(0, queue.DequeueDue(PendingTriggerTiming.AfterPlayerAction, 1).Count);
            IReadOnlyList<PendingTrigger> due = queue.DequeueDue(PendingTriggerTiming.AfterPlayerAction, 2);

            Assert.AreEqual(1, due.Count);
            Assert.AreEqual("spike", due[0].TriggerKey);
            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(0, queue.DequeueDue(PendingTriggerTiming.AfterPlayerAction, 3).Count);
        }

        [Test]
        public void DeathRewardPipeline_RemovesMonsterKilledByItemAndGrantsConfiguredRewards()
        {
            Await(async () =>
            {
                ModelId itemId = new ModelId("test", "knife");
                ModelDb.Register(new DamageItemModel(itemId, 99));
                GridState grid = NewGridWithPlayer();
                CardInstance monster = NewCard(2, CardType.Monster, hp: 4, attack: 0, defense: 0);
                monster.SetState("eliteGoldBonus", 15);
                grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
                DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
                CardInstance item = new CardInstance(new CardInstanceId(3), itemId, CardType.Item);
                InventorySlot slot = context.ItemInventory.Store(item);

                await new DomainFacade(context).SubmitIntentAsync(new UseItemIntent(slot, ItemTargetSelection.CardTarget(monster.InstanceId)));

                Assert.AreEqual(CardZone.Removed, monster.Zone);
                Assert.AreEqual(25, context.PlayerGold);
                Assert.IsTrue(context.Batches.Last().Events.Any(e => e.EventType == DomainEventType.MonsterDefeated && e.CardId == monster.InstanceId));
            });
        }

        [Test]
        public void RepresentativeMechanics_AggressiveAmbushScatterSpikeAndTeleportUseDomainPipelines()
        {
            Await(async () =>
            {
                ModelId aggressiveId = new ModelId("test", "aggressive");
                ModelId ambushId = new ModelId("test", "ambush");
                ModelId scatterId = new ModelId("test", "scatter");
                ModelId spikeId = new ModelId("test", "spike");
                ModelId teleportId = new ModelId("test", "teleport");
                ModelDb.Register(new AggressiveMonsterModel(aggressiveId));
                ModelDb.Register(new AmbushMonsterModel(ambushId));
                ModelDb.Register(new ScatterMonsterModel(scatterId));
                ModelDb.Register(new SpikeTrapModel(spikeId));
                ModelDb.Register(new TeleportTrapModel(teleportId));

                DomainActionContext aggressiveContext = NewContext();
                CardInstance aggressive = ModelDb.Get<CardModel>(aggressiveId).CreateInstance(new CardInstanceId(2));
                aggressiveContext.Grid.AddCardToGrid(aggressive, GridCoord.FromCellIndex(5), true);
                aggressiveContext.ActionCounter.Increment(new MovePlayerIntent(GridCoord.FromCellIndex(7)));
                await aggressiveContext.NotifyAfterPlayerActionCommittedAsync(new MovePlayerIntent(GridCoord.FromCellIndex(7)), new List<DomainEvent>());
                Assert.AreEqual(19, aggressiveContext.Grid.PlayerCard.CurrentHp, "Aggressive monster should damage player after defense on a player action.");

                DomainActionContext ambushContext = NewContext();
                CardInstance ambush = ModelDb.Get<CardModel>(ambushId).CreateInstance(new CardInstanceId(3));
                ambushContext.Grid.AddCardToGrid(ambush, GridCoord.FromCellIndex(4), false);
                List<DomainEvent> ambushEvents = new List<DomainEvent>();
                ambushEvents.AddRange(ambushContext.Grid.FlipCard(ambush, FlipReason.PlayerAdjacentReveal).Events);
                await ambushContext.ProcessLifecycleAsync(ambushEvents);
                Assert.AreEqual(1, ambush.GetState("firstStrike"), "Ambush should mark first strike when flipped.");

                DomainActionContext scatterContext = NewContext();
                CardInstance scatter = ModelDb.Get<CardModel>(scatterId).CreateInstance(new CardInstanceId(4));
                scatterContext.Grid.AddCardToGrid(scatter, GridCoord.FromCellIndex(3), true);
                await scatterContext.Combat.ApplyDamageAsync(new DamageInfo(
                    DamageSource.FromCard(scatterContext.Grid.PlayerCard.InstanceId),
                    DamageTarget.Card(scatter.InstanceId),
                    1,
                    DamageKind.Attack,
                    false,
                    "TestScatter"), new List<DomainEvent>());
                Assert.AreNotEqual(GridCoord.FromCellIndex(3), scatter.Coord.Value, "Scatter should move after taking damage.");

                DomainActionContext spikeContext = NewContext();
                CardInstance spike = ModelDb.Get<CardModel>(spikeId).CreateInstance(new CardInstanceId(5));
                spikeContext.Grid.AddCardToGrid(spike, GridCoord.FromCellIndex(2), false);
                List<DomainEvent> spikeEvents = new List<DomainEvent>();
                spikeEvents.AddRange(spikeContext.Grid.FlipCard(spike, FlipReason.PlayerAdjacentReveal).Events);
                await spikeContext.ProcessLifecycleAsync(spikeEvents);
                int hpAfterSpikeReveal = spikeContext.Grid.PlayerCard.CurrentHp;
                spikeContext.ActionCounter.Increment(new MovePlayerIntent(GridCoord.FromCellIndex(7)));
                await spikeContext.NotifyAfterPlayerActionCommittedAsync(new MovePlayerIntent(GridCoord.FromCellIndex(7)), spikeEvents);
                Assert.AreEqual(hpAfterSpikeReveal - 3, spikeContext.Grid.PlayerCard.CurrentHp, "Spike should fire from the pending trigger queue on the next player action.");

                DomainActionContext teleportContext = NewContext();
                CardInstance teleport = ModelDb.Get<CardModel>(teleportId).CreateInstance(new CardInstanceId(6));
                teleportContext.Grid.AddCardToGrid(teleport, GridCoord.FromCellIndex(9), false);
                List<DomainEvent> teleportEvents = new List<DomainEvent>();
                teleportEvents.AddRange(teleportContext.Grid.FlipCard(teleport, FlipReason.PlayerAdjacentReveal).Events);
                await teleportContext.ProcessLifecycleAsync(teleportEvents);
                GridCoord beforeTeleport = teleportContext.Grid.PlayerCard.Coord.Value;
                teleportContext.ActionCounter.Increment(new MovePlayerIntent(GridCoord.FromCellIndex(7)));
                await teleportContext.NotifyAfterPlayerActionCommittedAsync(new MovePlayerIntent(GridCoord.FromCellIndex(7)), teleportEvents);
                Assert.AreNotEqual(beforeTeleport, teleportContext.Grid.PlayerCard.Coord.Value, "Teleport should move the player through the pending trigger queue.");
            });
        }

        private static DomainActionContext NewContext()
        {
            return new DomainActionContext(NewGridWithPlayer(), new PlayerActionCounter());
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
                    ModelDb.Register(new BasicMonsterModel(modelId, hp, attack, defense));
                }
                else if (type == CardType.Trap)
                {
                    ModelDb.Register(new BasicTrapModel(modelId, hp, defense, contactDamage));
                }
                else
                {
                    ModelDb.Register(new BasicCardModel(modelId, type));
                }
            }

            return card;
        }

        private sealed class BasicCardModel : CardModel
        {
            private readonly CardType _cardType;
            public BasicCardModel(ModelId id, CardType cardType) { Id = id; _cardType = cardType; }
            public override ModelId Id { get; }
            public override CardType CardType => _cardType;
        }

        private sealed class BasicMonsterModel : MonsterCardModel
        {
            private readonly int _maxHp;
            private readonly int _attack;
            private readonly int _defense;
            public BasicMonsterModel(ModelId id, int maxHp, int attack, int defense) { Id = id; _maxHp = maxHp; _attack = attack; _defense = defense; }
            public override ModelId Id { get; }
            public override int Level => 1;
            public override int MaxHp => _maxHp;
            public override int Attack => _attack;
            public override int Defense => _defense;
        }

        private sealed class BasicTrapModel : TrapCardModel
        {
            private readonly int _maxHp;
            private readonly int _defense;
            private readonly int _contactDamage;
            public BasicTrapModel(ModelId id, int maxHp, int defense, int contactDamage) { Id = id; _maxHp = maxHp; _defense = defense; _contactDamage = contactDamage; }
            public override ModelId Id { get; }
            public override int MaxHp => _maxHp;
            public override int Defense => _defense;
            public override int ContactDamageToPlayer => _contactDamage;
        }

        private sealed class DamageItemModel : ItemCardModel
        {
            private readonly int _damage;
            public DamageItemModel(ModelId id, int damage) { Id = id; _damage = damage; }
            public override ModelId Id { get; }
            public override ItemTargetMode TargetMode => ItemTargetMode.MonsterCard;
            public override Task UseAsync(ItemUseContext ctx)
            {
                return ctx.ApplyDamageAsync(new DamageInfo(
                    DamageSource.FromCard(ctx.ItemCard.InstanceId),
                    DamageTarget.Card(((UseItemIntent)ctx.SourceIntent).Target.PrimaryCard),
                    _damage,
                    DamageKind.Item,
                    false,
                    "DamageItem"));
            }
        }

        private sealed class AggressiveMonsterModel : MonsterCardModel
        {
            public AggressiveMonsterModel(ModelId id) { Id = id; }
            public override ModelId Id { get; }
            public override int Level => 1;
            public override int MaxHp => 8;
            public override int Attack => 2;
            public override int Defense => 0;
            public override Task OnAfterPlayerActionCommittedAsync(PlayerActionContext ctx)
            {
                return ctx.ApplyDamageAsync(new DamageInfo(DamageSource.FromCard(ctx.ObservedCard.InstanceId), DamageTarget.Card(ctx.PlayerCard.InstanceId), 2, DamageKind.Attack, false, "Aggressive"));
            }
        }

        private sealed class AmbushMonsterModel : MonsterCardModel
        {
            public AmbushMonsterModel(ModelId id) { Id = id; }
            public override ModelId Id { get; }
            public override int Level => 1;
            public override int MaxHp => 5;
            public override int Attack => 3;
            public override int Defense => 0;
            public override Task OnRevealedAsync(CardRevealContext ctx)
            {
                ctx.Card.SetState("firstStrike", 1);
                return Task.CompletedTask;
            }
        }

        private sealed class ScatterMonsterModel : MonsterCardModel
        {
            public ScatterMonsterModel(ModelId id) { Id = id; }
            public override ModelId Id { get; }
            public override int Level => 1;
            public override int MaxHp => 5;
            public override int Attack => 1;
            public override int Defense => 0;
            public override Task OnAfterDamageAsync(DamageContext ctx, DamageResult result)
            {
                if (result.HpLoss <= 0 || !ctx.TargetCard.Coord.HasValue)
                {
                    return Task.CompletedTask;
                }

                GridCoord to = GridQueries.AllCoordsRowMajor().First(coord => coord.CellIndex != 8 && ctx.Domain.Grid.IsEmpty(coord));
                if (to.IsValid)
                {
                    ctx.Domain.Grid.MoveTopCardToTop(ctx.TargetCard, to);
                }

                return Task.CompletedTask;
            }
        }

        private sealed class SpikeTrapModel : TrapCardModel
        {
            public SpikeTrapModel(ModelId id) { Id = id; }
            public override ModelId Id { get; }
            public override int MaxHp => 3;
            public override Task OnRevealedAsync(TrapContext ctx)
            {
                ctx.Domain.PendingTriggers.Enqueue(new PendingTrigger(ctx.TrapCard.InstanceId, PendingTriggerTiming.AfterPlayerAction, ctx.ActionIndex + 1, "spike"));
                return Task.CompletedTask;
            }

            public override Task OnPendingTriggerAsync(PendingTriggerContext ctx)
            {
                return ctx.ApplyDamageAsync(new DamageInfo(DamageSource.FromCard(ctx.Card.InstanceId), DamageTarget.Card(ctx.PlayerCard.InstanceId), 3, DamageKind.Trap, true, "SpikeTrigger"));
            }
        }

        private sealed class TeleportTrapModel : TrapCardModel
        {
            public TeleportTrapModel(ModelId id) { Id = id; }
            public override ModelId Id { get; }
            public override int MaxHp => 3;
            public override Task OnRevealedAsync(TrapContext ctx)
            {
                ctx.Domain.PendingTriggers.Enqueue(new PendingTrigger(ctx.TrapCard.InstanceId, PendingTriggerTiming.AfterPlayerAction, ctx.ActionIndex + 1, "teleport"));
                return Task.CompletedTask;
            }

            public override Task OnPendingTriggerAsync(PendingTriggerContext ctx)
            {
                GridCoord to = GridCoord.FromCellIndex(6);
                if (ctx.Domain.Grid.IsEmpty(to))
                {
                    ctx.AddResult(ctx.Domain.Grid.MoveCardToEmptyCell(ctx.PlayerCard, to));
                }

                return Task.CompletedTask;
            }
        }

        private static void Await(System.Func<Task> asyncTest)
        {
            asyncTest().GetAwaiter().GetResult();
        }
    }
}
