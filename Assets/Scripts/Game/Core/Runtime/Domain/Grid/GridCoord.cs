using System;

namespace Game.Core.Domain.Grid
{
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public GridCoord(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public int Row { get; }
        public int Col { get; }

        public bool IsValid
        {
            get { return Row >= 0 && Row < 3 && Col >= 0 && Col < 3; }
        }

        public int CellIndex
        {
            get
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Invalid grid coord has no cell index.");
                }

                return Row * 3 + Col + 1;
            }
        }

        public static GridCoord FromCellIndex(int index)
        {
            if (index < 1 || index > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Cell index must be 1..9.");
            }

            int zeroBased = index - 1;
            return new GridCoord(zeroBased / 3, zeroBased % 3);
        }

        public bool IsOrthogonalNeighborOf(GridCoord other)
        {
            return ManhattanDistanceTo(other) == 1;
        }

        public int ManhattanDistanceTo(GridCoord other)
        {
            return Math.Abs(Row - other.Row) + Math.Abs(Col - other.Col);
        }

        public bool TryOffset(GridDirection direction, out GridCoord result)
        {
            switch (direction)
            {
                case GridDirection.Up:
                    result = new GridCoord(Row - 1, Col);
                    break;
                case GridDirection.Down:
                    result = new GridCoord(Row + 1, Col);
                    break;
                case GridDirection.Left:
                    result = new GridCoord(Row, Col - 1);
                    break;
                case GridDirection.Right:
                    result = new GridCoord(Row, Col + 1);
                    break;
                default:
                    result = this;
                    break;
            }

            return result.IsValid;
        }

        public bool Equals(GridCoord other)
        {
            return Row == other.Row && Col == other.Col;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Col;
            }
        }

        public override string ToString()
        {
            return "Cell" + CellIndex;
        }

        public static bool operator ==(GridCoord left, GridCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridCoord left, GridCoord right)
        {
            return !left.Equals(right);
        }
    }

    public enum GridDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}
