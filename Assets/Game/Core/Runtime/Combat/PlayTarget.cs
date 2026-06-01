using Game.Core.Entities;

namespace Game.Core.Combat
{
    public readonly struct PlayTarget
    {
        public PlayTarget(Creature creature)
        {
            Creature = creature;
        }

        public Creature Creature { get; }

        public bool HasCreature
        {
            get { return Creature != null; }
        }

        public static PlayTarget None => default;

        public static PlayTarget ForCreature(Creature creature)
        {
            return new PlayTarget(creature);
        }
    }
}
