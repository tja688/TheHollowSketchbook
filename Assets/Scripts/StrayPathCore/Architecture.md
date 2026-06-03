# StrayPathCore — 架构设计文档

> **版本**：v1.0  
> **日期**：2026-06-03  
> **范围**：`Assets/Scripts/StrayPathCore` 全部代码  
> **对照**：原始设计案 `Seal The Rift — 完整设计还原文档`（11 份专项设计文档）

---

## 1. 架构总览

### 1.1 设计目标

本架构基于原始 Godot 项目 `Seal The Rift`（~126K 行 C#，对标 Slay the Spire）的完整逆向工程，进行了以下核心净化与重构：

1. **消除上帝类**：将 `BattleManager`(10K 行)、`Mystery`(24K 行)、`CardEffects`(19K 行) 拆分为职责单一的子系统。
2. **消除静态全局状态污染**：将纯静态 `IWS` 替换为分层状态管理 + 事件总线。
3. **消除硬编码爆炸**：将卡牌、敌人、遗物、事件等数据提取为 `ScriptableObject`。
4. **消除侵入式遗物集成**：将遗物硬编码分支替换为非侵入式事件订阅。
5. **消除状态双重存储**：引入 `StatusEffectSystem` 作为唯一状态源。

### 1.2 分层架构

```
┌─────────────────────────────────────────────────────────────┐
│                    表现层 (Presentation)                       │
│  BattleUIManager / PlayerHandDisplay / CardUIProxy / ...      │
│  纯订阅事件刷新，不持有业务状态源                               │
├─────────────────────────────────────────────────────────────┤
│                    事件总线 (Event Bus)                        │
│  GameEventBus —— 所有跨模块通信的唯一通道                      │
│  支持强引用订阅 + 弱引用订阅（防内存泄漏）                      │
├─────────────────────────────────────────────────────────────┤
│                 核心系统层 (Gameplay Systems)                  │
│  BattleStateMachine / DeckManager / RelicManager / ...        │
│  单例 MonoBehaviour，职责单一，通过事件总线发布状态变更         │
├─────────────────────────────────────────────────────────────┤
│                 数据实体层 (Data & Entities)                   │
│  ICombatEntity / HeroCombatEntity / EnemyCombatEntity         │
│  CardData / EnemyData / HeroData / RelicData (ScriptableObject)│
├─────────────────────────────────────────────────────────────┤
│              持久化与配置层 (Persistence & Config)              │
│  GameStateManager (RunState / AccountState / BattleTransient)  │
│  SaveSystem (Newtonsoft.Json + 版本头 + AES 加密)              │
│  Configuration (分辨率/音量/VSync/语言)                        │
└─────────────────────────────────────────────────────────────┘
```

### 1.3 依赖原则

| 原则 | 说明 |
|------|------|
| **上层依赖下层** | UI → 事件总线 → 核心系统 → 数据实体 → 持久化 |
| **同层不直接调用** | 核心系统间不直接调用，通过 `GameEventBus` 通信 |
| **表现层单向依赖** | UI 只订阅事件，不反向修改业务系统（通过事件请求） |
| **数据驱动配置** | 所有玩法内容使用 ScriptableObject，逻辑代码只处理行为 |

---

## 2. 核心系统详解

### 2.1 全局基础设施层（`Core/`）

#### GameStateManager —— 三层状态架构

替代原始 `IWS`（纯静态全局状态池），采用显式分层：

```csharp
public class GameStateManager : MonoBehaviour
{
    public RunState CurrentRun;           // 单局持久化（HP、金币、牌组、Act...）
    public AccountState CurrentAccount;   // 账户级永久保留（等级、排行榜、解锁）
    public BattleTransientState BattleState; // 战斗内内存态（手牌、能量、意图）
}
```

**与原始设计的对比**：
- 原始：`IWS.AssignGold(x)` 直接读写加密 ConfigFile，任何模块随时调用。
- 现在：所有修改通过 `GameStateManager` 的方法执行，自动发布对应事件（如 `GoldChangedEvent`）。
- 优势：状态变更可追踪、可订阅、可调试；不再存在"某处偷偷改了全局状态"的隐式耦合。

#### GameEventBus —— 全局事件总线

替代 Godot 的 `signal/connect/emit` 系统，Unity 化实现：

- **强引用订阅**：`Subscribe<T>(Action<T>)`，性能最优，需手动 `Unsubscribe`
- **弱引用订阅**：`SubscribeWeak<T>(Action<T>)`，推荐 `MonoBehaviour` 使用，防内存泄漏
- **事件结构体**：所有事件为 `struct`，避免 GC 分配

**关键事件定义**：
```csharp
public struct BattleStartedEvent { public int ActID; public int BattleType; }
public struct CardPlayedEvent { public int CardID; public string TargetUID; }
public struct DamageDealtEvent { public string SourceUID; public string TargetUID; public int Damage; }
public struct TurnStartedEvent { public bool IsPlayerTurn; public int TurnNumber; }
```

#### SaveSystem —— 存档系统

- **序列化器**：Newtonsoft.Json（支持 `Dictionary`，替代原始 `JsonUtility`）
- **版本头**：`SaveFileHeader` 含版本号，支持未来迁移
- **加密**：可选 AES 对称加密（替代原始 Godot `ConfigFile.SaveEncryptedPass`）
- **文件**：`run_save.json` / `account_save.json` / `config_save.json`

### 2.2 数据定义层（`Data/`）

所有数据均使用 `ScriptableObject`，支持编辑器配置：

| 数据类 | 对应原始设计 | 核心改进 |
|--------|-------------|---------|
| `CardData` | `Card` 类（硬编码在 C# 中） | 编辑器可配置，支持升级双版本、关键词、效果方法名映射 |
| `EnemyData` | `Enemy` 类（硬编码字典） | 编辑器可配置，支持体型、特性列表、AI Profile 引用 |
| `HeroData` | `Hero` 类 | 编辑器可配置，支持按等级解锁内容、被动效果名 |
| `EnemyAIProfile` | `EnemyAbilities`（4093 行上帝类） | 将技能列表、权重、条件提取为数据资产 |
| `RelicData` | `Relic` 类 | 编辑器可配置，支持触发时机列表、充能、英雄等级锁 |
| `EventData` | `Mystery` 中硬编码事件（24K 行） | 将事件描述、选项、结果提取为数据资产 |

**卡牌 ID 编码规则**（继承原始设计）：
- 基础卡牌：`1 ~ N`
- 升级卡牌：`基础 ID + 1000`（如 `1001` 是 `1` 的升级版本）

### 2.3 战斗系统（`Combat/`）

#### BattleStateMachine —— 严格枚举状态机

替代原始 `BattleManager` 中大量的布尔标志（`isPlayerTurnOver`、`isEnemyTurnOver` 等）：

```csharp
public enum BattlePhase
{
    BattleStart,
    PlayerTurnStart, PlayerTurn, PlayerTurnEnd,
    EnemyTurnStart, EnemyTurn, EnemyTurnEnd,
    BattleEnd
}
```

状态切换由 `EnterPhase(BattlePhase)` 统一驱动，每个阶段执行固定的钩子序列。

#### ICombatEntity + CombatEntity —— 战斗实体契约

```csharp
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
```

- `HeroCombatEntity`（MonoBehaviour）和 `EnemyCombatEntity`（MonoBehaviour）均实现此接口
- 所有伤害计算面向接口编程，彻底解耦英雄与敌人的差异
- 原始设计中 `SpawnHero` / `SpawnEnemy` 分别处理伤害计算，逻辑高度重复；现在统一在 `DamageCalculator` 中

#### DamageCalculator —— 统一伤害计算

提供 `CalculatePlayerDamageToEnemy`、`CalculateEnemyDamageToHero`、`PreviewEnemyDamageToHero` 三个核心方法。

**伤害公式（精简）**：
```
BaseDamage
  × fragileMult (+30% if target has Fragile)
  × critMult (+50% if Crit active)
  × comboMult (+20~40% if Combo card)
  × finisherMult (+40% if Finisher active)
  × boostMult (+50~75% if Boost active)
  × weakMult (-30% if attacker has Weak)
  × spectralMult (-50% if target has SpectralForm)
  - Armor (fixed reduction)
  → Apply Block absorption
  → Apply HP deduction
```

`PreviewEnemyDamageToHero` 与真实伤害使用同一套修正逻辑，确保意图显示准确。

### 2.4 状态效果系统（`Status/`）

#### StatusEffectSystem —— 唯一状态源

原始设计中，Buff/Debuff 状态分散在 6+ 个类中（`SpawnHero`、`SpawnEnemy`、`BattleManager`、`PlayerHandDisplay`、`DeckManager`、`RelicManager`），`AfflictionManager` 不得不执行数十次跨模块拉取才能刷新 UI。

现在，所有状态统一由 `StatusEffectSystem` 管理：

```csharp
public class StatusEffectSystem : MonoBehaviour
{
    // 唯一状态源：目标 UID → 状态列表
    private Dictionary<string, List<StatusEffect>> _entityEffects = new ...;
    
    public void ApplyEffect(string targetUID, StatusEffect effect);
    public void RemoveEffect(string targetUID, StatusEffectType type);
    public void ReduceStack(string targetUID, StatusEffectType type, int amount);
    public void DecayTurnBasedEffects(string targetUID);
    public List<StatusEffect> GetEffects(string targetUID);
}
```

`HeroCombatEntity` / `EnemyCombatEntity` 中的状态属性（如 `WeakStacks`、`BleedStacks`）均改为**实时查询** `StatusEffectSystem`，彻底消除双重存储。

#### 状态持续时间类型（继承原始设计的四分法）

| 类型 | 衰减规则 | 示例 |
|------|---------|------|
| **Continuous** | 战斗结束或特定条件清除 | SensoryOverload, Berserk |
| **TurnBased** | 每回合开始/结束 -1，到 0 清除 | Fragile, Weak, BrokenGuard, Burn |
| **ChargeBased** | 触发一次 -1，到 0 清除 | Haste, Slow, Crit, StatusProtect, EnchantedArmor |
| **StackBased** | 按效果自身规则衰减 | Power, Toughness, Armor, Chill, Bleed |

### 2.5 牌组与能量系统（`Deck/`）

#### DeckManager —— 五堆模型

继承原始的"五堆牌组模型"，但将 `List<Card>` 中的 `Card` 从硬编码 C# 类替换为 `CardInstance`（运行时实例）+ `CardData`（ScriptableObject 模板）的分离架构：

```csharp
public class CardInstance
{
    public CardData Data;           // 引用 ScriptableObject 模板
    public int CopyCount;           // 实例唯一键的一部分
    public bool IsUpgraded;
    public bool IsBanished;
    public bool IsFake;             // 战斗内临时卡
    public int ExtraUpgrades;
    public int RemainingBanishCharges;
}
```

**关键操作**：
- `DrawCards(int count)`：自动处理抽牌堆耗尽时的洗牌补牌
- `DiscardCard(CardInstance card)`：根据放逐规则判定去向（Discard / Banish）
- `CreateFakeCard(int cardID)`：战斗内动态生成临时卡，自动分配 CopyCount

#### CardEffectDispatcher —— 效果分发器

替代原始 `CardEffects.cs`（19,420 行巨型类）中的 switch-case：

```csharp
public class CardEffectDispatcher : MonoBehaviour
{
    private Dictionary<int, Action<EnemyCombatEntity>> enemyCardEffectDict;
    private Dictionary<int, Action> heroCardEffectDict;
    private Dictionary<int, Action<CardInstance, EnemyCombatEntity>> advEnemyCardEffectDict;
    private Dictionary<int, Action<CardInstance>> advHeroCardEffectDict;
    
    public void RegisterCoreEffects() { /* 注册数十张核心卡牌效果 */ }
}
```

每张卡牌的效果是一个独立的 C# 方法，通过字典委托注册。新增卡牌只需：
1. 创建 `CardData` ScriptableObject
2. 在 `RegisterCoreEffects()` 中添加字典条目

#### EnergyManager —— 能量管理

管理当前/最大能量，计算卡牌实际费用（受 Combo、OnSlaught、Slow、Haste 影响）：

```csharp
public int GetEffectiveCost(CardInstance card)
{
    int cost = card.IsUpgraded ? card.Data.UpgradedEnergyCost : card.Data.EnergyCost;
    if (ComboActive && card.Data.IsCombo) cost--;
    if (OnslaughtActive) cost = 0;
    if (SlowStacks > 0) cost++;
    if (HasteStacks > 0 && cost > 0) cost--;
    return Mathf.Max(cost, 0);
}
```

### 2.6 AI 系统（`AI/`）

#### EnemyAbilityRegistry —— 技能注册表

替代原始 `EnemyAbilities.cs`（4093 行，40+ 敌人全部硬编码在一个类中）：

```csharp
public class EnemyAbilityRegistry : MonoBehaviour
{
    // enemyID → 技能列表
    private Dictionary<int, List<EnemyAbilityData>> _registry = new ...;
    
    public void RegisterDefaults() { /* 注册默认技能模板 */ }
    public List<EnemyAbilityData> GetAbilities(int enemyID);
    public EnemyAbilityData GetAbilityByName(int enemyID, string abilityName);
}
```

每个敌人的技能列表通过 `EnemyAIProfile`（ScriptableObject）配置，运行时注册到 Registry 中。

#### EnemyAIBehavior —— AI 核心

负责技能选择、意图显示与实际执行：

- **技能选择**：条件判定（回合数、HP%、Buff/Debuff、随机概率）+ 加权随机
- **历史行为惩罚**：`lastUsedAbilitiesForEnemy` 存储最近 2 次使用的技能，避免连续重复
- **准备技能链**：支持 "Prepare → 高伤害" 两阶段行为（如 WitchDoctor、Dragon）

#### IntentSystem —— 意图显示

维护 `CurrentIntents` 字典（EnemyUID → `IntentData`），提供：
- 意图图标与数值文本（支持多 hit 显示 `Nx D`）
- 本地化 Tooltip 文本
- Tween 动画（浮动、切换特效）

**与原始设计的对比**：原始 `EnemyAbilities` 中每个技能方法充斥着 `if/else` 分支处理三种意图状态；现在将意图显示与执行分离到 `IntentSystem` 和 `EnemyAIBehavior` 中，技能数据仅定义数值与效果。

### 2.7 遗物系统（`Relic/`）

#### RelicTriggerSystem —— 非侵入式事件订阅

原始设计中，遗物效果通过 `RelicID` 硬编码在 `BattleManager` / `SpawnHero` / `SpawnEnemy` 等各处：

```csharp
// 原始反模式（已消除）
Relic relic = relicManager.GetRelic(73);
if (relic != null && GetPlayerTurn() != 1)
    spawnHero.PrismaticEnergy();
```

现在，遗物通过 `RelicTriggerSystem` 统一订阅事件：

```csharp
public class RelicTriggerSystem : MonoBehaviour
{
    // 按触发时机分组的回调注册表
    private Dictionary<RelicTriggerTiming, List<RelicTriggerCallback>> _triggers;
    
    // 缓存上下文（最后打出的卡牌、进入的节点、能量变化）
    public CardInstance LastPlayedCard { get; private set; }
    public MapNodeType LastEnteredNode { get; private set; }
    
    public void RegisterRelic(RelicData relic);
    public void OnBattleStarted(BattleStartedEvent evt);
    public void OnCardPlayed(CardPlayedEvent evt);
    public void OnTurnStarted(TurnStartedEvent evt);
    // ...
}
```

**十大触发时机**（继承原始设计）：
1. 战斗开始 (StartOfBattle)
2. 回合开始 (StartOfTurn)
3. 回合结束 (EndOfTurn)
4. 打出卡牌 (OnCardPlayed)
5. 受到伤害 (OnDamageTaken)
6. 击杀敌人 (OnKill)
7. 进入节点 (OnNodeVisit)
8. 抽牌 (OnDraw)
9. 能量变化 (OnEnergyChange)
10. 洗牌 (OnDeckShuffle)

#### CurseSystem —— 诅咒系统

管理运行时诅咒列表，提供全局倍率查询：
- 金币倍率（Greed / Poverty）
- 商店价格倍率
- 营地恢复倍率
- 卡牌奖励数量
- MP 消耗增加

### 2.8 地图系统（`Map/`）

#### MapGenerator —— 地图生成器

按 Act 生成 3 条路径组（PG1/PG2/PG3），各 15 节点：

- **第 8 节点**：固定宝藏（Treasure）
- **第 15 节点**：固定 Boss
- **难度分配**：三条路径随机分配 Easy/Medium/Hard（无放回）
- **子路径洗牌**：子路径 1（节点 1–7）和子路径 2（节点 8–15）跨路径组随机洗牌

节点 ID 编码规则：`PG * 100 + index`（如 PG1 的第 5 节点 = 105）。

#### PathSystem —— 路径规则

静态类，提供移动合法性判断：
- 同一路径组
- 未访问过
- 顺序 +1

#### NodeRouter —— 场景路由

将 `MapNodeType` 映射到场景名，调用 `SceneTransitionManager` 切换：
- Battle / Shop / Mystery / Campfire / Treasure

#### ActSystem —— Act 推进

管理 3 个 Act 的切换，跨 Act 状态继承：
- **保留**：HP/MP、金币、牌组、遗物、诅咒
- **重置**：当前节点、路径组、地图滚动位置

### 2.9 事件节点系统（`EventNodes/`）

原始 `Mystery.cs`（24,469 行）被拆分为 6 个独立系统：

| 系统 | 职责 | 原始对应 |
|------|------|---------|
| `MysteryEventSystem` | 神秘事件：按 Act 分池、去重、四阶段流水线 | `Mystery` 事件逻辑 |
| `ShopSystem` | 商店：卡牌区（5+1）、遗物区（3）、升级服务 | `ShopManager` |
| `CampfireSystem` | 营地：休息恢复 HP / 燃烧删卡 | `CampfireChoice` |
| `TreasureSystem` | 宝藏：遗物 + 金币固定收益 | `TreasureManager` |
| `OldManSystem` | 老者：开局礼物 6 选 1 | `OldMan` |
| `ScoreboardSystem` | 计分：基础分 × 修正系数、XP、排行榜 | `Scoreboard` |

### 2.10 UI 系统（`UI/`）

#### BattleUIManager —— 战斗 UI 总控

协调所有子模块，管理目标选择状态机：
1. 玩家点击手牌
2. 若卡牌需要目标 → 进入 TargetingMode
3. 敌人高亮 → 玩家点击敌人 → 出牌执行

所有子模块通过订阅 `GameEventBus` 事件自动刷新，不直接查询业务系统：
- `PlayerHandDisplay`：订阅 `CardDrawnEvent`、`CardDiscardedEvent`、`HandRefreshedEvent`
- `HeroDisplay`：订阅 `DamageTakenEvent`、`BlockGainedEvent`、`HealEvent`
- `EnemyDisplay`：订阅 `EnemySpawnedEvent`、`EnemyDamagedEvent`、`EnemyIntentChangedEvent`

---

## 3. 从原始设计到落地的架构演进

### 3.1 演进总览

| 原始反模式 | 落地改进 | 关键文件 |
|-----------|---------|---------|
| **IWS 纯静态全局状态池** | `GameStateManager` 三层状态 + `GameEventBus` 事件通知 | `Core/GameStateManager.cs`, `Core/GameEventBus.cs` |
| **即时持久化（每次状态变更写磁盘）** | 内存态运行 + 显式存档点（战斗结束/场景切换） | `Core/SaveSystem.cs` |
| **硬编码卡牌/敌人/遗物数据** | `ScriptableObject` 数据驱动 | `Data/*.cs` |
| **BattleManager 上帝类（10K 行）** | 拆分为 `BattleStateMachine` + `DamageCalculator` + `BoostSystem` + `CombatRewardSystem` | `Combat/*.cs` |
| **Mystery 上帝类（24K 行）** | 拆分为 6 个独立事件系统 + `EventData` ScriptableObject | `EventNodes/*.cs` |
| **CardEffects 巨型 switch（19K 行）** | `CardEffectDispatcher` 字典委托分发 | `Deck/CardEffectDispatcher.cs` |
| **EnemyAbilities 上帝类（4K 行）** | `EnemyAbilityRegistry` + `EnemyAIProfile` ScriptableObject | `AI/EnemyAbilityRegistry.cs`, `Data/EnemyAIProfile.cs` |
| **碎片化状态管理（6+ 类分散存储）** | `StatusEffectSystem` 唯一状态源 | `Status/StatusEffectSystem.cs` |
| **侵入式遗物硬编码（RelicID 遍布代码）** | `RelicTriggerSystem` 非侵入式事件订阅 | `Relic/RelicTriggerSystem.cs` |
| **FindObjectOfType 滥用** | 单例引用 + `BattleStateMachine` 公开查询接口 | `Combat/BattleStateMachine.cs` |
| **双重存储（实体属性 + Buff 列表）** | 实体属性实时查询 `StatusEffectSystem` | `Combat/HeroCombatEntity.cs`, `Combat/EnemyCombatEntity.cs` |
| **Godot ConfigFile（INI-like）** | Newtonsoft.Json + 版本头 + AES 加密 | `Core/SaveSystem.cs` |
| **布尔标志爆炸** | 严格枚举状态机（`BattlePhase`） | `Combat/BattleStateMachine.cs` |
| **8 语言硬编码字典（UI 代码 70% 是文本）** | Unity Localization 系统 + ScriptableObject 多语言字段 | `Data/*.cs` |

### 3.2 设计决策记录（ADR）

#### ADR-001：为什么保留单例模式而非依赖注入？

**背景**：原始 Godot 项目中大量系统为 Node 树节点，通过 `GetNode` 获取引用。Unity 中可考虑依赖注入框架。

**决策**：保留单例模式，但限制使用范围：
- **允许单例**：全局基础设施（`GameStateManager`、`GameEventBus`、`SaveSystem`）、跨场景 Manager（`DeckManager`、`RelicManager`）
- **禁止单例**：表现层 UI、战斗实体（Hero/Enemy）、一次性工具类

**理由**：
1. 项目规模适中（~50 个脚本），DI 框架的学习成本与配置开销不划算。
2. Unity 的 Inspector 拖拽引用在场景级别足够表达局部依赖。
3. 单例模式在 Roguelike 卡牌游戏中是行业惯例（参考 Slay the Spire 的 Mod 社区架构）。

#### ADR-002：为什么卡牌效果仍用 C# 硬编码而非可视化脚本？

**背景**：`CardEffectDispatcher` 通过字典将 `CardID` 映射到 C# 委托。

**决策**：保留 C# 硬编码效果，但数据（数值、名称、描述、稀有度）提取为 ScriptableObject。

**理由**：
1. 卡牌效果需要高度自由的逻辑（条件判断、随机目标、延迟副作用、跨系统调用），可视化脚本或纯数据配置难以表达。
2. 每张卡牌效果代码量适中（平均 10~30 行），维护成本可控。
3. 新增卡牌的工作流已优化为：配置 `CardData` + 注册委托（2 个步骤）。

**未来扩展点**：可引入 `ICardEffect` 接口 + 继承体系，将效果逻辑拆分为可复用的 Effect 组件。

#### ADR-003：为什么事件总线使用 `struct` 而非 `class`？

**决策**：所有事件定义为 `struct`。

**理由**：
1. 战斗内事件高频发布（每回合数十次），`struct` 在栈上分配，避免 GC 压力。
2. 事件为一次性消费，不存在生命周期管理问题。
3. 事件字段均为值类型或字符串，浅拷贝安全。

#### ADR-004：为什么 `StatusEffectSystem` 使用字符串 UID 而非对象引用？

**决策**：状态存储的 Key 为 `string`（目标 UID）而非 `MonoBehaviour` 引用。

**理由**：
1. 敌人实体可能在战斗中被销毁（死亡动画后 `Destroy`），但状态需要保留到动画结束。
2. 支持跨场景的状态持久化（如 Burn 层数、Chill 层数需要在战斗间保留的场景，通过 `GameStateManager` 序列化）。
3. 避免 `MonoBehaviour` 被销毁后引用失效导致的空指针异常。

---

## 4. 数据流与事件机制

### 4.1 典型数据流：玩家打出一张攻击牌

```
[Input] 玩家点击手牌
  → CardUIProxy.OnClick()
    → BattleUIManager 检查目标需求
      → 若需目标：进入 TargetingMode，敌人高亮
        → 玩家点击敌人
          → BattleUIManager 确认目标
            → EnergyManager.ConsumeEnergy()
              → Publish(EnergyChangedEvent)
            → CardEffectDispatcher.ExecuteEffect(cardID, enemy)
              → DamageCalculator.CalculatePlayerDamageToEnemy()
                → 查询 StatusEffectSystem（Fragile/Crit/Weak/Boost...）
                → 计算最终伤害值
              → EnemyCombatEntity.TakeDamage(damage)
                → 消耗 Block → Publish(BlockConsumedEvent)
                → 扣除 HP → Publish(DamageTakenEvent)
              → Publish(CardPlayedEvent)
                → RelicTriggerSystem 检查 OnCardPlayed 触发
                → BattleStateMachine 更新 Combo/Finisher 计数
            → DeckManager.ProcessDiscard(card)
              → 判定去向（Discard / Banish）
              → Publish(CardDiscardedEvent)
              → PlayerHandDisplay 刷新手牌布局
```

### 4.2 典型数据流：敌人回合执行意图

```
[BattleStateMachine] EnterPhase(EnemyTurnStart)
  → 逐敌人执行 SelectedActions
    → EnemyAIBehavior.ExecuteAbility(enemy, ability)
      → DamageCalculator.CalculateEnemyDamageToHero()
        → 查询 StatusEffectSystem（Power/HobgoblinFury/Weak...）
      → HeroCombatEntity.TakeDamage(damage)
        → 消耗 Block → Publish(BlockConsumedEvent)
        → 扣除 HP → Publish(DamageTakenEvent)
        → 检查死亡 → 若有复活 → Publish(HeroRevivedEvent)
      → 施加 Debuff → StatusEffectSystem.ApplyEffect()
        → Publish(StatusEffectAppliedEvent)
    → IntentSystem.ClearIntent(enemy)
  → EnterPhase(EnemyTurnEnd)
    → StatusEffectSystem.DecayTurnBasedEffects()
      → Publish(StatusEffectDecayedEvent)
    → 检查胜负 → Publish(BattleEndedEvent)
```

### 4.3 事件订阅模式对比

| 模式 | 适用场景 | 示例 |
|------|---------|------|
| **强引用 Subscribe** | 全局基础设施、生命周期与游戏相同的系统 | `RelicTriggerSystem` 订阅 `CardPlayedEvent` |
| **弱引用 SubscribeWeak** | `MonoBehaviour` UI 组件、可能被销毁的对象 | `PlayerHandDisplay` 订阅 `CardDrawnEvent` |
| **状态查询（Pull）** | 需要批量聚合状态的场景 | `HeroAfflictionManager` 每回合从 `StatusEffectSystem` 拉取状态 |

---

## 5. 扩展指南

### 5.1 添加一名新英雄

1. **数据配置**：创建 `Data/HeroData` ScriptableObject
   - 设置 `HeroID`（如 `PG`）
   - 配置基础属性、起始牌组、等级解锁内容
   - 设置 `PassiveEffectMethodName`

2. **被动效果实现**：在 `BattleStateMachine` 或 `HeroCombatEntity` 中添加被动逻辑
   - 参考 `DS` 的 Combo 计数器和 `GM` 的元素机制

3. **UI 适配**：在 `HeroDisplay` 中添加专属资源引用

4. **存档兼容**：`HeroData` 的等级/XP 字段已纳入 `AccountState`，无需修改存档结构

### 5.2 添加一张新卡牌

1. **数据配置**：创建 `Data/CardData` ScriptableObject
   - 设置 `CardID`（避免与现有 ID 冲突）
   - 配置费用、数值、稀有度、关键词
   - 设置 `EffectMethodName`

2. **效果实现**：在 `CardEffectDispatcher.RegisterCoreEffects()` 中添加字典条目：
   ```csharp
   enemyCardEffectDict[123] = (enemy) => CardEffect123(enemy);
   enemyCardEffectDict[1123] = (enemy) => CardEffect1123(enemy); // 升级版本
   ```

3. **实现效果方法**：
   ```csharp
   private void CardEffect123(EnemyCombatEntity enemy)
   {
       int damage = 10;
       // 查询遗物、Buff 等...
       enemy.TakeDamage(damage);
       // 播放 VFX、音效...
   }
   ```

### 5.3 添加一个新敌人

1. **数据配置**：创建 `Data/EnemyData` ScriptableObject
   - 设置 `EnemyID`、`体型`、`基础属性`
   - 配置 `EnemyTraitType` 列表

2. **AI 配置**：创建 `Data/EnemyAIProfile` ScriptableObject
   - 定义技能列表（`EnemyAbilityData`）
   - 设置每个技能的意图类型、伤害、格挡、权重、条件

3. **注册技能**：在 `EnemyAbilityRegistry.RegisterDefaults()` 中注册（或使用 ScriptableObject 自动注册）

4. **遭遇配置**：在 `Data/EnemyEncounterData` 中添加该敌人到对应 Act/BattleType 的遭遇池

### 5.4 添加一个新遗物

1. **数据配置**：创建 `Data/RelicData` ScriptableObject
   - 设置 `RelicID`、稀有度、分类、触发时机列表

2. **触发时机**：在 `RelicTriggerSystem` 中选择合适的 `RelicTriggerTiming`

3. **效果实现**：在 `RelicTriggerSystem` 的对应时机处理函数中，检查该遗物是否存在并执行效果：
   ```csharp
   private void OnCardPlayed(CardPlayedEvent evt)
   {
       if (HasRelic(999)) // 你的新遗物 ID
       {
           // 执行效果...
       }
   }
   ```

---

## 6. 已知限制与未来演进

### 6.1 当前限制

1. **卡牌效果仍为硬编码 C#**：虽然数据层已 ScriptableObject 化，但行为层（效果逻辑）仍需修改代码并重新编译。对于频繁迭代的卡牌设计，可考虑引入 Lua/C# 脚本热重载。

2. **事件总线无历史回放**：`GameEventBus` 仅支持实时发布-订阅，不支持事件队列或回放。调试复杂战斗时可能需要日志追踪。

3. **单例模式在测试中的限制**：单元测试难以替换单例实现。未来可考虑将 Manager 提取接口，支持 Mock 注入。

### 6.2 推荐演进方向

| 优先级 | 方向 | 理由 |
|--------|------|------|
| **高** | 引入 `ICardEffect` 接口体系 | 将效果逻辑拆分为可复用组件，支持组合式卡牌设计 |
| **高** | 单元测试覆盖 | 为 `DamageCalculator`、`StatusEffectSystem`、`DeckManager` 添加单元测试 |
| **中** | 存档迁移工具 | 随着版本迭代，`SaveFileHeader` 需要配套的迁移脚本 |
| **中** | 事件日志系统 | 记录战斗内所有事件序列，支持回放与调试 |
| **低** | ECS 架构探索 | 若敌人/卡牌数量大幅增长，可考虑 Unity ECS 替代 MonoBehaviour |

---

## 7. 附录：原始设计文档索引

| 序号 | 文档 | 设计重点 | 代码映射 |
|------|------|---------|---------|
| 00 | `00_综述与导航.md` | 模块依赖关系图、设计范式索引 | 本架构整体 |
| 01 | `01_游戏概述与核心循环.md` | 英雄系统、核心循环、难度 | `Data/HeroData`, `Core/Configuration` |
| 02 | `02_全局数据与存档系统.md` | IWS 存档结构、场景异步加载 | `Core/GameStateManager`, `Core/SaveSystem` |
| 03 | `03_世界地图与流程节点系统.md` | 地图生成算法、Act 结构 | `Map/MapGenerator`, `Map/ActSystem` |
| 04 | `04_战斗核心与角色系统.md` | 回合状态机、Boost、伤害计算 | `Combat/BattleStateMachine`, `Combat/DamageCalculator` |
| 05 | `05_卡牌与牌组系统.md` | 五堆模型、手牌布局、交互 | `Deck/DeckManager`, `UI/PlayerHandDisplay` |
| 06 | `06_卡牌效果与法术系统.md` | 效果分发、12 类效果范式 | `Deck/CardEffectDispatcher`, `Deck/SpellSystem` |
| 07 | `07_BuffDebuff与敌人AI系统.md` | 状态模型、意图系统、Boss 机制 | `Status/StatusEffectSystem`, `AI/EnemyAIBehavior` |
| 08 | `08_遗物与特殊机制.md` | 10 大触发范式、诅咒、PRD | `Relic/RelicTriggerSystem`, `Relic/CurseSystem`, `Utils/PRDCalculator` |
| 09 | `09_特殊事件节点系统.md` | Mystery 流水线、商店、营地 | `EventNodes/*.cs` |
| 10 | `10_UI交互与反馈系统.md` | 跨场景 HUD、SFX 对象池 | `UI/BattleUIManager`, `Utils/SceneTransitionManager` |
