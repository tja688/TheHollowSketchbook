# Seal The Rift (StrayPath) → Unity 核心移植架构设计

> 本文档定义整体架构决策、模块边界与数据流。供后续开发者理解系统设计与改造方向。  
> **文档版本**: 2.0 | **基于整改报告**: 2026-06-03

---

## 1. 架构总览

### 1.1 设计原则（与原 Godot 项目的核心差异）

| 原 Godot 设计 | Unity 移植设计 | 理由 |
|---|---|---|
| `IWS` 纯静态全局状态池 | `GameStateManager` ServiceLocator 单例 | 静态全局状态是设计文档明确指出的反模式，改为可注入的服务 |
| Godot `signal`/`connect`/`emit` | `GameEventBus` ScriptableObject + C# Event | Unity 无内置信号系统；SO EventBus 可跨场景、可序列化调试 |
| 硬编码 C# 卡牌/敌人/遗物数据 | `ScriptableObject` 数据表 + 运行时数据类 | 解耦数据与逻辑，支持编辑器配置 |
| Godot `await`/`Timer` 异步地狱 | C# `async/await` + Coroutine 分层 | 核心逻辑用 async/await，动画/表现层用 Coroutine，彻底解耦 |
| 敌人 AI 全堆在 `EnemyAbilities.cs` | `EnemyAIProfile` SO + `EnemyAbility` 委托注册 | 按敌人拆分配置，支持数据驱动扩展 |
| 遗物硬编码侵入各系统 | `RelicTriggerSystem` 事件总线订阅模式 | 遗物通过 EventBus 订阅事件，非侵入式 |
| `FindObjectOfType` 运行时反射查找 | `BattleStateMachine` 中心化查询接口 + 单例引用 | 消除运行时反射，提升性能与类型安全 |
| `JsonUtility` 不支持 Dictionary | `Newtonsoft.Json` + 版本化存档头 | 稳定支持复杂类型序列化，具备向后兼容迁移能力 |

### 1.2 模块依赖层级

```
┌─────────────────────────────────────────────────────────────┐
│                      表现层 (Presentation)                    │
│  BattleUIManager │ PlayerHandDisplay │ EnemyDisplay │ VFX   │
│  HeroDisplay │ EnergyDisplay │ EndTurnButton │ BoostDisplay │
├─────────────────────────────────────────────────────────────┤
│                     场景控制器 (Scene Controllers)            │
│  WorldMapController │ BattleStateMachine │ ShopSystem │ Campfire │
├─────────────────────────────────────────────────────────────┤
│                     玩法逻辑层 (Gameplay Logic)               │
│  DeckManager │ CardEffectDispatcher │ SpellSystem │ Relic   │
│  HeroCombatEntity │ EnemyCombatEntity │ EnemyAI │ Boost     │
├─────────────────────────────────────────────────────────────┤
│                     状态与数据层 (State & Data)               │
│  GameStateManager │ SaveSystem │ GameEventBus │ PRD          │
│  CardData │ EnemyData │ HeroData │ RelicData │ EventData    │
│  EnemyAIProfile │ EnemyEncounterData                                  │
├─────────────────────────────────────────────────────────────┤
│                     基础设施层 (Infrastructure)               │
│  Utils │ SceneTransitionManager │ Configuration              │
└─────────────────────────────────────────────────────────────┘
```

**关键规则**：上层可依赖下层；同层通过 EventBus 通信；禁止跨层直接调用。

---

## 2. 核心基础设施 (Core)

### 2.1 GameEventBus — 全局事件总线

```csharp
// 强引用订阅（默认，高性能）
GameEventBus.Instance.Subscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
GameEventBus.Instance.Unsubscribe<PlayerTurnStartedEvent>(OnPlayerTurnStarted);

// 弱引用订阅（安全，推荐MonoBehaviour使用）
GameEventBus.Instance.SubscribeWeak<PlayerTurnStartedEvent>(OnPlayerTurnStarted);
// 弱引用无需手动取消订阅，自动GC清理
```

- 使用 `ScriptableObject` 单例，支持编辑器调试
- 事件为纯数据 POCO 类
- **双轨订阅模式**：强引用（默认高性能）+ 弱引用（自动GC安全）
- `Publish<T>` 遍历弱引用时自动清理已GC的引用

### 2.2 GameStateManager — 全局运行时状态

替代原 `IWS`，管理以下运行时状态：

```csharp
public class RunState {
    public string SelectedHeroID;        // "Hero_DS" / "Hero_GM" / "Hero_PG"
    public int Act;                      // 1~3
    public int Gold;
    public int CurrentHP, MaxHP;
    public int CurrentMP, MaxMP;         // 默认 MaxMP=3
    public bool Defeated;
    public int BoostBarValue, BoostEnergy;
    public List<int> PathHistory;        // 已访问节点
    public int CurrentPID, CurrentPG;    // 当前节点/路径组
    public List<CardRuntime> DeckCards;  // 牌组持久化数据
    public List<RelicRuntime> Relics;
    public List<int> Spells;
    // ... 其他运行时状态（含 Dictionary 字段，Newtonsoft.Json 序列化）
}
```

- **不持久化**：战斗内状态（手牌、弃牌堆等）为内存态
- **持久化**：RunState 通过 SaveSystem 序列化为 JSON（Newtonsoft.Json）
- **账户级数据**：AccountState（英雄等级/XP/解锁/设置）单独存储

### 2.3 SaveSystem — 存档系统

- **序列化器**：Newtonsoft.Json（替代 JsonUtility，支持 Dictionary）
- **格式**：JSON（`Application.persistentDataPath/run_save.json`）
- **版本化结构**：
  ```json
  {
    "header": { "Version": 1, "SaveDate": "...", "GameVersion": "1.0" },
    "data": { /* RunState */ }
  }
  ```
- **兼容迁移**：加载时检查 `header.Version`，不匹配时调用 `MigrateRunState`
- **结构**：RunState + AccountState + Configuration + Scoreboard 分离
- **即时写入**：关键状态变更时立即 Save（保留原设计）
- **加密**：可选 AES 加密（保留原设计意图）

### 2.4 Configuration — 全局配置

- 分辨率、音量、语言等设置
- 排行榜数据（Top10 Score / Top10 Runtime）

---

## 3. 数据层 (Data)

所有静态配置使用 `ScriptableObject`，运行时实例使用纯 C# 类。

### 3.1 卡牌数据

```csharp
[CreateAssetMenu(fileName = "CardData", menuName = "StrayPath/CardData")]
public class CardData : ScriptableObject {
    public int CardID;
    public string CardName, UpgradedName;
    public string Description, UpgradedDescription;
    public CardRarity Rarity;           // Common/Uncommon/Rare
    public int BasePrice;
    public int EnergyCost, UpgradedEnergyCost;
    public int AttackValue, UpgradedAttackValue;
    public int DefendValue, UpgradedDefendValue;
    public int BanishCharges, UpgradedBanishCharges;
    public bool IsBoostable, IsCombo, IsFinisher;
    public bool TargetsEnemy;
    public string EffectMethodName;     // 映射到 CardEffectDispatcher
}
```

运行时副本：`CardRuntime`（含 `CopyCount`, `IsUpgraded`, `IsBanished`, `ExtraUpgrades`）

### 3.2 敌人数据

```csharp
[CreateAssetMenu(fileName = "EnemyData", menuName = "StrayPath/Data/EnemyData")]
public class EnemyData : ScriptableObject {
    public int EnemyID;
    public string EnemyName;
    public int BaseHP;
    public int BasePower, BaseArmor, BaseThorns;
    public EnemySize Size;              // Small/Medium/Big/Huge
    public List<EnemyTraitType> Traits;
    public EnemyAIProfile AIProfile;
    public bool IsBoss, IsElite;
}
```

### 3.3 敌人遭遇配置（新增）

```csharp
[CreateAssetMenu(fileName = "EnemyEncounterData", menuName = "StrayPath/Data/EnemyEncounterData")]
public class EnemyEncounterData : ScriptableObject {
    public int EncounterID;
    public int ActID;                   // 1~3, 0=不限
    public int BattleType;              // 1=普通, 2=精英, 3=Boss
    public List<EnemyEncounterEntry> Enemies;
    public int SpawnWeight = 10;
    public int MaxPerRun = 0;
}
```

战斗开始时，`BattleStateMachine` 从 `Resources/StrayPath/Data/Encounters` 加载配置，按Act/BattleType筛选后加权随机选择。

### 3.4 遗物数据

```csharp
[CreateAssetMenu(fileName = "RelicData", menuName = "StrayPath/RelicData")]
public class RelicData : ScriptableObject {
    public int RelicID;
    public string RelicName, Description;
    public RelicRarity Rarity;
    public int BasePrice;
    public int MaxCharges;
    public RelicCategory Category;
    public List<RelicTrigger> Triggers;
}
```

---

## 4. 战斗核心 (Combat)

### 4.1 回合状态机

```csharp
public enum BattlePhase {
    BattleStart,        // 初始化
    PlayerTurnStart,    // 抽牌、恢复能量、回合开始触发
    PlayerTurn,         // 玩家可操作
    PlayerTurnEnd,      // 弃牌、回合结束触发
    EnemyTurnStart,     // 敌人回合开始触发
    EnemyTurn,          // 敌人依次执行意图
    EnemyTurnEnd,       // Buff/Debuff 衰减
    BattleEnd           // 胜负判定、奖励结算
}
```

- `BattleStateMachine` 作为状态机主控，通过 EventBus 驱动流程
- 状态切换为同步 + 事件通知，动画由表现层订阅事件后异步播放
- **公开查询接口**（替代 FindObjectOfType）：
  ```csharp
  public HeroCombatEntity GetHero();
  public EnemyCombatEntity GetEnemyByUID(string uid);
  public IReadOnlyList<EnemyCombatEntity> GetAllEnemies();
  ```

### 4.2 战斗实体接口（ICombatEntity）

```csharp
public interface ICombatEntity {
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

- `HeroCombatEntity` 与 `EnemyCombatEntity` 均实现 `ICombatEntity`
- `DamageCalculator` 公共API统一接收 `ICombatEntity`，内部安全转型获取特化属性
- `CombatEntity` 抽象类保留作为可选共享基类

### 4.3 伤害计算管线

```
BaseDamage
  ├─ Source Power/Toughness 修正
  ├─ Buff/Debuff 百分比修正（Fragile×1.3, Weak×0.7, Boost×1.5 等）
  ├─ Armor 固定减伤
  ├─ Block 吸收
  └─ 最终伤害 → HP 扣除
```

- `DamageCalculator` 静态工具类，统一所有伤害计算
- `PreviewDamage()` 与 `TakeDamage()` 共用同一套修正逻辑
- 统一接口：`CalculatePlayerDamageToEnemy(ICombatEntity source, ICombatEntity target, int baseDamage)`

### 4.4 Boost 系统

- `BoostSystem` 独立组件，单例模式
- 状态：Inactive → PreActive（等待弃2张牌）→ Active → Expired（打出1张牌后）
- 效果：伤害×1.5、穿透 Block、格挡+20%减免

---

## 5. 卡牌系统 (Deck)

### 5.1 五堆模型

```csharp
public class DeckManager {
    public List<CardRuntime> DrawPile;      // 抽牌堆
    public List<CardRuntime> Hand;          // 手牌
    public List<CardRuntime> DiscardPile;   // 弃牌堆
    public List<CardRuntime> BanishPile;    // 放逐堆
    public List<CardRuntime> HoldPile;      // 保留堆
}
```

### 5.2 卡牌效果分发

```csharp
public class CardEffectDispatcher {
    // 四层字典映射
    private Dictionary<int, Action<EnemyCombatEntity>> enemyEffects;
    private Dictionary<int, Action> heroEffects;
    private Dictionary<int, Action<CardRuntime, EnemyCombatEntity>> advEnemyEffects;
    private Dictionary<int, Action<CardRuntime>> advHeroEffects;
}
```

- 注册方式：在 `Initialize()` 中批量注册，非反射
- 升级 ID = 基础 ID + 1000
- **当前已注册**：37 张基础卡牌（含升级版本约 60+ 个映射）

---

## 6. 状态与 AI (Status & AI)

### 6.1 Buff/Debuff 状态机

统一为 `StatusEffect` 数据类，按四类区分：

```csharp
public enum StatusDurationType {
    Continuous,     // ∞ 无限
    TurnBased,      // 按回合衰减
    ChargeBased,    // 按次数消耗
    StackBased      // 层数叠加
}
```

- `StatusEffectSystem` 作为**唯一状态源**，`_entityEffects` 字典管理所有状态
- `BattleTransientState` 仅作为UI读取的快照，单向同步
- `HeroCombatEntity` / `EnemyCombatEntity` 中的状态查询均委托给 `StatusEffectSystem`

### 6.2 敌人 AI

```csharp
public class EnemyAIBehavior {
    public EnemyAbilityData SelectAbility(EnemyRuntime enemy, int turnNumber);
    public void ExecuteAbility(EnemyAbilityData ability, EnemyRuntime enemy);
    public void DisplayIntent(EnemyAbilityData ability, EnemyRuntime enemy);
}
```

- 每个敌人一个 `EnemyAIProfile` SO
- 加权随机 + 历史行为记忆避免重复
- `AbilityAction` 委托三态：Preview / Update / Execute

---

## 7. 遗物系统 (Relic)

### 7.1 遗物触发范式

通过 EventBus 订阅以下 10 大时机：

```csharp
public enum RelicTriggerTiming {
    BattleStart, PlayerTurnStart, PlayerTurnEnd,
    EnemyTurnStart, EnemyTurnEnd,
    CardPlayed, DamageTaken, EnemyKilled,
    NodeEntered, CardDrawn, EnergyChanged, DeckShuffled
}
```

- `RelicTriggerSystem` 在对应时机 Publish 事件
- 每个遗物订阅自己关心的时机，非侵入式
- **限制机制**：`_oncePerBattleRelics` / `_oncePerTurnRelics` 两套标记，自动重置
- **当前已注册**：37 个遗物效果，覆盖全部触发时机

### 7.2 PRD 伪随机

- `PRDCalculator` 实现 20 步周期伪随机分布
- 每个效果独立状态隔离

---

## 8. 地图与事件 (Map & EventNodes)

### 8.1 地图生成

- `MapGenerator`：按 Act 生成 3 条路径组，各 15 节点
- 难度模板 + 子路径洗牌算法
- 节点类型：Battle/Elite/Shop/Mystery/Campfire/Treasure/Boss

### 8.2 场景路由

`SceneTransitionManager`：
- 淡出 → 切换场景 → 淡入
- 通过 `GameStateManager.RunState` 传递跨场景数据

---

## 9. UI表现层（新增）

### 9.1 模块定位

UI层为**纯表现层**，所有状态从 `GameStateManager` / `BattleStateMachine` 读取，不持有核心游戏状态。

**可摘除保证**：删除整个 `UI/` 目录后，游戏逻辑层（战斗状态机、牌组管理、伤害计算等）仍可正常运行。

### 9.2 UI模块清单

| 脚本 | 职责 | 数据来源 |
|------|------|---------|
| `BattleUIManager` | UI总控，管理目标选择状态，订阅并分发事件 | EventBus |
| `PlayerHandDisplay` | 手牌水平排列，点击打出/目标选择 | `DeckManager.Instance.Hand` |
| `CardUIProxy` | 单张卡牌视觉代理（费用、名称、升级边框） | `CardRuntime` |
| `EnergyDisplay` | 能量文本（当前/最大） | `GameStateManager.Instance.BattleState` |
| `EndTurnButton` | 结束回合按钮 | 调用 `BattleStateMachine.Instance.EndPlayerTurn()` |
| `EnemyDisplay` | 敌人HP条、Block、意图显示，点击选目标 | `EnemyCombatEntity` |
| `HeroDisplay` | 英雄HP条、Block、关键Buff/Debuff | `HeroCombatEntity` |
| `BoostDisplay` | Boost Bar进度、Boost Energy、激活按钮 | `GameStateManager.Instance.CurrentRun` |

### 9.3 目标选择流程

```
玩家点击卡牌 → BattleUIManager判断是否需要目标
    → 不需要目标：直接调用 DeckManager.Instance.PlayCard(card, null)
    → 需要目标：进入目标选择模式，高亮敌人
        → 玩家点击敌人 → DeckManager.Instance.PlayCard(card, enemyUID)
        → 取消选择 → 退出目标选择模式
```

---

## 10. 代码组织约定

### 10.1 命名规范

- 类名：PascalCase（`BattleManager`, `CardEffectDispatcher`）
- 接口：I 前缀（`ICombatEntity`, `IStatusEffect`）
- 方法：PascalCase
- 字段：camelCase（public）/ _camelCase（private）
- 常量：UPPER_SNAKE_CASE
- 事件类：XxxEvent 后缀

### 10.2 目录结构

```
Assets/Scripts/StrayPathCore/
├── Core/           # EventBus, GameStateManager, SaveSystem, Config
├── Data/           # ScriptableObject 定义（含 EnemyEncounterData）
├── Map/            # 地图生成、Act推进、场景切换
├── Combat/         # 回合状态机、实体、伤害计算、Boost
├── Deck/           # 五堆模型、效果分发、法术、奖励
├── Status/         # Buff/Debuff 状态机、AfflictionManager
├── AI/             # 敌人AI、意图系统、行为注册
├── Relic/          # 遗物管理器、触发系统、诅咒、PRD
├── EventNodes/     # Mystery、Campfire、Shop、Treasure、Scoreboard
├── UI/             # 战斗UI总控、手牌、能量、敌人、英雄、Boost
└── Utils/          # 静态工具类、扩展方法
```

### 10.3 关键设计约束

1. **禁止在逻辑层直接操作 Unity 组件**（Transform, Renderer 等）
2. **禁止在逻辑层使用 Coroutine**，Coroutine 仅限表现层和场景控制器
3. **所有跨模块通信走 EventBus**，禁止直接引用其他 Manager
4. **数据层与逻辑层分离**：SO 只读，运行时数据为纯 C# 类
5. **状态变更后立即 Publish 事件**，保持事件驱动的一致性
6. **禁止新的 FindObjectOfType/FindObjectsOfType**，实体查询统一走 `BattleStateMachine` 公开接口
7. **UI层必须是可摘除的**，不持有核心游戏状态

---

## 11. 后续改造指南

### 11.1 添加新英雄

1. 创建 `HeroData` SO，配置基础属性
2. 在 `HeroCombatEntity` 中添加专属被动逻辑
3. 创建英雄专属卡牌 SO（DS 201-233, GM 206-238）
4. 在 `CardEffectDispatcher` 中注册新卡牌效果

### 11.2 添加新敌人

1. 创建 `EnemyData` SO，配置 HP/特性/AI
2. 创建 `EnemyAIProfile` SO，配置技能列表与权重
3. 在 `EnemyAbilityRegistry` 中注册技能委托
4. 创建 `EnemyEncounterData` SO，配置敌人组合与出现条件
5. 放入 `Resources/StrayPath/Data/Encounters/`

### 11.3 添加新卡牌

1. 创建 `CardData` SO，配置所有数值字段
2. 设置 `EffectMethodName` 映射到 `CardEffectDispatcher` 中的方法
3. 在 `CardEffectDispatcher.InitializeEffects()` 中注册效果逻辑
4. 升级版本需要同时注册 `cardID` 和 `cardID + 1000`

### 11.4 添加新遗物

1. 创建 `RelicData` SO，配置 ID/名称/触发时机
2. 在 `RelicTriggerSystem.InitializeRelicTriggers()` 中注册触发逻辑
3. 如需限制（每场战斗一次/每回合一次），使用 `_oncePerBattleRelics` / `_oncePerTurnRelics`
4. 在掉落池配置中加入新遗物

---

*文档版本: 2.0*  
*基于 Seal The Rift 设计还原文档的 Unity 移植架构*  
*更新记录: 2026-06-03 全面整改 — 添加ICombatEntity、Newtonsoft.Json存档、UI层、遭遇配置、弱引用事件*
