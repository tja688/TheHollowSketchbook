using System;

namespace Game.Core
{
    public readonly struct ModelId : IEquatable<ModelId>
    {
        public string Category { get; }
        public string Entry { get; }

        public ModelId(string category, string entry)
        {
            Category = category ?? throw new ArgumentNullException(nameof(category));
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Category) || string.IsNullOrEmpty(Entry);
            }
        }

        public bool Equals(ModelId other)
        {
            return string.Equals(Category, other.Category, StringComparison.Ordinal)
                && string.Equals(Entry, other.Entry, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ModelId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Category != null ? Category.GetHashCode() : 0) * 397)
                    ^ (Entry != null ? Entry.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return Category + ":" + Entry;
        }

        public static bool operator ==(ModelId left, ModelId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ModelId left, ModelId right)
        {
            return !left.Equals(right);
        }
    }
}
