using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.ContentContracts;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;

namespace Game.Core.Domain.Rooms
{
    /// <summary>
    /// Orchestrates room transitions: route card generation after room clear,
    /// route card placement with squeeze strategy, and new room setup.
    ///
    /// Flow:
    ///   1. Room cleared -> GenerateAndPlaceRouteCards()
    ///   2. Player selects route -> EnterRoom()
    ///   3. New room initialized with fresh grid, deck, and player card
    /// </summary>
    public sealed class RoomTransitionService
    {
        private readonly DungeonMapGenerator _mapGenerator;
        private readonly DungeonDeckBuilder _deckBuilder;
        private readonly GridDealer _gridDealer;

        public RoomTransitionService(
            DungeonMapGenerator mapGenerator,
            DungeonDeckBuilder deckBuilder,
            GridDealer gridDealer)
        {
            _mapGenerator = mapGenerator ?? throw new ArgumentNullException(nameof(mapGenerator));
            _deckBuilder = deckBuilder ?? throw new ArgumentNullException(nameof(deckBuilder));
            _gridDealer = gridDealer ?? throw new ArgumentNullException(nameof(gridDealer));
        }

        /// <summary>
        /// After a room is cleared, generate route choice cards and place them
        /// on the grid. If there are not enough empty cells, use squeeze strategy
        /// (stack route cards on top of existing cards).
        /// </summary>
        public List<DomainEvent> GenerateAndPlaceRouteCards(
            DomainActionContext domain,
            IRng rng)
        {
            List<DomainEvent> events = new List<DomainEvent>();
            RunProgressionState progression = domain.Progression;
            if (progression == null)
            {
                return events;
            }

            int currentNode = progression.NodeIndex;
            RoomType? forced = _mapGenerator.GetForcedNextRoomAfterNode(currentNode);
            IReadOnlyList<RoomType> routeRoomTypes;

            if (forced.HasValue)
            {
                // Node 7 -> forced Restaurant
                routeRoomTypes = new[] { forced.Value };
            }
            else if (progression.CurrentRoomType == RoomType.BossCombat)
            {
                // After boss -> single entrance to next layer
                routeRoomTypes = new[] { RoomType.Combat };
            }
            else
            {
                IReadOnlyList<RoomType> pool = _mapGenerator.GetChoicePoolAfterNode(currentNode);
                int count = Math.Min(pool.Count, rng.NextInt(2, Math.Min(4, pool.Count + 1)));
                List<RoomType> shuffled = new List<RoomType>(pool);
                rng.Shuffle(shuffled);
                routeRoomTypes = shuffled.Take(count).ToList();
            }

            if (routeRoomTypes.Count == 0)
            {
                return events;
            }

            // Place route choice cards on the grid
            List<GridCoord> emptyCells = GetEmptyNonPlayerCells(domain.Grid);
            int routeIndex = 0;

            foreach (RoomType roomType in routeRoomTypes)
            {
                ModelId routeModelId = new ModelId("route", roomType.ToString().ToLowerInvariant());
                CardModel routeModel = ModelDb.Get<CardModel>(routeModelId);
                CardInstance routeCard = routeModel.CreateInstance(new CardInstanceId((uint)(90000 + routeIndex)));
                routeIndex++;

                if (emptyCells.Count > 0)
                {
                    GridCoord targetCell = emptyCells[0];
                    emptyCells.RemoveAt(0);
                    GridOperationResult result = domain.Grid.AddCardToGrid(routeCard, targetCell, true);
                    events.AddRange(result.Events);
                }
                else
                {
                    // Squeeze strategy: stack on top of an existing non-player cell
                    GridCoord squeezeTarget = PickSqueezeTarget(domain.Grid, rng);
                    GridOperationResult result = domain.Grid.CoverCellWithCard(routeCard, squeezeTarget, true);
                    events.AddRange(result.Events);
                }
            }

            // Update progression with pending choices
            domain.Progression = new RunProgressionState(
                progression.LayerIndex,
                progression.NodeIndex,
                progression.CurrentRoomType,
                routeRoomTypes);

            events.Add(new DomainEvent(DomainEventType.RouteChoicesGenerated)
            {
                Amount = routeRoomTypes.Count
            });

            return events;
        }

        /// <summary>
        /// Enter the next room: set up a fresh grid with dungeon deck and player card.
        /// Called when the player interacts with a route choice card.
        /// </summary>
        public List<DomainEvent> EnterRoom(
            CardInteractionContext ctx,
            RoomType nextRoomType)
        {
            List<DomainEvent> events = new List<DomainEvent>();
            DomainActionContext domain = ctx.Domain;
            RunProgressionState current = domain.Progression;
            if (current == null)
            {
                return events;
            }

            CardInstance playerCard = domain.Grid != null ? domain.Grid.PlayerCard : null;
            int playerHp = playerCard != null ? playerCard.CurrentHp : 20;
            int playerMaxHp = playerCard != null ? playerCard.MaxHp : 20;
            int playerAttack = playerCard != null ? playerCard.Attack : 3;
            int playerDefense = playerCard != null ? playerCard.Defense : 1;

            // Determine next node and layer
            int nextNodeIndex = current.NodeIndex + 1;
            int nextLayerIndex = current.LayerIndex;
            if (nextNodeIndex > 9)
            {
                nextLayerIndex++;
                nextNodeIndex = 1;
            }

            // Check for game completion (defeated floor 3 boss)
            if (nextLayerIndex > 3)
            {
                events.Add(new DomainEvent(DomainEventType.RunEnded)
                {
                    Reason = "Victory"
                });
                return events;
            }

            // Create room plan for the next room
            IReadOnlyList<RoomPlan> layerPlans = _mapGenerator.GenerateLayerPlans(nextLayerIndex);
            int planIndex = Math.Max(0, Math.Min(nextNodeIndex - 1, layerPlans.Count - 1));
            RoomPlan basePlan = layerPlans[planIndex];
            RoomPlan roomPlan = new RoomPlan(
                nextRoomType,
                nextLayerIndex,
                nextNodeIndex,
                nextRoomType == RoomType.EliteCombat,
                nextRoomType == RoomType.BossCombat,
                basePlan.GenerationRngState);

            // Build new dungeon deck
            IRng rng = domain.Rng ?? new DeterministicRng(1);
            DungeonDeck newDeck = _deckBuilder.Build(roomPlan, null, rng);

            // Create new grid with player card
            GridState newGrid = new GridState();
            GridCoord playerStart = GridCoord.FromCellIndex(8);

            CardInstance newPlayerCard = new CardInstance(
                new CardInstanceId(1),
                new ModelId("player", "hero"),
                CardType.Player);
            newPlayerCard.ConfigureCombatStats(playerMaxHp, playerAttack, playerDefense, 0, 0);
            newPlayerCard.SetCurrentHp(playerHp);
            newGrid.AddCardToGrid(newPlayerCard, playerStart, true);

            // Deal dungeon deck onto new grid
            DealPolicy dealPolicy = nextRoomType == RoomType.Restaurant
                ? DealPolicy.RestaurantDefault()
                : DealPolicy.CombatDefault();

            if (newDeck.Count > 0)
            {
                _gridDealer.Deal(newGrid, newDeck, dealPolicy, rng);
            }

            // Replace domain state
            domain.ReplaceGrid(newGrid);
            domain.DungeonDeck = newDeck;
            domain.ActionCounter.RestoreValue(0);

            domain.Progression = new RunProgressionState(
                nextLayerIndex,
                nextNodeIndex,
                nextRoomType,
                Array.Empty<RoomType>());

            events.Add(new DomainEvent(DomainEventType.RoomEntered)
            {
                Reason = nextRoomType.ToString()
            });

            return events;
        }

        private static List<GridCoord> GetEmptyNonPlayerCells(GridState grid)
        {
            List<GridCoord> result = new List<GridCoord>();
            IReadOnlyList<GridCoord> allCoords = GridQueries.AllCoordsRowMajor();
            for (int i = 0; i < allCoords.Count; i++)
            {
                if (allCoords[i].CellIndex != 8 && grid.IsEmpty(allCoords[i]))
                {
                    result.Add(allCoords[i]);
                }
            }

            return result;
        }

        private static GridCoord PickSqueezeTarget(GridState grid, IRng rng)
        {
            List<GridCoord> candidates = new List<GridCoord>();
            IReadOnlyList<GridCoord> allCoords = GridQueries.AllCoordsRowMajor();
            for (int i = 0; i < allCoords.Count; i++)
            {
                if (allCoords[i].CellIndex != 8)
                {
                    candidates.Add(allCoords[i]);
                }
            }

            return candidates[rng.NextInt(0, candidates.Count)];
        }
    }
}
