using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Inventory;
using Game.Core.Domain.Progression;
using Game.Core.Domain.Rooms;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;

namespace Game.Core.Domain
{
    public sealed class DomainActionContext
    {
        public DomainActionContext(GridState grid, PlayerActionCounter actionCounter)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            ActionCounter = actionCounter ?? throw new ArgumentNullException(nameof(actionCounter));
            Combat = new CombatResolution(grid);
            Combat.Domain = this;
            RoomClearChecker = new RoomClearChecker();
            ItemInventory = new PlayerInventory();
            Relics = new RelicInventory();
            ChoiceSessions = new ChoiceSessionStore();
            PendingTriggers = new PendingTriggerQueue();
            PlayerRunState = CreateInitialPlayerRunState(grid);
        }

        public GridState Grid { get; internal set; }
        public PlayerActionCounter ActionCounter { get; }
        public CombatResolution Combat { get; }
        public RoomClearChecker RoomClearChecker { get; }
        public DungeonDeck DungeonDeck { get; set; }
        public IRng Rng { get; set; }
        public RunProgressionState Progression { get; set; }
        public RoomTransitionService RoomTransition { get; set; }
        public RoomContentCatalog ContentCatalog { get; set; }
        public PlayerInventory ItemInventory { get; }
        public RelicInventory Relics { get; }
        public ChoiceSessionStore ChoiceSessions { get; }
        public PendingTriggerQueue PendingTriggers { get; }
        public PlayerRunState PlayerRunState { get; set; }
        public int PlayerGold { get; private set; }

        public void ReplaceGrid(GridState grid)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Combat.SetGrid(grid);
        }

        public void SetPlayerGold(int value)
        {
            PlayerGold = Math.Max(0, value);
        }

        public ChoiceSession OpenChoiceSession(string sessionId, CardInstance sourceCard, string choiceKind, IReadOnlyList<string> optionKeys, ICollection<DomainEvent> events)
        {
            if (sourceCard == null)
            {
                throw new ArgumentNullException(nameof(sourceCard));
            }

            if (optionKeys == null || optionKeys.Count == 0)
            {
                throw new ArgumentException("Choice session must expose at least one option.", nameof(optionKeys));
            }

            ChoiceSession session = ChoiceSessions.Open(sessionId, optionKeys.Count, choiceKind, sourceCard.InstanceId, optionKeys);
            events?.Add(new DomainEvent(DomainEventType.ChoiceOpened)
            {
                CardId = sourceCard.InstanceId,
                Amount = optionKeys.Count,
                Reason = sessionId
            });
            return session;
        }

        public async Task ResolveChoiceSessionAsync(ChoiceSession session, int optionIndex, ICollection<DomainEvent> events)
        {
            if (session == null || session.SourceCardId.IsEmpty)
            {
                return;
            }

            if (!Grid.TryGetCard(session.SourceCardId, out CardInstance sourceCard))
            {
                return;
            }

            if (!TryResolveCardModel(sourceCard, out CardModel model))
            {
                return;
            }

            ChoiceResolutionContext context = new ChoiceResolutionContext(this, sourceCard, session, optionIndex, events);
            await model.OnChoiceResolvedAsync(context).ConfigureAwait(false);
        }

        public void AddPlayerModifier(StatModifier modifier, ICollection<DomainEvent> events, string reason = null)
        {
            PlayerRunState?.AddModifier(modifier);
            ApplyPlayerRunState(events, reason ?? GetPlayerStatReason(modifier.Stat));
        }

        public void RemovePlayerModifiersBySource(string source, ICollection<DomainEvent> events, string reason = null)
        {
            PlayerRunState?.RemoveModifiersBySource(source);
            ApplyPlayerRunState(events, reason ?? source);
        }

        public void SetPlayerKeyword(string keyword, int value, StatModifierScope scope)
        {
            PlayerRunState?.SetKeyword(keyword, value, scope);
        }

        public void RemovePlayerKeyword(string keyword, StatModifierScope scope)
        {
            PlayerRunState?.RemoveKeyword(keyword, scope);
        }

        public void AddPlayerTrait(ModelId traitId, StatModifierScope scope, string source, ICollection<DomainEvent> events)
        {
            PlayerRunState?.AddTrait(traitId, scope, source);
            events?.Add(new DomainEvent(DomainEventType.TraitAcquired)
            {
                Reason = traitId.ToString(),
                Amount = (int)scope
            });
        }

        public void RemovePlayerTraitsBySource(string source)
        {
            PlayerRunState?.RemoveTraitsBySource(source);
        }

        public void ApplyPlayerRunState(ICollection<DomainEvent> events, string reason)
        {
            CardInstance playerCard = Grid.PlayerCard;
            if (playerCard == null || PlayerRunState == null)
            {
                return;
            }

            int oldMaxHp = playerCard.MaxHp;
            int oldAttack = playerCard.Attack;
            int oldDefense = playerCard.Defense;
            PlayerRunState.ApplyTo(playerCard);

            AppendPlayerStatChanged(events, PlayerStat.MaxHp, oldMaxHp, playerCard.MaxHp, reason);
            AppendPlayerStatChanged(events, PlayerStat.Attack, oldAttack, playerCard.Attack, reason);
            AppendPlayerStatChanged(events, PlayerStat.Defense, oldDefense, playerCard.Defense, reason);
        }

        public void HealPlayer(int amount, ICollection<DomainEvent> events, string reason)
        {
            CardInstance playerCard = Grid.PlayerCard;
            if (playerCard == null)
            {
                return;
            }

            int before = playerCard.CurrentHp;
            playerCard.SetCurrentHp(playerCard.CurrentHp + Math.Max(0, amount));
            int recovered = playerCard.CurrentHp - before;
            if (recovered > 0)
            {
                events?.Add(new DomainEvent(DomainEventType.HealingApplied)
                {
                    CardId = playerCard.InstanceId,
                    Amount = recovered,
                    SecondaryAmount = playerCard.CurrentHp,
                    Reason = reason
                });
            }
        }

        public void RestorePlayerToFull(ICollection<DomainEvent> events, string reason)
        {
            CardInstance playerCard = Grid.PlayerCard;
            if (playerCard == null)
            {
                return;
            }

            int missing = playerCard.MaxHp - playerCard.CurrentHp;
            if (missing > 0)
            {
                HealPlayer(missing, events, reason);
            }
        }

        public ModelId AcquireRelic(RelicModel relic, ICollection<DomainEvent> events)
        {
            if (relic == null)
            {
                throw new ArgumentNullException(nameof(relic));
            }

            if (relic.Kind == RelicKind.Active)
            {
                ModelId replacedActive = default;
                if (!Relics.ActiveSlot.IsEmpty && !Relics.ActiveSlot.Contains(relic.Id))
                {
                    replacedActive = Relics.ActiveSlot.RelicId;
                    RemoveRelicBonuses(replacedActive, events);
                    Relics.ActiveSlot.Clear();
                }

                if (!Relics.ActiveSlot.Contains(relic.Id))
                {
                    Relics.ActiveSlot.Assign(relic.Id, relic.MaxUsesPerRoom);
                    ApplyRelicBonuses(relic, events);
                }

                events?.Add(new DomainEvent(DomainEventType.RelicAcquired)
                {
                    Reason = relic.Id.ToString(),
                    Amount = (int)relic.Kind
                });
                return replacedActive;
            }

            if (!Relics.Contains(relic.Id))
            {
                Relics.AddPassive(relic.Id);
                ApplyRelicBonuses(relic, events);
                events?.Add(new DomainEvent(DomainEventType.RelicAcquired)
                {
                    Reason = relic.Id.ToString(),
                    Amount = (int)relic.Kind
                });
            }

            return default;
        }

        private static PlayerRunState CreateInitialPlayerRunState(GridState grid)
        {
            CardInstance player = grid != null ? grid.PlayerCard : null;
            if (player == null)
            {
                return new PlayerRunState(0, 0, 0);
            }

            return new PlayerRunState(player.MaxHp, player.Attack, player.Defense);
        }

        public List<DomainEventBatch> Batches { get; } = new List<DomainEventBatch>();

        public CardModel ResolveCardModel(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            return ModelDb.Get<CardModel>(card.ModelId);
        }

        public bool TryResolveCardModel(CardInstance card, out CardModel model)
        {
            if (card == null)
            {
                model = null;
                return false;
            }

            return ModelDb.TryGet(card.ModelId, out model);
        }

        public bool TryResolveRelicModel(ModelId relicId, out RelicModel relic)
        {
            return ModelDb.TryGet(relicId, out relic);
        }

        public async Task ProcessLifecycleAsync(ICollection<DomainEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (events is not List<DomainEvent> eventList)
            {
                return;
            }

            await ProcessLifecycleEventsAsync(eventList, 0);
        }

        public async Task ProcessLifecycleAsync(PlayerIntent sourceIntent, ICollection<DomainEvent> events)
        {
            await NotifyAfterPlayerActionCommittedAsync(sourceIntent, events);
            await ProcessLifecycleAsync(events);
        }

        public async Task NotifyAfterPlayerActionCommittedAsync(PlayerIntent sourceIntent, ICollection<DomainEvent> events)
        {
            if (sourceIntent == null)
            {
                return;
            }

            List<CardInstanceId> observerIds = CollectActionObserverIds();
            if (events is not List<DomainEvent> eventList)
            {
                return;
            }

            await NotifyAfterPlayerActionCommittedAsync(observerIds, sourceIntent, eventList);
        }

        private List<CardInstanceId> CollectActionObserverIds()
        {
            List<CardInstanceId> observerIds = new List<CardInstanceId>();
            foreach (CardInstance card in Grid.AllGridCards)
            {
                if (card.IsFaceUp && card.CardType != CardType.Player)
                {
                    observerIds.Add(card.InstanceId);
                }
            }

            return observerIds;
        }

        private async Task NotifyAfterPlayerActionCommittedAsync(IReadOnlyList<CardInstanceId> observerIds, PlayerIntent sourceIntent, List<DomainEvent> events)
        {
            int actionIndex = ActionCounter.Value;
            for (int i = 0; i < observerIds.Count; i++)
            {
                if (!Grid.TryGetCard(observerIds[i], out CardInstance card))
                {
                    continue;
                }

                if (card.Zone != CardZone.Grid || !card.IsFaceUp || card.CardType == CardType.Player)
                {
                    continue;
                }

                if (!TryResolveCardModel(card, out CardModel model))
                {
                    continue;
                }

                PlayerActionContext context = new PlayerActionContext(this, card, sourceIntent, actionIndex, events);
                await model.OnAfterPlayerActionCommittedAsync(context);
                await NotifyCardTraitHooksAsync(card, trait => trait.OnPlayerActionCommittedAsync(context));
            }

            await NotifyPlayerTraitHooksAsync(trait => trait.OnPlayerActionCommittedAsync(new PlayerActionContext(this, Grid.PlayerCard, sourceIntent, actionIndex, events))).ConfigureAwait(false);

            await DispatchDuePendingTriggersAsync(sourceIntent, actionIndex, events).ConfigureAwait(false);
        }

        private async Task DispatchDuePendingTriggersAsync(PlayerIntent sourceIntent, int actionIndex, List<DomainEvent> events)
        {
            IReadOnlyList<PendingTrigger> due = PendingTriggers.DequeueDue(PendingTriggerTiming.AfterPlayerAction, actionIndex);
            for (int i = 0; i < due.Count; i++)
            {
                PendingTrigger trigger = due[i];
                if (!Grid.TryGetCard(trigger.CardId, out CardInstance card) || card.Zone != CardZone.Grid)
                {
                    continue;
                }

                if (!TryResolveCardModel(card, out CardModel model))
                {
                    continue;
                }

                events.Add(new DomainEvent(DomainEventType.TrapTriggered)
                {
                    CardId = card.InstanceId,
                    Reason = trigger.TriggerKey,
                    Amount = actionIndex
                });

                PendingTriggerContext context = new PendingTriggerContext(this, card, trigger, actionIndex, events);
                await model.OnPendingTriggerAsync(context).ConfigureAwait(false);
            }

            ResolveDeadCards(events);
        }

        public void ResolveDeadCards(ICollection<DomainEvent> events)
        {
            List<CardInstance> deadCards = Grid.AllGridCards
                .Where(card => card.HasHitPoints && !card.IsAlive && card.Zone == CardZone.Grid)
                .ToList();

            for (int i = 0; i < deadCards.Count; i++)
            {
                CardInstance card = deadCards[i];
                if (card.Zone != CardZone.Grid || !card.HasHitPoints || card.IsAlive)
                {
                    continue;
                }

                RemoveReason reason = card.CardType == CardType.Trap ? RemoveReason.Destroyed : RemoveReason.Defeated;
                GridOperationResult remove = Grid.RemoveCard(card, reason);
                if (!remove.Succeeded)
                {
                    continue;
                }

                foreach (DomainEvent domainEvent in remove.Events)
                {
                    events?.Add(domainEvent);
                }

                if (card.CardType == CardType.Monster)
                {
                    events?.Add(new DomainEvent(DomainEventType.MonsterDefeated)
                    {
                        CardId = card.InstanceId,
                        Reason = reason.ToString()
                    });

                    int reward = card.GoldOnRemoved + card.GetState("eliteGoldBonus", 0) + card.GetState("bossGoldBonus", 0);
                    GainGold(reward, events, "MonsterRemoved");
                }
            }
        }

        private async Task ProcessLifecycleEventsAsync(List<DomainEvent> events, int startIndex)
        {
            for (int i = startIndex; i < events.Count; i++)
            {
                DomainEvent domainEvent = events[i];
                if (domainEvent == null)
                {
                    continue;
                }

                if (domainEvent.EventType == DomainEventType.CardFlipped)
                {
                    if (Grid.TryGetCard(domainEvent.CardId, out CardInstance revealedCard) && TryResolveCardModel(revealedCard, out CardModel revealedModel))
                    {
                        CardRevealContext context = new CardRevealContext(this, revealedCard, domainEvent.Reason, events);
                        await revealedModel.OnRevealedAsync(context);
                        await NotifyCardTraitHooksAsync(revealedCard, trait => trait.OnCardFlippedAsync(context));
                        ResolveDeadCards(events);
                    }
                }
                else if (domainEvent.EventType == DomainEventType.CardRemoved)
                {
                    if (Grid.TryGetCard(domainEvent.CardId, out CardInstance removedCard) && TryResolveCardModel(removedCard, out CardModel removedModel))
                    {
                        CardDestroyedContext context = new CardDestroyedContext(this, removedCard, domainEvent.Reason, events);
                        await removedModel.OnDestroyedAsync(context);
                        await NotifyCardTraitHooksAsync(removedCard, trait => trait.OnCardRemovedAsync(context));
                        await NotifyFaceUpObserverTraitsAsync(removedCard.InstanceId, trait => trait.OnCardRemovedAsync(context)).ConfigureAwait(false);
                        await NotifyPlayerTraitHooksAsync(trait => trait.OnCardRemovedAsync(context)).ConfigureAwait(false);
                    }
                }
                else if (domainEvent.EventType == DomainEventType.MonsterDefeated)
                {
                    if (Grid.TryGetCard(domainEvent.CardId, out CardInstance defeatedMonster))
                    {
                        MonsterDefeatedContext context = new MonsterDefeatedContext(
                            this,
                            defeatedMonster,
                            defeatedMonster.GetState("elite", 0) > 0,
                            defeatedMonster.GetState("boss", 0) > 0,
                            events);

                        await NotifyRelicHooksAsync(relic => relic.OnMonsterDefeatedAsync(context)).ConfigureAwait(false);
                        await NotifyPlayerTraitHooksAsync(trait => trait.OnMonsterDefeatedAsync(context)).ConfigureAwait(false);

                        if (context.WasElite || context.WasBoss)
                        {
                            await NotifyRelicHooksAsync(relic => relic.OnEliteOrBossDefeatedAsync(context)).ConfigureAwait(false);
                        }
                    }
                }
                else if (domainEvent.EventType == DomainEventType.RoomCleared)
                {
                    await NotifyRoomClearedAsync(events).ConfigureAwait(false);
                }
            }
        }

        public void AppendPlayerDefeatedIfNeeded(ICollection<DomainEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            CardInstance defeatedPlayer = null;
            foreach (CardInstance card in Grid.AllKnownCards)
            {
                if (card.CardType == CardType.Player && card.HasHitPoints && !card.IsAlive)
                {
                    defeatedPlayer = card;
                    break;
                }
            }

            if (defeatedPlayer == null)
            {
                return;
            }

            foreach (DomainEvent domainEvent in events)
            {
                if (domainEvent != null && domainEvent.EventType == DomainEventType.RunEnded && domainEvent.CardId == defeatedPlayer.InstanceId)
                {
                    return;
                }
            }

            events.Add(new DomainEvent(DomainEventType.RunEnded)
            {
                CardId = defeatedPlayer.InstanceId,
                Reason = "PlayerDefeated"
            });
        }

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

        public bool TrySpendGold(int amount, ICollection<DomainEvent> events, string reason)
        {
            int cost = Math.Max(0, amount);
            if (cost == 0)
            {
                return true;
            }

            if (PlayerGold < cost)
            {
                return false;
            }

            PlayerGold -= cost;
            events?.Add(new DomainEvent(DomainEventType.GoldChanged)
            {
                Amount = -cost,
                SecondaryAmount = PlayerGold,
                Reason = reason
            });
            return true;
        }

        #region Combat Hooks

        public async Task NotifyBeforeDamageAsync(DamageContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            await NotifyRelicHooksAsync(r => r.OnBeforeDamageAsync(ctx)).ConfigureAwait(false);
            await NotifyCardTraitHooksAsync(ctx.SourceCard, t => t.OnBeforeDamageAsync(ctx)).ConfigureAwait(false);
            await NotifyCardTraitHooksAsync(ctx.TargetCard, t => t.OnBeforeDamageAsync(ctx)).ConfigureAwait(false);
            if (ctx.SourceCard?.CardType == CardType.Player || ctx.TargetCard?.CardType == CardType.Player)
            {
                await NotifyPlayerTraitHooksAsync(t => t.OnBeforeDamageAsync(ctx)).ConfigureAwait(false);
            }
        }

        public async Task NotifyAfterDamageAsync(DamageContext ctx, DamageResult result)
        {
            if (ctx == null)
            {
                return;
            }

            await NotifyRelicHooksAsync(r => r.OnAfterDamageAsync(ctx, result)).ConfigureAwait(false);
            await NotifyCardModelHookAsync(ctx.SourceCard, m => m.OnAfterDamageAsync(ctx, result)).ConfigureAwait(false);
            await NotifyCardModelHookAsync(ctx.TargetCard, m => m.OnAfterDamageAsync(ctx, result)).ConfigureAwait(false);
            await NotifyCardTraitHooksAsync(ctx.SourceCard, t => t.OnAfterDamageAsync(ctx, result)).ConfigureAwait(false);
            await NotifyCardTraitHooksAsync(ctx.TargetCard, t => t.OnAfterDamageAsync(ctx, result)).ConfigureAwait(false);
            if (ctx.SourceCard?.CardType == CardType.Player || ctx.TargetCard?.CardType == CardType.Player)
            {
                await NotifyPlayerTraitHooksAsync(t => t.OnAfterDamageAsync(ctx, result)).ConfigureAwait(false);
            }
        }

        public async Task<int> ModifyDamageDealtAsync(DamageContext ctx, int current)
        {
            if (ctx == null)
            {
                return current;
            }

            int value = current;

            foreach (ModelId relicId in Relics.AllRelics)
            {
                if (TryResolveRelicModel(relicId, out RelicModel relic))
                {
                    value = relic.ModifyDamageDealt(ctx, value);
                }
            }

            if (ctx.SourceCard != null)
            {
                value = ModifyDamageByCardTraits(ctx.SourceCard, t => t.ModifyDamageDealt(ctx, value), value);
                if (ctx.SourceCard.CardType == CardType.Player)
                {
                    value = ModifyDamageByPlayerTraits(t => t.ModifyDamageDealt(ctx, value), value);
                }
            }

            foreach (CardInstance card in Grid.AllGridCards)
            {
                if (card.CardType == CardType.Player || !card.IsFaceUp)
                {
                    continue;
                }

                if (card.InstanceId == ctx.SourceCard?.InstanceId || card.InstanceId == ctx.TargetCard?.InstanceId)
                {
                    continue;
                }

                value = ModifyDamageByCardTraits(card, t => t.ModifyDamageDealt(ctx, value), value);
            }

            return value;
        }

        public async Task<int> ModifyDamageTakenAsync(DamageContext ctx, int current)
        {
            if (ctx == null)
            {
                return current;
            }

            int value = current;

            foreach (ModelId relicId in Relics.AllRelics)
            {
                if (TryResolveRelicModel(relicId, out RelicModel relic))
                {
                    value = relic.ModifyDamageTaken(ctx, value);
                }
            }

            if (ctx.TargetCard != null)
            {
                value = ModifyDamageByCardTraits(ctx.TargetCard, t => t.ModifyDamageTaken(ctx, value), value);
                if (ctx.TargetCard.CardType == CardType.Player)
                {
                    value = ModifyDamageByPlayerTraits(t => t.ModifyDamageTaken(ctx, value), value);
                }
            }

            // Notify field observers (face-up monsters/traps) — ordered by cell index, top-first
            foreach (CardInstance card in Grid.AllGridCards)
            {
                if (card.CardType == CardType.Player || !card.IsFaceUp)
                {
                    continue;
                }

                if (card.InstanceId == ctx.SourceCard?.InstanceId || card.InstanceId == ctx.TargetCard?.InstanceId)
                {
                    continue;
                }

                value = ModifyDamageByCardTraits(card, t => t.ModifyDamageTaken(ctx, value), value);
            }

            return value;
        }

        private async Task NotifyRelicHooksAsync(Func<RelicModel, Task> notify)
        {
            foreach (ModelId relicId in Relics.AllRelics)
            {
                if (TryResolveRelicModel(relicId, out RelicModel relic))
                {
                    await notify(relic).ConfigureAwait(false);
                }
            }
        }

        public async Task NotifyRoomEnteredAsync(ICollection<DomainEvent> events)
        {
            RoomLifecycleContext context = CreateRoomLifecycleContext(events);
            await NotifyRelicHooksAsync(relic => relic.OnRoomEnteredAsync(context)).ConfigureAwait(false);
            await NotifyPlayerTraitHooksAsync(trait => trait.OnRoomEnteredAsync(context)).ConfigureAwait(false);
        }

        private async Task NotifyRoomClearedAsync(ICollection<DomainEvent> events)
        {
            RoomLifecycleContext context = CreateRoomLifecycleContext(events);
            await NotifyRelicHooksAsync(relic => relic.OnRoomClearedAsync(context)).ConfigureAwait(false);
            await NotifyPlayerTraitHooksAsync(trait => trait.OnRoomClearedAsync(context)).ConfigureAwait(false);
        }

        private RoomLifecycleContext CreateRoomLifecycleContext(ICollection<DomainEvent> events)
        {
            RoomType roomType = Progression != null ? Progression.CurrentRoomType : RoomType.Combat;
            int layerIndex = Progression != null ? Progression.LayerIndex : 0;
            int nodeIndex = Progression != null ? Progression.NodeIndex : 0;
            return new RoomLifecycleContext(this, roomType, layerIndex, nodeIndex, events ?? new List<DomainEvent>());
        }

        private async Task NotifyPlayerTraitHooksAsync(Func<TraitModel, Task> notify)
        {
            if (PlayerRunState == null)
            {
                return;
            }

            foreach (PlayerTraitState traitState in PlayerRunState.Traits)
            {
                if (ModelDb.TryGet(traitState.TraitId, out TraitModel trait))
                {
                    await notify(trait).ConfigureAwait(false);
                }
            }
        }

        private async Task NotifyFaceUpObserverTraitsAsync(CardInstanceId excludedCardId, Func<TraitModel, Task> notify)
        {
            foreach (CardInstance card in Grid.AllGridCards)
            {
                if (!card.IsFaceUp || card.CardType == CardType.Player || card.InstanceId == excludedCardId)
                {
                    continue;
                }

                await NotifyCardTraitHooksAsync(card, notify).ConfigureAwait(false);
            }
        }

        private async Task NotifyCardTraitHooksAsync(CardInstance card, Func<TraitModel, Task> notify)
        {
            if (card == null)
            {
                return;
            }

            if (!TryResolveCardModel(card, out CardModel model))
            {
                return;
            }

            IReadOnlyList<ModelId> traitIds = GetTraitIdsFromModel(model);
            foreach (ModelId traitId in traitIds)
            {
                if (ModelDb.TryGet(traitId, out TraitModel trait))
                {
                    await notify(trait).ConfigureAwait(false);
                }
            }
        }

        private async Task NotifyCardModelHookAsync(CardInstance card, Func<CardModel, Task> notify)
        {
            if (card == null || !TryResolveCardModel(card, out CardModel model))
            {
                return;
            }

            await notify(model).ConfigureAwait(false);
        }

        private int ModifyDamageByCardTraits(CardInstance card, Func<TraitModel, int> modify, int current)
        {
            if (card == null)
            {
                return current;
            }

            if (!TryResolveCardModel(card, out CardModel model))
            {
                return current;
            }

            int value = current;
            IReadOnlyList<ModelId> traitIds = GetTraitIdsFromModel(model);
            foreach (ModelId traitId in traitIds)
            {
                if (ModelDb.TryGet(traitId, out TraitModel trait))
                {
                    value = modify(trait);
                }
            }

            return value;
        }

        private int ModifyDamageByPlayerTraits(Func<TraitModel, int> modify, int current)
        {
            if (PlayerRunState == null)
            {
                return current;
            }

            int value = current;
            foreach (PlayerTraitState traitState in PlayerRunState.Traits)
            {
                if (ModelDb.TryGet(traitState.TraitId, out TraitModel trait))
                {
                    value = modify(trait);
                }
            }

            return value;
        }

        private void ApplyRelicBonuses(RelicModel relic, ICollection<DomainEvent> events)
        {
            string source = GetRelicBonusSource(relic.Id);
            PlayerRunState?.RemoveModifiersBySource(source);

            if (relic.AttackBonus != 0)
            {
                PlayerRunState?.AddModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Permanent, relic.AttackBonus, source));
            }

            if (relic.DefenseBonus != 0)
            {
                PlayerRunState?.AddModifier(new StatModifier(PlayerStat.Defense, StatModifierScope.Permanent, relic.DefenseBonus, source));
            }

            if (relic.MaxHpBonus != 0)
            {
                PlayerRunState?.AddModifier(new StatModifier(PlayerStat.MaxHp, StatModifierScope.Permanent, relic.MaxHpBonus, source));
            }

            ApplyPlayerRunState(events, source);
            if (relic.MaxHpBonus > 0)
            {
                HealPlayer(relic.MaxHpBonus, events, source + ":heal");
            }
        }

        private void RemoveRelicBonuses(ModelId relicId, ICollection<DomainEvent> events)
        {
            PlayerRunState?.RemoveModifiersBySource(GetRelicBonusSource(relicId));
            ApplyPlayerRunState(events, GetRelicBonusSource(relicId));
        }

        private void AppendPlayerStatChanged(ICollection<DomainEvent> events, PlayerStat stat, int previousValue, int currentValue, string reason)
        {
            if (events == null || previousValue == currentValue)
            {
                return;
            }

            events.Add(new DomainEvent(DomainEventType.StatChanged)
            {
                CardId = Grid.PlayerCard != null ? Grid.PlayerCard.InstanceId : default,
                Amount = currentValue - previousValue,
                SecondaryAmount = currentValue,
                Reason = string.IsNullOrWhiteSpace(reason) ? GetPlayerStatReason(stat) : reason
            });
        }

        private static string GetPlayerStatReason(PlayerStat stat)
        {
            return stat switch
            {
                PlayerStat.MaxHp => "player:max-hp",
                PlayerStat.Attack => "player:attack",
                PlayerStat.Defense => "player:defense",
                _ => "player:stat"
            };
        }

        private static string GetRelicBonusSource(ModelId relicId)
        {
            return "relic:" + relicId;
        }

        private static IReadOnlyList<ModelId> GetTraitIdsFromModel(CardModel model)
        {
            if (model is MonsterCardModel monster)
            {
                return monster.TraitIds;
            }

            return Array.Empty<ModelId>();
        }

        #endregion
    }
}
