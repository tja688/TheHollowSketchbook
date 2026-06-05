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
using Game.Core.Domain.Validation;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class DomainP0Tests
    {
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
        public async Task PlayerMove_ToEmptyCellCountsActionAndRevealsAdjacentTopCards()
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
        }

        [Test]
        public async Task InteractWithMonster_PlayerPositionStaysAndDeadMonsterRemovedWithGold()
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
        }

        [Test]
        public async Task InvalidInteractWithFaceDownCardIsRejectedAndDoesNotCountAction()
        {
            GridState grid = NewGridWithPlayer();
            CardInstance monster = NewCard(2, CardType.Monster, hp: 4);
            grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), false);
            DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
            DomainFacade facade = new DomainFacade(context);

            DomainEventBatch batch = await facade.SubmitIntentAsync(new InteractWithCardIntent(monster.InstanceId));

            Assert.AreEqual(0, context.ActionCounter.Value);
            Assert.IsTrue(batch.Events.Any(evt => evt.EventType == DomainEventType.IntentRejected && evt.Reason == "TargetFaceDown"));
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
            GridState grid = NewGridWithPlayer();
            CardInstance player = grid.PlayerCard;
            player.ConfigureCombatStats(20, 5, 3, 0, 0);
            CardInstance trap = NewCard(2, CardType.Trap, hp: 6, attack: 0, defense: 2, contactDamage: 4);
            grid.AddCardToGrid(trap, GridCoord.FromCellIndex(5), true);
            DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
            int oldPlayerHp = player.CurrentHp;

            context.Combat.ResolvePlayerVsTrap(player, trap, null);

            Assert.AreEqual(3, trap.CurrentHp);
            Assert.AreEqual(oldPlayerHp - 4, player.CurrentHp);
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
            GridState grid = NewGridWithPlayer();
            CardInstance player = grid.PlayerCard;
            player.ConfigureCombatStats(20, 5, 0, 0, 0);
            CardInstance monster = NewCard(2, CardType.Monster, hp: 10, attack: 20, defense: 0);
            monster.SetState("firstStrike", 1);
            grid.AddCardToGrid(monster, GridCoord.FromCellIndex(5), true);
            DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());

            context.Combat.ResolvePlayerVsMonster(player, monster, null);

            // Monster has first strike, so it attacks first and kills player.
            // Player should NOT get a counter-attack because player is dead.
            Assert.AreEqual(0, player.CurrentHp);
            Assert.AreEqual(10, monster.CurrentHp); // Player never got to attack
        }

        [Test]
        public void CombatResolution_SimultaneousDeath_BothDie()
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

            context.Combat.ResolvePlayerVsMonster(player, monster, events);

            // Both should die because damage is applied simultaneously
            Assert.AreEqual(0, player.CurrentHp);
            Assert.AreEqual(0, monster.CurrentHp);
            Assert.IsTrue(events.Any(e => e.EventType == DomainEventType.DamageApplied && e.Reason.Contains("PlayerAttackMonster")));
            Assert.IsTrue(events.Any(e => e.EventType == DomainEventType.DamageApplied && e.Reason.Contains("MonsterCounterAttack")));
        }

        [Test]
        public void CombatResolution_DamagePrevention_BlocksDamage()
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

            DamageResult result = context.Combat.ApplyDamage(info, events);

            Assert.AreEqual(0, result.HpLoss);
            Assert.IsTrue(result.Prevented);
            Assert.AreEqual(oldHp, player.CurrentHp);
            Assert.AreEqual(0, player.GetState("damageImmunity"));
        }

        [Test]
        public async Task PlayerDefeated_TriggersRunEnded()
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
        }

        [Test]
        public void ApplyDamage_NonCreatureTarget_DoesNothing()
        {
            GridState grid = NewGridWithPlayer();
            CardInstance gold = NewCard(2, CardType.Gold, hp: 0, attack: 0, defense: 0);
            grid.AddCardToGrid(gold, GridCoord.FromCellIndex(5), true);
            DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
            List<DomainEvent> events = new List<DomainEvent>();

            DamageResult result = context.Combat.ApplyDamage(new DamageInfo(
                DamageSource.FromCard(grid.PlayerCard.InstanceId),
                DamageTarget.Card(gold.InstanceId),
                5,
                DamageKind.Attack,
                false,
                "Test"), events);

            Assert.AreEqual(0, result.HpLoss);
            Assert.IsFalse(result.Killed);
        }

        [Test]
        public async Task StoreItemIntent_MovesItemToInventory()
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
            CardInstance card = new CardInstance(new CardInstanceId(id), new ModelId("test", id.ToString()), type);
            card.ConfigureCombatStats(hp, attack, defense, contactDamage, type == CardType.Monster ? 10 : 0);
            return card;
        }
    }
}
