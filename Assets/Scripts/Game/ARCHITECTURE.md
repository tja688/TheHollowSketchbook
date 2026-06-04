# Game 架构文档 —— 清理后基础设施说明

> **版本**: 2025.06.04 —  post-StS-cleanup  
> **目的**: 本文档描述从杀戮尖塔移植代码中保留下来的通用基础设施，以及新系统（《深入地牢》九宫格实体卡牌系统）应如何与之对接。

---

## 一、总览

清理后的代码库保留了 **58 个 C# 文件**（原 87 个），分布在三层：

```
Game/
├── Core/              # 零 Unity 依赖，纯 C# 逻辑层
│   ├── Models/        # 数据框架（原型-实例模式）
│   ├── Actions/       # 异步动作队列
│   ├── Random/        # 确定性随机
│   ├── Map/           # 地图 DAG 数据结构
│   ├── Entities/      # 角色/敌人/生物骨架
│   ├── Combat/        # 战斗状态 + 伤害计算 + 命令
│   ├── Powers/        # Buff/Debuff 拦截器框架
│   ├── Hooks/         # 事件扩展点
│   ├── Rewards/       # 奖励抽象
│   ├── Rooms/         # 房间抽象 + 工厂
│   ├── Runs/          # 爬塔流程状态机
│   ├── Saves/         # 二进制存档
│   ├── Common/        # 枚举 + ModelId
│   ├── Logging/       # 日志
│   └── Compatibility/ # C# 兼容垫片
│
├── Presentation/      # Unity MonoBehaviour + UI + 输入
│   ├── Services/      # 游戏服务（Tween/Audio/VFX/飘字）
│   ├── RunFlow/       # 地图/房间/奖励 UI
│   ├── Combat/        # 生物视图（血条/受击/死亡）
│   ├── Input/         # 射线检测服务
│   └── Bootstrap/     # 启动器
│
└── Content/           # 【已清空，待新建】
```

### 核心设计原则（保留）

| 原则 | 说明 |
|:---|:---|
| **Core / Presentation 严格分离** | Core 层零 Unity 引用，可独立编译和单元测试 |
| **事件驱动** | Core 层通过 C# `event` 暴露状态变化，Presentation 层订阅更新视图 |
| **数据驱动 + 原型模式** | 所有配置继承 `AbstractModel`，通过 `ModelDb` 注册，`CloneMutable()` 创建运行时实例 |
| **命令队列** | 所有战斗行为封装为 `GameAction`，通过 `ActionQueueSet` 顺序异步执行 |
| **确定性** | `DeterministicRng` + `RngState` 确保存档/读档后随机序列完全一致 |

---

## 二、保留的基础设施详细说明

### 2.1 数据框架 —— `Models/` + `Common/`

**文件**: `AbstractModel.cs` / `ModelDb.cs` / `ModelId.cs`

```csharp
// 所有配置数据的基类
public abstract class AbstractModel
{
    public bool IsCanonical { get; }      // true = 注册在 ModelDb 中的原型
    public ModelId Id { get; }
    
    public T CloneMutable<T>() { ... }    // 创建可变的运行时副本
    protected void AssertMutable() { ... } // 防止误改原型
}

// 全局注册表
public static class ModelDb
{
    public static void Register<T>(T model) where T : AbstractModel;
    public static T Get<T>(ModelId id);
    public static T CreateMutable<T>(ModelId id);  // Get + CloneMutable
    public static IReadOnlyList<T> All<T>();
}

// 复合 ID（Category + Entry）
public readonly struct ModelId
{
    public string Category { get; }
    public string Entry { get; }
}
```

**对接方式**:

```csharp
// 1. 定义新模型（示例：怪物配置）
public class MonsterModel : AbstractModel
{
    public override string Name => "骷髅";
    public virtual int BaseHp => 6;
    public virtual int BaseAttack => 2;
    public virtual int BaseDefense => 0;
}

// 2. 注册（通常在 Content 层的初始化代码中）
ModelDb.Register(new SkeletonModel { Id = new ModelId("Monster", "Skeleton") });

// 3. 运行时获取可变实例
MonsterModel instance = ModelDb.CreateMutable<MonsterModel>(new ModelId("Monster", "Skeleton"));
instance.SetCurrentHp(6);  // 安全：这是副本，不影响原型
```

**边界**: 数据框架只管"注册-查找-克隆"，不关心业务逻辑。所有新系统的配置数据都应继承 `AbstractModel`。

---

### 2.2 动作队列 —— `Actions/`

**文件**: `ActionSystem.cs`

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
    public void Clear();  // 取消未执行的队列
}

public sealed class ActionExecutor
{
    public async Task ExecuteAllAsync();
}
```

**对接方式**:

```csharp
// 定义新动作（示例：玩家移动）
public class MovePlayerAction : GameAction
{
    private readonly FieldCard _player;
    private readonly GridSlot _target;
    
    protected override async Task ExecuteActionAsync(GameActionExecutionContext ctx)
    {
        // 1. 执行移动逻辑
        _player.MoveTo(_target);
        // 2. 等待动画（Presentation 层监听事件后播放）
        await WaitForAnimation();
        // 3. 触发后续（如视野更新）
    }
}

// 使用
CombatManager.Instance.EnqueueAction(new MovePlayerAction(player, targetSlot));
await CombatManager.Instance.ProcessActionsAsync();
```

**边界**: `ActionSystem` 只提供"排队 + 顺序执行 + 异步等待"能力。动作的语义（移动/战斗/翻转）由子类定义。

---

### 2.3 确定性随机 —— `Random/`

**文件**: `DeterministicRng.cs`

```csharp
public interface IRng
{
    int NextInt(int minInclusive, int maxExclusive);
    float NextFloat();
    T Pick<T>(IReadOnlyList<T> list);
    void Shuffle<T>(List<T> list);
}

public sealed class DeterministicRng : IRng
{
    public DeterministicRng(RngState state);
    public RngState CaptureState();
}

public readonly struct RngState
{
    public uint Value { get; }
}
```

**对接方式**: `RunState` 持有一个 `IRng` 实例。所有随机操作（房间生成、怪物种类选择、遗物品质）都通过此接口执行。存档时捕获 `RngState`，读档时恢复，确保完全一致的体验。

**边界**: 纯随机数生成器，不参与任何业务逻辑。

---

### 2.4 地图系统 —— `Map/`

**文件**: `ActMap.cs` / `MapCoord.cs` / `MapPoint.cs` / `MapPointType.cs` / `StandardActMapGenerator.cs`

```csharp
public sealed class ActMap
{
    public MapPoint StartingMapPoint { get; }
    public MapPoint BossMapPoint { get; }
    public IReadOnlyList<MapPoint> Points { get; }
    public MapPoint GetPoint(MapCoord coord);
    public void AddPoint(MapPoint point);
    public void Connect(MapPoint parent, MapPoint child);
}

public sealed class MapPoint
{
    public MapCoord Coord { get; }       // (Column, Row)
    public MapPointType PointType { get; set; }
    public bool IsVisited { get; set; }
    public bool IsCompleted { get; set; }
    public IReadOnlySet<MapPoint> Parents { get; }
    public IReadOnlySet<MapPoint> Children { get; }
    public void AddChild(MapPoint child);
}

public readonly struct MapCoord : IEquatable<MapCoord>
{
    public int Column { get; }
    public int Row { get; }
}
```

**对接方式**:

```csharp
// 替换 StS 的 7x8 DAG 生成器为《深入地牢》的每层9节点生成器
public class DungeonMapGenerator
{
    public ActMap Generate(IRng rng, ActModel act)
    {
        var map = new ActMap(1, 9);
        // ... 创建9个节点，按规则连接 ...
        return map;
    }
}

// RunManager 中使用
_runManager = new RunManager(
    mapGenerator: new DungeonMapGenerator(),
    roomFactory: new DungeonRoomFactory(),
    saveManager: new SaveManager()
);
```

**边界**: `ActMap` 只表示"有向无环图 + 节点属性"。节点的具体含义（Monster/Elite/Shop/Restaurant 等）由 `MapPointType` 和房间工厂解释。

---

### 2.5 生物框架 —— `Entities/Creature.cs` + `Powers/PowerModel.cs`

**文件**: `Creature.cs` / `PowerModel.cs` / `Combat/DamageInfo.cs` / `Combat/DamageResult.cs`

```csharp
public sealed class Creature
{
    public int CurrentHp { get; }
    public int MaxHp { get; }
    public int Block { get; }           // 语义已改为"防御值"（攻击-防御=伤害）
    public bool IsAlive { get; }
    public IReadOnlyList<PowerModel> Powers { get; }
    
    public event Action<int, int> HpChanged;       // (old, new)
    public event Action<int, int> BlockChanged;
    public event Action<PowerModel> PowerApplied;
    public event Action<PowerModel> PowerRemoved;
    public event Action<Creature> Died;
    
    public void SetCurrentHp(int value);
    public void SetMaxHp(int value);
    public void SetBlock(int value);     // 注意：原先是"叠加护盾"，现在是"设置防御值"
    public void AddPower(PowerModel power);
    public bool RemovePower(PowerModel power);
}

public abstract class PowerModel : AbstractModel
{
    public abstract string Name { get; }
    public abstract PowerType Type { get; }  // Buff / Debuff
    public int Amount { get; }
    
    // 拦截器方法：子类重写以修改伤害
    public virtual int ModifyDamageDealt(DamageInfo info, int amount) => amount;
    public virtual int ModifyDamageTaken(DamageInfo info, int amount) => amount;
}
```

**对接方式**:

```csharp
// 定义新词条（示例：刺皮）
public class ThornSkinPower : PowerModel
{
    public override string Name => "刺皮";
    public override PowerType Type => PowerType.Buff;
    
    public override int ModifyDamageTaken(DamageInfo info, int amount)
    {
        // 被攻击时对攻击者反击
        if (info.Source != null)
        {
            CombatManager.EnqueueAction(new CounterDamageAction(info.Source, info.Amount));
        }
        return amount;
    }
}

// 在战斗结算中使用
CreatureCmd.DealDamage(combat, attacker, defender, damageAmount);
// DealDamage 内部会自动遍历双方的 PowerModel 并调用 ModifyDamageDealt / ModifyDamageTaken
```

**边界**: 
- `Creature` 只管 HP/Block/Power 的生命周期和事件。
- `Block` 的语义已从"临时护盾"改为"固定防御值"。如果《深入地牢》需要不同的伤害计算方式，可以直接绕过 `CreatureCmd.DealDamage`，自己写结算逻辑。
- `PowerModel` 的拦截器模式适用于简单伤害修改。如果词条逻辑非常复杂（如"历战"的换目标攻击复原），建议直接在新系统中写逻辑，不完全依赖拦截器。

---

### 2.6 命令模式 —— `Combat/Commands/CoreCommands.cs`

**文件**: `CoreCommands.cs`

保留了 `CreatureCmd`，删除了 `CardPileCmd`（抽牌/弃牌/洗牌）和 `PlayerCmd`（耗能量/回能量）。

```csharp
public static class CreatureCmd
{
    public static async Task<DamageResult> DealDamage(CombatState combat, Creature source, Creature target, int amount, DamageType type = DamageType.Attack);
    public static async Task GainBlock(CombatState combat, Creature target, int amount);
    public static async Task ApplyPower(CombatState combat, Creature target, PowerModel power, int amount);
    public static Task RemovePower(Creature target, PowerModel power);
    public static void TakeDamage(Creature target, int amount);
}
```

**对接方式**: 《深入地牢》的伤害计算更简单（`攻击 - 防御`），可以直接调用 `CreatureCmd.DealDamage`（它内部会先算 `modified = amount`，然后遍历 Power 拦截器，最后扣减 HP）。如果不需要拦截器链，也可以直接调用 `target.SetCurrentHp(target.CurrentHp - damage)`。

**边界**: `CreatureCmd` 是"建议性"的工具方法，不是强制性入口。新系统完全可以自己写伤害结算。

---

### 2.7 Hook 事件系统 —— `Hooks/Hook.cs`

**文件**: `Hook.cs`

```csharp
public static class Hook
{
    // 战斗生命周期
    public static Task BeforeCombatStart(CombatState combat);
    public static Task AfterCombatEnd(CombatState combat);
    
    // 回合/行动生命周期（保留接口，语义改为"玩家行动前后"）
    public static Task BeforeTurnStart(CombatState combat);
    public static Task AfterTurnStart(CombatState combat);
    public static Task BeforeTurnEnd(CombatState combat);
    public static Task AfterTurnEnd(CombatState combat);
    
    // 伤害/格挡/能力
    public static Task BeforeDamageApplied(CombatState combat, DamageInfo info);
    public static Task AfterDamageApplied(CombatState combat, DamageInfo info, DamageResult result);
    public static Task BeforeBlockGained(CombatState combat, Creature target, int amount);
    public static Task AfterBlockGained(CombatState combat, Creature target, int amount);
    public static Task BeforePowerApplied(CombatState combat, Creature target, PowerModel power, int amount);
    public static Task AfterPowerApplied(CombatState combat, Creature target, PowerModel power, int amount);
    
    // 死亡
    public static Task BeforeCreatureDied(Creature creature);
    public static Task AfterCreatureDied(Creature creature);
}
```

**对接方式**:

```csharp
// 遗物系统通过订阅 Hook 介入
public class VillageSwordRelic : RelicModel
{
    public override void OnEquipped(Player player)
    {
        // 订阅战斗结束事件
        CombatManager.Instance.CombatWon += OnCombatWon;
    }
    
    void OnCombatWon(CombatState combat)
    {
        // 击败精英/层主后攻击+2
    }
}

// 或者直接在 Hook 方法中插入逻辑（ relic 管理器负责调用）
// Hook.BeforeDamageApplied += (combat, info) => { ... };
```

**边界**: Hook 是**扩展点**，不是**执行管道**。当前实现是空方法（返回 `Task.CompletedTask`）。需要在新系统中建立一个"Hook 调用器"，在关键时机调用这些方法。遗物/词条系统通过替换这些方法的实现来介入。

---

### 2.8 战斗管理器骨架 —— `Combat/CombatManager.cs`

**文件**: `CombatManager.cs` / `CombatState.cs`

```csharp
public sealed class CombatManager
{
    public CombatState State { get; }
    public bool IsInProgress { get; }
    
    public event Action<CombatState> CombatSetUp;
    public event Action<CombatState> CombatWon;
    public event Action<CombatState> CombatEnded;
    public event Action<bool> PlayerActionsDisabledChanged;
    public event Action<CombatState> CreaturesChanged;
    
    public void SetUpCombat(CombatState state);
    public async Task StartCombatAsync();
    public void EnqueueAction(GameAction action);
    public async Task ProcessActionsAsync();
    public async Task<bool> CheckWinConditionAsync();
    public void Reset();
}

public sealed class CombatState
{
    public RunState RunState { get; }
    public IReadOnlyList<Player> Players { get; }
    public IReadOnlyList<Creature> Enemies { get; }
    public bool IsInProgress { get; }
    public bool IsCombatEnded { get; }
    public bool PlayerWon { get; }
    public int ActionCount { get; set; }  // 新增：行动计数
}
```

**边界**: 这是一个**骨架**。StS 的回合制循环（玩家回合/敌人回合/抽牌/能量/EndTurn）已全部删除。保留的部分：
- `ActionQueueSet` 管理 + `ActionExecutor` 执行
- 事件系统（`CombatSetUp` / `CombatWon` / `CombatEnded` / `CreaturesChanged`）
- 胜负判定框架（`CheckWinConditionAsync`）
- 生物生命周期订阅（HP/Block/Power/Died 事件自动触发 `CreaturesChanged`）

**新系统应如何扩展**:

```csharp
// 建议：继承或组合 CombatManager，添加九宫格相关逻辑
public sealed class GridCombatManager
{
    private readonly CombatManager _core = new CombatManager();
    public BattleGrid Grid { get; }  // 新系统的九宫格
    
    public async Task StartCombatAsync(CombatRoom room)
    {
        var state = new CombatState(run, players, enemies);
        _core.SetUpCombat(state);
        
        // 生成九宫格
        Grid.GenerateRoomCards(room);
        Grid.PlacePlayerCard(state.Players[0]);
        Grid.UpdateVisibility();
        
        await _core.StartCombatAsync();
    }
}
```

---

### 2.9 爬塔流程 —— `Runs/RunManager.cs` + `RunState.cs`

**文件**: `RunManager.cs` / `RunState.cs`

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
    public ActMap GenerateActMap();
    public AbstractRoom EnterMapCoord(MapCoord coord);
    public void CompleteCurrentRoom();
    public void ProceedToMap();  // 返回地图选择，Boss 房则 RunEnded
    public void SaveRun();
    public RunState LoadRun();
    public void DeleteRun();
}

public sealed class RunState
{
    public int Seed { get; }
    public IRng Rng { get; }
    public IReadOnlyList<Player> Players { get; }
    public IReadOnlyList<ActModel> Acts { get; }
    public int CurrentActIndex { get; set; }
    public bool IsGameOver { get; set; }
    public ActMap Map { get; set; }
    public MapCoord? CurrentMapCoord { get; set; }
    public AbstractRoom CurrentRoom { get; set; }
    public ActModel CurrentAct { get; }
}
```

**边界**: `RunManager` 管理"一次完整的爬塔流程"，不关心房间内的具体内容。保留的地图推进逻辑：
1. `StartNewRun` → 创建 Player + 生成地图
2. `EnterMapCoord` → 创建房间（通过 `RoomFactory`）
3. `CompleteCurrentRoom` → 生成奖励，标记完成
4. `ProceedToMap` → 返回地图选择（或结束）

**新系统对接点**: `RoomEntered` 事件触发时，新系统应接管并启动九宫格战斗。`RoomCompleted` 事件触发时，显示奖励选择。

---

### 2.10 房间系统 —— `Rooms/`

**文件**: `AbstractRoom.cs` / `RoomFactory.cs` / `CombatRoom.cs` / `BossRoom.cs` / `TreasureRoom.cs` / `EventRoomPlaceholder.cs` / `RestSiteRoomPlaceholder.cs` / `ShopRoomPlaceholder.cs` / `RoomType.cs`

```csharp
public abstract class AbstractRoom
{
    public RoomType RoomType { get; }
    public MapPoint MapPoint { get; }
    public bool IsCompleted { get; }
    public IReadOnlyList<Reward> Rewards { get; }
    public bool HasPendingRewards { get; }
    
    public virtual IReadOnlyList<Reward> GenerateRewards(RunState run);
}

public sealed class RoomFactory
{
    public AbstractRoom CreateRoomForMapPoint(RunState run, MapPoint point);
}
```

**对接方式**:

```csharp
// 扩展房间类型
public class RestaurantRoom : AbstractRoom
{
    public RestaurantRoom(MapPoint mapPoint) : base(RoomType.Restaurant, mapPoint) { }
    
    public override IReadOnlyList<Reward> GenerateRewards(RunState run)
    {
        return new Reward[] { new FoodReward(), new ChoiceReward(new[] { "刺皮", "硬皮", "历战" }) };
    }
}

// 扩展工厂
public class DungeonRoomFactory : RoomFactory
{
    public override AbstractRoom CreateRoomForMapPoint(RunState run, MapPoint point)
    {
        return point.PointType switch
        {
            MapPointType.Restaurant => new RestaurantRoom(point),
            // ... 其他类型
            _ => base.CreateRoomForMapPoint(run, point)
        };
    }
}
```

**边界**: 房间系统只管"创建房间 + 生成奖励"。房间内的具体玩法（九宫格战斗/商店购买/事件选择）不由房间系统实现。

---

### 2.11 奖励系统 —— `Rewards/`

**文件**: `Reward.cs` / `RewardType.cs` / `GoldReward.cs` / `ChoiceReward.cs`（原 CardRewardChoice）/ `RewardGenerator.cs`

```csharp
public abstract class Reward
{
    public bool IsResolved { get; }
    public abstract RewardType Type { get; }
    public abstract string Label { get; }
    public void Resolve(RunState run, Player player);
}

public sealed class GoldReward : Reward { ... }
public sealed class ChoiceReward : Reward  // 通用选择奖励，替代原 CardRewardChoice
{
    public IReadOnlyList<string> Choices { get; }
    public int SelectedIndex { get; }
    public void Select(int index);
    public void Skip();
}
```

**边界**: 奖励系统抽象了"获得某种东西"。`GoldReward` 直接加金币。`ChoiceReward` 是通用选择（当前用字符串标签占位，后续可扩展为带类型数据的选择）。

**对接方式**: 定义新的 Reward 子类（如 `ItemReward`、`RelicReward`、`StatBoostReward`），重写 `Apply` 方法。

---

### 2.12 存档系统 —— `Saves/`

**文件**: `SaveManager.cs` / `RunSaveDto.cs`

**核心能力**:
- 自定义二进制序列化（`BinaryReader` / `BinaryWriter`）
- 确定性 RNG 状态恢复
- 完整地图恢复（节点坐标、类型、访问状态、父子连接）
- 房间状态恢复
- 玩家状态恢复（HP / MaxHp / Gold）

**版本管理**: `SaveVersion = 2`（清理后升级）。读旧存档会失败（因为结构已变），这是预期行为。

**边界**: 存档框架可复用，但 DTO 结构需要随新系统扩展。新增字段（如玩家攻击/防御/词条/遗物/九宫格状态）需要添加到 `RunSaveDto` 和序列化方法中。

---

### 2.13 Presentation 服务层 —— `Presentation/Services/`

**文件**: `GameServices.cs` / `ITweenService.cs` / `IAudioService.cs` / `IVfxService.cs` / `IFloatingTextService.cs` + 实现

```csharp
public static class GameServices
{
    public static ITweenService Tween { get; set; }
    public static IAudioService Audio { get; set; }
    public static IVfxService Vfx { get; set; }
    public static IFloatingTextService FloatingText { get; set; }
}

public interface ITweenService
{
    Task PunchScale(Transform target, Vector3 punch, float duration);
    Task ScaleTo(Transform target, Vector3 endValue, float duration);
    Task FadeCanvasGroup(CanvasGroup group, float endValue, float duration);
    // ...
}
```

**边界**: 服务定位器模式。Core 层不依赖这些服务（零 Unity 引用）。Presentation 层的视图代码使用它们播放动画/音效/特效。

---

### 2.14 Presentation 视图层 —— `Presentation/RunFlow/` + `Presentation/Combat/Creatures/`

保留的视图组件：
- `PrototypeRunMapView` — 地图节点选择 UI
- `PrototypeRunRoomPanel` — 房间信息面板
- `PrototypeRewardPanel` — 奖励选择面板（已适配 ChoiceReward）
- `CreatureHealthBar` — 血条/格挡条 UI
- `EnemyView` — 生物视图（受击反应、死亡淡出、高亮）
- `CombatRaycastService` — 3D 射线检测基础设施

**边界**: 这些视图是**原型级**实现（大量代码动态创建 UI + 反射注入字段）。建议后续重构为 Prefab + SerializedField，但当前可作为功能参考。

---

## 三、已删除的系统（不要试图复用）

| 系统 | 删除原因 | 新系统替代方案 |
|:---|:---|:---|
| `CardModel` + `CardPile`（6种牌堆） | 手牌/抽牌/弃牌/消耗循环与《深入地牢》无关 | `FieldCard`（地图实体卡） |
| `CardEnergyCost` + `PlayerCombatState.Energy` | 无能量系统 | 无替代（行动驱动） |
| `CardPlay` + `CardPlayContext` + `PlayTarget` | 手牌打出上下文 | `InteractionContext`（拖动互动上下文） |
| `EnemyIntent` + `BuildIntent/ExecuteIntent` | 回合制意图预告不适用 | `TraitModel`（怪物词条驱动行为） |
| `PlayerCombatState`（5牌堆+能量） | 无手牌/无能量 | 无替代 |
| `ImmediatePlayCardAction` + `CardPlayRequest` | 手牌打出动作 | `MovePlayerAction` / `InteractCardAction` |
| `CombatManager` 回合制循环 | 玩家回合/敌人回合/抽牌/EndTurn 不适用 | 行动计数制 + 事件驱动 |
| `CardDragController` + `CombatInputController` | 2D手牌拖放到目标 | `GridDragController`（3D实体卡拖动） |
| `PlayerHandView` + `ArcHandLayout` + `CardView` | 手牌扇形排列 | `FieldCardView`（九宫格内3D卡牌） |
| `EnergyPanel` + `EndTurnButton` + `PileButtonsView` | 能量/结束回合/牌堆按钮 | 无替代 |
| `IntentView` | 敌人意图图标 | 无替代（无意图系统） |
| `CombatPrototypeController` | 绑定 StS 战斗全UI | 新的 GridCombatSceneController |
| Content 层全部 | 所有具体卡牌/敌人/遭遇/角色/Power | 新建《深入地牢》Content 层 |

---

## 四、新系统对接指南

### 4.1 推荐的开发顺序

```
Phase 1: 基础设施验证
    └── 确保 Core 层编译通过（零 Unity 依赖）
    └── 写一个最小测试：ModelDb 注册 → 创建 Player → 创建 CombatState → DealDamage

Phase 2: 九宫格核心（从零构建）
    ├── GridCoord + GridSlot + BattleGrid
    ├── FieldCard（地图实体卡）
    ├── GridDragController（3D 拖动交互）
    └── 最小可玩原型：移动 + 翻转 + 简单战斗

Phase 3: 内容层 + 战斗完善
    ├── MonsterModel + TraitModel（怪物词条）
    ├── KeywordModel（玩家技能词条）
    ├── TrapModel（机关卡）
    ├── ItemModel + ItemBar（道具系统）
    └── 房间生成规则（GenerateFieldCards）

Phase 4: Meta 系统
    ├── RelicModel + 遗物格 UI
    ├── 经济系统（金币获取 + 商品卡购买）
    ├── 存档扩展（攻击/防御/词条/遗物/九宫格状态）
    └── 地图生成器改造（DungeonMapGenerator）
```

### 4.2 关键对接点速查表

| 新系统 | 复用/对接的基础设施 | 备注 |
|:---|:---|:---|
| **FieldCard**（实体卡） | `AbstractModel`（作为配置基类） | 所有卡类型继承 `CardModel`（改造后） |
| **BattleGrid**（九宫格） | 无（从零构建） | 纯数据结构，放 Core 层 |
| **GridDragController** | `CombatRaycastService`（射线检测） | 扩展 `CombatRaycastService` 或自建 |
| **伤害结算** | `CreatureCmd.DealDamage` 或自建 | 简单场景可直接 `SetCurrentHp` |
| **词条/能力** | `PowerModel` 拦截器框架 | 复杂词条建议直接写逻辑，不完全依赖拦截器 |
| **遗物系统** | `Hook` 事件系统 + `CombatManager` 事件 | 遗物订阅 `CombatWon` / `CombatEnded` / Hook 方法 |
| **房间生成** | `CombatRoom` + `RoomFactory` | 扩展 `GenerateRewards`，新增 `GenerateFieldCards` |
| **地图** | `ActMap` + `MapPoint` + `StandardActMapGenerator` | 重写生成算法，保留数据结构 |
| **流程** | `RunManager` + `RunState` | `RoomEntered` 事件启动九宫格战斗 |
| **存档** | `SaveManager` + `RunSaveBinarySerializer` | 扩展 DTO，版本号 = 2 |
| **随机** | `DeterministicRng` + `IRng` | 所有随机操作通过 `RunState.Rng` |
| **动画/特效** | `GameServices.Tween` / `Vfx` / `FloatingText` | Presentation 层使用 |
| **视图绑定** | `Creature` 的事件（`HpChanged` / `Died` 等） | `CreatureHealthBar` + `EnemyView` 可直接复用 |
| **动作排队** | `GameAction` + `ActionQueueSet` + `ActionExecutor` | 所有战斗行为封装为 `GameAction` |

### 4.3 命名空间约定

保留的命名空间：
- `Game.Core.Models` — 数据框架
- `Game.Core.Actions` — 动作队列
- `Game.Core.Random` — 随机
- `Game.Core.Map` — 地图
- `Game.Core.Entities` — 角色/敌人/生物
- `Game.Core.Combat` — 战斗状态/伤害/命令
- `Game.Core.Powers` — Buff/Debuff
- `Game.Core.Hooks` — 事件扩展点
- `Game.Core.Rewards` — 奖励
- `Game.Core.Rooms` — 房间
- `Game.Core.Runs` — 爬塔流程
- `Game.Core.Saves` — 存档
- `Game.Presentation.Services` — 游戏服务
- `Game.Presentation.RunFlow` — 地图/房间/奖励 UI
- `Game.Presentation.Combat.Creatures` — 生物视图

**建议的新命名空间**：
- `Game.Core.Grid` — 九宫格核心（`GridCoord`, `GridSlot`, `BattleGrid`）
- `Game.Core.Cards` — 实体卡牌（`FieldCard`, `CardCategory`）
- `Game.Core.Items` — 道具系统
- `Game.Core.Relics` — 遗物系统
- `Game.Core.Keywords` — 技能词条
- `Game.Core.Traits` — 怪物词条
- `Game.Core.Traps` — 机关卡
- `Game.Presentation.Grid` — 九宫格视图 + 拖动交互

---

## 五、编译检查清单

清理后首次打开 Unity 时，检查以下项目：

1. **删除 `Game.Content` 和 `Game.Core.Tests` 的 asmdef 引用**
   - `Game.Presentation.asmdef` 已移除对 `Game.Content` 的引用
   - `Game.Content.asmdef` 和 `Game.Core.Tests.asmdef` 已删除

2. **Content 层为空**
   - `Assets/Scripts/Game/Content/` 目录已清空（只剩 meta 文件）
   - 新建 Content 代码时，需要重新创建 `Game.Content.asmdef`

3. **无编译错误**
   - 所有 `using Game.Core.Cards;` 已清理
   - 所有 `using Game.Content;` 已清理
   - `PrototypeRunController` 中 `StarterContentRegistry` 引用已移除

4. **存档兼容性**
   - 旧存档（SaveVersion = 1）无法读取，这是预期行为
   - 首次运行会创建 SaveVersion = 2 的新存档

---

## 六、总结

清理后的代码库是一个**稳固的通用框架**，包含：

| 能力 | 状态 |
|:---|:---|
| 数据框架（原型-实例） | ✅ 直接复用 |
| 异步动作队列 | ✅ 直接复用 |
| 确定性随机 + 存档一致性 | ✅ 直接复用 |
| 地图 DAG 数据结构 | ✅ 直接复用 |
| 爬塔流程状态机 | ✅ 直接复用 |
| 房间抽象 + 工厂 | ✅ 直接复用 |
| 奖励抽象 | ✅ 直接复用 |
| 二进制存档框架 | ✅ 直接复用 |
| 生物属性 + 事件 | ✅ 直接复用 |
| Buff/Debuff 拦截器 | ✅ 直接复用（简化后） |
| 伤害计算命令 | ✅ 直接复用（可选） |
| Hook 扩展点 | ✅ 直接复用（需激活） |
| 视图服务（Tween/Audio/VFX） | ✅ 直接复用 |
| 生物视图（血条/受击/死亡） | ✅ 直接复用 |
| 九宫格空间系统 | ❌ 从零构建 |
| 实体卡牌 + 拖动交互 | ❌ 从零构建 |
| 视野/翻转系统 | ❌ 从零构建 |
| 机关卡系统 | ❌ 从零构建 |
| 怪物 AI（Trait 驱动） | ❌ 从零构建 |
| 遗物/道具/词条系统 | ❌ 从零构建 |

**核心原则**: 保留的代码提供了"如何做"（排队执行、事件驱动、数据克隆、确定性随机），删除的代码是"做什么"（抽牌、能量、回合、Intent）。新系统需要重新实现"做什么"，但可以完全依赖保留的"如何做"。
