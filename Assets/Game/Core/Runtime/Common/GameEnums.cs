namespace Game.Core
{
    public enum PileType
    {
        Deck,
        Draw,
        Hand,
        Discard,
        Exhaust,
        Play
    }

    public enum CombatSide
    {
        None,
        Player,
        Enemy
    }

    public enum CardType
    {
        Attack,
        Skill,
        Power,
        Status,
        Curse
    }

    public enum CardRarity
    {
        Basic,
        Common,
        Uncommon,
        Rare,
        Special
    }

    public enum CardTargeting
    {
        None,
        Self,
        SingleEnemy,
        AllEnemies
    }

    public enum CardKeyword
    {
        Exhaust,
        Ethereal,
        Retain
    }

    public enum DamageType
    {
        Attack,
        HpLoss
    }

    public enum PowerType
    {
        Buff,
        Debuff
    }

    public enum EnemyIntentType
    {
        None,
        Attack,
        Defend,
        Buff,
        Debuff
    }
}
