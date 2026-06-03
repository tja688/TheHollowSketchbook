// 整改: 2026-06-03 修复了 CombatEntity 抽象问题与 FindObjectOfType 滥用
using System;
using System.Collections.Generic;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Status;
using UnityEngine;

namespace StrayPathCore.Combat
{
    /// <summary>
    /// 敌人战斗实体 —— 管理敌人 HP、格挡、状态、意图与技能执行。
    /// </summary>
    public class EnemyCombatEntity : MonoBehaviour, ICombatEntity
    {
        public string UniqueID { get; private set; }
        public EnemyData Data { get; private set; }
        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentBlock { get; private set; }
        public int Power { get; private set; }
        public int Armor { get; private set; }
        public int Chill { get; private set; }
        public int SpectralForm { get; private set; }
        public bool IsDead => CurrentHP <= 0;
        public bool IsBoss { get; private set; }
        public bool IsElite { get; private set; }

        // 兼容属性
        public string UID => UniqueID;
        public int EnemyID => Data?.EnemyID ?? 0;
        public int CurrentPower => Power;
        public int CurrentArmor => Armor;
        public int CurrentThorns => Thorns;
        public int Block => CurrentBlock;
        public bool IsStunned { get; set; }
        public bool HasPrepared { get; set; }
        public string PreparedFollowUpAbility { get; set; }
        public bool IsFleeing { get; set; }
        public List<string> LastUsedAbilities { get; private set; } = new List<string>();
        public EnemyAbilityData CurrentIntent { get; set; }
        public int PreviewDamage { get; set; }

        // 本地状态（状态效果层数统一从 StatusEffectSystem 查询，消除双重存储）
        public int WeakStacks => StatusEffectSystem.Instance?.GetEffectValue(UniqueID, StatusEffectType.Weak) ?? 0;
        public int FragileStacks => StatusEffectSystem.Instance?.GetEffectValue(UniqueID, StatusEffectType.Fragile) ?? 0;
        public int BurnStacks => StatusEffectSystem.Instance?.GetEffectValue(UniqueID, StatusEffectType.Burn) ?? 0;
        public int Thorns { get; private set; }
        public int StatusProtect { get; private set; }
        public int Illusion { get; private set; }
        public bool HasBarrier { get; private set; }

        private HashSet<EnemyTraitType> _traits = new HashSet<EnemyTraitType>();

        public void Initialize(EnemyData data, int actID, int battleType, bool isEasyMode)
        {
            Data = data;
            UniqueID = Guid.NewGuid().ToString("N").Substring(0, 8);
            IsBoss = data != null && data.IsBoss;
            IsElite = data != null && data.IsElite;

            _traits.Clear();
            if (data != null && data.Traits != null)
            {
                foreach (var t in data.Traits)
                    _traits.Add(t);
            }

            float hpMult = 1.0f;
            if (isEasyMode) hpMult = 0.75f;
            else if (battleType == 2) hpMult = 1.3f;
            else if (battleType == 3) hpMult = 1.5f;
            else if (actID > 1) hpMult = 1.0f + (actID - 1) * 0.15f;

            MaxHP = data != null ? Mathf.RoundToInt(data.BaseHP * hpMult) : 30;
            CurrentHP = MaxHP;
            Power = data != null ? data.BasePower : 0;
            Armor = data != null ? data.BaseArmor : 0;
            Thorns = data != null ? data.BaseThorns : 0;
            CurrentBlock = 0;
            Chill = 0;
            SpectralForm = 0;
            StatusProtect = 0;
            Illusion = 0;
            // 清除敌人身上的旧状态效果
            StatusEffectSystem.Instance?.RemoveAllEffects(UniqueID);
            HasBarrier = false;
            IsStunned = false;
            HasPrepared = false;
            PreparedFollowUpAbility = null;
            IsFleeing = false;
            LastUsedAbilities.Clear();
            CurrentIntent = null;
            PreviewDamage = 0;
        }

        public bool HasTrait(EnemyTraitType trait)
        {
            return _traits.Contains(trait);
        }

        public bool HasStatusEffect(StatusEffectType type)
        {
            switch (type)
            {
                case StatusEffectType.Weak: return WeakStacks > 0;
                case StatusEffectType.Fragile: return FragileStacks > 0;
                case StatusEffectType.Burn: return BurnStacks > 0;
                case StatusEffectType.Chill: return Chill > 0;
                case StatusEffectType.SpectralForm: return SpectralForm > 0;
                case StatusEffectType.StatusProtect: return StatusProtect > 0;
                case StatusEffectType.Illusion: return Illusion > 0;
                case StatusEffectType.Barrier: return HasBarrier;
                default: return false;
            }
        }

        public void TakeDamage(int damage, bool pierceBlock = false)
        {
            if (IsDead || damage <= 0) return;

            int block = CurrentBlock;
            int remaining = DamageCalculator.ApplyBlock(damage, ref block, pierceBlock);
            if (block != CurrentBlock)
            {
                int consumed = CurrentBlock - block;
                CurrentBlock = block;
                GameEventBus.Instance.Publish(new BlockConsumedEvent
                {
                    TargetUID = UniqueID,
                    Amount = consumed,
                    RemainingBlock = CurrentBlock
                });
            }

            if (remaining > 0)
            {
                CurrentHP = Mathf.Max(0, CurrentHP - remaining);
                GameEventBus.Instance.Publish(new DamageTakenEvent
                {
                    TargetUID = UniqueID,
                    Damage = remaining,
                    RemainingHP = CurrentHP
                });
            }

            CheckDeath();
        }

        /// <summary>
        /// ICombatEntity 接口实现 —— 将通用调用转发到具体签名的 TakeDamage。
        /// </summary>
        void ICombatEntity.TakeDamage(int damage, object source)
        {
            TakeDamage(damage, false);
        }

        public void TakeMultipleDamage(int damagePerHit, int hitCount)
        {
            for (int i = 0; i < hitCount; i++)
            {
                if (IsDead) break;
                TakeDamage(damagePerHit);
            }
        }

        public void GainBlock(int amount)
        {
            if (amount <= 0) return;
            CurrentBlock += amount;
            GameEventBus.Instance.Publish(new BlockGainedEvent
            {
                TargetUID = UniqueID,
                Amount = amount,
                TotalBlock = CurrentBlock
            });
        }

        /// <summary>
        /// ICombatEntity 接口实现。
        /// </summary>
        void ICombatEntity.GainBlock(int amount)
        {
            GainBlock(amount);
        }

        public void ResetBlock()
        {
            if (CurrentBlock > 0)
            {
                int old = CurrentBlock;
                CurrentBlock = 0;
                GameEventBus.Instance.Publish(new BlockConsumedEvent
                {
                    TargetUID = UniqueID,
                    Amount = old,
                    RemainingBlock = 0
                });
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
            GameEventBus.Instance.Publish(new HealEvent
            {
                TargetUID = UniqueID,
                Amount = amount,
                CurrentHP = CurrentHP
            });
        }

        public void ApplyBurnStacks(int stacks)
        {
            if (stacks <= 0) return;
            StatusEffectSystem.Instance?.ApplyEffect(UniqueID, StatusEffectType.Burn, stacks, StatusDurationType.StackBased);
        }

        public void ApplyChill(int amount)
        {
            if (amount <= 0) return;
            Chill += amount;
            GameEventBus.Instance.Publish(new StatusEffectAppliedEvent
            {
                TargetUID = UniqueID,
                EffectType = StatusEffectType.Chill,
                Value = Chill,
                DurationType = StatusDurationType.StackBased
            });
        }

        public void DecayChill()
        {
            if (Chill > 0)
            {
                Chill = Mathf.Max(0, Chill - 1);
                GameEventBus.Instance.Publish(new StatusEffectDecayedEvent
                {
                    TargetUID = UniqueID,
                    EffectType = StatusEffectType.Chill,
                    RemainingValue = Chill
                });
            }
        }

        public void CheckDeath()
        {
            if (CurrentHP <= 0)
            {
                CurrentHP = 0;
                GameEventBus.Instance.Publish(new EnemyDiedEvent
                {
                    EnemyUID = UniqueID,
                    EnemyID = Data != null ? Data.EnemyID : 0
                });
            }
        }

        public void SetIntent(EnemyAbilityData ability, int previewDamage)
        {
            CurrentIntent = ability;
            PreviewDamage = previewDamage;
            if (ability != null)
            {
                int abilityIndex = -1;
                if (Data != null && Data.AIProfile != null && Data.AIProfile.Abilities != null)
                    abilityIndex = Data.AIProfile.Abilities.IndexOf(ability);

                GameEventBus.Instance.Publish(new EnemyIntentDisplayedEvent
                {
                    EnemyUID = UniqueID,
                    AbilityIndex = abilityIndex,
                    PreviewDamage = previewDamage
                });
            }
        }

        public void ClearIntent()
        {
            CurrentIntent = null;
            PreviewDamage = 0;
        }

        public void ExecuteAbility(EnemyAbilityData ability)
        {
            if (ability == null || IsDead) return;

            if (ability.Effects != null)
            {
                foreach (var effect in ability.Effects)
                    ApplyEffect(effect);
            }

            if (ability.BaseDamage > 0 && ability.NumberOfHits > 0)
            {
                var hero = BattleStateMachine.Instance?.GetHero();
                if (hero != null)
                {
                    for (int i = 0; i < ability.NumberOfHits; i++)
                    {
                        if (hero.IsDead) break;
                        int dmg = DamageCalculator.PreviewEnemyDamageToHero(ability.BaseDamage, hero, this);
                        hero.TakeDamage(dmg, this);
                    }
                }
            }

            if (ability.BlockValue > 0)
                GainBlock(ability.BlockValue);

            ClearIntent();
        }

        private void ApplyEffect(EnemyAbilityEffect effect)
        {
            switch (effect.Type)
            {
                case EffectType.Damage:
                case EffectType.MultiDamage:
                    break;
                case EffectType.Heal:
                    Heal(effect.Value);
                    break;
                case EffectType.Block:
                    GainBlock(effect.Value);
                    break;
                case EffectType.Buff:
                    if (effect.Target == EffectTarget.Self && effect.Value > 0)
                        GainBlock(effect.Value);
                    break;
                case EffectType.Debuff:
                    {
                        var hero = BattleStateMachine.Instance?.GetHero();
                        if (hero != null && effect.Target == EffectTarget.Player)
                            hero.ApplyStatusEffect(StatusEffectType.Weak, effect.Value);
                    }
                    break;
                case EffectType.AddCardToDiscard:
                    GameEventBus.Instance.Publish(new CardDiscardedEvent
                    {
                        CardID = effect.CardID,
                        CopyCount = 0,
                        TargetPile = "discard"
                    });
                    break;
                case EffectType.AddCardToDeck:
                    GameEventBus.Instance.Publish(new CardDiscardedEvent
                    {
                        CardID = effect.CardID,
                        CopyCount = 0,
                        TargetPile = "deck"
                    });
                    break;
                case EffectType.Summon:
                case EffectType.Escape:
                case EffectType.RemoveBuff:
                    break;
            }
        }

        public void ClearBarrier()
        {
            if (HasBarrier)
            {
                HasBarrier = false;
                GameEventBus.Instance.Publish(new StatusEffectRemovedEvent
                {
                    TargetUID = UniqueID,
                    EffectType = StatusEffectType.Barrier
                });
            }
        }

        public void RecordAbilityUse(string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName)) return;
            LastUsedAbilities.Insert(0, abilityName);
            while (LastUsedAbilities.Count > 2)
                LastUsedAbilities.RemoveAt(LastUsedAbilities.Count - 1);
        }
    }
}
