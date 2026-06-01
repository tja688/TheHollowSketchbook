using System;

namespace Game.Core.Map
{
    public readonly struct MapCoord : IEquatable<MapCoord>
    {
        public MapCoord(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public int Column { get; }
        public int Row { get; }

        public bool Equals(MapCoord other)
        {
            return Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is MapCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Column * 397) ^ Row;
            }
        }

        public override string ToString()
        {
            return "(" + Column + "," + Row + ")";
        }

        public static bool operator ==(MapCoord left, MapCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MapCoord left, MapCoord right)
        {
            return !left.Equals(right);
        }
    }
}
