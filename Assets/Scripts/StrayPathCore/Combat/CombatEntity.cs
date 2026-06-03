// 整改: 2026-06-03 修复了 CombatEntity 抽象问题 —— 定义 ICombatEntity 接口，统一英雄与敌人的战斗实体契约
using System;
using System.Collections.Generic;
using StrayPathCore.Core;
using UnityEngine;

namespace StrayPathCore.Combat
{
    /// <summary>
    /// 战斗实体接口 —— 英雄与敌人的统一契约。
    /// 所有伤害计算、目标选择与效果触发均通过此接口交互，彻底解耦具体类型。
    /// </summary>
    public interface ICombatEntity
    {
        string UID { get; }
        int CurrentHP { get; }
        int MaxHP { get; }
        int Block { get; }
        bool IsDead { get; }
        void TakeDamage(int damage, object source = null);
        void Heal(int amount);
        void GainBlock(int amount);
        void ResetBlock();
        bool HasStatusEffect(StatusEffectType type);
    }

    /// <summary>
    /// 战斗实体基类 —— 英雄与敌人的共享属性与行为。
    /// 保留作为可选的共享基类，新代码推荐使用 ICombatEntity 接口编程。
    /// </summary>
    public abstract class CombatEntity : ICombatEntity
    {
        public string UID { get; protected set; }
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }
        public int Block { get; set; }
        public bool IsDead => CurrentHP <= 0;

        protected CombatEntity(string uid)
        {
            UID = uid;
        }

        public virtual void TakeDamage(int damage, object source = null)
        {
            if (damage <= 0) return;
            int remaining = damage;
            if (Block > 0)
            {
                int blocked = Mathf.Min(Block, damage);
                Block -= blocked;
                remaining -= blocked;
                GameEventBus.Instance.Publish(new BlockConsumedEvent
                {
                    TargetUID = UID,
                    Amount = blocked,
                    RemainingBlock = Block
                });
            }
            if (remaining > 0)
            {
                CurrentHP = Mathf.Max(0, CurrentHP - remaining);
                GameEventBus.Instance.Publish(new DamageTakenEvent
                {
                    TargetUID = UID,
                    Damage = remaining,
                    RemainingHP = CurrentHP
                });
            }
        }

        public virtual void Heal(int amount)
        {
            if (amount <= 0) return;
            int oldHP = CurrentHP;
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
            GameEventBus.Instance.Publish(new HealEvent
            {
                TargetUID = UID,
                Amount = CurrentHP - oldHP,
                CurrentHP = CurrentHP
            });
        }

        public virtual void GainBlock(int amount)
        {
            if (amount <= 0) return;
            Block += amount;
            GameEventBus.Instance.Publish(new BlockGainedEvent
            {
                TargetUID = UID,
                Amount = amount,
                TotalBlock = Block
            });
        }

        public virtual void ResetBlock()
        {
            Block = 0;
        }

        public virtual bool HasStatusEffect(StatusEffectType type)
        {
            return false;
        }
    }
}
