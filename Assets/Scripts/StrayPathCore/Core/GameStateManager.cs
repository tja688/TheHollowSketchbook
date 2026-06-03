using System;
using System.Collections.Generic;
using StrayPathCore.Deck;
using UnityEngine;

namespace StrayPathCore.Core
{
    /// <summary>
    /// 全局运行时状态管理器 —— 替代原 Godot IWS 静态全局状态池。
    /// 采用单例 MonoBehaviour 模式，支持生命周期管理与依赖注入。
    /// 所有状态变更均通过 Publish 事件通知下游系统。
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Event Bus")]
        [SerializeField] private GameEventBus eventBus;
        public GameEventBus EventBus => eventBus ?? GameEventBus.Instance;

        // ==================== 单局运行时状态 (RunState) ====================
        [Header("Run State")]
        public RunState CurrentRun = new RunState();

        // ==================== 账户级状态 (AccountState) ====================
        [Header("Account State")]
        public AccountState CurrentAccount = new AccountState();

        // ==================== 战斗内临时状态 (内存态，不持久化) ====================
        [Header("Battle State (Transient)")]
        public BattleTransientState BattleState = new BattleTransientState();

        // ==================== 场景切换标记 ====================
        public string NextSceneName { get; set; }
        public bool IsReturningFromSubScene { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (eventBus == null)
                eventBus = GameEventBus.Instance;
        }

        // ==================== RunState 快捷访问 ====================

        public string SelectedHeroID => CurrentRun.SelectedHeroID;
        public int CurrentAct => CurrentRun.Act;
        public int CurrentGold => CurrentRun.Gold;
        public int CurrentHP => CurrentRun.CurrentHP;
        public int MaxHP => CurrentRun.MaxHP;
        public int CurrentMP => CurrentRun.CurrentMP;
        public int MaxMP => CurrentRun.MaxMP;
        public bool IsDefeated => CurrentRun.Defeated;
        public int BoostBar => CurrentRun.BoostBarValue;
        public int BoostEnergy => CurrentRun.BoostEnergy;

        // ==================== RunState 修改方法 (带事件发布) ====================

        public void SetSelectedHero(string heroID)
        {
            CurrentRun.SelectedHeroID = heroID;
        }

        public void SetAct(int act)
        {
            CurrentRun.Act = act;
        }

        public void AddGold(int amount, string reason = "")
        {
            if (amount == 0) return;
            int old = CurrentRun.Gold;
            CurrentRun.Gold = Math.Max(0, CurrentRun.Gold + amount);
            EventBus.Publish(new GoldChangedEvent { OldAmount = old, NewAmount = CurrentRun.Gold, Reason = reason });
        }

        public bool SpendGold(int amount, string reason = "")
        {
            if (CurrentRun.Gold < amount) return false;
            int old = CurrentRun.Gold;
            CurrentRun.Gold -= amount;
            EventBus.Publish(new GoldChangedEvent { OldAmount = old, NewAmount = CurrentRun.Gold, Reason = reason });
            return true;
        }

        public void SetHP(int hp)
        {
            int old = CurrentRun.CurrentHP;
            CurrentRun.CurrentHP = Mathf.Clamp(hp, 0, CurrentRun.MaxHP);
            EventBus.Publish(new HPChangedEvent { OldHP = old, NewHP = CurrentRun.CurrentHP, MaxHP = CurrentRun.MaxHP });
        }

        public void HealHP(int amount, string reason = "")
        {
            if (amount <= 0) return;
            int old = CurrentRun.CurrentHP;
            CurrentRun.CurrentHP = Mathf.Min(CurrentRun.CurrentHP + amount, CurrentRun.MaxHP);
            EventBus.Publish(new HPChangedEvent { OldHP = old, NewHP = CurrentRun.CurrentHP, MaxHP = CurrentRun.MaxHP });
            EventBus.Publish(new HealEvent { TargetUID = "hero", Amount = amount, CurrentHP = CurrentRun.CurrentHP });
        }

        public void DamageHP(int amount, string reason = "")
        {
            if (amount <= 0) return;
            int old = CurrentRun.CurrentHP;
            CurrentRun.CurrentHP = Mathf.Max(0, CurrentRun.CurrentHP - amount);
            EventBus.Publish(new HPChangedEvent { OldHP = old, NewHP = CurrentRun.CurrentHP, MaxHP = CurrentRun.MaxHP });
            if (CurrentRun.CurrentHP <= 0)
            {
                CurrentRun.Defeated = true;
            }
        }

        public void SetMaxHP(int maxHp, bool healToFull = false)
        {
            CurrentRun.MaxHP = maxHp;
            if (healToFull)
                CurrentRun.CurrentHP = maxHp;
            else
                CurrentRun.CurrentHP = Mathf.Min(CurrentRun.CurrentHP, maxHp);
        }

        public void SetMP(int mp)
        {
            int old = CurrentRun.CurrentMP;
            CurrentRun.CurrentMP = Mathf.Clamp(mp, 0, CurrentRun.MaxMP);
            EventBus.Publish(new EnergyChangedEvent { OldValue = old, NewValue = CurrentRun.CurrentMP, Reason = "direct" });
        }

        public void SetMaxMP(int maxMp)
        {
            CurrentRun.MaxMP = maxMp;
            CurrentRun.CurrentMP = Mathf.Min(CurrentRun.CurrentMP, maxMp);
        }

        public void AddBoostBar(int amount)
        {
            CurrentRun.BoostBarValue = Mathf.Min(CurrentRun.BoostBarValue + amount, 20);
            if (CurrentRun.BoostBarValue >= 20)
            {
                CurrentRun.BoostBarValue -= 20;
                CurrentRun.BoostEnergy++;
            }
        }

        public void ConsumeBoostEnergy()
        {
            if (CurrentRun.BoostEnergy > 0)
            {
                CurrentRun.BoostEnergy--;
                CurrentRun.BoostBarValue = 0;
            }
        }

        // ==================== 地图状态 ====================

        public void SetPathGroup(int pg)
        {
            CurrentRun.CurrentPlayerPathGroup = pg;
        }

        public void SetPID(int pid)
        {
            CurrentRun.CurrentPID = pid;
        }

        public void AddToPathHistory(int nodeId)
        {
            if (!CurrentRun.PathHistory.Contains(nodeId))
                CurrentRun.PathHistory.Add(nodeId);
        }

        // ==================== 牌组操作 ====================

        public void SetDeckCards(List<CardRuntime> cards)
        {
            CurrentRun.DeckCards = new List<CardRuntime>(cards);
        }

        public void AddCardToDeck(CardRuntime card)
        {
            CurrentRun.DeckCards ??= new List<CardRuntime>();
            CurrentRun.DeckCards.Add(card);
        }

        public bool RemoveCardFromDeck(int cardId, int copyCount)
        {
            if (CurrentRun.DeckCards == null) return false;
            var card = CurrentRun.DeckCards.Find(c => c.CardID == cardId && c.CopyCount == copyCount);
            if (card != null)
            {
                CurrentRun.DeckCards.Remove(card);
                return true;
            }
            return false;
        }

        // ==================== 遗物操作 ====================

        public void AddRelic(RelicRuntime relic)
        {
            CurrentRun.Relics ??= new List<RelicRuntime>();
            CurrentRun.Relics.Add(relic);
        }

        public bool RemoveRelic(int relicId)
        {
            if (CurrentRun.Relics == null) return false;
            var relic = CurrentRun.Relics.Find(r => r.RelicID == relicId);
            if (relic != null)
            {
                CurrentRun.Relics.Remove(relic);
                return true;
            }
            return false;
        }

        public RelicRuntime GetRelic(int relicId)
        {
            return CurrentRun.Relics?.Find(r => r.RelicID == relicId);
        }

        public bool HasRelic(int relicId)
        {
            return CurrentRun.Relics?.Exists(r => r.RelicID == relicId && r.IsActive) ?? false;
        }

        // ==================== 法术操作 ====================

        public void AddSpell(int spellId)
        {
            if (CurrentRun.Spells == null) CurrentRun.Spells = new List<int>();
            if (!CurrentRun.Spells.Contains(spellId))
                CurrentRun.Spells.Add(spellId);
        }

        // ==================== 账户级操作 ====================

        public void AddHeroXP(string heroID, int xp)
        {
            if (!CurrentAccount.HeroXP.ContainsKey(heroID))
                CurrentAccount.HeroXP[heroID] = 0;
            CurrentAccount.HeroXP[heroID] += xp;
            // 检查升级
            int currentLevel = CurrentAccount.HeroLevels.GetValueOrDefault(heroID, 1);
            int requiredXP = currentLevel * 100;
            while (CurrentAccount.HeroXP[heroID] >= requiredXP && currentLevel < 10)
            {
                CurrentAccount.HeroXP[heroID] -= requiredXP;
                currentLevel++;
                requiredXP = currentLevel * 100;
            }
            CurrentAccount.HeroLevels[heroID] = currentLevel;
        }

        public int GetHeroLevel(string heroID)
        {
            return CurrentAccount.HeroLevels.GetValueOrDefault(heroID, 1);
        }

        // ==================== 存档 / 读档 ====================

        public void SaveRunState()
        {
            SaveSystem.SaveRunState(CurrentRun);
        }

        public void LoadRunState()
        {
            var loaded = SaveSystem.LoadRunState();
            if (loaded != null)
                CurrentRun = loaded;
        }

        public void SaveAccountState()
        {
            SaveSystem.SaveAccountState(CurrentAccount);
        }

        public void LoadAccountState()
        {
            var loaded = SaveSystem.LoadAccountState();
            if (loaded != null)
                CurrentAccount = loaded;
        }

        public void ClearRunState()
        {
            CurrentRun = new RunState();
            BattleState = new BattleTransientState();
        }

        // ==================== 战斗内临时状态管理 ====================

        public void ResetBattleState()
        {
            BattleState = new BattleTransientState();
        }
    }

    // ==================== 数据类定义 ====================

    [Serializable]
    public class RunState
    {
        public string SelectedHeroID = "";
        public int Act = 1;
        public int Gold = 0;
        public int CurrentHP = 80;
        public int MaxHP = 80;
        public int CurrentMP = 3;
        public int MaxMP = 3;
        public bool Defeated = true; // 默认true表示无存档
        public int BoostBarValue = 0;
        public int BoostEnergy = 0;
        public int CurrentPID = 0;
        public int CurrentPlayerPathGroup = 0;
        public List<int> PathHistory = new List<int>();
        public int MapScroll = 0;
        public int PersonalID = 0;
        public int BattleType = 1; // 1=Normal, 2=Elite, 3=Boss
        public bool BattleIsFake = false;
        public List<int> MysteryEventHistory = new List<int>();
        public List<int> Spells = new List<int>();
        public List<CardRuntime> DeckCards = new List<CardRuntime>();
        public List<RelicRuntime> Relics = new List<RelicRuntime>();
        public int RunTime = 0;
        public int TotalPainDuringRun = 0;
        public int TotalGoldDuringRun = 0;
        public int NormalBattleAmount = 0;
        public int HardBattleAmount = 0;
        public int BossBattleAmount = 0;
        public int MysteryEventAmount = 0;
        public int TauntAmount = 0;
        public int LesserCurseAmount = 0;
        public int ModerateCurseAmount = 0;
        public int GreaterCurseAmount = 0;
        public List<int> ActiveCurses = new List<int>();
        public Dictionary<string, int> InfinityCharges = new Dictionary<string, int>();
        public Dictionary<string, int> HellfireCharges = new Dictionary<string, int>();
        public Dictionary<string, int> OmniCharges = new Dictionary<string, int>();
        public int EasyHeroHP = 0;
        public int EasyEnemyHP = 0;
        public int EasyGold = 0;
        public bool TreasureMapAct1 = false;
        public bool GenieGoldCurse = false;
        public bool GenieRelicCurse = false;
        // 地图节点图标数组 (Act × PathGroup)
        public List<int> IconArrayPG1_Act1 = new List<int>();
        public List<int> IconArrayPG2_Act1 = new List<int>();
        public List<int> IconArrayPG3_Act1 = new List<int>();
        public List<int> IconArrayPG1_Act2 = new List<int>();
        public List<int> IconArrayPG2_Act2 = new List<int>();
        public List<int> IconArrayPG3_Act2 = new List<int>();
        public List<int> IconArrayPG1_Act3 = new List<int>();
        public List<int> IconArrayPG2_Act3 = new List<int>();
        public List<int> IconArrayPG3_Act3 = new List<int>();
        // 各Act遭遇战ID去重池
        public List<int> EncounterIDs_Act1 = new List<int>();
        public List<int> EncounterIDs_Act2 = new List<int>();
        public List<int> EncounterIDs_Act3 = new List<int>();
    }

    [Serializable]
    public class AccountState
    {
        public bool IsCheater = false;
        public int RunAmount = 0;
        public bool TutorialViewed = false;
        public bool MapTutorialViewed = false;
        public Dictionary<string, int> HeroLevels = new Dictionary<string, int>();
        public Dictionary<string, int> HeroXP = new Dictionary<string, int>();
        public List<bool> CurseUnlocked = new List<bool>(new bool[15]); // 15种诅咒
        public List<bool> CurseConquered = new List<bool>(new bool[15]);
        public string Language = "EN";
        public int ResolutionSetting = 1;
        public bool VSync = true;
        public float MasterVolume = 1.0f;
        public float MusicVolume = 0.75f;
        public float SFXVolume = 0.75f;
        public bool ShowRunTimer = false;
        public List<ScoreEntry> HighScores = new List<ScoreEntry>();
        public List<ScoreEntry> BestRuntimes = new List<ScoreEntry>();
    }

    [Serializable]
    public class BattleTransientState
    {
        public int PlayerTurn = 0;
        public int EnemyTurn = 0;
        public bool IsPlayerTurn = true;
        public int CurrentEnergy = 3;
        public int CurrentMaxEnergy = 3;
        public int CurrentBlock = 0;
        public int CurrentHeroPower = 0;
        public int CurrentHeroToughness = 0;
        public int CurrentHeroArmor = 0;
        public int CurrentHeroThorns = 0;
        public int CurrentHeroCrit = 0;
        public int CurrentHeroHaste = 0;
        public int CurrentHeroSlow = 0;
        public int BleedStacks = 0;
        public int DemonicBrandStacks = 0;
        public int EnchantedArmorStacks = 0;
        public int IllusionStacks = 0;
        public int StatusProtectStacks = 0;
        public bool ComboActive = false;
        public bool OnslaughtActive = false;
        public bool FinisherActive = false;
        public bool BoostActive = false;
        public bool BoostedAttackPierce = false;
        public bool SensoryOverload = false;
        public bool SensoryOverloadUpgraded = false;
        public bool SweepingStrikes = false;
        public bool Juggernaut = false;
        public bool GrowingPower = false;
        public bool Motivation = false;
        public bool AdrenalineRush = false;
        public bool Berserk = false;
        public bool FreezingBarrier = false;
        public bool BurningBarrier = false;
        public bool IceClone = false;
        public bool FireRadiance = false;
        public bool Panic = false;
        public bool ArcaneTrance = false;
        public bool ArcaneSecrets = false;
        public int ArcaneDestructionCharges = 0;
        public bool Overheat = false;
        public int CounterMeasures = 0;
        public bool TemporalStasisActive = false;
        public List<string> StunnedCards = new List<string>();
        public Dictionary<string, int> WeakeningCounters = new Dictionary<string, int>();
        public List<Action> StartOfTurnEffects = new List<Action>();
        public List<Action> EndOfTurnEffects = new List<Action>();
        public bool SpellCastThisTurn = false;
        public Dictionary<int, bool> BossSpellActivated = new Dictionary<int, bool>();
        public string CurrentRulemakerRule = "";
        public bool RulemakerSucceeded = false;
        public int CardsPlayedThisTurn = 0;
        public int AttackCardsPlayedThisTurn = 0;
        public int DefenseCardsPlayedThisTurn = 0;
        public int ZeroCostCardsPlayedThisTurn = 0;
        public int CardsDiscardedThisTurn = 0;
        public int CardsBanishedThisTurn = 0;
        public int CardsExhaustedThisTurn = 0;
        // Boss 特殊标记
        public bool DragonHasPrepared = false;
        public bool WitchDoctorHasPrepared = false;
        public bool WitchDoctorHasHealed = false;
        public bool HobGoblinFury = false;
        public int CurseOfDoomCD = 0;
        public bool GiantSnake = false;
        public bool Corruptor = false;
        public bool WitchDoctor = false;
        public bool Golem = false;
        public bool Lich = false;
        public bool Wraith = false;
        public bool Dragon = false;
        public bool Phoenix = false;
        public bool Demons = false;
    }

    [Serializable]
    public class ScoreEntry
    {
        public int Score;
        public ulong Time;
        public int CurseAmount;
        public int TauntAmount;
        public string Date;
        public string Hero;
        public int State; // 1=Defeat, 2=Victory
    }

    [Serializable]
    public class CardRuntime
    {
        public int CardID;
        public int CopyCount;
        public int ExtraUpgrades;
        public bool IsUpgraded;
        public bool IsBanished;
        public bool IsFake;
    }

    [Serializable]
    public class RelicRuntime
    {
        public int RelicID;
        public bool IsActive;
        public int CurrentCharges;
    }
}
