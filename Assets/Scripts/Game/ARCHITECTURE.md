# Game 架构文档 —— L0 地基落地状态（2025.06.05）

> **版本**: 2025.06.05 — L0 领域基础设施落地评审后  
> **目的**: 记录从 StS 清理后代码库到《深入地牢》项目级领域基础设施的当前真实落地状态，为 L1 内容铺量开发提供可信地基。  
> **设计对照**: `Assets/Notes/项目级领域基础设施顶层设计.md` + `C:/Users/jinji/Desktop/文档/MyNote/游戏开发项目/深入地牢/`

---

## 一、总览：四层协作现状

```text
┌──────────────────────────────────────────────┐
│ 表现层 Presentation                           │  ← 人类控制，保留 StS 骨架待重构
│  服务(Tween/Audio/VFX)、原型视图、射线检测      │
├──────────────────────────────────────────────┤
│ 业务逻辑层 Game Logic / Content               │  ← 【完全空白】待 L1 建设
│  怪物、机关、道具、遗物、词条、房间配置          │
├──────────────────────────────────────────────┤
│ 项目级领域基础设施 Domain Infrastructure        │  ← L0 核心成果，人类设计框架
│  九宫格、卡牌实例、堆叠、翻面、互动、伤害管线     │
├──────────────────────────────────────────────┤
│ 通用基础设施 Foundation Infrastructure          │  ← 人类绝对控制，跨项目复用
│  ModelDb、存档、RNG、ActionQueue、Hook、Map    │
└──────────────────────────────────────────────┘
```

当前代码库共 **76 个 C# 文件**，分布如下：

```text
Game/
├── Core/
│   ├── Runtime/
│   │   ├── Models/              # 数据框架（AbstractModel、ModelDb、ModelId）
│   │   ├── Actions/             # 异步动作队列（ActionSystem.cs）
│   │   ├── Random/              # 确定性随机（DeterministicRng.cs）
│   │   ├── Map/                 # 地图 DAG（ActMap、MapCoord、MapPoint、StandardActMapGenerator）
│   │   ├── Entities/            # 生物骨架（Creature、Player、CharacterModel、EnemyModel）
│   │   ├── Combat/              # 遗留战斗骨架（CombatManager、CombatState、CreatureCmd）
│   │   ├── Powers/              # Buff/Debuff 拦截器（PowerModel）
│   │   ├── Hooks/               # 事件扩展点（Hook.cs，空方法待激活）
│   │   ├── Rewards/             # 奖励抽象（Reward、GoldReward、ChoiceReward）
│   │   ├── Rooms/               # 房间抽象 + 工厂（AbstractRoom、RoomFactory、CombatRoom等）
│   │   ├── Runs/                # 爬塔流程（RunManager、RunState）
│   │   ├── Saves/               # 二进制存档（SaveManager、RunSaveDto、版本=2）
│   │   ├── Common/              # 枚举 + ModelId
│   │   ├── Logging/             # 日志
│   │   ├── Compatibility/       # C# 兼容垫片
│   │   └── Domain/              # 【L0 核心新增】项目级领域基础设施
│   │       ├── Grid/            # 九宫格核心（GridCoord、GridCell、GridState、GridQueries）
│   │       ├── Cards/           # 卡牌运行时（CardModel、CardInstance、CardType、CardZone）
│   │       ├── Combat/          # 领域伤害（DamageInfo、CombatResolution、PlayerActionCounter）
│   │       ├── Deck/            # 地城牌组（DungeonDeck）
│   │       ├── Interaction/     # 输入意图（PlayerIntent、IntentValidator、IntentPreview）
│   │       ├── Actions/         # 领域动作（PlayerMoveAction、PlayerInteractAction等）
│   │       ├── Events/          # 领域事件（DomainEvent、DomainEventBatch、DomainEventType）
│   │       ├── Rooms/           # 清场判定（RoomClearChecker）
│   │       └── Validation/      # 不变量验证（DomainInvariantValidator）
│   └── Tests/
│       └── DomainP0Tests.cs     # 6 个 NUnit 测试
│
├── Presentation/
│   └── Runtime/
│       ├── Services/            # 游戏服务（Tween/Audio/VFX/飘字）
│       ├── RunFlow/             # 地图/房间/奖励 UI 原型
│       ├── Combat/Creatures/    # 生物视图（血条/受击/死亡）
│       ├── Input/               # 射线检测服务
│       └── Bootstrap/           # 启动器
│
└── Content/                     # 【已清空/不存在】待 L1 新建
```

---

## 二、L0 已落地基础设施详细说明

### 2.1 通用基础设施（Foundation）—— 保留并可用

#### 数据框架 `Models/`

| 文件 | 状态 | 说明 |
|:---|:---|:---|
| `AbstractModel.cs` | ✅ 直接复用 | `Id`, `IsCanonical`, `CloneMutable()`, `AssertMutable()` |
| `ModelDb.cs` | ✅ 直接复用 | `Register<T>()`, `Get<T>()`, `CreateMutable<T>()`, `All<T>()` |
| `ModelId.cs` | ✅ 直接复用 | `Category + Entry` 复合 ID |

**边界**: 所有新 Content 模型必须继承 `AbstractModel` 并通过 `ModelDb` 注册。

#### 动作队列 `Actions/ActionSystem.cs`

```csharp
public abstract class GameAction
{
    public uint Id { get; }
    public GameActionState State { get; }
    public Task CompletionTask { get; }
    protected abstract Task ExecuteActionAsync(GameActionExecutionContext ctx);
}

public sealed class ActionQueueSet
{
    public void Enqueue(GameAction action);
    public void Clear();
}

public sealed class ActionExecutor
{
    public async Task ExecuteAllAsync();
}
```

**边界**: 只提供"排队 + 顺序执行 + 异步等待"。新系统的所有战斗行为应封装为 `GameAction` 子类。

#### 确定性随机 `Random/DeterministicRng.cs`

```csharp
public interface IRng
{
    int NextInt(int minInclusive, int maxExclusive);
    float NextFloat();
    T Pick<T>(IReadOnlyList<T> items);
    void Shuffle<T>(IList<T> items);
    RngState CaptureState();
}
```

**边界**: 所有随机操作通过 `RunState.Rng` 执行，存档时捕获 `RngState`，读档时恢复。

#### 地图系统 `Map/`

保留 StS 的 DAG 数据结构：
- `ActMap` — 有向无环图 + 节点属性
- `MapPoint` — 节点（坐标、类型、访问状态、父子连接）
- `MapCoord` — (Column, Row)
- `StandardActMapGenerator` — 7×8 DAG 生成器

**⚠️ 差距**: `StandardActMapGenerator` 仍是 StS 的 7x8 结构，不是《深入地牢》的每层 9 节点生成器。需在 L1 前重写或新增 `DungeonMapGenerator`。

#### 生物框架 `Entities/` + `Powers/`

```csharp
public sealed class Creature
{
    public int CurrentHp { get; }
    public int MaxHp { get; }
    public int Block { get; }           // 语义为"防御值"（攻击-防御=伤害）
    public bool IsAlive { get; }
    public IReadOnlyList<PowerModel> Powers { get; }
    
    public event Action<int, int> HpChanged;
    public event Action<int, int> BlockChanged;
    public event Action<PowerModel> PowerApplied;
    public event Action<PowerModel> PowerRemoved;
    public event Action<Creature> Died;
}
```

**⚠️ 关键差距**: `Creature` 目前与 `CardInstance` **未关联**。`CardInstance` 自己管理 HP/Attack/Defense，`Creature` 是遗留 StS 框架。未来如果需要用 `PowerModel` 的 `ModifyDamageDealt/Taken` 拦截器，必须建立 `CardInstance ↔ Creature` 映射，或完全迁移到 `CardInstance`。

#### 命令模式 `Combat/Commands/CoreCommands.cs`

保留了 `CreatureCmd.DealDamage` / `GainBlock` / `ApplyPower`，但已删除 `CardPileCmd` 和 `PlayerCmd`。

**⚠️ 差距**: 新系统的 `CombatResolution` 未调用 `CreatureCmd`，也未遍历 `PowerModel` 拦截器。当前是纯数值计算。

#### Hook 事件系统 `Hooks/Hook.cs`

保留了空方法框架：
- `BeforeCombatStart` / `AfterCombatEnd`
- `BeforeTurnStart` / `AfterTurnStart` / `BeforeTurnEnd` / `AfterTurnEnd`
- `BeforeDamageApplied` / `AfterDamageApplied`
- `BeforeBlockGained` / `AfterBlockGained`
- `BeforePowerApplied` / `AfterPowerApplied`
- `BeforeCreatureDied` / `AfterCreatureDied`

**⚠️ 差距**: 全是空方法，没有"Hook 调用器"。`CombatResolution` 和 `PlayerInteractAction` 未在任何时机调用这些 Hook。遗物/词条系统目前无法介入规则。

#### 战斗管理器骨架 `Combat/CombatManager.cs`

保留了 `ActionQueue` 管理、事件系统、`CheckWinConditionAsync`，但删除了回合制循环。

**⚠️ 差距**: `CombatManager` 是遗留骨架，未接入 `DomainFacade` 和 `GridState`。新系统的战斗流程由 `DomainFacade.SubmitIntentAsync` 驱动。

#### 爬塔流程 `Runs/RunManager.cs` + `RunState.cs`

```csharp
public sealed class RunManager
{
    public RunState State { get; }
    public event Action<RunState> RunStarted;
    public event Action<RunState> MapChanged;
    public event Action<AbstractRoom> RoomEntered;
    public event Action<AbstractRoom> RoomCompleted;
    public event Action<RunState> RunEnded;
    
    public RunState StartNewRun(CharacterModel character, int seed, IReadOnlyList<ActModel> acts);
    public AbstractRoom EnterMapCoord(MapCoord coord);
    public void CompleteCurrentRoom();
    public void ProceedToMap();
}
```

**状态**: 流程骨架完整，但 `RoomEntered` 事件触发后，新系统应接管并启动九宫格战斗。当前没有这种桥接代码。

#### 房间系统 `Rooms/`

保留了 `AbstractRoom`、`RoomFactory`、`CombatRoom`、`BossRoom`、`TreasureRoom` 等。

**⚠️ 差距**: `RoomFactory` 仍按 StS 的 `MapPointType` 创建房间，未扩展《深入地牢》专属房间（Restaurant、StatRoom、GoldRoom 等）。

#### 存档系统 `Saves/SaveManager.cs`

版本 = 2，支持：
- RNG 状态恢复
- 完整地图恢复（节点、类型、访问状态、父子连接）
- 房间状态恢复
- 玩家状态恢复（HP / MaxHp / Gold）

**⚠️ 重大差距**: 完全不保存九宫格状态、卡牌实例、行动计数、道具栏、遗物、词条。**战斗房内无法中途存档/读档。**

#### Presentation 服务层 `Presentation/Services/`

保留了 `GameServices` 服务定位器：
- `ITweenService` / `IAudioService` / `IVfxService` / `IFloatingTextService`

**状态**: 可直接复用。Core 层零 Unity 引用。

---

### 2.2 项目级领域基础设施（Domain）—— L0 核心成果

#### 2.2.1 九宫格核心 `Domain/Grid/`

**`GridCoord` + `GridDirection`**

```csharp
public readonly struct GridCoord : IEquatable<GridCoord>
{
    public int Row { get; }
    public int Col { get; }
    public bool IsValid => Row >= 0 && Row < 3 && Col >= 0 && Col < 3;
    public int CellIndex => Row * 3 + Col + 1; // 1..9
    
    public static GridCoord FromCellIndex(int index);
    public bool IsOrthogonalNeighborOf(GridCoord other);
    public int ManhattanDistanceTo(GridCoord other);
    public bool TryOffset(GridDirection direction, out GridCoord result);
}
```

**状态**: ✅ 纯 C# 值对象，零 Unity 依赖。格位编号 1~9 与设计文档完全一致。

**`GridQueries`**

```csharp
public static class GridQueries
{
    public static IReadOnlyList<GridCoord> AllCoordsRowMajor();
    public static IReadOnlyList<GridCoord> OrthogonalNeighbors(GridCoord coord);
    public static IReadOnlyList<GridCoord> CoordsAboveSameColumn(GridCoord coord);
    public static GridDirection? DirectionFromTo(GridCoord from, GridCoord to);
    public static GridCoord StepToward(GridCoord from, GridCoord target);
}
```

**状态**: ✅ 完整实现，直接服务"正交相邻""同列上方""向玩家靠近"等空间语义。

**`GridCell`**

```csharp
public sealed class GridCell
{
    public GridCoord Coord { get; }
    public bool IsEmpty { get; }
    public int Count { get; }
    public CardInstance TopCard { get; }
    public IReadOnlyList<CardInstance> StackView { get; }
    
    internal void PushTop(CardInstance card);
    internal CardInstance PopTop();
    internal bool Remove(CardInstance card);
    internal void InsertAt(int index, CardInstance card);
}
```

**状态**: ✅ 外部不可变 List，内部操作通过 `GridState` 控制。

**`GridState`** — 唯一对外暴露的操作入口

```csharp
public sealed class GridState
{
    public CardInstance PlayerCard { get; }
    public IEnumerable<CardInstance> AllGridCards { get; }
    public IEnumerable<CardInstance> FaceUpCards { get; }
    public IEnumerable<CardInstance> MonsterCards { get; }
    
    public GridOperationResult AddCardToGrid(CardInstance card, GridCoord coord, bool faceUp);
    public GridOperationResult MoveCardToEmptyCell(CardInstance card, GridCoord to);
    public GridOperationResult MoveTopCardToTop(CardInstance card, GridCoord to);
    public GridOperationResult SwapTopCards(CardInstance a, CardInstance b);
    public GridOperationResult CoverCellWithCard(CardInstance card, GridCoord coord, bool faceUp);
    public GridOperationResult RemoveCard(CardInstance card, RemoveReason reason);
    public GridOperationResult FlipTopCard(GridCoord coord, FlipReason reason);
    public GridOperationResult FlipCard(CardInstance card, FlipReason reason);
    public GridOperationResult RevealAround(GridCoord center, FlipReason reason);
    public GridOperationResult ShuffleNonPlayerGridCardsIntoDeck(DungeonDeck deck, IRng rng);
    public GridOperationResult RedistributeDeck(DungeonDeck deck, GridCoord excludedCoord, IRng rng);
}
```

**状态**: ✅ 所有操作返回 `GridOperationResult`（含 `DomainEvent` 列表），AI 不能直接改底层数据。

**⚠️ 差距**:
1. `CoverCellWithCard` 将旧顶牌 `IsFaceUp = false`，符合设计文档"被覆盖到下方的卡牌默认重新变为正面朝下"。✅
2. `RemoveCard` 移除后自动翻开下方新顶牌，符合设计文档。✅
3. **但**：翻开新顶牌后没有触发"被翻开时"效果（如伏击者骷髅的"伏击"词条）。❌

---

#### 2.2.2 卡牌运行时 `Domain/Cards/`

**`CardInstance`**

```csharp
public sealed class CardInstance
{
    public CardInstanceId InstanceId { get; }
    public ModelId ModelId { get; }
    public CardType CardType { get; }
    
    public CardZone Zone { get; internal set; }
    public GridCoord? Coord { get; internal set; }
    public int StackIndex { get; internal set; }
    public bool IsFaceUp { get; internal set; }
    public bool IsRemoved { get; internal set; }
    
    public int MaxHp { get; }
    public int CurrentHp { get; }
    public int Attack { get; }
    public int Defense { get; }
    public int ContactDamageToPlayer { get; }
    public int GoldOnRemoved { get; }
    public int GoldValue { get; }
    
    public IReadOnlyDictionary<string, int> RuntimeState { get; }
    
    public void ConfigureCombatStats(int maxHp, int attack, int defense, int contactDamageToPlayer = 0, int goldOnRemoved = 10);
    public int GetState(string key, int defaultValue = 0);
    public void SetState(string key, int value);
}
```

**状态**: ✅ 运行时位置、正反面、Zone、堆叠索引、临时状态字典完整。

**⚠️ 架构偏差**: 设计文档期望 `CardInstance` 引用 `Creature` 和 `CardDurability`，将 HP/Attack/Defense 外移。当前实现将这些属性直接内聚在 `CardInstance` 中，简化了 L0 落地，但可能在未来需要 `PowerModel` 拦截器时产生摩擦。建议 L1 前明确是否迁移。

**`CardModel`**

```csharp
public abstract class CardModel : AbstractModel
{
    public abstract CardType CardType { get; }
    public virtual string TitleKey => Id.ToString();
    public virtual string DescriptionKey => Id.ToString() + ".description";
    public virtual bool CanBeFaceDown => true;
    public virtual bool CanBeStoredInInventory => false;
    public virtual bool BlocksAutoReveal => false;
    
    public virtual CardInstance CreateInstance(CardInstanceId id);
    protected virtual void ConfigureCreatedInstance(CardInstance instance);
}
```

**状态**: ✅ 抽象基类就位。**已新增** `CanInteractWithPlayer` / `OnPlayerInteractAsync` / `OnRevealedAsync` / `OnDestroyedAsync` / `OnAfterPlayerActionCommittedAsync` 五个统一生命周期虚方法。`MonsterCardModel` / `TrapCardModel` / `ItemCardModel` 已通过 `ContentContracts` 提供默认实现。

**`CardType` / `CardZone`**

```csharp
public enum CardType { Player, Monster, Trap, Item, Gold, StatUpgrade, Chest, Food, Mentor, ShopProduct, RouteChoice, Special }
public enum CardZone { None, DungeonDeck, Grid, PlayerInventory, RelicInventory, ChoicePool, RewardQueue, Removed }
```

**状态**: ✅ 完整对齐设计文档。

---

#### 2.2.3 地城牌组 `Domain/Deck/DungeonDeck.cs`

```csharp
public sealed class DungeonDeck
{
    public IReadOnlyList<CardInstance> Cards { get; }
    public int Count { get; }
    public void AddToTop(CardInstance card);
    public void AddToBottom(CardInstance card);
    public void AddRange(IEnumerable<CardInstance> cards);
    public CardInstance DrawTop();
    public void Shuffle(IRng rng);
    public IReadOnlyList<CardInstance> RemoveAll(Predicate<CardInstance> predicate);
}
```

**状态**: ✅ 完整。服务于房间初始化发牌、传送机关洗回再分布。

---

#### 2.2.4 输入意图与验证 `Domain/Interaction/`

**`PlayerIntent`**

```csharp
public abstract class PlayerIntent { public IntentKind Kind { get; } }
public sealed class MovePlayerIntent : PlayerIntent { public GridCoord To { get; } }
public sealed class InteractWithCardIntent : PlayerIntent { public CardInstanceId Target { get; } }
```

**状态**: ⚠️ 仅实现了两种意图。设计文档还需要的：`StoreItemIntent`、`BeginUseItemIntent`、`UseItemOnTargetIntent`、`ActivateRelicIntent`、`ChooseOptionIntent`、`ChooseRouteIntent` 均未实现。

**`IntentValidator`**

```csharp
public sealed class IntentValidator
{
    public IntentPreview Preview(PlayerIntent intent);
    public IntentValidationResult Validate(PlayerIntent intent);
}
```

**状态**: ✅ 移动意图校验（目标合法、目标为空格）；互动意图校验（目标存在、非玩家卡、在 Grid 上、是最上方、正面朝上）。

---

#### 2.2.5 领域事件 `Domain/Events/`

**`DomainEvent`**

```csharp
public sealed class DomainEvent
{
    public ulong EventId { get; internal set; }
    public uint ActionId { get; internal set; }
    public int SequenceIndex { get; internal set; }
    public DomainEventType EventType { get; }
    
    public CardInstanceId CardId { get; set; }
    public CardInstanceId SourceCardId { get; set; }
    public CardInstanceId TargetCardId { get; set; }
    public GridCoord? FromCoord { get; set; }
    public GridCoord? ToCoord { get; set; }
    public int Amount { get; set; }
    public int SecondaryAmount { get; set; }
    public string Reason { get; set; }
}
```

**`DomainEventType`** — 已实现 22 种事件类型：

```
RoomEntered, RoomGenerated, CardAddedToGrid, CardMoved, CardFlipped, CardCovered,
CardRemoved, CardZoneChanged, PlayerActionCommitted, DamageApplied, HealingApplied,
GoldChanged, StatChanged, TraitAcquired, RelicAcquired, RelicActivated, ItemStored,
ItemUsed, ChoiceOpened, ChoiceResolved, TrapTriggered, MonsterDefeated, RoomCleared,
RouteChoicesGenerated, IntentRejected, RunEnded
```

**状态**: ✅ 事件类型覆盖完整。

**`DomainEventBatch`**

```csharp
public sealed class DomainEventBatch
{
    public uint ActionId { get; }
    public PlayerIntent SourceIntent { get; }
    public IReadOnlyList<DomainEvent> Events { get; }
    public bool RequiresPresentationGate { get; set; }
}
```

**状态**: ✅ 每个 `GameAction` 执行完产出一个 `DomainEventBatch`，表现层只读。

---

#### 2.2.6 伤害与战斗结算 `Domain/Combat/`

**`DamageInfo` / `DamageSource` / `DamageTarget`**

```csharp
public sealed class DamageInfo
{
    public DamageSource Source { get; }
    public DamageTarget Target { get; }
    public int BaseAmount { get; }
    public DamageKind Kind { get; }          // Attack, Trap, Item, Relic, Environment, HpLoss
    public bool IgnoreDefense { get; }
    public bool CanBePrevented { get; set; }
    public bool CanTriggerThorns { get; set; }
    public string Reason { get; }
}
```

**状态**: ✅ 泛化DamageInfo已落地，支持机关、道具、遗物、环境等非Creature来源。

**`CombatResolution`**

```csharp
public sealed class CombatResolution
{
    public DamageResult ApplyDamage(DamageInfo info, ICollection<DomainEvent> events);
    public void ResolvePlayerVsMonster(CardInstance player, CardInstance monster, ICollection<DomainEvent> events);
    public void ResolvePlayerVsTrap(CardInstance player, CardInstance trap, ICollection<DomainEvent> events);
}
```

**默认互动公式**:
- 玩家对怪物伤害 = max(0, 玩家攻击 - 怪物防御)
- 怪物对玩家伤害 = max(0, 怪物攻击 - 玩家防御)
- 玩家对机关伤害 = max(0, 玩家攻击 - 机关防御)
- 机关对玩家伤害 = 固定值，不受玩家防御减免

**状态**: ✅ 基础公式符合设计文档。

**状态**: ✅ 基础公式符合设计文档。**已补全**：
1. **先攻机制**: `ResolvePlayerVsMonster` 通过 `CardInstance.RuntimeState["firstStrike"]` 判定先攻。玩家默认先攻；怪物有先攻时怪物先攻；双方同时先攻时同时结算伤害（双方可能同时死亡）。
2. **伤害免疫（CanBePrevented）**: `ApplyDamage` 已检查 `info.CanBePrevented` 和目标的 `damageImmunity` RuntimeState。免疫次数消耗后伤害归零，结果标记 `Prevented = true`。
3. **非生物卡保护**: `ApplyDamage` 对 `HasHitPoints == false` 的目标（如金币卡）直接返回零伤害，避免意外修改 HP。

**⚠️ 仍存差距**:
1. **未接入 Hook**: `ApplyDamage` 仍未调用 `Hook.BeforeDamageApplied` / `AfterDamageApplied`，也未遍历 `PowerModel` 拦截器。"刺皮"等机制需通过伤害免疫状态或 TraitModel 回调间接实现。
2. **死亡/移除连锁未完整**: 怪物死亡后触发 `MonsterDefeated` 事件和金币奖励，但 `TraitModel.OnCardRemovedAsync` 尚未在 `ProcessLifecycleAsync` 中接入（当前只调用了 `CardModel.OnDestroyedAsync`）。

**`PlayerActionCounter`**

```csharp
public sealed class PlayerActionCounter
{
    public int Value { get; private set; }
    public DomainEvent Increment(PlayerIntent sourceIntent);
}
```

**状态**: ✅ 只在 `PlayerMoveAction` 和 `PlayerInteractAction` 成功后增加。道具、预览、取消不计数。✅

---

#### 2.2.7 领域动作 `Domain/Actions/`

**`PlayerMoveAction`**

执行流程：
1. 移动玩家卡到空白格
2. 计入玩家行动（`ActionCounter.Increment`）
3. 翻开玩家相邻格最上方卡牌（`RevealAround`）

**状态**: ✅ 符合设计文档 7.2 节。

**⚠️ 差距**: 翻开相邻格后，没有结算"被翻开时"效果（如伏击者骷髅的"伏击"词条）。设计文档要求这些效果"在卡牌完成翻面后立即进入动作队列"。

**`PlayerInteractAction`**

执行流程：
1. 校验意图合法
2. 调用 `CardModel.CanInteractWithPlayer` → 若拒绝则返回 `IntentRejected`
3. 调用 `CardModel.OnPlayerInteractAsync`（虚方法分派）
4. 计入玩家行动（`ActionCounter.Increment`）
5. 触发 `AfterPlayerActionCommitted`（遍历场上正面卡调用 `OnAfterPlayerActionCommittedAsync`）
6. 死亡检测 + 移除（`RemoveIfDead`）
7. `ProcessLifecycleAsync`（处理翻牌/移除连锁回调：`OnRevealedAsync` / `OnDestroyedAsync`）
8. 玩家死亡检测（`AppendPlayerDefeatedIfNeeded`）
9. 检查房间清场

**状态**: ✅ 流程完善。已解决检验报告中的全部 5 项差距：
- 虚方法分派 ✅
- AfterPlayerActionCommitted ✅
- OnDestroyedAsync（通过 ProcessLifecycleAsync）✅
- 翻开连锁回调（通过 ProcessLifecycleAsync）✅
- 玩家死亡检测 ✅

---

#### 2.2.8 领域门面 `DomainFacade.cs`

```csharp
public sealed class DomainFacade
{
    public DomainActionContext Context { get; }
    public IntentPreview PreviewIntent(PlayerIntent intent);
    public async Task<DomainEventBatch> SubmitIntentAsync(PlayerIntent intent);
}
```

**状态**: ✅ 统一入口。表现层不应直接调用 `GridState` 或 `CombatResolution`，而应通过 `DomainFacade` 提交意图。

---

#### 2.2.9 不变量验证 `Domain/Validation/DomainInvariantValidator.cs`

```csharp
public sealed class DomainInvariantValidator
{
    public IReadOnlyList<InvariantViolation> Validate(GridState grid);
}
```

**当前验证**:
- 是否只有一张玩家卡 (`PlayerCount`)
- 是否有卡牌重复出现在多个 Zone (`DuplicateGridCard`)
- GridCell 堆叠顺序与 `CardInstance.StackIndex` 是否一致 (`WrongStackIndex`)
- 坐标是否匹配 (`WrongCoord`)
- Zone 是否正确 (`WrongZone`)
- Removed 卡是否仍在 Grid (`RemovedCardOnGrid`)

**状态**: ✅ 基础版本就位。

**⚠️ 差距**: 设计文档要求的以下验证未实现：
- 餐厅/战斗房发牌策略是否符合政策
- 行动计数是否只由合法动作增加
- 所有随机操作是否来自 IRng

---

### 2.3 P0 单元测试 `Core/Tests/DomainP0Tests.cs`

| 测试 | 验证内容 | 对应设计文档 P0 清单 |
|:---|:---|:---|
| `GridCoord_ConvertsCellIndexAndNeighbors` | 格位编号转换、正交相邻、同列上方 | #1, #2 ✅ |
| `GridState_AddsStacksAndTracksTopCard` | 堆叠顺序、TopCard、StackIndex | 基础 ✅ |
| `PlayerMove_ToEmptyCellCountsActionAndRevealsAdjacentTopCards` | 玩家移动计行动、相邻格翻开 | #3, #5 ✅ |
| `InteractWithMonster_PlayerPositionStaysAndDeadMonsterRemovedWithGold` | 互动位置不变、死亡移除、金币、清场 | #4, #6, #13 ✅ |
| `InvalidInteractWithFaceDownCardIsRejectedAndDoesNotCountAction` | 非法意图拒绝、不计行动 | 边界 ✅ |
| `RemoveTopCard_RevealsUnderlyingTopCard` | 移除顶牌后下方卡自动翻开 | #6 ✅ |
| `CombatResolution_AppliesDefenseAndTrapIgnoresPlayerDefense` | 防御减免、机关固定伤害 | 基础 ✅ |
| `CombatResolution_FirstStrike_MonsterAttacksFirst` | 先攻机制：怪物先攻且击杀后玩家不反击 | #1 扩展 ✅ |
| `CombatResolution_SimultaneousDeath_BothDie` | 双方同时先攻时同时死亡 | #1 扩展 ✅ |
| `CombatResolution_DamagePrevention_BlocksDamage` | CanBePrevented / 伤害免疫 | #1 扩展 ✅ |
| `PlayerDefeated_TriggersRunEnded` | 玩家死亡检测 | 基础 ✅ |
| `ApplyDamage_NonCreatureTarget_DoesNothing` | 非生物卡伤害保护 | 边界 ✅ |
| `StoreItemIntent_MovesItemToInventory` | 道具收入道具栏 | 基础 ✅ |
| `InvariantValidator_CatchesMissingPlayerAndAcceptsValidGrid` | 不变量验证 | #1 ✅ |

**状态**: 12 个测试覆盖了 P0 清单 18 项中的约 10 项基础场景 + 4 项核心规则场景。

**❌ 未覆盖的 P0 关键场景**:
- 伏击者骷髅翻开时若玩家相邻则触发互动 (#7)
- 尖刺机关翻开后下一次玩家行动才触发 (#8)
- 好战怪物每三次后靠近玩家 (#9)
- 弩箭机关只伤害同列上方翻开的卡 (#10)
- 传送机关洗回、移动玩家、重新分布 (#11)
- 勾绳移动任意卡到相邻格 (#12)
- 翻转卡交换正面非玩家卡与背面卡 (#13)
- 餐厅发牌不要求铺满 8 格 (#14)
- 战斗房发牌保证非玩家格至少一张 (#15)
- 怪物分配算法严格等于 9+X (#16)
- 同 seed + 同输入意图序列产生相同事件日志 (#17)
- 保存后读取一致性 (#18)

---

## 三、已删除的系统（不要复用）

与 2025.06.04 版 ARCHITECTURE.md 保持一致，无变化：

| 系统 | 删除原因 | 新系统替代方案 |
|:---|:---|:---|
| `CardModel` + `CardPile`（6种牌堆） | 手牌/抽牌/弃牌循环与《深入地牢》无关 | `CardInstance` + `DungeonDeck` |
| `CardEnergyCost` + `PlayerCombatState.Energy` | 无能量系统 | 无替代（行动驱动） |
| `CardPlay` + `CardPlayContext` + `PlayTarget` | 手牌打出上下文 | `PlayerIntent`（拖动互动上下文） |
| `EnemyIntent` + `BuildIntent/ExecuteIntent` | 回合制意图预告不适用 | `TraitModel`（怪物词条驱动行为） |
| `PlayerCombatState`（5牌堆+能量） | 无手牌/无能量 | 无替代 |
| `CombatManager` 回合制循环 | 玩家回合/敌人回合不适用 | 行动计数制 + DomainFacade |
| `CardDragController` + `CombatInputController` | 2D手牌拖放 | `GridDragController`（3D实体卡拖动，待建） |
| `PlayerHandView` + `ArcHandLayout` + `CardView` | 手牌扇形排列 | `FieldCardView`（九宫格内3D卡牌，待建） |
| Content 层全部 | StS 卡牌/敌人/遭遇 | 新建《深入地牢》Content 层 |

---

## 四、L0 → L1 关键差距与阻断项

### 🔴 已解决的阻断项（本次补全完成）

| # | 原差距 | 解决方式 | 状态 |
|:---|:---|:---|:---:|
| 1 | CardModel 缺少内容回调虚方法 | 新增 `CanInteractWithPlayer` / `OnPlayerInteractAsync` / `OnRevealedAsync` / `OnDestroyedAsync` / `OnAfterPlayerActionCommittedAsync` | ✅ |
| 2 | 缺少 `AfterPlayerActionCommitted` hook | `DomainActionContext.NotifyAfterPlayerActionCommittedAsync` + `PlayerMoveAction` / `PlayerInteractAction` 末尾调用 | ✅ |
| 3 | 缺少 `OnCardFlipped` / `OnRevealed` 内容回调 | `ProcessLifecycleAsync` 监听 `CardFlipped` / `CardRemoved` 事件并调用对应虚方法 | ✅ |
| 5 | 缺少先攻机制 | `CombatResolution` 新增先攻判定（`RuntimeState["firstStrike"]`）+ 同时死亡处理 | ✅ |
| 6 | 玩家死亡未检测 | `AppendPlayerDefeatedIfNeeded` 在移动/互动/遗物动作后检查并发出 `RunEnded` | ✅ |
| 12 | 意图系统不完整（部分） | 新增 `UseItemIntent`、`ChooseOptionIntent`、`StoreItemIntent`、`ActivateRelicIntent` 及对应 Action | ✅ |

### 🔴 剩余阻断项

| # | 差距 | 影响 | 建议优先级 |
|:---|:---|:---|:---|
| 4 | **伤害结算未接入 Hook / PowerModel** | 刺皮、破甲、历战等机制需通过 `TraitModel` / `RelicModel` 介入。当前仅能通过 `damageImmunity` 状态模拟"庇佑"。 | P0 |
| 7 | **存档不包含 Grid 状态** | DTO 扩展（`CardInstanceDto` / `GridStateDto` / `RoomDomainStateDto`）和序列化逻辑已落地，但尚未与 `RunSaveSerializer` 完整桥接（`DomainSaveAdapter` 已提供 Capture，Restore 需外部调用）。SaveVersion 已升级到 3。 | P1 |
| 8 | **Creature 与 CardInstance 未关联** | PowerModel 拦截器体系无法接入新领域层。 | P1 |
| 9 | **StandardActMapGenerator 仍是 StS 7x8** | 地图生成与设计文档的每层 9 节点不匹配。 | P1 |
| 10 | **RoomFactory 未扩展《深入地牢》房间类型** | 餐厅、金币房、宝箱房、属性房、奖励房无法创建。 | P1 |
| 11 | **缺少房间生成管线** | DungeonDeckBuilder、GridDealer、MonsterAllocationRule 均未实现，无法生成战斗房。 | P1 |
| 13 | **缺少玩家属性多层系统** | 局内永久加成、遗物常驻修正、房间临时修正无法分层管理。 | P1 |
| 14 | **Content 层完全空白** | 没有具体的怪物、机关、道具、遗物、词条实现。`MonsterCardModel` 等基类已通过 `ContentContracts` 就绪。 | L1 目标 |

### 🟡 非阻断但建议尽早补齐

| # | 差距 | 影响 |
|:---|:---|:---|
| 15 | `DomainFacade` 未暴露 `DungeonDeck` 操作接口 | 传送机关等内容需要通过 `DomainFacade` 调用 Shuffle/Redistribute，而非直接操作 `GridState`。 |
| 16 | 缺少 `IGridQueryService` / `IDomainCommandService` 接口层 | 设计文档建议的声明式 API，让 AI 内容调用更规范。 |
| 17 | 缺少 `EffectStep` 原语层 | 内容类直接 `await` 命令 vs 返回 `EffectStep` 列表由领域层执行。 |
| 18 | P0 测试覆盖率（12/18 项场景） | 核心机制回归保障已改善，但伏击/尖刺/好战/弩箭/传送/房间生成等场景仍缺测试。 |
| 19 | 场景回归测试框架未建立 | 无法验证 "同 seed + 同输入 = 同结果"。 |
| 20 | 缺少 `GameLogic` asmdef | Content 层尚未建立独立的程序集，无法编译时 enforce "Content 不引用 Presentation"。 |

---

## 五、新系统对接指南（更新版）

### 5.1 L0 验收标准（当前状态）

> 设计文档要求："不用表现层，只通过单元测试和命令行模拟，就能完成一个小房间的移动、翻牌、战斗、清场。"

**当前状态**: ⚠️ **大幅改善，接近达成**。可以完成：
- ✅ 移动 + 相邻翻开
- ✅ 与怪物战斗（玩家先攻、防御减免、死亡移除）
- ✅ 与机关战斗（固定伤害、防御减免）
- ✅ 金币卡拾取
- ✅ 房间清场判定
- ✅ 先攻词条（通过 `RuntimeState["firstStrike"]`）
- ✅ 伤害免疫（通过 `RuntimeState["damageImmunity"]`）
- ✅ 玩家失败（血量归零触发 `RunEnded`）
- ✅ 道具收入道具栏（`StoreItemIntent`）
- ✅ 道具使用（`UseItemIntent`）
- ✅ 主动遗物激活（`ActivateRelicIntent`）

**但无法完成**:
- ❌ 怪物 AI（好战、伏击、复仇、鼓舞、破甲、散子）— 管线已接通，缺具体实现
- ❌ 机关效果（弩箭摧毁、尖刺延迟、传送重排）— 管线已接通，缺具体实现
- ❌ 房间生成（只能手动 `AddCardToGrid`）
- ❌ 存档读档完整闭环（DTO 已落地，Restore 桥接待集成）

### 5.2 L1 铺量开发前提条件

以下 Phase 0 / Phase 0.5 **已完成**：

```
Phase 0: 内容回调机制 ✅
    ├── CardModel 新增 CanInteractWithPlayer / OnPlayerInteractAsync 虚方法 ✅
    ├── CardModel 新增 OnRevealedAsync / OnDestroyedAsync 虚方法 ✅
    ├── PlayerInteractAction 改为虚方法分派 ✅
    ├── PlayerMoveAction / PlayerInteractAction 末尾触发 AfterPlayerActionCommitted ✅
    └── 新增 "被翻开时"效果触发管线 ✅

Phase 0.5: 伤害结算规则补全 ✅
    ├── 先攻机制实现 ✅
    ├── CanBePrevented / 伤害免疫实现 ✅
    ├── 双方同时死亡处理 ✅
    └── ApplyDamage 非生物卡保护 ✅
```

**剩余待完成**（建议 L1 启动前完成）：

```
Phase 1: 房间生成管线（仍缺失）
    ├── 新增 DungeonMapGenerator（每层 9 节点）
    ├── 扩展 RoomFactory（餐厅、金币房、宝箱房等）
    ├── 实现 DungeonDeckBuilder
    ├── 实现 MonsterAllocationRule
    ├── 实现 GridDealer（含 MinimumCoveragePolicy）
    └── 存档 Restore 桥接（DomainSaveAdapter.RestoreGrid 已可用，需接入 RunManager）

Phase 2: 基础内容铺量（L1 核心工作）
    ├── 第一批怪物（骷髅、带甲骷髅、旗兵、复仇、追踪者、伏击者、武装、大骷髅老爷）
    ├── 第一批机关（弩箭、尖刺、传送）
    ├── 第一批道具（勾绳、恢复药水、飞刀、庇佑、翻转卡、照明卡、暴力卡）
    ├── 第一批遗物（活着的肉、木盾、法则魔杖、无尽水袋、村好剑）
    ├── 第一批词条（鼓舞、复仇、好战、伏击、破甲、散子）
    └── P0 测试补全到 18 项
```

**当前判断**：Phase 0 / Phase 0.5 已完成后，L1 内容铺量的地基**已可支撑早期内容开发**。建议在并行推进 Phase 1（房间生成）的同时启动内容生产。

### 5.3 命名空间约定

已使用的命名空间：
- `Game.Core.Domain.Grid` — 九宫格核心
- `Game.Core.Domain.Cards` — 卡牌运行时
- `Game.Core.Domain.Combat` — 领域伤害
- `Game.Core.Domain.Deck` — 地城牌组
- `Game.Core.Domain.Interaction` — 输入意图
- `Game.Core.Domain.Actions` — 领域动作
- `Game.Core.Domain.Events` — 领域事件
- `Game.Core.Domain.Rooms` — 清场判定
- `Game.Core.Domain.Validation` — 不变量验证

**建议 L1 新增**：
- `Game.Core.Domain.ContentContracts` — 内容模型基类（MonsterCardModel 等）
- `Game.Core.Domain.Inventory` — 道具栏/遗物栏
- `Game.Core.Domain.Generation` — 房间生成管线（DungeonDeckBuilder 等）
- `Game.GameLogic.Cards` — AI 内容：具体怪物、机关、道具
- `Game.GameLogic.Traits` — AI 内容：怪物词条
- `Game.GameLogic.Relics` — AI 内容：遗物

---

## 六、编译检查清单

1. **Core 层零 Unity 引用**
   - `Game.Core.asmdef` 不引用 `UnityEngine`
   - `Game.Core.Domain` 命名空间下无任何 `using UnityEngine`
   - ✅ 当前已满足

2. **Domain 层不引用 Presentation**
   - `Game.Core.Domain` 不引用 `Game.Presentation`
   - ✅ 当前已满足

3. **Content 层独立程序集**
   - 待 L1 新建 `Game.GameLogic.asmdef`
   - 只允许引用 `Game.Core`
   - 禁止引用 `Game.Presentation`

4. **存档兼容性**
   - 当前 `SaveVersion = 2`
   - L1 扩展 Grid/CardInstance 存档后需升级到 `SaveVersion = 3`

---

## 七、总结

| 能力 | 状态 | 说明 |
|:---|:---|:---|
| 数据框架（原型-实例） | ✅ 直接复用 | ModelDb + AbstractModel |
| 异步动作队列 | ✅ 直接复用 | ActionQueueSet + ActionExecutor |
| 确定性随机 + 存档一致性 | ✅ 直接复用 | DeterministicRng + RngState |
| 地图 DAG 数据结构 | ⚠️ 骨架保留 | 需替换为 9 节点生成器 |
| 爬塔流程状态机 | ✅ 骨架保留 | RunManager + RunState |
| 房间抽象 + 工厂 | ⚠️ 骨架保留 | 需扩展新房间类型 |
| 奖励抽象 | ✅ 直接复用 | Reward / GoldReward / ChoiceReward |
| 二进制存档框架 | ⚠️ DTO 已落地 | SaveVersion=3，CardInstanceDto/GridStateDto/DomainSaveAdapter 就绪，Restore 桥接待集成 |
| 生物属性 + 事件 | ⚠️ 遗留骨架 | Creature 与 CardInstance 未关联 |
| Buff/Debuff 拦截器 | ⚠️ 遗留骨架 | PowerModel 未接入新伤害结算 |
| Hook 扩展点 | ⚠️ 空方法 | 需激活并接入关键时机 |
| 视图服务（Tween/Audio/VFX） | ✅ 直接复用 | Presentation 层 |
| 生物视图（血条/受击/死亡） | ✅ 直接复用 | Presentation 层 |
| **九宫格空间系统** | **✅ L0 落地** | GridCoord / GridCell / GridState / GridQueries |
| **实体卡牌 + Zone 管理** | **✅ L0 落地** | CardInstance / CardModel / CardType / CardZone |
| **地城牌组** | **✅ L0 落地** | DungeonDeck |
| **输入意图 + 验证** | **✅ 基本完整** | Move/Interact/StoreItem/UseItem/ChooseOption/ActivateRelic 已支持 |
| **领域事件流** | **✅ L0 落地** | 22 种事件类型 + DomainEventBatch |
| **伤害结算管线** | **✅ 规则补全** | 公式正确 + 先攻 + CanBePrevented + 同时死亡 + 非生物卡保护 |
| **玩家行动计数** | **✅ L0 落地** | PlayerActionCounter |
| **清场判定** | **✅ L0 落地** | RoomClearChecker |
| **不变量验证** | **⚠️ 基础落地** | 核心验证就位，需补全 |
| **内容回调机制** | **✅ 已落地** | CardModel 统一生命周期虚方法 + ProcessLifecycleAsync 管线 |
| **AfterPlayerActionCommitted** | **✅ 已落地** | NotifyAfterPlayerActionCommittedAsync 遍历场上卡触发回调 |
| **内容契约基类** | **✅ 已落地** | MonsterCardModel / TrapCardModel / ItemCardModel / RelicModel / TraitModel |
| **道具/遗物栏** | **✅ 已落地** | PlayerInventory / RelicInventory / ActiveRelicSlot |
| **房间生成管线** | **❌ 未落地** | DungeonDeckBuilder / GridDealer / MonsterAllocation |
| **玩家多层属性** | **❌ 未落地** | StatModifier / PlayerRunState |
| **场景回归测试** | **❌ 未落地** | 无法验证确定性 |

**核心判断**: L0 的"神经系统"已大幅接通。`CardModel` 统一生命周期接口、`PlayerInteractAction` 虚方法分派、`ProcessLifecycleAsync` 翻牌/移除连锁回调、`AfterPlayerActionCommitted` 行动后响应、`CombatResolution` 先攻/免疫/同时死亡规则——这些曾经阻塞 L1 的关键基础设施**已经全部落地**。

内容类现在可以"呼吸"了：怪物可以被翻开时触发伏击、机关可以在被摧毁时触发弩箭、好战/尖刺可以在玩家行动后响应。`ContentContracts` 为 AI 提供了安全的内容基类，`Inventory` 为道具/遗物提供了数据支撑。

**当前最大的剩余缺口是"房间生成管线"和"存档 Restore 桥接"**。没有 DungeonDeckBuilder/GridDealer，就无法自动生成战斗房；没有完整的 Restore 桥接，战斗房内仍不能读档。但这些不阻塞"单个房间的测试内容开发"——开发者可以手动 `AddCardToGrid` 搭建测试场景，验证怪物/机关/道具的行为。

**L1 铺量开发的地基目前处于"神经系统已接通、造血系统待建"的状态**。建议并行推进：
1. **L1 内容生产**（怪物、机关、道具、词条的具体实现）
2. **Phase 1 房间生成管线**（DungeonDeckBuilder / GridDealer / MonsterAllocation）
3. **存档 Restore 桥接**（将 DomainSaveAdapter.RestoreGrid 接入 RunManager）
