using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Deck;
using Game.Core.Random;

namespace Game.Core.Domain.Grid
{
    public sealed class GridDealer
    {
        public void Deal(GridState grid, DungeonDeck deck, DealPolicy policy, IRng rng)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            List<GridCoord> targets = GridQueries.AllCoordsRowMajor()
                .Where(coord => !policy.ExcludedCoords.Contains(coord))
                .ToList();
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("Deal policy excludes every grid cell.");
            }

            rng.Shuffle(targets);

            if (policy.MinimumCoverage == MinimumCoveragePolicy.AllNonPlayerCells)
            {
                if (deck.Count < targets.Count)
                {
                    throw new InvalidOperationException("Deck does not contain enough cards to cover all non-player cells.");
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    DealOne(grid, deck, targets[i], policy.FaceDownByDefault);
                }
            }
            else if (policy.MinimumCoverage == MinimumCoveragePolicy.SpecificCells)
            {
                IReadOnlyList<GridCoord> required = policy.MinimumCoverageCoords;
                if (deck.Count < required.Count)
                {
                    throw new InvalidOperationException("Deck does not contain enough cards to cover required cells.");
                }

                for (int i = 0; i < required.Count; i++)
                {
                    DealOne(grid, deck, required[i], policy.FaceDownByDefault);
                }
            }

            while (deck.Count > 0)
            {
                DealOne(grid, deck, rng.Pick(targets), policy.FaceDownByDefault);
            }
        }

        private static void DealOne(GridState grid, DungeonDeck deck, GridCoord coord, bool faceDownByDefault)
        {
            CardInstance card = deck.DrawTop();
            grid.AddCardToGrid(card, coord, !faceDownByDefault);
        }
    }

    public sealed class DealPolicy
    {
        public DealPolicy(GridCoord playerStartCoord, MinimumCoveragePolicy minimumCoverage, bool faceDownByDefault, IReadOnlyList<GridCoord> excludedCoords, IReadOnlyList<GridCoord> minimumCoverageCoords = null)
        {
            PlayerStartCoord = playerStartCoord;
            MinimumCoverage = minimumCoverage;
            FaceDownByDefault = faceDownByDefault;
            ExcludedCoords = excludedCoords ?? Array.Empty<GridCoord>();
            MinimumCoverageCoords = minimumCoverageCoords ?? Array.Empty<GridCoord>();
        }

        public GridCoord PlayerStartCoord { get; }
        public MinimumCoveragePolicy MinimumCoverage { get; }
        public bool FaceDownByDefault { get; }
        public IReadOnlyList<GridCoord> ExcludedCoords { get; }
        public IReadOnlyList<GridCoord> MinimumCoverageCoords { get; }

        public static DealPolicy CombatDefault()
        {
            GridCoord playerStart = GridCoord.FromCellIndex(8);
            return new DealPolicy(playerStart, MinimumCoveragePolicy.AllNonPlayerCells, true, new[] { playerStart });
        }

        public static DealPolicy RestaurantDefault()
        {
            GridCoord playerStart = GridCoord.FromCellIndex(8);
            return new DealPolicy(playerStart, MinimumCoveragePolicy.None, true, new[] { playerStart });
        }
    }

    public enum MinimumCoveragePolicy
    {
        None,
        AllNonPlayerCells,
        SpecificCells
    }
}
