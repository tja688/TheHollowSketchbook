using System.Collections.Generic;
using UnityEngine;

namespace StrayPathCore.Data
{
    public enum EnemySize { Small, Medium, Big, Huge }
    public enum EnemyTraitType
    {
        ReformBody, Airborne, CheapShot, GroupTactics, Lacerate,
        BeyondTheGrave, Ingested, FollowTheRules, Resolve,
        DarkPact, Maelstrom, Volley, DemonicPower, Discord,
        Infinity, TemporalStasis, Herald, Rocksolid
    }

    /// <summary>
    /// 敌人静态数据定义 —— ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "StrayPath/Data/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public int EnemyID;
        public string EnemyName;
        public Sprite EnemySprite;
        public EnemySize Size = EnemySize.Medium;
        public bool IsAnimated = false;
        public bool IsBoss = false;
        public bool IsElite = false;

        [Header("Stats")]
        public int BaseHP = 30;
        public int BaseEnergy = 0;
        public int BasePower = 0;
        public int BaseArmor = 0;
        public int BaseThorns = 0;

        [Header("Traits")]
        public List<EnemyTraitType> Traits = new List<EnemyTraitType>();

        [Header("AI Profile")]
        public EnemyAIProfile AIProfile;

        [Header("Rewards")]
        public int GoldReward = 10;
        public int XPReward = 5;
    }
}
