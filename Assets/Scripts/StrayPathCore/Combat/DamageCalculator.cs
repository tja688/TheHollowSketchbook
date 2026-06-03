// 整改: 2026-06-03 修复了 CombatEntity 抽象问题 —— 统一接收 ICombatEntity 接口，内部按需安全转型
using StrayPathCore.Core;
using UnityEngine;

namespace StrayPathCore.Combat
{
    /// <summary>
    /// 统一伤害计算工具类 —— PreviewDamage 与 TakeDamage 共用同一套修正逻辑。
    /// 公共 API 统一使用 ICombatEntity，内部按具体类型安全转型以获取特化属性。
    /// </summary>
    public static class DamageCalculator
    {
        public static int CalculatePlayerDamageToEnemy(
            int baseDamage, ICombatEntity enemy, ICombatEntity hero,
            bool isCombo, bool isFinisher, bool isUpgraded,
            bool boostActive, bool hasRelic75)
        {
            if (enemy == null || baseDamage <= 0) return 0;

            var enemyEntity = enemy as EnemyCombatEntity;
            var heroEntity = hero as HeroCombatEntity;

            float fragileMult = enemy.HasStatusEffect(StatusEffectType.Fragile) ? 1.3f : 1.0f;
            float chillBonus = 0f;
            if (enemyEntity != null && enemyEntity.Chill > 4)
                chillBonus = enemyEntity.Chill >= 10 ? 0.2f : 0.1f;

            float critMult = (heroEntity != null && heroEntity.CritCharges > 0) ? 1.5f : 1.0f;
            float comboMult = isCombo ? (isUpgraded ? 1.4f : 1.2f) : 1.0f;
            float finisherMult = isFinisher ? 1.4f : 1.0f;
            float boostMult = boostActive ? (hasRelic75 ? 1.75f : 1.5f) : 1.0f;
            float weakMult = (hero != null && hero.HasStatusEffect(StatusEffectType.Weak)) ? 0.7f : 1.0f;
            float spectralMult = (enemyEntity != null && enemyEntity.SpectralForm > 0) ? 0.5f : 1.0f;

            float totalMult = (fragileMult + chillBonus) * critMult * comboMult * finisherMult * boostMult * weakMult * spectralMult;
            int damage = Mathf.RoundToInt(baseDamage * totalMult);
            damage = ApplyArmor(damage, enemyEntity != null ? enemyEntity.Armor : 0);
            return Mathf.Max(0, damage);
        }

        public static int CalculateEnemyDamageToHero(
            int baseDamage, ICombatEntity hero, ICombatEntity enemy,
            bool hasHobgoblinFury, bool hasBoostedDef, bool hasFrostArmor)
        {
            if (hero == null || baseDamage <= 0) return 0;

            var heroEntity = hero as HeroCombatEntity;
            var enemyEntity = enemy as EnemyCombatEntity;

            int damage = baseDamage + (enemyEntity != null ? enemyEntity.Power : 0);

            float mult = 1.0f;
            if (hasHobgoblinFury) mult *= 1.5f;
            if (hero.HasStatusEffect(StatusEffectType.Fragile)) mult *= 1.3f;
            if (hasBoostedDef) mult *= 0.8f;
            if (hero.HasStatusEffect(StatusEffectType.Weak)) mult *= 0.7f;
            if (hasFrostArmor) mult *= 0.8f;

            damage = Mathf.RoundToInt(damage * mult);
            damage = ApplyArmor(damage, heroEntity != null ? heroEntity.Armor : 0);
            return Mathf.Max(0, damage);
        }

        public static int PreviewEnemyDamageToHero(
            int baseDamage, ICombatEntity hero, ICombatEntity enemy)
        {
            bool hobgoblinFury = GameStateManager.Instance != null && GameStateManager.Instance.BattleState.HobGoblinFury;
            bool boostedDef = GameStateManager.Instance != null && GameStateManager.Instance.BattleState.BoostActive;
            bool frostArmor = hero != null && hero.HasStatusEffect(StatusEffectType.Barrier);
            return CalculateEnemyDamageToHero(baseDamage, hero, enemy, hobgoblinFury, boostedDef, frostArmor);
        }

        public static int ApplyArmor(int damage, int armor)
        {
            return Mathf.Max(0, damage - armor);
        }

        public static int ApplyBlock(int damage, ref int block, bool pierce)
        {
            if (pierce || block <= 0) return damage;
            if (damage <= block)
            {
                block -= damage;
                return 0;
            }
            int remaining = damage - block;
            block = 0;
            return remaining;
        }
    }
}
