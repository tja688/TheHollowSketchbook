using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Rooms;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;

namespace Game.Core.Domain
{
    /// <summary>
    /// Primary entry point for the new Domain-based run flow.
    /// This replaces the old RunManager/StandardActMapGenerator/RoomFactory prototype pipeline.
    ///
    /// Old flow (prototype, still exists at Runs/RunManager.cs):
    ///   RunManager -> StandardActMapGenerator -> RoomFactory -> AbstractRoom
    ///   Uses MapPoint, ActMap, RunState. NOT connected to DomainActionContext.
    ///
    /// New flow (Domain, this class):
    ///   DomainRunFlow -> DungeonMapGenerator -> RoomPlan
    ///   DomainRunFlow -> DungeonDeckBuilder -> GridDealer -> GridState
    ///   DomainRunFlow -> RoomTransitionService (route cards, room transitions)
    ///   All state flows through DomainActionContext and DomainFacade.
    ///
    /// L1 should use DomainRunFlow exclusively. RunManager is retained only
    /// as a legacy prototype reference and should NOT be connected to new content.
    /// </summary>
    public sealed class DomainRunFlow
    {
        private readonly DungeonMapGenerator _mapGenerator;
        private readonly DungeonDeckBuilder _deckBuilder;
        private readonly GridDealer _gridDealer;
        private readonly RoomTransitionService _transitionService;

        public DomainRunFlow()
            : this(new DungeonMapGenerator(), new DungeonDeckBuilder(), new GridDealer())
        {
        }

        public DomainRunFlow(
            DungeonMapGenerator mapGenerator,
            DungeonDeckBuilder deckBuilder,
            GridDealer gridDealer)
        {
            _mapGenerator = mapGenerator ?? throw new ArgumentNullException(nameof(mapGenerator));
            _deckBuilder = deckBuilder ?? throw new ArgumentNullException(nameof(deckBuilder));
            _gridDealer = gridDealer ?? throw new ArgumentNullException(nameof(gridDealer));
            _transitionService = new RoomTransitionService(mapGenerator, deckBuilder, gridDealer);
        }

        public RoomTransitionService TransitionService
        {
            get { return _transitionService; }
        }

        /// <summary>
        /// Start a new run: create the initial DomainActionContext with the first room
        /// (Node 1, Reward room) set up and ready for player interaction.
        /// </summary>
        public DomainActionContext StartNewRun(int seed, ModelId playerModelId, int playerMaxHp, int playerAttack, int playerDefense)
        {
            DeterministicRng rng = new DeterministicRng(seed);

            // Node 1 is always Reward room
            IReadOnlyList<RoomPlan> layerPlans = _mapGenerator.GenerateLayerPlans(1);
            RoomPlan firstPlan = layerPlans[0]; // Node 1 = Reward

            // Build initial dungeon deck
            DungeonDeck deck = _deckBuilder.Build(firstPlan, null, rng);

            // Create grid and deal cards
            GridState grid = new GridState();
            GridCoord playerStart = GridCoord.FromCellIndex(8);

            CardInstance playerCard = new CardInstance(
                new CardInstanceId(1),
                playerModelId,
                CardType.Player);
            playerCard.ConfigureCombatStats(playerMaxHp, playerAttack, playerDefense, 0, 0);
            grid.AddCardToGrid(playerCard, playerStart, true);

            DealPolicy dealPolicy = firstPlan.RoomType == RoomType.Restaurant
                ? DealPolicy.RestaurantDefault()
                : DealPolicy.CombatDefault();
            _gridDealer.Deal(grid, deck, dealPolicy, rng);

            // Create domain context
            DomainActionContext context = new DomainActionContext(grid, new PlayerActionCounter());
            context.DungeonDeck = deck;
            context.Rng = rng;
            context.RoomTransition = _transitionService;
            context.Progression = new RunProgressionState(
                1, 1, firstPlan.RoomType, Array.Empty<RoomType>());

            return context;
        }

        /// <summary>
        /// Generate layer plans for inspection (e.g., map display).
        /// </summary>
        public IReadOnlyList<RoomPlan> GenerateLayerPlans(int layerIndex)
        {
            return _mapGenerator.GenerateLayerPlans(layerIndex);
        }
    }
}
