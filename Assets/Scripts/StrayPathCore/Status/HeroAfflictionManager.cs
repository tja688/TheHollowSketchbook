using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Core;
using UnityEngine;

namespace StrayPathCore.Status
{
    /// <summary>
    /// 英雄状态看板 —— 聚合英雄当前所有状态效果，供 UI 层查询与刷新。
    /// </summary>
    public class HeroAfflictionManager : MonoBehaviour
    {
        [SerializeField] private StatusEffectSystem statusSystem;

        private void Awake()
        {
            if (statusSystem == null)
                statusSystem = StatusEffectSystem.Instance;
        }

        /// <summary>
        /// 获取英雄当前所有状态效果的聚合字典。
        /// Key: StatusEffectType, Value: 当前层数/数值。
        /// </summary>
        public Dictionary<object, int> GetAggregatedStatus()
        {
            var result = new Dictionary<object, int>();
            if (statusSystem == null) return result;

            var effects = statusSystem.GetAllEffects("hero");
            foreach (var effect in effects)
            {
                result[effect.Type] = effect.Value;
            }
            return result;
        }

        /// <summary>
        /// 获取指定状态类型的本地化描述。
        /// </summary>
        public string GetStatusDescription(StatusEffectType type, int value)
        {
            if (value <= 0) return "";

            switch (type)
            {
                // Buffs
                case StatusEffectType.Power: return $"Power {value}: Increases damage dealt by {value}.";
                case StatusEffectType.Toughness: return $"Toughness {value}: Reduces incoming damage by {value}.";
                case StatusEffectType.Armor: return $"Armor {value}: Blocks {value} damage from each attack.";
                case StatusEffectType.Thorns: return $"Thorns {value}: Deals {value} damage to attackers.";
                case StatusEffectType.Haste: return $"Haste {value}: Draw {value} extra card(s) per turn.";
                case StatusEffectType.Crit: return $"Crit {value}: +{value * 5}% critical hit chance.";
                case StatusEffectType.StatusProtect: return $"Status Protect {value}: Blocks {value} debuff applications.";
                case StatusEffectType.EnchantedArmor: return $"Enchanted Armor {value}: Blocks {value} and reflects {value / 2}.";
                case StatusEffectType.Illusion: return $"Illusion {value}: Next {value} attack(s) miss.";
                case StatusEffectType.Barrier: return $"Barrier {value}: Absorbs {value * 5} damage.";

                // Debuffs
                case StatusEffectType.Weak: return $"Weak {value}: Deals {(int)(value * 25)}% less damage.";
                case StatusEffectType.Fragile: return $"Fragile {value}: Takes {(int)(value * 25)}% more damage.";
                case StatusEffectType.BrokenGuard: return $"Broken Guard {value}: Block reduced by {value * 10}%.";
                case StatusEffectType.Amnesia: return $"Amnesia {value}: Cannot draw more than {value} card(s) per turn.";
                case StatusEffectType.Bleed: return $"Bleed {value}: Takes {value * 3} damage at turn end, then -1.";
                case StatusEffectType.Slow: return $"Slow {value}: Draw {value} fewer card(s) per turn.";

                // Special
                case StatusEffectType.Burn: return $"Burn {value}: Takes {value * 2} damage at turn end, then -1.";
                case StatusEffectType.Chill: return $"Chill {value}: Reduces energy by {value} next turn.";
                case StatusEffectType.Combustion: return $"Combustion {value}: Burn spreads to adjacent enemies.";
                case StatusEffectType.Inferno: return $"Inferno {value}: All burns deal +{value} damage.";
                case StatusEffectType.DemonicBrand: return $"Demonic Brand {value}: Takes {value * 5} damage at turn end.";

                // Continuous flags
                case StatusEffectType.SensoryOverload: return "Sensory Overload: All attacks hit all enemies.";
                case StatusEffectType.SweepingStrikes: return "Sweeping Strikes: Attacks deal 50% damage to all enemies.";
                case StatusEffectType.Juggernaut: return "Juggernaut: Gain 2 Block when you play an Attack.";
                case StatusEffectType.GrowingPower: return "Growing Power: +1 Power every 2 turns.";
                case StatusEffectType.Motivation: return "Motivation: +1 Energy per turn.";
                case StatusEffectType.AdrenalineRush: return "Adrenaline Rush: Draw +2 when HP below 25%.";
                case StatusEffectType.RegenerationPotion: return "Regeneration: Heal 3 HP at turn end.";
                case StatusEffectType.FireRadiance: return "Fire Radiance: Deal 2 Burn to attackers.";
                case StatusEffectType.IceAge: return "Ice Age: Chill lasts an extra turn.";
                case StatusEffectType.TemperatureShock: return "Temperature Shock: Frozen enemies take 2x damage.";
                case StatusEffectType.Berserk: return "Berserk: +3 Power, -2 Armor.";
                case StatusEffectType.Panic: return "Panic: 50% chance to dodge, but -1 card draw.";

                default: return $"{type} {value}";
            }
        }
    }
}
