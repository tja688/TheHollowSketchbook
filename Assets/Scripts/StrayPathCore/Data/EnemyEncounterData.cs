using System.Collections.Generic;
using UnityEngine;

namespace StrayPathCore.Data
{
    /// <summary>
    /// 单场遭遇的敌人组合配置。
    /// </summary>
    [System.Serializable]
    public class EnemyEncounterEntry
    {
        [Tooltip("敌人数据SO")]
        public EnemyData EnemyData;
        [Tooltip("是否为Easy模式专属")]
        public bool EasyModeOnly = false;
    }

    /// <summary>
    /// 遭遇配置数据 —— 按Act与BattleType定义可用的敌人组合池。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyEncounterData", menuName = "StrayPath/Data/EnemyEncounterData")]
    public class EnemyEncounterData : ScriptableObject
    {
        [Header("标识")]
        public int EncounterID;
        public string EncounterName;

        [Header("适用场景")]
        [Tooltip("适用Act (1~3, 0=不限)")]
        public int ActID = 1;
        [Tooltip("战斗类型: 1=普通, 2=精英, 3=Boss")]
        public int BattleType = 1;

        [Header("敌人组合")]
        [Tooltip("该遭遇包含的敌人列表")]
        public List<EnemyEncounterEntry> Enemies = new List<EnemyEncounterEntry>();

        [Header("生成权重")]
        [Tooltip("在符合Act/BattleType的池子中的随机权重")]
        public int SpawnWeight = 10;

        [Header("限制")]
        [Tooltip("每场Run中该遭遇最多出现次数 (0=不限)")]
        public int MaxPerRun = 0;
    }
}
