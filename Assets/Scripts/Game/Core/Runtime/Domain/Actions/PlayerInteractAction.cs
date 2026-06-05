using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;

namespace Game.Core.Domain.Actions
{
    public sealed class PlayerInteractAction : GameAction
    {
        private readonly DomainActionContext _domain;
        private readonly InteractWithCardIntent _intent;

        public PlayerInteractAction(DomainActionContext domain, InteractWithCardIntent intent)
        {
            _domain = domain;
            _intent = intent;
        }

        protected override Task ExecuteActionAsync(GameActionExecutionContext ctx)
        {
            List<DomainEvent> events = new List<DomainEvent>();
            IntentValidationResult validation = new IntentValidator(_domain.Grid).Validate(_intent);
            if (!validation.IsValid)
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = validation.FailureCode });
                AddBatch(events);
                return Task.CompletedTask;
            }

            CardInstance player = _domain.Grid.PlayerCard;
            CardInstance target = _domain.Grid.GetCard(_intent.Target);
            events.Add(_domain.ActionCounter.Increment(_intent));

            switch (target.CardType)
            {
                case CardType.Monster:
                    _domain.Combat.ResolvePlayerVsMonster(player, target, events);
                    RemoveIfDead(target, RemoveReason.Defeated, events);
                    RemoveIfDead(player, RemoveReason.Defeated, events);
                    break;
                case CardType.Trap:
                    _domain.Combat.ResolvePlayerVsTrap(player, target, events);
                    RemoveIfDead(target, RemoveReason.Destroyed, events);
                    RemoveIfDead(player, RemoveReason.Defeated, events);
                    break;
                case CardType.Gold:
                    _domain.GainGold(target.GoldValue, events, "GoldCard");
                    events.AddRange(_domain.Grid.RemoveCard(target, RemoveReason.Collected).Events);
                    break;
                default:
                    events.AddRange(_domain.Grid.RemoveCard(target, RemoveReason.Consumed).Events);
                    break;
            }

            if (_domain.RoomClearChecker.IsRoomCleared(_domain.Grid))
            {
                events.Add(new DomainEvent(DomainEventType.RoomCleared));
            }

            AddBatch(events);
            return Task.CompletedTask;
        }

        private void RemoveIfDead(CardInstance card, RemoveReason reason, List<DomainEvent> events)
        {
            if (card.HasHitPoints && !card.IsAlive && card.Zone == CardZone.Grid)
            {
                events.AddRange(_domain.Grid.RemoveCard(card, reason).Events);
                if (card.CardType == CardType.Monster)
                {
                    events.Add(new DomainEvent(DomainEventType.MonsterDefeated)
                    {
                        CardId = card.InstanceId,
                        Reason = reason.ToString()
                    });
                    _domain.GainGold(card.GoldOnRemoved, events, "MonsterRemoved");
                }
            }
        }

        private void AddBatch(IEnumerable<DomainEvent> events)
        {
            DomainEventBatch batch = new DomainEventBatch(Id, _intent);
            batch.AddRange(events);
            _domain.Batches.Add(batch);
        }
    }
}
