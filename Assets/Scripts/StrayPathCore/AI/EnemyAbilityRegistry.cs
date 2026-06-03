using System.Collections.Generic;
using StrayPathCore.Core;
using StrayPathCore.Data;
using UnityEngine;

namespace StrayPathCore.AI
{
    /// <summary>
    /// 敌人技能注册表 —— 替代原巨型 EnemyAbilities.cs，按敌人ID分表管理技能数据。
    /// 支持运行时注册、查询与默认技能初始化。
    /// </summary>
    public class EnemyAbilityRegistry : MonoBehaviour
    {
        public static EnemyAbilityRegistry Instance { get; private set; }

        private Dictionary<int, List<EnemyAbilityData>> _abilityDatabase = new Dictionary<int, List<EnemyAbilityData>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ==================== 注册与查询 ====================

        public void RegisterAbility(int enemyID, EnemyAbilityData ability)
        {
            if (ability == null) return;

            if (!_abilityDatabase.TryGetValue(enemyID, out var list))
            {
                list = new List<EnemyAbilityData>();
                _abilityDatabase[enemyID] = list;
            }

            // 同名覆盖
            var existing = list.Find(a => a.AbilityName == ability.AbilityName);
            if (existing != null)
                list.Remove(existing);

            list.Add(ability);
        }

        public List<EnemyAbilityData> GetAbilities(int enemyID)
        {
            if (_abilityDatabase.TryGetValue(enemyID, out var list))
                return new List<EnemyAbilityData>(list);
            return new List<EnemyAbilityData>();
        }

        public EnemyAbilityData GetAbilityByName(int enemyID, string abilityName)
        {
            if (!_abilityDatabase.TryGetValue(enemyID, out var list)) return null;
            return list.Find(a => a.AbilityName == abilityName);
        }

        public void ClearAll()
        {
            _abilityDatabase.Clear();
        }

        // ==================== 默认技能初始化 ====================

        /// <summary>
        /// 注册约40+敌人的默认技能。框架占位，具体数值可在编辑器中通过 EnemyAIProfile SO 覆盖。
        /// </summary>
        public void InitializeDefaultAbilities()
        {
            _abilityDatabase.Clear();

            // ==================== Act 1 普通敌人 ====================

            RegisterGoblinAbilities();
            RegisterSlimeAbilities();
            RegisterSkeletonAbilities();
            RegisterWolfAbilities();
            RegisterBanditAbilities();
            RegisterBatAbilities();
            RegisterSpiderAbilities();
            RegisterZombieAbilities();

            // ==================== Act 1 精英 ====================

            RegisterHobgoblinAbilities();
            RegisterWraithAbilities();
            RegisterGolemAbilities();

            // ==================== Act 1 Boss ====================

            RegisterWitchDoctorAbilities();

            // ==================== Act 2 普通敌人 ====================

            RegisterCultistAbilities();
            RegisterImpAbilities();
            RegisterMagmaSpawnAbilities();
            RegisterIceElementalAbilities();
            RegisterShadowAbilities();
            RegisterHarpyAbilities();
            RegisterSerpentAbilities();
            RegisterDemonDogAbilities();

            // ==================== Act 2 精英 ====================

            RegisterLichAbilities();
            RegisterPhoenixAbilities();
            RegisterCorruptorAbilities();

            // ==================== Act 2 Boss ====================

            RegisterDragonAbilities();

            // ==================== Act 3 普通敌人 ====================

            RegisterVoidSpawnAbilities();
            RegisterAbyssalWormAbilities();
            RegisterDemonKnightAbilities();
            RegisterSoulReaperAbilities();
            RegisterChaosMageAbilities();
            RegisterPlagueBearerAbilities();
            RegisterInfernalHoundAbilities();
            RegisterSpectralArcherAbilities();

            // ==================== Act 3 精英 ====================

            RegisterVoidTitanAbilities();
            RegisterDemonLordAbilities();
            RegisterAbyssalHorrorAbilities();

            // ==================== Act 3 Boss ====================

            RegisterEldritchAbominationAbilities();

            Debug.Log($"[EnemyAbilityRegistry] Initialized {_abilityDatabase.Count} enemy ability sets.");
        }

        // ==================== 各敌人技能定义（占位模板） ====================

        private void RegisterGoblinAbilities()
        {
            int id = 101;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Slash",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 6,
                NumberOfHits = 1,
                BaseWeight = 15
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Stab",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 4,
                NumberOfHits = 2,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Defend",
                PrimaryIntent = IntentType.Defend,
                BlockValue = 5,
                BaseWeight = 8
            });
        }

        private void RegisterSlimeAbilities()
        {
            int id = 102;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Bounce",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 5,
                BaseWeight = 12
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Split",
                PrimaryIntent = IntentType.PositiveEffect,
                BaseWeight = 5
            });
        }

        private void RegisterSkeletonAbilities()
        {
            int id = 103;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "BoneStrike",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 7,
                BaseWeight = 12
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Reform",
                PrimaryIntent = IntentType.PositiveEffect,
                BaseWeight = 5
            });
        }

        private void RegisterWolfAbilities()
        {
            int id = 104;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Bite",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 5,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "PackTactics",
                PrimaryIntent = IntentType.PositiveEffect,
                BaseWeight = 8
            });
        }

        private void RegisterBanditAbilities()
        {
            int id = 105;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Mug",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 8,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "SmokeBomb",
                PrimaryIntent = IntentType.Defend,
                BlockValue = 8,
                BaseWeight = 8
            });
        }

        private void RegisterBatAbilities()
        {
            int id = 106;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "SonicScreech",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 4,
                BaseWeight = 12
            });
        }

        private void RegisterSpiderAbilities()
        {
            int id = 107;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "VenomBite",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 5,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Web",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 8
            });
        }

        private void RegisterZombieAbilities()
        {
            int id = 108;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Grasp",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 6,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Rot",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 6
            });
        }

        // ==================== 精英敌人 ====================

        private void RegisterHobgoblinAbilities()
        {
            int id = 201;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "FuriousSlash",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 10,
                BaseWeight = 12,
                Effects = new List<EnemyAbilityEffect>
                {
                    new EnemyAbilityEffect { Target = EffectTarget.Self, Type = EffectType.Buff, Value = (int)StatusEffectType.HobgoblinFury, Duration = 1 }
                }
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Howl",
                PrimaryIntent = IntentType.PositiveEffect,
                BaseWeight = 6
            });
        }

        private void RegisterWraithAbilities()
        {
            int id = 202;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "SpectralTouch",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 8,
                BaseWeight = 10,
                Effects = new List<EnemyAbilityEffect>
                {
                    new EnemyAbilityEffect { Target = EffectTarget.Self, Type = EffectType.Buff, Value = (int)StatusEffectType.SpectralForm, Duration = 1 }
                }
            });
        }

        private void RegisterGolemAbilities()
        {
            int id = 203;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Crush",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 12,
                BaseWeight = 10,
                Effects = new List<EnemyAbilityEffect>
                {
                    new EnemyAbilityEffect { Target = EffectTarget.Self, Type = EffectType.Buff, Value = (int)StatusEffectType.Rocksolid, Duration = 1 }
                }
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "StoneShield",
                PrimaryIntent = IntentType.Defend,
                BlockValue = 15,
                BaseWeight = 8
            });
        }

        // ==================== Bosses ====================

        private void RegisterWitchDoctorAbilities()
        {
            int id = 301;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Hex",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "VoodooStrike",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 10,
                BaseWeight = 12
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "SummonFetish",
                PrimaryIntent = IntentType.Special,
                BaseWeight = 5
            });
        }

        private void RegisterDragonAbilities()
        {
            int id = 302;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Claw",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 10,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "FireBreath",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 15,
                BaseWeight = 8,
                IsPreparation = true,
                FollowUpAbilityName = "InfernoBlast"
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "InfernoBlast",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 25,
                BaseWeight = 0 // 仅通过准备触发
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "WingGust",
                PrimaryIntent = IntentType.Defend,
                BlockValue = 12,
                BaseWeight = 6
            });
        }

        private void RegisterEldritchAbominationAbilities()
        {
            int id = 303;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "TentacleSlam",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 12,
                NumberOfHits = 2,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "VoidGaze",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 8
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "RealityTear",
                PrimaryIntent = IntentType.Special,
                BaseWeight = 4
            });
        }

        // ==================== Act 2 敌人 ====================

        private void RegisterCultistAbilities()
        {
            int id = 401;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Dagger",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 5,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Ritual",
                PrimaryIntent = IntentType.PositiveEffect,
                BaseWeight = 6
            });
        }

        private void RegisterImpAbilities()
        {
            int id = 402;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Fireball",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 6,
                BaseWeight = 12
            });
        }

        private void RegisterMagmaSpawnAbilities()
        {
            int id = 403;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "MagmaStrike",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 8,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "HeatWave",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 6
            });
        }

        private void RegisterIceElementalAbilities()
        {
            int id = 404;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "IceShard",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 7,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "FrostArmor",
                PrimaryIntent = IntentType.Defend,
                BlockValue = 10,
                BaseWeight = 8
            });
        }

        private void RegisterShadowAbilities()
        {
            int id = 405;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "ShadowStrike",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 9,
                BaseWeight = 10
            });
        }

        private void RegisterHarpyAbilities()
        {
            int id = 406;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Talons",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 6,
                NumberOfHits = 2,
                BaseWeight = 10
            });
        }

        private void RegisterSerpentAbilities()
        {
            int id = 407;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "VenomSpit",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 5,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Constrict",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 8
            });
        }

        private void RegisterDemonDogAbilities()
        {
            int id = 408;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Rip",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 7,
                BaseWeight = 12
            });
        }

        // ==================== Act 2 精英 ====================

        private void RegisterLichAbilities()
        {
            int id = 501;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "DeathBolt",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 12,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "RaiseDead",
                PrimaryIntent = IntentType.Special,
                BaseWeight = 5
            });
        }

        private void RegisterPhoenixAbilities()
        {
            int id = 502;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "FlameWing",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 10,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Rebirth",
                PrimaryIntent = IntentType.PositiveEffect,
                BaseWeight = 3
            });
        }

        private void RegisterCorruptorAbilities()
        {
            int id = 503;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Corrupt",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 8
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "BlightStrike",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 9,
                BaseWeight = 10
            });
        }

        // ==================== Act 3 敌人 ====================

        private void RegisterVoidSpawnAbilities()
        {
            int id = 601;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "VoidTouch",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 8,
                BaseWeight = 10
            });
        }

        private void RegisterAbyssalWormAbilities()
        {
            int id = 602;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Burrow",
                PrimaryIntent = IntentType.Defend,
                BlockValue = 12,
                BaseWeight = 8
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Maw",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 14,
                BaseWeight = 10
            });
        }

        private void RegisterDemonKnightAbilities()
        {
            int id = 603;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "DarkBlade",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 11,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "DemonicShield",
                PrimaryIntent = IntentType.Defend,
                BlockValue = 10,
                BaseWeight = 8
            });
        }

        private void RegisterSoulReaperAbilities()
        {
            int id = 604;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Harvest",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 9,
                BaseWeight = 10
            });
        }

        private void RegisterChaosMageAbilities()
        {
            int id = 605;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "ChaosBolt",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 7,
                NumberOfHits = 2,
                BaseWeight = 10
            });
        }

        private void RegisterPlagueBearerAbilities()
        {
            int id = 606;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Infect",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Pox",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 5,
                BaseWeight = 8
            });
        }

        private void RegisterInfernalHoundAbilities()
        {
            int id = 607;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "InfernoBite",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 10,
                BaseWeight = 12
            });
        }

        private void RegisterSpectralArcherAbilities()
        {
            int id = 608;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "PhantomArrow",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 9,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Volley",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 4,
                NumberOfHits = 3,
                BaseWeight = 6
            });
        }

        // ==================== Act 3 精英 ====================

        private void RegisterVoidTitanAbilities()
        {
            int id = 701;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Annihilate",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 16,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "VoidShell",
                PrimaryIntent = IntentType.Defend,
                BlockValue = 20,
                BaseWeight = 6
            });
        }

        private void RegisterDemonLordAbilities()
        {
            int id = 702;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Hellfire",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 14,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "SummonImp",
                PrimaryIntent = IntentType.Special,
                BaseWeight = 4
            });
        }

        private void RegisterAbyssalHorrorAbilities()
        {
            int id = 703;
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "Devour",
                PrimaryIntent = IntentType.Attack,
                BaseDamage = 18,
                BaseWeight = 10
            });
            RegisterAbility(id, new EnemyAbilityData
            {
                AbilityName = "MindShatter",
                PrimaryIntent = IntentType.NegativeEffect,
                BaseWeight = 6
            });
        }
    }
}
