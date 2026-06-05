using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Rooms;
using Game.Core.Random;

namespace Game.Core.Domain
{
    public sealed class DomainActionContext
    {
        public DomainActionContext(GridState grid, PlayerActionCounter actionCounter)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            ActionCounter = actionCounter ?? throw new ArgumentNullException(nameof(actionCounter));
            Combat = new CombatResolution(grid);
            RoomClearChecker = new RoomClearChecker();
        }

        public GridState Grid { get; }
        public PlayerActionCounter ActionCounter { get; }
        public CombatResolution Combat { get; }
        public RoomClearChecker RoomClearChecker { get; }
        public DungeonDeck DungeonDeck { get; set; }
        public IRng Rng { get; set; }
        public int PlayerGold { get; private set; }
        public List<DomainEventBatch> Batches { get; } = new List<DomainEventBatch>();

        public void GainGold(int amount, ICollection<DomainEvent> events, string reason)
        {
            int delta = Math.Max(0, amount);
            if (delta == 0)
            {
                return;
            }

            PlayerGold += delta;
            events?.Add(new DomainEvent(DomainEventType.GoldChanged)
            {
                Amount = delta,
                SecondaryAmount = PlayerGold,
                Reason = reason
            });
        }
    }
}
