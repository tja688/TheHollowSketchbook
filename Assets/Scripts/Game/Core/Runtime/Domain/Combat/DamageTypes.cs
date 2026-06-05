using System;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Combat
{
    public enum DamageKind
    {
        Attack,
        Trap,
        Item,
        Relic,
        Environment,
        HpLoss
    }

    public readonly struct DamageSource
    {
        private DamageSource(CardInstanceId cardId, string label)
        {
            CardId = cardId;
            Label = label ?? string.Empty;
        }

        public CardInstanceId CardId { get; }
        public string Label { get; }

        public static DamageSource FromCard(CardInstanceId cardId)
        {
            return new DamageSource(cardId, string.Empty);
        }

        public static DamageSource Environment(string label)
        {
            return new DamageSource(default, label);
        }
    }

    public readonly struct DamageTarget
    {
        private DamageTarget(CardInstanceId cardId)
        {
            CardId = cardId;
        }

        public CardInstanceId CardId { get; }

        public static DamageTarget Card(CardInstanceId cardId)
        {
            return new DamageTarget(cardId);
        }
    }

    public sealed class DamageInfo
    {
        public DamageInfo(DamageSource source, DamageTarget target, int baseAmount, DamageKind kind, bool ignoreDefense, string reason)
        {
            Source = source;
            Target = target;
            BaseAmount = Math.Max(0, baseAmount);
            Kind = kind;
            IgnoreDefense = ignoreDefense;
            Reason = reason ?? string.Empty;
            CanBePrevented = true;
            CanTriggerThorns = true;
        }

        public DamageSource Source { get; }
        public DamageTarget Target { get; }
        public int BaseAmount { get; }
        public DamageKind Kind { get; }
        public bool IgnoreDefense { get; }
        public bool CanBePrevented { get; set; }
        public bool CanTriggerThorns { get; set; }
        public string Reason { get; }
    }

    public sealed class DamageResult
    {
        public CardInstanceId TargetCardId { get; set; }
        public int OriginalAmount { get; set; }
        public int DefenseReducedAmount { get; set; }
        public int HpLoss { get; set; }
        public bool Killed { get; set; }
    }
}
