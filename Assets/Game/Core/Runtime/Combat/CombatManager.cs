using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Cards;
using Game.Core.Combat.Commands;
using Game.Core.Entities;
using Game.Core.Logging;
using Game.Core.Random;

namespace Game.Core.Combat
{
    public sealed class CardPlayRequest
    {
        public Player Player { get; init; }
        public CardModel Card { get; init; }
        public PlayTarget Target { get; init; }
        public bool IsAutoPlay { get; init; }
    }

    public sealed class ImmediatePlayCardAction : GameAction
    {
        private readonly CombatState _combat;
        private readonly CardPlayRequest _request;

        public ImmediatePlayCardAction(CombatState combat, CardPlayRequest request)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public CardPlayRequest Request
        {
            get { return _request; }
        }

        protected override async Task ExecuteActionAsync(GameActionExecutionContext ctx)
        {
            if (!_combat.IsInProgress || _combat.IsCombatEnded)
            {
                throw new GameException("Combat is not active.");
            }

            if (!_combat.IsPlayPhase || _combat.CurrentSide != CombatSide.Player)
            {
                throw new GameException("Cards can only be played during the player play phase.");
            }

            Player player = _request.Player ?? throw new GameException("Card play request has no player.");
            CardModel card = _request.Card ?? throw new GameException("Card play request has no card.");

            if (card.Owner != player)
            {
                throw new GameException("Card owner does not match the requesting player.");
            }

            if (player.PlayerCombatState == null)
            {
                throw new GameException("Player has no combat state.");
            }

            if (!player.PlayerCombatState.Hand.Contains(card))
            {
                throw new GameException("Card is not in hand.");
            }

            if (!TryValidateTarget(card, _request.Target, _combat, out string targetReason))
            {
                throw new GameException(targetReason);
            }

            if (!card.CanPlay(out string reason))
            {
                throw new GameException(reason);
            }

            int energySpent = card.EnergyCost.GetSpendAmount(player.PlayerCombatState.Energy);
            await PlayerCmd.SpendEnergy(player, energySpent);
            player.PlayerCombatState.PlayPile.Add(card);

            PileType resultPile = card.HasKeyword(CardKeyword.Exhaust) ? PileType.Exhaust : PileType.Discard;
            CardPlay play = new CardPlay
            {
                Card = card,
                Target = _request.Target,
                ResultPile = resultPile,
                Resources = new ResourceInfo(energySpent),
                IsAutoPlay = _request.IsAutoPlay,
                PlayIndex = 0,
                PlayCount = 1
            };

            CardPlayContext playContext = new CardPlayContext(_combat, _combat.RunState.Rng);
            await card.OnPlayWrapper(playContext, play);
            MoveCardToResultPile(player, card, resultPile);
        }

        internal static bool TryValidateTarget(CardModel card, PlayTarget target, CombatState combat, out string reason)
        {
            switch (card.Targeting)
            {
                case CardTargeting.None:
                    reason = null;
                    return !target.HasCreature;
                case CardTargeting.Self:
                    if (target.Creature == card.Owner?.Creature)
                    {
                        reason = null;
                        return true;
                    }

                    reason = "Card must target the player.";
                    return false;
                case CardTargeting.SingleEnemy:
                    if (target.HasCreature && target.Creature.Side == CombatSide.Enemy && target.Creature.IsAlive)
                    {
                        reason = null;
                        return true;
                    }

                    reason = "Card must target a living enemy.";
                    return false;
                case CardTargeting.AllEnemies:
                    reason = null;
                    return true;
                default:
                    reason = "Unsupported target type.";
                    return false;
            }
        }

        internal static void MoveCardToResultPile(Player player, CardModel card, PileType resultPile)
        {
            CardPile targetPile = resultPile switch
            {
                PileType.Exhaust => player.PlayerCombatState.ExhaustPile,
                PileType.Discard => player.PlayerCombatState.DiscardPile,
                PileType.Play => player.PlayerCombatState.PlayPile,
                _ => null
            };

            if (targetPile == null)
            {
                throw new GameException("Unsupported result pile: " + resultPile);
            }

            if (card.CurrentPile != targetPile)
            {
                targetPile.Add(card);
            }
        }
    }

    public sealed class CombatManager
    {
        private readonly ActionQueueSet _actions = new ActionQueueSet();
        private readonly ActionExecutor _executor;
        private bool _isProcessingActions;

        public CombatManager()
        {
            _executor = new ActionExecutor(_actions);
        }

        public CombatState State { get; private set; }
        public bool IsInProgress => State != null && State.IsInProgress && !State.IsCombatEnded;

        public event Action<CombatState> CombatSetUp;
        public event Action<CombatState> TurnStarted;
        public event Action<CombatState> TurnEnded;
        public event Action<CombatState> CreaturesChanged;
        public event Action<CombatState> CombatWon;
        public event Action<CombatState> CombatEnded;
        public event Action<bool> PlayerActionsDisabledChanged;
        public event Action<Creature, EnemyIntent> EnemyIntentRolled;

        public void SetUpCombat(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            State = state;
            State.RoundNumber = 1;
            State.CurrentSide = CombatSide.Player;
            State.IsCombatEnded = false;
            State.PlayerWon = false;
            State.IsPlayPhase = false;
            State.IsInProgress = true;
            State.ClearEnemyIntents();
            _actions.Clear();

            SubscribeCreatures(State);

            for (int i = 0; i < State.Players.Count; i++)
            {
                Player player = State.Players[i];
                player.ResetCombatState();
                player.PopulateCombatDrawPileFromDeck();
            }

            for (int i = 0; i < State.Enemies.Count; i++)
            {
                Creature enemy = State.Enemies[i];
                enemy.SetBlock(0);
            }

            CombatSetUp?.Invoke(State);
            CreaturesChanged?.Invoke(State);
        }

        public async Task StartCombatAsync()
        {
            EnsureState();
            await Hook.BeforeCombatStart(State);
            await StartPlayerTurnAsync();
        }

        public async Task StartPlayerTurnAsync()
        {
            EnsureState();
            await Hook.BeforeTurnStart(State);
            State.CurrentSide = CombatSide.Player;
            State.IsPlayPhase = true;

            for (int i = 0; i < State.Players.Count; i++)
            {
                Player player = State.Players[i];
                player.Creature.SetBlock(0);
                player.PlayerCombatState.ResetEnergy(player.MaxEnergy);
                CardPileCmd.Draw(player, 5, State.RunState.Rng);
            }

            RollEnemyIntents();
            PlayerActionsDisabledChanged?.Invoke(false);
            TurnStarted?.Invoke(State);
            CreaturesChanged?.Invoke(State);
            await Hook.AfterTurnStart(State);
        }

        public void RequestEndTurn(Player player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            EnsureState();
            if (!State.IsPlayPhase)
            {
                return;
            }

            State.IsPlayPhase = false;
            PlayerActionsDisabledChanged?.Invoke(true);
            _actions.Clear();
            _ = EndPlayerTurnAsync();
        }

        public async Task EndPlayerTurnAsync()
        {
            EnsureState();
            if (State.CurrentSide != CombatSide.Player)
            {
                return;
            }

            await Hook.BeforeTurnEnd(State);
            DiscardHands();
            TurnEnded?.Invoke(State);
            CreaturesChanged?.Invoke(State);
            await Hook.AfterTurnEnd(State);
            await ExecuteEnemyTurnAsync();
        }

        public async Task ExecuteEnemyTurnAsync()
        {
            EnsureState();
            await Hook.BeforeTurnStart(State);
            State.CurrentSide = CombatSide.Enemy;
            State.IsPlayPhase = false;
            TurnStarted?.Invoke(State);

            CardPlayContext ctx = new CardPlayContext(State, State.RunState.Rng);
            for (int i = 0; i < State.Enemies.Count; i++)
            {
                Creature enemy = State.Enemies[i];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                if (!State.TryGetEnemyIntent(enemy, out EnemyIntent intent))
                {
                    intent = enemy.EnemyModel.BuildIntent(State, enemy, State.RunState.Rng);
                    State.SetEnemyIntent(enemy, intent);
                }

                await enemy.EnemyModel.ExecuteIntent(ctx, enemy, intent);
                CreaturesChanged?.Invoke(State);
                if (await CheckWinConditionAsync())
                {
                    return;
                }
            }

            await EndEnemyTurnAsync();
        }

        public async Task EndEnemyTurnAsync()
        {
            EnsureState();
            for (int i = 0; i < State.Enemies.Count; i++)
            {
                Creature enemy = State.Enemies[i];
                if (enemy.IsAlive)
                {
                    enemy.SetBlock(0);
                }
            }

            TurnEnded?.Invoke(State);
            await Hook.AfterTurnEnd(State);
            State.RoundNumber++;
            await CheckWinConditionAsync();
            if (!State.IsCombatEnded)
            {
                await StartPlayerTurnAsync();
            }
        }

        public async Task SubmitCardPlayRequestAsync(CardPlayRequest request)
        {
            EnsureState();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _actions.Enqueue(new ImmediatePlayCardAction(State, request));
            await ProcessActionsAsync();
            await CheckWinConditionAsync();
            CreaturesChanged?.Invoke(State);
        }

        public async Task<bool> CheckWinConditionAsync()
        {
            EnsureState();
            if (State.IsCombatEnded)
            {
                return true;
            }

            if (State.ArePlayersDefeated)
            {
                State.IsCombatEnded = true;
                State.IsInProgress = false;
                State.PlayerWon = false;
                State.IsPlayPhase = false;
                PlayerActionsDisabledChanged?.Invoke(true);
                CombatEnded?.Invoke(State);
                await Hook.AfterCombatEnd(State);
                return true;
            }

            if (State.AreEnemiesDefeated)
            {
                State.IsCombatEnded = true;
                State.IsInProgress = false;
                State.PlayerWon = true;
                State.IsPlayPhase = false;
                PlayerActionsDisabledChanged?.Invoke(true);
                CombatWon?.Invoke(State);
                CombatEnded?.Invoke(State);
                await Hook.AfterCombatEnd(State);
                return true;
            }

            return false;
        }

        public void Reset()
        {
            if (State != null)
            {
                UnsubscribeCreatures(State);
            }

            _actions.Clear();
            State = null;
            _isProcessingActions = false;
            PlayerActionsDisabledChanged?.Invoke(true);
        }

        private async Task ProcessActionsAsync()
        {
            if (_isProcessingActions)
            {
                return;
            }

            _isProcessingActions = true;
            try
            {
                await _executor.ExecuteAllAsync();
            }
            finally
            {
                _isProcessingActions = false;
            }
        }

        private void RollEnemyIntents()
        {
            State.ClearEnemyIntents();
            for (int i = 0; i < State.Enemies.Count; i++)
            {
                Creature enemy = State.Enemies[i];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                EnemyIntent intent = enemy.EnemyModel.BuildIntent(State, enemy, State.RunState.Rng);
                State.SetEnemyIntent(enemy, intent);
                EnemyIntentRolled?.Invoke(enemy, intent);
            }
        }

        private void DiscardHands()
        {
            for (int i = 0; i < State.Players.Count; i++)
            {
                Player player = State.Players[i];
                List<CardModel> cards = player.PlayerCombatState.Hand.Cards.ToList();
                for (int cardIndex = 0; cardIndex < cards.Count; cardIndex++)
                {
                    player.PlayerCombatState.DiscardPile.Add(cards[cardIndex]);
                }

                List<CardModel> playedCards = player.PlayerCombatState.PlayPile.Cards.ToList();
                for (int cardIndex = 0; cardIndex < playedCards.Count; cardIndex++)
                {
                    if (playedCards[cardIndex].CurrentPile == player.PlayerCombatState.PlayPile)
                    {
                        player.PlayerCombatState.DiscardPile.Add(playedCards[cardIndex]);
                    }
                }
            }
        }

        private void SubscribeCreatures(CombatState combat)
        {
            for (int i = 0; i < combat.Creatures.Count(); i++)
            {
                Creature creature = combat.Creatures.ElementAt(i);
                creature.HpChanged += OnCreatureStateChanged;
                creature.BlockChanged += OnCreatureStateChanged;
                creature.PowerApplied += OnCreaturePowerChanged;
                creature.PowerRemoved += OnCreaturePowerChanged;
                creature.Died += OnCreatureDied;
            }
        }

        private void UnsubscribeCreatures(CombatState combat)
        {
            for (int i = 0; i < combat.Creatures.Count(); i++)
            {
                Creature creature = combat.Creatures.ElementAt(i);
                creature.HpChanged -= OnCreatureStateChanged;
                creature.BlockChanged -= OnCreatureStateChanged;
                creature.PowerApplied -= OnCreaturePowerChanged;
                creature.PowerRemoved -= OnCreaturePowerChanged;
                creature.Died -= OnCreatureDied;
            }
        }

        private void OnCreatureStateChanged(int _, int __)
        {
            if (State != null)
            {
                CreaturesChanged?.Invoke(State);
            }
        }

        private void OnCreaturePowerChanged(Game.Core.Powers.PowerModel _)
        {
            if (State != null)
            {
                CreaturesChanged?.Invoke(State);
            }
        }

        private void OnCreatureDied(Creature _)
        {
            if (State != null)
            {
                CreaturesChanged?.Invoke(State);
            }
        }

        private void EnsureState()
        {
            if (State == null)
            {
                throw new GameException("Combat has not been set up.");
            }
        }
    }
}
