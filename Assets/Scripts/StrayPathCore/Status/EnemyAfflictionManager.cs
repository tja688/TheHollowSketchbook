using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrayPathCore.Status
{
    /// <summary>
    /// 敌人状态看板 —— 聚合指定敌人当前所有状态效果，供 UI 层查询与刷新。
    /// </summary>
    public class EnemyAfflictionManager : MonoBehaviour
    {
        [SerializeField] private StatusEffectSystem statusSystem;

        private void Awake()
        {
            if (statusSystem == null)
                statusSystem = StatusEffectSystem.Instance;
        }

        /// <summary>
        /// 获取指定敌人当前所有状态效果的聚合字典。
        /// Key: StatusEffectType, Value: 当前层数/数值。
        /// </summary>
        public Dictionary<object, int> GetAggregatedStatus(string enemyUID)
        {
            var result = new Dictionary<object, int>();
            if (statusSystem == null || string.IsNullOrEmpty(enemyUID)) return result;

            var effects = statusSystem.GetAllEffects(enemyUID);
            foreach (var effect in effects)
            {
                result[effect.Type] = effect.Value;
            }
            return result;
        }
    }
}
