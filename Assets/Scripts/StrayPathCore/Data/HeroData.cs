using System.Collections.Generic;
using UnityEngine;

namespace StrayPathCore.Data
{
    public enum HeroID { DragonSlayer, GrandMage, PossessedGunslinger }

    /// <summary>
    /// 英雄静态数据定义 —— ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "HeroData", menuName = "StrayPath/Data/HeroData")]
    public class HeroData : ScriptableObject
    {
        [Header("Identity")]
        public HeroID ID;
        public string HeroName;
        public string HeroCode; // "DS", "GM", "PG"
        public Sprite HeroPortrait;
        public Sprite BattleSprite;

        [Header("Base Stats")]
        public int BaseHP = 80;
        public int BaseEnergy = 3;
        public int BaseMP = 3;
        public int BaseBoostEnergy = 1;

        [Header("Starting Deck")]
        public List<int> StartingCardIDs = new List<int>();

        [Header("Unlocks by Level")]
        public List<HeroUnlock> LevelUnlocks = new List<HeroUnlock>();

        [Header("Special Logic")]
        public string PassiveEffectName; // 映射到战斗中的被动逻辑
    }

    [System.Serializable]
    public class HeroUnlock
    {
        public int Level;
        public List<int> UnlockCardIDs;
        public List<int> UnlockRelicIDs;
    }
}
