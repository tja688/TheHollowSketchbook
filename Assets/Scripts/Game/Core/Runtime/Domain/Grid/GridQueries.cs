using System;
using System.Collections.Generic;

namespace Game.Core.Domain.Grid
{
    public static class GridQueries
    {
        private static readonly GridCoord[] AllCoords =
        {
            new GridCoord(0, 0), new GridCoord(0, 1), new GridCoord(0, 2),
            new GridCoord(1, 0), new GridCoord(1, 1), new GridCoord(1, 2),
            new GridCoord(2, 0), new GridCoord(2, 1), new GridCoord(2, 2)
        };

        public static IReadOnlyList<GridCoord> AllCoordsRowMajor()
        {
            return AllCoords;
        }

        public static IReadOnlyList<GridCoord> OrthogonalNeighbors(GridCoord coord)
        {
            List<GridCoord> result = new List<GridCoord>(4);
            AddIfValid(result, new GridCoord(coord.Row - 1, coord.Col));
            AddIfValid(result, new GridCoord(coord.Row + 1, coord.Col));
            AddIfValid(result, new GridCoord(coord.Row, coord.Col - 1));
            AddIfValid(result, new GridCoord(coord.Row, coord.Col + 1));
            return result;
        }

        public static IReadOnlyList<GridCoord> CoordsAboveSameColumn(GridCoord coord)
        {
            List<GridCoord> result = new List<GridCoord>(2);
            for (int row = coord.Row - 1; row >= 0; row--)
            {
                result.Add(new GridCoord(row, coord.Col));
            }

            return result;
        }

        public static GridDirection? DirectionFromTo(GridCoord from, GridCoord to)
        {
            if (!from.IsOrthogonalNeighborOf(to))
            {
                return null;
            }

            if (to.Row < from.Row)
            {
                return GridDirection.Up;
            }

            if (to.Row > from.Row)
            {
                return GridDirection.Down;
            }

            if (to.Col < from.Col)
            {
                return GridDirection.Left;
            }

            return GridDirection.Right;
        }

        public static GridCoord StepToward(GridCoord from, GridCoord target)
        {
            if (from.Row != target.Row)
            {
                int row = from.Row + Math.Sign(target.Row - from.Row);
                return new GridCoord(row, from.Col);
            }

            if (from.Col != target.Col)
            {
                int col = from.Col + Math.Sign(target.Col - from.Col);
                return new GridCoord(from.Row, col);
            }

            return from;
        }

        private static void AddIfValid(List<GridCoord> result, GridCoord coord)
        {
            if (coord.IsValid)
            {
                result.Add(coord);
            }
        }
    }
}
