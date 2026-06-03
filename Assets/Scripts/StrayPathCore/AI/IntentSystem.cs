using System.Collections.Generic;
using StrayPathCore.Combat;
using StrayPathCore.Core;
using StrayPathCore.Data;
using UnityEngine;

namespace StrayPathCore.AI
{
    /// <summary>
    /// 敌人意图显示系统 —— 负责计算并广播意图数据，供表现层 UI 渲染。
    /// 逻辑层不直接操作 Transform/Renderer，仅通过事件和字典维护状态。
    /// </summary>
    public class IntentSystem : MonoBehaviour
    {
        public static IntentSystem Instance { get; private set; }

        /// <summary>
        /// 当前显示的意图数据：EnemyUID -> IntentData
        /// </summary>
        public Dictionary<string, IntentData> CurrentIntents { get; private set; } = new Dictionary<string, IntentData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ==================== 意图显示 ====================

        /// <summary>
        /// 显示敌人的意图。
        /// </summary>
        public void DisplayIntent(EnemyCombatEntity enemy, IntentType primary, IntentType secondary, int damage, int hits)
        {
            if (enemy == null) return;

            var data = new IntentData
            {
                EnemyUID = enemy.UID,
                Primary = primary,
                Secondary = secondary,
                Damage = damage,
                Hits = hits
            };

            CurrentIntents[enemy.UID] = data;

            // 广播意图显示事件，供 UI 层订阅
            GameEventBus.Instance.Publish(new EnemyIntentDisplayedEvent
            {
                EnemyUID = enemy.UID,
                AbilityIndex = -1,
                PreviewDamage = damage
            });
        }

        /// <summary>
        /// 更新意图数值（状态变化后刷新预览伤害）。
        /// </summary>
        public void UpdateIntentValue(EnemyCombatEntity enemy, int newDamage)
        {
            if (enemy == null || !CurrentIntents.TryGetValue(enemy.UID, out var data)) return;

            data.Damage = newDamage;
            CurrentIntents[enemy.UID] = data;

            GameEventBus.Instance.Publish(new EnemyIntentDisplayedEvent
            {
                EnemyUID = enemy.UID,
                AbilityIndex = -1,
                PreviewDamage = newDamage
            });
        }

        /// <summary>
        /// 清除敌人的意图。
        /// </summary>
        public void ClearIntent(EnemyCombatEntity enemy)
        {
            if (enemy == null) return;
            CurrentIntents.Remove(enemy.UID);
        }

        /// <summary>
        /// 获取所有敌人的当前意图（供 UI 批量刷新）。
        /// </summary>
        public IReadOnlyDictionary<string, IntentData> GetAllIntents()
        {
            return CurrentIntents;
        }

        // ==================== 描述与 Tooltip ====================

        /// <summary>
        /// 获取指定意图类型的本地化描述文本。
        /// </summary>
        public string GetIntentDescription(IntentType type, int value, int hits)
        {
            string valueText = GetIntentValueText(value, hits);

            switch (type)
            {
                case IntentType.Attack:
                    return hits > 1
                        ? $"Attack: Deals {valueText} damage across {hits} hits."
                        : $"Attack: Deals {valueText} damage.";

                case IntentType.Defend:
                    return $"Defend: Gains {value} Block.";

                case IntentType.NegativeEffect:
                    return $"Debuff: Applies a harmful effect.";

                case IntentType.PositiveEffect:
                    return $"Buff: Empowers itself.";

                case IntentType.DeckManipulation:
                    return $"Deck: Manipulates your cards.";

                case IntentType.Preparing:
                    return $"Preparing: Charging a powerful attack...";

                case IntentType.Special:
                    return $"Special: Unknown ability.";

                case IntentType.Stunned:
                    return $"Stunned: Cannot act this turn.";

                case IntentType.Flee:
                    return $"Flee: Attempting to escape.";

                default:
                    return "";
            }
        }

        /// <summary>
        /// 获取意图的数值显示文本。
        /// 单hit显示伤害值；多hit显示 "Nx D"；无数值显示 "" 或 "x"。
        /// </summary>
        public string GetIntentValueText(int damage, int hits)
        {
            if (damage <= 0 && hits <= 1) return "";
            if (damage <= 0) return "x";
            if (hits <= 1) return damage.ToString();
            return $"{hits}x {damage}";
        }

        /// <summary>
        /// 获取意图的完整 Tooltip 文本（含数值预览）。
        /// </summary>
        public string GetIntentTooltip(EnemyCombatEntity enemy)
        {
            if (enemy == null || !CurrentIntents.TryGetValue(enemy.UID, out var data))
                return "No intent.";

            string primaryDesc = GetIntentDescription(data.Primary, data.Damage, data.Hits);
            string secondaryDesc = data.Secondary != IntentType.None
                ? GetIntentDescription(data.Secondary, 0, 1)
                : "";

            return string.IsNullOrEmpty(secondaryDesc)
                ? primaryDesc
                : $"{primaryDesc}\n{secondaryDesc}";
        }
    }

    /// <summary>
    /// 意图运行时数据 —— 纯数据结构。
    /// </summary>
    public struct IntentData
    {
        public string EnemyUID;
        public IntentType Primary;
        public IntentType Secondary;
        public int Damage;
        public int Hits;
    }
}
