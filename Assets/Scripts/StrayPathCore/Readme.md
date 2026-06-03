# StrayPathCore — 快速上手指南

> **定位**：本目录是项目核心逻辑层，包含完整的 Roguelike Deck-Building 战斗框架。所有代码均为从原始 Godot 项目逆向设计后，在 Unity 中的**重新架构与净化实现**。

---

## 1. 项目速览

| 项目 | 说明 |
|------|------|
| **游戏类型** | 回合制 Deck-Building Roguelike（对标 Slay the Spire） |
| **原始设计** | `Seal The Rift` — 基于完整逆向工程代码分析的设计还原文档集 |
| **技术栈** | Unity 2022+ / C# / ScriptableObject / 事件驱动架构 |
| **核心规模** | ~50 个 C# 脚本，11 个功能子目录，约 11 个命名空间 |
| **架构模式** | 事件总线 + 单例服务定位器 + 数据驱动（ScriptableObject） |

---

## 2. 目录导航（按依赖层级排序）

**建议阅读顺序**：`Core → Data → Status → Combat → Deck → AI → Map → Relic → EventNodes → UI → Utils`

| 目录 | 核心文件 | 一句话职责 |
|------|---------|-----------|
| `Core/` | `GameStateManager`, `GameEventBus`, `SaveSystem`, `Configuration` | 全局基础设施：状态管理、事件总线、存档、配置 |
| `Data/` | `CardData`, `EnemyData`, `HeroData`, `RelicData`, `EnemyAIProfile` | 纯数据定义（ScriptableObject），所有玩法内容的配置入口 |
| `Status/` | `StatusEffectSystem`, `StatusEffect` | **唯一状态源**：Buff/Debuff 的统一管理、查询、生命周期 |
| `Combat/` | `BattleStateMachine`, `CombatEntity`, `HeroCombatEntity`, `EnemyCombatEntity`, `DamageCalculator` | 战斗流程：回合状态机、实体接口、伤害计算 |
| `Deck/` | `DeckManager`, `EnergyManager`, `CardEffectDispatcher`, `SpellSystem` | 五堆牌库、能量、卡牌效果分发、法术 |
| `AI/` | `EnemyAIBehavior`, `IntentSystem`, `EnemyAbilityRegistry` | 敌人 AI：技能选择、意图显示、技能注册表 |
| `Map/` | `MapGenerator`, `WorldMapController`, `ActSystem`, `NodeRouter` | 爬塔地图生成、路径锁定、Act 推进、场景路由 |
| `Relic/` | `RelicManager`, `RelicTriggerSystem`, `CurseSystem` | 遗物管理、**非侵入式触发**、诅咒系统 |
| `EventNodes/` | `ShopSystem`, `CampfireSystem`, `TreasureSystem`, `MysteryEventSystem`, `OldManSystem`, `ScoreboardSystem` | 特殊事件节点：商店、营地、宝藏、神秘事件、老者、计分 |
| `UI/` | `BattleUIManager`, `PlayerHandDisplay`, `CardUIProxy`, `EnergyDisplay`, `HeroDisplay`, `EnemyDisplay` | 战斗 UI 表现层，纯订阅事件刷新 |
| `Utils/` | `PRDCalculator`, `SceneTransitionManager`, `StringEncryption` | 通用工具：伪随机、场景切换、存档加密 |

---

## 3. 核心概念速查

### 3.1 状态三层架构
```
AccountState   ← 跨 Run 永久保留（英雄等级、排行榜、解锁进度）
RunState       ← 单局持久化（HP、金币、牌组、遗物、Act、路径历史）
BattleTransientState ← 战斗内内存态（手牌、能量、敌人状态、意图）
```

### 3.2 五堆牌组模型
| 堆名 | 说明 |
|------|------|
| **DrawPile** | 抽牌堆，耗尽时自动将 DiscardPile 洗牌补入 |
| **Hand** | 手牌，上限无硬限制，UI 自动缩放适配 |
| **DiscardPile** | 弃牌堆，打出/回合结束弃牌进入 |
| **BanishPile** | 放逐堆，战斗内暂离，战后视情况回收 |
| **HoldPile** | 保留堆，被 Hold 的卡牌暂存，可后续抽回 |

### 3.3 战斗回合流程
```
BattleStart
  → PlayerTurnStart（抽牌、回能量、回合开始效果）
  → PlayerTurn（玩家行动：出牌、法术、Boost、EndTurn）
  → PlayerTurnEnd（弃手牌、回合结束效果）
  → EnemyTurnStart
  → EnemyTurn（逐敌人执行意图）
  → EnemyTurnEnd（状态衰减、胜负判定）
  → 循环至 PlayerTurnStart
```

### 3.4 卡牌效果分发（4 层字典）
```
enemyCardEffectDict        : cardID → Action<Enemy>          （对敌单体）
heroCardEffectDict         : cardID → Action                 （对己）
advEnemyCardEffectDict     : cardID → Action<Card, Enemy>    （需卡牌上下文+敌人）
advHeroCardEffectDict      : cardID → Action<Card>           （需卡牌上下文）
```

### 3.5 意图系统三态
同一套 `AbilityAction` 委托通过参数分支为三种行为：
- `isPreview=true, isIntentUpdate=false` → **首次显示意图**
- `isPreview=true, isIntentUpdate=true` → **刷新意图数值**
- `isPreview=false` → **实际执行技能**

---

## 4. 关键文件索引（按开发场景）

### 如果你想...

| 目标 | 先看这个文件 | 再看这些 |
|------|-------------|---------|
| **理解全局状态如何流转** | `Core/GameStateManager.cs` | `Core/GameEventBus.cs` |
| **添加一张新卡牌** | `Data/CardData.cs` | `Deck/CardEffectDispatcher.cs` |
| **添加一个新敌人** | `Data/EnemyData.cs` | `AI/EnemyAbilityRegistry.cs`, `Data/EnemyAIProfile.cs` |
| **添加一个新遗物** | `Data/RelicData.cs` | `Relic/RelicTriggerSystem.cs` |
| **修改战斗回合规则** | `Combat/BattleStateMachine.cs` | `Combat/HeroCombatEntity.cs`, `Combat/EnemyCombatEntity.cs` |
| **修改地图生成算法** | `Map/MapGenerator.cs` | `Map/PathSystem.cs`, `Map/NodeRouter.cs` |
| **修改 UI 表现** | `UI/BattleUIManager.cs` | `UI/PlayerHandDisplay.cs`, `UI/CardUIProxy.cs` |
| **修改存档格式** | `Core/SaveSystem.cs` | `Core/Configuration.cs` |
| **修改概率算法** | `Utils/PRDCalculator.cs` | — |

---

## 5. 开发约定

### 5.1 命名空间
所有代码必须放在 `StrayPathCore.{目录名}` 命名空间中。例如：
```csharp
namespace StrayPathCore.Combat { }
namespace StrayPathCore.Deck { }
```

### 5.2 单例模式
Manager 级系统使用 `MonoBehaviour` 单例，模板：
```csharp
public static XxxManager Instance { get; private set; }
private void Awake() {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    // 跨场景保留则加: DontDestroyOnLoad(gameObject);
}
```

### 5.3 跨模块通信
**禁止**直接跨 Manager 调用业务逻辑。正确方式：
1. **首选**：通过 `GameEventBus.Instance.Publish(new MyEvent{...})` 发布事件
2. **次选**：通过 `GameStateManager.Instance` 查询/修改全局状态
3. **禁止**：`SomeManager.Instance.DirectCall()` 导致硬耦合

### 5.4 数据驱动
所有玩法配置数据（卡牌、敌人、遗物、事件）必须使用 `ScriptableObject`，在编辑器中配置。
业务逻辑（效果、AI 行为）在 C# 中通过字典委托分发。

---

## 6. 原始设计文档对照

原始逆向设计文档集位于：
```
C:\Users\jinji\Desktop\文档\MyNote\游戏项目拆解\Seal The Rift — 完整设计还原文档
```

| 设计文档 | 对应代码目录 | 核心改进 |
|---------|-------------|---------|
| `02_全局数据与存档系统.md` | `Core/` | IWS 静态全局池 → 事件驱动的三层状态架构 |
| `03_世界地图与流程节点系统.md` | `Map/` | 硬编码地图生成 → ScriptableObject 模板驱动 |
| `04_战斗核心与角色系统.md` | `Combat/` | 布尔标志爆炸 → 严格枚举状态机 |
| `05_卡牌与牌组系统.md` | `Deck/` | 硬编码 Card 类 → ScriptableObject CardData |
| `06_卡牌效果与法术系统.md` | `Deck/CardEffectDispatcher.cs` | 巨型 switch-case → 字典委托分发 |
| `07_BuffDebuff与敌人AI系统.md` | `Status/`, `AI/` | 碎片化状态 → StatusEffectSystem 唯一状态源 |
| `08_遗物与特殊机制.md` | `Relic/` | 侵入式硬编码 → 非侵入式事件订阅触发 |
| `09_特殊事件节点系统.md` | `EventNodes/` | 24K 行 Mystery → 拆分为 6 个独立系统 |
| `10_UI交互与反馈系统.md` | `UI/` | 场景脚本 Godot 风格 → Unity 事件驱动 UI |

---

> **提示**：若需要深度理解架构决策与演进原因，请阅读同目录下的 `Architecture.md`。
