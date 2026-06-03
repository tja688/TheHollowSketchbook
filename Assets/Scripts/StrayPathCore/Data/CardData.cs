using UnityEngine;

namespace StrayPathCore.Data
{
    public enum CardRarity { Common = 1, Uncommon = 2, Rare = 3 }
    public enum CardType { Attack, Defense, Skill, Power, Curse, Status }

    /// <summary>
    /// 卡牌静态数据定义 —— ScriptableObject，在编辑器中配置所有卡牌模板。
    /// </summary>
    [CreateAssetMenu(fileName = "CardData", menuName = "StrayPath/Data/CardData")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public int CardID;
        public string CardName;
        public string UpgradedName;
        public CardRarity Rarity = CardRarity.Common;
        public CardType Type = CardType.Attack;
        public Sprite CardArt;

        [Header("Description")]
        [TextArea(3, 5)]
        public string Description;
        [TextArea(3, 5)]
        public string UpgradedDescription;

        [Header("Costs & Values")]
        public int EnergyCost = 1;
        public int UpgradedEnergyCost = 1;
        public int AttackValue = 0;
        public int UpgradedAttackValue = 0;
        public int DefendValue = 0;
        public int UpgradedDefendValue = 0;
        public int BanishCharges = 0;
        public int UpgradedBanishCharges = 0;
        public int BasePrice = 50;

        [Header("Flags")]
        public bool IsBoostable = false;
        public bool IsCombo = false;
        public bool IsFinisher = false;
        public bool TargetsEnemy = true;
        public bool IsContinuous = false;      // 是否持续效果卡
        public bool IsUpgradable = true;
        public bool IsRemovable = true;        // 是否可被删除

        [Header("Effect Mapping")]
        public string EffectMethodName;        // 映射到 CardEffectDispatcher 的方法名
        public string UpgradedEffectMethodName;

        [Header("Special")]
        public int[] RelatedRelicIDs;          // 关联遗物ID
        public string[] Keywords;              // Tooltip 关键词

        public int GetEnergyCost(bool upgraded) => upgraded ? UpgradedEnergyCost : EnergyCost;
        public int GetAttackValue(bool upgraded) => upgraded ? UpgradedAttackValue : AttackValue;
        public int GetDefendValue(bool upgraded) => upgraded ? UpgradedDefendValue : DefendValue;
        public int GetBanishCharges(bool upgraded) => upgraded ? UpgradedBanishCharges : BanishCharges;
        public string GetName(bool upgraded) => upgraded ? UpgradedName : CardName;
        public string GetDescription(bool upgraded) => upgraded ? UpgradedDescription : Description;
    }
}
