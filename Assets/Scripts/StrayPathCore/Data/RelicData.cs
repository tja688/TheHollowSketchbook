using System.Collections.Generic;
using UnityEngine;
using StrayPathCore.Core;

namespace StrayPathCore.Data
{
    public enum RelicRarity { Common = 1, Uncommon = 2, Rare = 3 }
    public enum RelicCategory { Generic, DS, GM, PG, Mystery, TaintedGift }

    /// <summary>
    /// 遗物静态数据定义 —— ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "RelicData", menuName = "StrayPath/Data/RelicData")]
    public class RelicData : ScriptableObject
    {
        [Header("Identity")]
        public int RelicID;
        public string RelicName;
        public string Description;
        public RelicRarity Rarity = RelicRarity.Common;
        public RelicCategory Category = RelicCategory.Generic;
        public Sprite RelicIcon;

        [Header("Economy")]
        public int BasePrice = 100;
        public int MaxCharges = 0; // 0 = 无限

        [Header("Trigger")]
        public List<RelicTriggerData> Triggers = new List<RelicTriggerData>();

        [Header("Flags")]
        public bool IsSingleUse = false;      // 单场战斗一次
        public bool IsPermanent = true;       // 是否持续到战斗结束
        public int RequiredHeroLevel = 1;     // 解锁等级要求
        public string RequiredHeroID;         // 专属英雄（空=通用）
    }

    [System.Serializable]
    public class RelicTriggerData
    {
        public RelicTriggerTiming Timing;
        public string EffectMethodName;       // 映射到 RelicEffectDispatcher
        public int EffectValue;
        public string Condition;              // 可选条件表达式
    }
}
