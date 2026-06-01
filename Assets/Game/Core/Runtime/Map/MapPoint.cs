using System;
using System.Collections.Generic;

namespace Game.Core.Map
{
    public sealed class MapPoint
    {
        private readonly HashSet<MapPoint> _parents = new HashSet<MapPoint>();
        private readonly HashSet<MapPoint> _children = new HashSet<MapPoint>();

        public MapPoint(MapCoord coord, MapPointType pointType)
        {
            Coord = coord;
            PointType = pointType;
        }

        public MapCoord Coord { get; }
        public MapPointType PointType { get; set; }
        public bool IsVisited { get; set; }
        public bool IsCompleted { get; set; }

        public IReadOnlyCollection<MapPoint> Parents
        {
            get { return _parents; }
        }

        public IReadOnlyCollection<MapPoint> Children
        {
            get { return _children; }
        }

        public void AddChild(MapPoint child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (_children.Add(child))
            {
                child._parents.Add(this);
            }
        }

        public override string ToString()
        {
            return PointType + "@" + Coord;
        }
    }
}
