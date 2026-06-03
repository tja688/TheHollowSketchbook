using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Combat;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Status;
using UnityEngine;

namespace StrayPathCore.AI
{
    /// <summary>
    /// 敌人AI核心 —— 负责技能选择、意图显示与实际执行。
    /// AbilityAction 委托三态:
    ///   isPreview=true, isIntentUpdate=false: 首次显示意图 (DisplayIntent)
    ///   isPreview=true, isIntentUpdate=true:  刷新意图数值 (UpdateIntentText)
    ///   isPreview=false, isIntentUpdate=false: 实际执行 (ExecuteAbility)
    /// </summary>
    public class EnemyAIBehavior : MonoBehaviour
    {
        public static EnemyAIBehavior Instance { get; private set; }

        public delegate void AbilityAction(EnemyAbilityData ability, EnemyCombatEntity enemy, bool isPreview, bool isIntentUpdate);

        public Dictionary<EnemyCombatEntity, EnemyAbilityData> SelectedActions { get; private set; } = new Dictionary<EnemyCombatEntity, EnemyAbilityData>();

        private Dictionary<string, List<string>> _lastUsedAbilitiesForEnemy = new Dictionary<string, List<string>>();

        [Header("Systems")]
        [SerializeField] private IntentSystem intentSystem;
        [SerializeField] private EnemyAbilityRegistry abilityRegistry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ==================== 技能选择 ====================

        public void SelectAbilitiesForTurn(List<EnemyCombatEntity> enemies, int playerTurnNumber)
        {
            SelectedActions.Clear();
            if (enemies == null) return;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead || enemy.IsStunned) continue;
                var ability = SelectAbilityForEnemy(enemy, playerTurnNumber);
                if (ability != null)
                {
                    SelectedActions[enemy] = ability;
                }
            }
        }

        public EnemyAbilityData SelectAbilityForEnemy(EnemyCombatEntity enemy, int turnNumber)
        {
            if (enemy?.Data == null) return null;

            // 如果有准备技能的后续，强制使用
            if (enemy.HasPrepared && !string.IsNullOrEmpty(enemy.PreparedFollowUpAbility))
            {
                var followUp = abilityRegistry?.GetAbilityByName(enemy.EnemyID, enemy.PreparedFollowUpAbility);
                if (followUp != null)
                {
                    enemy.HasPrepared = false;
                    enemy.PreparedFollowUpAbility = null;
                    return followUp;
                }
            }

            var abilities = abilityRegistry?.GetAbilities(enemy.EnemyID);
            if (abilities == null || abilities.Count == 0)
            {
                abilities = enemy.Data.AIProfile?.Abilities;
            }
            if (abilities == null || abilities.Count == 0) return null;

            // 筛选满足条件的技能
            var validAbilities = abilities.Where(a => EvaluateConditions(a, enemy, turnNumber)).ToList();
            if (validAbilities.Count == 0) validAbilities = abilities.ToList();

            int index = GetWeightedRandomAbilityIndex(validAbilities, enemy.UID);
            if (index < 0 || index >= validAbilities.Count) return validAbilities[0];

            var selected = validAbilities[index];
            enemy.RecordAbilityUse(selected.AbilityName);
            return selected;
        }

        // ==================== 意图与执行 ====================

        public void DisplayIntent(EnemyAbilityData ability, EnemyCombatEntity enemy)
        {
            if (ability == null || enemy == null) return;

            int previewDamage = 0;
            var hero = FindHeroEntity();
            if (hero != null)
                previewDamage = CalculatePreviewDamage(ability, enemy, hero);

            enemy.CurrentIntent = ability;
            enemy.PreviewDamage = previewDamage;

            intentSystem?.DisplayIntent(enemy, ability.PrimaryIntent, ability.SecondaryIntent,
                previewDamage, ability.NumberOfHits);

            GameEventBus.Instance.Publish(new EnemyIntentDisplayedEvent
            {
                EnemyUID = enemy.UID,
                AbilityIndex = abilityRegistry?.GetAbilities(enemy.EnemyID)?.IndexOf(ability) ?? -1,
                PreviewDamage = previewDamage
            });
        }

        public void UpdateIntentText(EnemyAbilityData ability, EnemyCombatEntity enemy)
        {
            if (ability == null || enemy == null) return;

            var hero = FindHeroEntity();
            int previewDamage = hero != null ? CalculatePreviewDamage(ability, enemy, hero) : enemy.PreviewDamage;
            enemy.PreviewDamage = previewDamage;

            intentSystem?.UpdateIntentValue(enemy, previewDamage);
        }

        public void ExecuteAbility(EnemyAbilityData ability, EnemyCombatEntity enemy)
        {
            if (ability == null || enemy == null || enemy.IsDead) return;
            if (enemy.IsStunned)
            {
                enemy.IsStunned = false;
                intentSystem?.ClearIntent(enemy);
                return;
            }

            var hero = FindHeroEntity();
            if (hero == null) return;

            foreach (var effect in ability.Effects)
            {
                ExecuteEffect(effect, enemy, hero);
            }

            enemy.RecordAbilityUse(ability.AbilityName);

            if (ability.IsPreparation && !string.IsNullOrEmpty(ability.FollowUpAbilityName))
            {
                enemy.HasPrepared = true;
                enemy.PreparedFollowUpAbility = ability.FollowUpAbilityName;
            }

            intentSystem?.ClearIntent(enemy);
        }

        // ==================== 伤害预览 ====================

        public int CalculatePreviewDamage(EnemyAbilityData ability, EnemyCombatEntity enemy, HeroCombatEntity hero)
        {
            if (ability == null || enemy == null || hero == null) return 0;
            if (ability.BaseDamage <= 0) return 0;

            int baseDmg = ability.BaseDamage + enemy.CurrentPower;

            // Fragile 增伤 (玩家有 Fragile 时受到更多伤害)
            int fragileStacks = StatusEffectSystem.Instance?.GetEffectValue(hero.UID, StatusEffectType.Fragile) ?? 0;
            if (fragileStacks > 0)
                baseDmg = Mathf.RoundToInt(baseDmg * (1f + fragileStacks * 0.25f));

            // Weak 减伤 (敌人有 Weak 时造成更少伤害)
            int weakStacks = StatusEffectSystem.Instance?.GetEffectValue(enemy.UID, StatusEffectType.Weak) ?? 0;
            if (weakStacks > 0)
                baseDmg = Mathf.RoundToInt(baseDmg * (1f - weakStacks * 0.25f));

            // 最低1点伤害
            return Mathf.Max(1, baseDmg);
        }

        // ==================== 加权随机 ====================

        private int GetWeightedRandomAbilityIndex(List<EnemyAbilityData> abilities, string enemyUID)
        {
            if (abilities == null || abilities.Count == 0) return -1;
            if (abilities.Count == 1) return 0;

            if (!_lastUsedAbilitiesForEnemy.TryGetValue(enemyUID, out var history))
            {
                history = new List<string>();
                _lastUsedAbilitiesForEnemy[enemyUID] = history;
            }

            var weights = new List<int>();
            for (int i = 0; i < abilities.Count; i++)
            {
                int w = abilities[i].BaseWeight;
                // 历史行为惩罚：最近2次使用的技能权重降低
                if (history.Contains(abilities[i].AbilityName))
                    w = Mathf.Max(1, w / 2);
                weights.Add(w);
            }

            int totalWeight = weights.Sum();
            if (totalWeight <= 0) return 0;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return i;
            }
            return abilities.Count - 1;
        }

        // ==================== 条件判定 ====================

        private bool EvaluateConditions(EnemyAbilityData ability, EnemyCombatEntity enemy, int turnNumber)
        {
            if (ability.Conditions == null || ability.Conditions.Count == 0) return true;
            var hero = FindHeroEntity();

            foreach (var cond in ability.Conditions)
            {
                bool passed = false;
                switch (cond.Type)
                {
                    case ConditionType.TurnNumber:
                        passed = Compare(turnNumber, cond.Value, cond.Comparison);
                        break;
                    case ConditionType.EnemyHPPercent:
                        int hpPercent = enemy.MaxHP > 0 ? (enemy.CurrentHP * 100 / enemy.MaxHP) : 0;
                        passed = Compare(hpPercent, cond.Value, cond.Comparison);
                        break;
                    case ConditionType.PlayerHPPercent:
                        int playerHPPercent = hero != null && hero.MaxHP > 0 ? (hero.CurrentHP * 100 / hero.MaxHP) : 0;
                        passed = Compare(playerHPPercent, cond.Value, cond.Comparison);
                        break;
                    case ConditionType.AllyCount:
                        passed = Compare(1, cond.Value, cond.Comparison); // 简化：至少自己
                        break;
                    case ConditionType.HasBuff:
                        passed = StatusEffectSystem.Instance?.HasEffect(enemy.UID, (StatusEffectType)cond.Value) ?? false;
                        break;
                    case ConditionType.HasDebuff:
                        passed = StatusEffectSystem.Instance?.HasEffect(hero?.UID ?? "", (StatusEffectType)cond.Value) ?? false;
                        break;
                    case ConditionType.LastAbilityUsed:
                        passed = enemy.LastUsedAbilities.Count > 0 && enemy.LastUsedAbilities[0] == ability.AbilityName;
                        break;
                    case ConditionType.RandomChance:
                        passed = Random.Range(0, 100) < cond.Value;
                        break;
                }
                if (!passed) return false;
            }
            return true;
        }

        private bool Compare(int a, int b, string op)
        {
            switch (op)
            {
                case "==": return a == b;
                case ">=": return a >= b;
                case "<=": return a <= b;
                case ">": return a > b;
                case "<": return a < b;
                default: return a == b;
            }
        }

        // ==================== 效果执行 ====================

        private void ExecuteEffect(EnemyAbilityEffect effect, EnemyCombatEntity enemy, HeroCombatEntity hero)
        {
            switch (effect.Type)
            {
                case EffectType.Damage:
                case EffectType.MultiDamage:
                    {
                        int dmg = CalculatePreviewDamage(new EnemyAbilityData { BaseDamage = effect.Value }, enemy, hero);
                        int hits = effect.Type == EffectType.MultiDamage ? Mathf.Max(1, effect.Value / 10) : 1;
                        for (int i = 0; i < hits; i++)
                        {
                            GameEventBus.Instance.Publish(new DamageDealtEvent
                            {
                                SourceUID = enemy.UID,
                                TargetUID = hero.UID,
                                BaseDamage = effect.Value,
                                FinalDamage = dmg,
                                IsBlocked = false
                            });
                        }
                    }
                    break;

                case EffectType.Heal:
                    GameEventBus.Instance.Publish(new HealEvent
                    {
                        TargetUID = enemy.UID,
                        Amount = effect.Value,
                        CurrentHP = enemy.CurrentHP + effect.Value
                    });
                    break;

                case EffectType.Block:
                    GameEventBus.Instance.Publish(new BlockGainedEvent
                    {
                        TargetUID = enemy.UID,
                        Amount = effect.Value,
                        TotalBlock = enemy.Block + effect.Value
                    });
                    break;

                case EffectType.Buff:
                    StatusEffectSystem.Instance?.ApplyEffect(enemy.UID, (StatusEffectType)effect.Value, effect.Duration,
                        StatusDurationType.TurnBased, effect.Duration, "enemyturn", enemy.UID);
                    break;

                case EffectType.Debuff:
                    StatusEffectSystem.Instance?.ApplyEffect(hero.UID, (StatusEffectType)effect.Value, effect.Duration,
                        StatusDurationType.TurnBased, effect.Duration, "playerturn", enemy.UID);
                    break;

                case EffectType.Summon:
                    // 召唤逻辑由 BattleManager 处理
                    break;

                case EffectType.AddCardToDeck:
                case EffectType.AddCardToDiscard:
                    // 牌库污染由 DeckManager 订阅事件处理
                    break;

                case EffectType.RemoveBuff:
                    // 移除目标一个随机buff
                    break;

                case EffectType.Escape:
                    enemy.IsFleeing = true;
                    break;
            }
        }

        // ==================== 辅助 ====================

        private HeroCombatEntity FindHeroEntity()
        {
            // 简化：在真实项目中通过 BattleManager 或 ServiceLocator 获取
            // 此处返回一个单例引用占位
            return HeroCombatEntityReference.Instance?.Hero;
        }
    }

    /// <summary>
    /// 英雄战斗实体的运行时单例引用 —— 供 AI 系统获取当前英雄。
    /// 由 BattleManager 在战斗开始时设置。
    /// </summary>
    public class HeroCombatEntityReference : MonoBehaviour
    {
        public static HeroCombatEntityReference Instance { get; private set; }
        public HeroCombatEntity Hero { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }
}
