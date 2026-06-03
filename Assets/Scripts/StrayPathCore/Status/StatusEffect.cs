using StrayPathCore.Core;

namespace StrayPathCore.Status
{
    /// <summary>
    /// 状态效果纯数据容器 —— 描述一个 Buff/Debuff/Special 的完整信息。
    /// </summary>
    public class StatusEffect
    {
        public StatusEffectType Type;
        public int Value;              // 层数/回合数/充能数
        public StatusDurationType DurationType;
        public int TurnValue;          // 持续回合基准
        public string TurnType;        // "playerturn" / "enemyturn"
        public string SourceUID;       // 来源实体UID

        public StatusEffect(StatusEffectType type, int value, StatusDurationType durationType,
            int turnValue = 0, string turnType = "playerturn", string sourceUID = "")
        {
            Type = type;
            Value = value;
            DurationType = durationType;
            TurnValue = turnValue;
            TurnType = turnType;
            SourceUID = sourceUID;
        }

        public StatusEffect Clone()
        {
            return new StatusEffect(Type, Value, DurationType, TurnValue, TurnType, SourceUID);
        }
    }
}
