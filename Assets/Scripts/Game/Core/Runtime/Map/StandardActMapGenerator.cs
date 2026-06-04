using System;
using System.Collections.Generic;
using Game.Core.Entities;
using Game.Core.Random;

namespace Game.Core.Map
{
    public sealed class StandardActMapGenerator
    {
        public ActMap Generate(IRng rng, ActModel act)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            if (act == null)
            {
                throw new ArgumentNullException(nameof(act));
            }

            int columnCount = Math.Max(3, act.ColumnCount);
            int rowCount = Math.Max(5, act.MapLength);
            ActMap map = new ActMap(columnCount, rowCount);

            MapPoint start = new MapPoint(new MapCoord(columnCount / 2, 0), MapPointType.Start);
            MapPoint boss = new MapPoint(new MapCoord(columnCount / 2, rowCount - 1), MapPointType.Boss);
            map.SetStartingPoint(start);
            map.SetBossPoint(boss);

            List<int> startingColumns = CreateStartingColumns(rng, columnCount);
            for (int i = 0; i < startingColumns.Count; i++)
            {
                MapPoint current = map.GetOrCreatePoint(new MapCoord(startingColumns[i], 1), MapPointType.Monster);
                map.Connect(start, current);

                for (int row = 2; row <= rowCount - 2; row++)
                {
                    int nextColumn = ChooseNextColumn(map, current, row, columnCount, rng);
                    MapPoint next = map.GetOrCreatePoint(new MapCoord(nextColumn, row), MapPointType.Monster);
                    map.Connect(current, next);
                    current = next;
                }

                map.Connect(current, boss);
            }

            AssignPointTypes(map, rng, rowCount);
            return map;
        }

        private static List<int> CreateStartingColumns(IRng rng, int columnCount)
        {
            List<int> columns = new List<int>(columnCount);
            for (int i = 0; i < columnCount; i++)
            {
                columns.Add(i);
            }

            rng.Shuffle(columns);
            int count = Math.Max(3, Math.Min(columnCount, 5));
            List<int> result = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(columns[i]);
            }

            return result;
        }

        private static int ChooseNextColumn(ActMap map, MapPoint current, int nextRow, int columnCount, IRng rng)
        {
            List<int> candidates = new List<int>(3);
            for (int offset = -1; offset <= 1; offset++)
            {
                int targetColumn = current.Coord.Column + offset;
                if (targetColumn < 0 || targetColumn >= columnCount)
                {
                    continue;
                }

                if (!WouldCreateCrossing(map, current, nextRow, targetColumn))
                {
                    candidates.Add(targetColumn);
                }
            }

            if (candidates.Count == 0)
            {
                candidates.Add(Math.Max(0, Math.Min(columnCount - 1, current.Coord.Column)));
            }

            return rng.Pick(candidates);
        }

        private static bool WouldCreateCrossing(ActMap map, MapPoint current, int nextRow, int targetColumn)
        {
            if (targetColumn == current.Coord.Column)
            {
                return false;
            }

            if (!map.TryGetPoint(new MapCoord(targetColumn, current.Coord.Row), out MapPoint sibling))
            {
                return false;
            }

            foreach (MapPoint child in sibling.Children)
            {
                if (child.Coord.Row == nextRow && child.Coord.Column == current.Coord.Column)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssignPointTypes(ActMap map, IRng rng, int rowCount)
        {
            foreach (MapPoint point in map.GetPointsInRow(1))
            {
                point.PointType = MapPointType.Monster;
            }

            foreach (MapPoint point in map.GetPointsInRow(rowCount - 2))
            {
                point.PointType = MapPointType.Rest;
            }

            for (int row = 2; row <= rowCount - 3; row++)
            {
                foreach (MapPoint point in map.GetPointsInRow(row))
                {
                    point.PointType = PickMiddlePointType(rng);
                }
            }
        }

        private static MapPointType PickMiddlePointType(IRng rng)
        {
            MapPointType[] weights =
            {
                MapPointType.Monster,
                MapPointType.Monster,
                MapPointType.Monster,
                MapPointType.Event,
                MapPointType.Event,
                MapPointType.Treasure,
                MapPointType.Shop,
                MapPointType.Elite,
                MapPointType.Rest
            };

            return rng.Pick(weights);
        }
    }
}
