using System.Collections.Generic;
using UnityEngine;

namespace StrayPathCore.Data
{
    public enum EventChoiceType
    {
        None, Combat, Heal, Damage, GoldChange, CardReward,
        CardRemove, CardUpgrade, CardDuplicate, CardSwap,
        RelicReward, MaxHPChange, MPChange, BoostChange,
        AddCurse, RemoveCurse, Shop, Leave, MultiStage
    }

    /// <summary>
    /// 事件节点静态数据定义 —— ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "EventData", menuName = "StrayPath/Data/EventData")]
    public class EventData : ScriptableObject
    {
        [Header("Identity")]
        public int EventID;
        public string EventTitle;
        [TextArea(5, 10)]
        public string EventDescription;
        public Sprite BackgroundImage;

        [Header("Availability")]
        public int MinAct = 1;
        public int MaxAct = 3;
        public bool IsRepeatable = false;
        public List<string> RequiredFlags = new List<string>();
        public List<string> BlockingFlags = new List<string>();

        [Header("Choices")]
        public List<EventChoiceData> Choices = new List<EventChoiceData>();
    }

    [System.Serializable]
    public class EventChoiceData
    {
        public string ChoiceText;
        public EventChoiceType ChoiceType;
        public string NextDescription; // 多阶段事件的下一段描述
        public int Value; // 数值参数（伤害/治疗/金币等）
        public int CardID; // 关联卡牌ID
        public int RelicID; // 关联遗物ID
        public List<EventChoiceData> SubChoices; // 子选项（多阶段）
        public string RequiredCondition; // 条件检查（如金币足够）
        public string SetFlag; // 选择后设置的标志
    }
}
