using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Map
{
    public sealed class ActMap
    {
        private readonly Dictionary<MapCoord, MapPoint> _points = new Dictionary<MapCoord, MapPoint>();

        public ActMap(int columnCount, int rowCount)
        {
            if (columnCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            }

            if (rowCount <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount));
            }

            ColumnCount = columnCount;
            RowCount = rowCount;
        }

        public int ColumnCount { get; }
        public int RowCount { get; }
        public MapPoint StartingMapPoint { get; private set; }
        public MapPoint BossMapPoint { get; private set; }

        public IReadOnlyCollection<MapPoint> Points
        {
            get { return _points.Values; }
        }

        public void AddPoint(MapPoint point)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            _points[point.Coord] = point;
        }

        public MapPoint GetOrCreatePoint(MapCoord coord, MapPointType pointType = MapPointType.Monster)
        {
            if (_points.TryGetValue(coord, out MapPoint point))
            {
                return point;
            }

            point = new MapPoint(coord, pointType);
            _points.Add(coord, point);
            return point;
        }

        public bool TryGetPoint(MapCoord coord, out MapPoint point)
        {
            return _points.TryGetValue(coord, out point);
        }

        public MapPoint GetPoint(MapCoord coord)
        {
            if (!_points.TryGetValue(coord, out MapPoint point))
            {
                throw new InvalidOperationException("Map point not found: " + coord);
            }

            return point;
        }

        public IEnumerable<MapPoint> GetPointsInRow(int row)
        {
            return _points.Values.Where(point => point.Coord.Row == row).OrderBy(point => point.Coord.Column);
        }

        public void SetStartingPoint(MapPoint point)
        {
            AddPoint(point);
            point.PointType = MapPointType.Start;
            StartingMapPoint = point;
        }

        public void SetBossPoint(MapPoint point)
        {
            AddPoint(point);
            point.PointType = MapPointType.Boss;
            BossMapPoint = point;
        }

        public void Connect(MapPoint parent, MapPoint child)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            AddPoint(parent);
            AddPoint(child);
            parent.AddChild(child);
        }
    }
}
