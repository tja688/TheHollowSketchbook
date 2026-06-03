using System.Collections.Generic;
using UnityEngine;

namespace StrayPathCore.Data
{
    public enum IntentType
    {
        Attack, Defend, NegativeEffect, PositiveEffect,
        DeckManipulation, Preparing, Special, Stunned, Flee, None
    }

    /// <summary>
    /// 敌人AI行为配置 —— ScriptableObject，按敌人定义技能列表与权重。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAIProfile", menuName = "StrayPath/Data/EnemyAIProfile")]
    public class EnemyAIProfile : ScriptableObject
    {
        [Header("Enemy Identity")]
        public int EnemyID;
        public string EnemyName;

        [Header("Abilities")]
        public List<EnemyAbilityData> Abilities = new List<EnemyAbilityData>();
    }

    [System.Serializable]
    public class EnemyAbilityData
    {
        public string AbilityName;
        public IntentType PrimaryIntent = IntentType.Attack;
        public IntentType SecondaryIntent = IntentType.None;
        public int BaseDamage = 0;
        public int NumberOfHits = 1;
        public int BlockValue = 0;
        public int BaseWeight = 10;
        public List<EnemyAbilityCondition> Conditions = new List<EnemyAbilityCondition>();
        public List<EnemyAbilityEffect> Effects = new List<EnemyAbilityEffect>();
        public bool IsPreparation = false; // 是否为准备/蓄力技能
        public string FollowUpAbilityName; // 准备后的后续技能
    }

    [System.Serializable]
    public class EnemyAbilityCondition
    {
        public ConditionType Type;
        public int Value;
        public string Comparison; // "==", ">=", "<=", "<", ">"
    }

    public enum ConditionType
    {
        TurnNumber, EnemyHPPercent, AllyCount, PlayerHPPercent,
        HasBuff, HasDebuff, LastAbilityUsed, RandomChance
    }

    [System.Serializable]
    public class EnemyAbilityEffect
    {
        public EffectTarget Target;
        public EffectType Type;
        public int Value;
        public int Duration; // 回合数
        public int CardID; // 牌库污染用
    }

    public enum EffectTarget { Self, Player, AllEnemies, RandomEnemy, AllPlayers }
    public enum EffectType
    {
        Damage, MultiDamage, Heal, Block, Buff, Debuff,
        Summon, AddCardToDeck, AddCardToDiscard, RemoveBuff, Escape
    }
}
