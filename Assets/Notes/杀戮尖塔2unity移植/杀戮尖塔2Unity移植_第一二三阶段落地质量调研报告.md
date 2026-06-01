# 杀戮尖塔2 Unity移植 — 第一、二、三阶段落地质量调研报告

> 生成日期：2026-06-01
> 调研范围：Assets/Game（核心代码）、Assets/Notes（落地笔记）、.codegraph（代码图辅助）
> 参考基准：《类杀戮尖塔2原型落地方案.md》+《五个大阶段落地计划.md》+ STS2源码逆向报告

---

## 一、执行摘要

| 维度 | 评估 | 说明 |
|------|------|------|
| **阶段1（核心骨架）** | 优秀 | 全部任务完成，7项EditMode测试通过，架构纯净 |
| **阶段2（战斗MVP）** | 良好 | 核心闭环完成，可运行单场战斗，存在3个已知UI问题 |
| **阶段3（Run闭环）** | 良好 | 完整闭环打通，地图/房间/奖励/存档全部可用，有修复记录 |
| **架构合规性** | 优秀 | 四层asmdef分离正确，Core零Unity引用 |
| **代码质量** | 中等偏上 | 核心逻辑清晰，Presentation层有过度集中问题 |
| **测试覆盖** | 薄弱 | 仅7个EditMode测试，无PlayMode测试，阶段3零测试 |
| **总体建议** | **继续开发（阶段4），但需先补3项前置修复** | 见第11章 |

---

## 二、项目概况

### 2.1 代码规模

| 指标 | 数值 |
|------|------|
| C#文件总数 | 101个 |
| Core层代码行数 | ~4,158行 |
| CodeGraph索引类 | 107个 |
| CodeGraph索引方法 | 502个 |
| 场景文件 | 1个（New Scene.unity） |

### 2.2 目录结构（实际）

```
Assets/Game/
  Core/              -- 纯C#逻辑层（noEngineReferences=true）
    Runtime/         -- 42个.cs文件
      Models/        -- AbstractModel, ModelDb
      Entities/      -- Player, Creature, EnemyModel...
      Cards/         -- CardModel, CardPile, CardEnergyCost
      Combat/        -- CombatManager, CombatState, Commands
      Actions/       -- GameAction, ActionQueueSet, ActionExecutor
      Hooks/         -- Hook.cs（18个Hook空壳）
      Map/           -- ActMap, StandardActMapGenerator
      Rooms/         -- AbstractRoom, CombatRoom, RoomFactory...
      Rewards/       -- Reward, GoldReward, CardRewardChoice...
      Runs/          -- RunManager, RunState
      Saves/         -- SaveManager, RunSaveDto, BinarySerializer
      Random/        -- IRng, DeterministicRng
      Logging/       -- Log, GameException
      Common/        -- GameEnums, ModelId
    Tests/           -- CoreLogicTests.cs（7个测试）
  Content/           -- 纯C#内容定义（依赖Core）
    Runtime/         -- 17个.cs文件
      Cards/         -- StrikeCard, DefendCard, BashCard...
      Enemies/       -- DebugCultist, DebugSlime, DebugBoss...
      Characters/    -- PrototypeHero
      Powers/        -- Strength, Vulnerable, Weak
      Encounters/    -- 5个遭遇配置
      Acts/          -- PrototypeAct
      StarterContentRegistry.cs
  Presentation/      -- Unity表现层（依赖Core+Content+TextMeshPro）
    Runtime/         -- 24个.cs文件
      Bootstrap/     -- DebugCombatBootstrap
      Combat/        -- CombatPrototypeController, CardView...
      Input/         -- CardDragController, CombatInputController...
      RunFlow/       -- PrototypeRunController, MapView, RewardPanel...
      Services/      -- GameServices, Tween, Audio, Vfx...
  Editor/            -- 空（仅asmdef占位）
```

### 2.3 Assembly定义检查

| Asmdef | 依赖 | noEngineReferences | 合规 |
|--------|------|-------------------|------|
| Game.Core | 无 | true | 合规 |
| Game.Content | Game.Core | true | 合规 |
| Game.Presentation | Game.Core, Game.Content, Unity.TextMeshPro | false | 合规 |
| Game.Editor | 无定义 | - | 空壳 |
| Game.Core.Tests | 未单独定义（在Core中） | - | 待完善 |

**结论**：Assembly依赖方向完全正确，符合设计方案要求。

---

## 三、参考文档对齐分析

### 3.1 与《类杀戮尖塔2原型落地方案.md》的对齐

| 设计方案要求 | 实际落地 | 对齐度 |
|-------------|---------|--------|
| 三层分离（Data/Logic/Presentation） | 已实现（Core/Content/Presentation） | 100% |
| ModelDb + Canonical/Mutable | 已实现（AbstractModel.CloneMutable） | 100% |
| 18个初版Hook | 已实现（Hook.cs含18个空壳） | 100% |
| Action队列+执行器 | 已实现（ActionQueueSet+ActionExecutor） | 100% |
| CombatManager标准流程 | 已实现（Setup→Start→PlayerTurn→EnemyTurn） | 100% |
| 五大牌堆 | 已实现（Deck/Draw/Hand/Discard/Exhaust/Play） | 100% |
| CardPlayContext + CardPlay | 已实现 | 100% |
| 命令层（CardCmd/CreatureCmd/PlayerCmd） | 已实现 | 100% |
| 地图DAG结构 | 已实现（ActMap+MapPoint+parents/children） | 100% |
| 存档DTO（ModelId+状态） | 已实现（RunSaveDto+BinarySerializer） | 100% |
| 服务内容注册（StarterContentRegistry） | 已实现 | 100% |
| PlayTarget预留BufferSlot | 未预留（当前仅有Creature） | 0% |
| SaveManager用JSON | 实际用Binary | 偏离 |

### 3.2 与《五个大阶段落地计划.md》任务清单的对齐

#### 阶段1任务清单

| 任务 | 要求 | 实际 | 状态 |
|------|------|------|------|
| 1.1 创建asmdef与目录 | 4个asmdef | 4个asmdef+1个Tests | 超额完成 |
| 1.2 基础类型 | ModelId, IRng, AbstractModel, ModelDb, GameAction... | 全部实现 | 完成 |
| 1.3 实体基础 | Player, Creature, CardPile, CardModel, PowerModel... | 全部实现 | 完成 |
| 1.4 牌堆命令 | Draw, Move, Discard, Exhaust, ShuffleDiscardIntoDraw | 全部实现 | 完成 |
| 1.5 伤害/格挡/Power | DealDamage, GainBlock, ApplyPower | 全部实现 | 完成 |
| 1.6 最小内容 | 5张卡、2敌人、1角色 | 5卡、4敌人、1角色、1Boss、1Elite | 超额完成 |
| 1.7 EditMode测试 | 7个测试 | 7个测试，全部通过 | 完成 |

#### 阶段2任务清单

| 任务 | 要求 | 实际 | 状态 |
|------|------|------|------|
| 2.1 CombatManager标准流程 | Setup/Start/PlayerTurn/EndTurn/EnemyTurn | 完整实现 | 完成 |
| 2.2 ImmediatePlayCardAction | 检查+费用+目标+OnPlayWrapper+移堆 | 完整实现 | 完成 |
| 2.3 输入与目标选择 | CardDragController, RaycastService | 已实现 | 完成 |
| 2.4 敌人意图 | 玩家回合开始时roll | 已实现（BuildIntent+ExecuteIntent） | 完成 |
| 2.5 表现服务 | Tween/Audio/Vfx/FloatingText | 4个服务接口+占位实现 | 完成 |
| 2.8 调试入口 | DebugCombatBootstrap | 已实现 | 完成 |

#### 阶段3任务清单

| 任务 | 要求 | 实际 | 状态 |
|------|------|------|------|
| 3.1 RunManager | StartNewRun/EnterMapCoord/EnterRoom/Complete/ProceedToMap/Save/Load | 全部实现 | 完成 |
| 3.2 地图生成 | StandardActMapGenerator, 7列, 起点→Boss | 已实现 | 完成 |
| 3.3 房间系统 | Combat/Event/Treasure/Rest/Shop/Boss | 全部实现 | 完成 |
| 3.5 奖励系统 | Gold+CardReward(+Relic optional) | 已实现 | 完成 |
| 3.6 存档/读档 | SaveManager.Save/TryLoad/Delete + DTO | 已实现（Binary格式） | 完成 |

---

## 四、阶段1：核心逻辑骨架 — 深度评估

### 4.1 架构纯净度

**检查结果：优秀**

- Game.Core.asmdef: `noEngineReferences: true`，零Unity引用
- 未使用 `GameObject`、`MonoBehaviour`、`Transform`、`Coroutine`
- 仅 `Compatibility/IsExternalInit.cs` 引入 `System.Runtime.CompilerServices`（C# init accessor兼容）

### 4.2 关键类实现质量

#### AbstractModel（基类）

| 设计要点 | 实现状态 | 评价 |
|---------|---------|------|
| ModelId抽象属性 | 已实现 | 正确 |
| IsCanonical标记 | 已实现 | 正确 |
| CloneMutable | 已实现（MemberwiseClone+钩子） | 正确，但缺少泛型约束检查 |
| DeepCloneFieldsFrom虚方法 | 已实现 | 正确 |
| 存档友好的ID系统 | ModelId是record struct | 优秀 |

**问题**：`CloneMutable<T>` 未在运行时验证T是否匹配实际类型，若子类错误调用可能抛InvalidCastException。

#### ModelDb（注册中心）

| 设计要点 | 实现状态 | 评价 |
|---------|---------|------|
| 静态字典存储 | 已实现 | 正确 |
| Register强制Canonical | 已实现 | 正确 |
| Get<T>泛型访问 | 已实现 | 正确 |
| CreateMutable克隆 | 已实现 | 正确 |
| All<T>按类型筛选 | 已实现 | 正确 |

**问题**：`ModelDb.Clear()` 在测试SetUp中被调用，但生产代码中无保护机制，误调用会导致运行时内容丢失。

#### CardPile（牌堆）

| 设计要点 | 实现状态 | 评价 |
|---------|---------|------|
| 6种PileType | 已实现（Deck/Draw/Hand/Discard/Exhaust/Play） | 正确 |
| ContentsChanged/CardAdded/CardRemoved事件 | 已实现 | 正确 |
| Add/Remove/Shuffle | 已实现 | 正确 |
| Draw自动ShuffleDiscard | 已实现（CardPileCmd.Draw） | 正确 |
| 手牌上限10 | 未明确限制 | 遗漏 |

#### Action系统

| 设计要点 | 实现状态 | 评价 |
|---------|---------|------|
| GameAction抽象基类 | 已实现 | 正确 |
| ActionQueueSet | 已实现 | 正确 |
| ActionExecutor异步执行 | 已实现 | 正确 |
| 串行化执行 | 已实现（ProcessActionsAsync锁） | 正确 |

### 4.3 测试状态

| 测试名 | 状态 | 覆盖功能 |
|--------|------|---------|
| ModelDb_RegisterAndCloneMutable | 通过 | 注册+克隆 |
| CardPile_Draw_ShuffleDiscardWhenDrawEmpty | 通过 | 抽牌+洗入弃牌 |
| CreatureCmd_Damage_BlockAbsorbsBeforeHp | 通过 | 格挡吸收+HP损失 |
| CreatureCmd_StrengthModifiesAttackDamage | 通过 | Strength修改伤害 |
| Card_Strike_DealsDamage | 通过 | Strike卡牌效果 |
| Card_Defend_GainsBlock | 通过 | Defend卡牌效果 |
| ActionQueue_ExecutesInOrder | 通过 | 动作队列顺序 |

**覆盖率**：仅7个测试，覆盖核心数据结构和基础战斗计算。未覆盖：Power堆叠、EnemyAI、Map生成、存档序列化。

---

## 五、阶段2：标准战斗MVP — 深度评估

### 5.1 CombatManager实现

**文件**：`Assets/Game/Core/Runtime/Combat/CombatManager.cs`（513行）

| 功能 | 实现 | 评价 |
|------|------|------|
| SetUpCombat | 完整 | 正确初始化玩家/敌人状态 |
| StartCombatAsync | 完整 | 调用Hook.BeforeCombatStart |
| StartPlayerTurnAsync | 完整 | 清格挡、回能量、抽5张、Roll意图 |
| RequestEndTurn/EndPlayerTurnAsync | 完整 | 清动作队列、弃牌、切敌人回合 |
| ExecuteEnemyTurnAsync | 完整 | 逐个敌人执行意图、检查胜负 |
| EndEnemyTurnAsync | 完整 | 清敌人格挡、轮数++、回玩家回合 |
| SubmitCardPlayRequestAsync | 完整 | 入队ImmediatePlayCardAction |
| CheckWinConditionAsync | 完整 | 玩家失败/敌人失败/战斗结束 |
| Reset | 完整 | 取消事件订阅、清理状态 |

**问题发现**：
1. `DiscardHands` 方法在遍历手牌时使用 `.ToList()` 创建副本，但 `PlayPile` 的处理逻辑较绕（检查CurrentPile是否仍为PlayPile）
2. `ImmediatePlayCardAction` 中 `MoveCardToResultPile` 已加幂等保护（阶段2.5修复）
3. 缺少 `CombatHistory`（设计方案要求，但阶段2验收不要求）

### 5.2 ImmediatePlayCardAction

| 检查项 | 实现 | 评价 |
|--------|------|------|
| 战斗中 | 检查 `IsInProgress` | 正确 |
| PlayPhase | 检查 `IsPlayPhase` + `CurrentSide` | 正确 |
| 卡在手牌 | 检查 `Hand.Contains(card)` | 正确 |
| 能量足够 | `CanPlay` + `EnergyCost.GetSpendAmount` | 正确 |
| 目标有效 | `TryValidateTarget` | 正确（支持None/Self/SingleEnemy/AllEnemies） |
| 支付费用 | `PlayerCmd.SpendEnergy` | 正确 |
| 移入PlayPile | 直接Add | 正确 |
| OnPlayWrapper | 调用卡牌效果 | 正确 |
| 移入Discard/Exhaust | `MoveCardToResultPile` | 正确 |

### 5.3 表现层（Presentation）

#### 服务层

| 服务 | 接口 | 实现 | 状态 |
|------|------|------|------|
| ITweenService | 完整 | CoroutineTweenService | 占位可用 |
| IAudioService | 完整 | UnityAudioService(Debug.Log) | 占位 |
| IVfxService | 完整 | SimpleVfxService | 占位 |
| IFloatingTextService | 完整 | FloatingTextService | 占位 |

#### 3D卡牌交互

| 组件 | 实现 | 评价 |
|------|------|------|
| CardView | World Space Canvas，动态创建视觉元素 | 可用但字号偏小（已知问题P2） |
| CardViewPool | 简化对象池 | 可用 |
| ArcHandLayout | 扇形布局算法 | 可用 |
| PlayerHandView | 订阅Hand事件，同步CardView列表 | 可用 |
| CardDragController | Begin/Update/EndDrag，射线判定 | 可用（阶段2.5修复async void） |
| CombatInputController | 提交CardPlayRequest | 可用 |
| CombatRaycastService | 射线检测Card/Enemy/PlayArea | 可用 |

#### 敌人视图

| 组件 | 实现 | 评价 |
|------|------|------|
| EnemyView | SpriteRenderer占位+HealthBar | 简陋但可用 |
| CreatureHealthBar | World Space Canvas | 可用 |
| IntentView | World Space Canvas | **阶段2.5修复后可用**（原P1问题） |

#### 已知问题（来自阶段2笔记）

| 优先级 | 问题 | 根因 | 修复状态 |
|--------|------|------|---------|
| P1 | IntentView不显示 | Canvas未设置worldCamera | **阶段2.5已修复** |
| P1 | CombatManager事件泄漏 | 销毁时未Reset | **阶段2.5已修复** |
| P1 | 卡牌重复移动 | MoveCardToResultPile未检查 | **阶段2.5已修复** |
| P2 | CardDragController async void | Unity事件回调异常无法捕获 | **阶段2.5已修复** |
| P2 | EnergyPanel不监听变化 | 缺少EnergyChanged事件 | **阶段2.5已修复** |
| P2 | EnemyView未播放受击 | OnHpChanged未检测下降 | **阶段2.5已修复** |
| P2 | Hook缺失 | 阶段1只实现8个 | **阶段2.5已修复（补全18个）** |

---

## 六、阶段3：Run闭环 — 深度评估

### 6.1 RunManager

**文件**：`Assets/Game/Core/Runtime/Runs/RunManager.cs`（184行）

| 功能 | 实现 | 评价 |
|------|------|------|
| StartNewRun | 完整（character, seed, acts三参数） | 正确 |
| GenerateActMap | 完整 | 正确 |
| EnterMapCoord | 完整（CanEnterPoint检查） | 正确 |
| CompleteCurrentRoom | 完整（生成奖励+标记完成+保存） | 正确 |
| ProceedToMap | 完整（检查奖励+Boss判断+清理） | 正确 |
| SaveRun/LoadRun/DeleteRun | 完整 | 正确 |

**问题发现**：
1. `CanEnterPoint` 使用 `ReferenceEquals` 检查子节点，这是对象引用比较。由于MapPoint是重新生成的，此检查**可能不工作**。应改用 `coord.Equals` 比较坐标。
2. `StartNewRun` 签名从单参数改为三参数（阶段3修复记录#4），但保留了旧单参数调用兼容性（通过默认参数）。

### 6.2 地图生成（StandardActMapGenerator）

| 设计要点 | 实现 | 评价 |
|---------|------|------|
| 7列网格 | 可配置（`act.ColumnCount`） | 正确 |
| 起点→Boss路径 | 多路径生成 | 正确 |
| 避免交叉 | `WouldCreateCrossing` 检查 | 正确 |
| 类型分配 | Monster/Event/Treasure/Shop/Elite/Rest | 基本实现 |
| 倒数第二行Rest | 强制分配 | 正确 |

**问题发现**：
1. `AssignPointTypes` 对中间行整行统一随机类型，可能导致某行全是Monster或全是Shop（阶段3笔记P2问题#5）
2. 缺少路径修剪（PruneDuplicateSegments）
3. 缺少后处理（CenterGrid/Spread/Straighten）
4. 与设计方案的 `MapPointTypeCounts` 和复杂约束（兄弟节点不能共享类型等）未实现

### 6.3 房间系统

| 房间类型 | 实现 | 评价 |
|---------|------|------|
| CombatRoom | 完整（遭遇+敌人列表） | 正确 |
| BossRoom | 继承CombatRoom+isBoss标记 | 正确 |
| TreasureRoom | 完整（直接给奖励） | 正确 |
| EventRoomPlaceholder | 占位（TakeRisk直接改HP） | **问题：直接修改HP，未走Command层** |
| RestSiteRoomPlaceholder | 占位（Rest回血） | 可用 |
| ShopRoomPlaceholder | 占位（直接完成） | 可用 |

### 6.4 奖励系统

| 奖励类型 | 实现 | 评价 |
|---------|------|------|
| GoldReward | 完整 | 正确 |
| CardRewardChoice | 完整（3选1+Skip） | 正确 |
| RewardGenerator | 从ModelDb动态筛选（排除Basic） | **阶段3修复#5** |

### 6.5 存档系统

| 功能 | 实现 | 评价 |
|------|------|------|
| SaveCurrentRun | 完整 | 正确 |
| TryLoadCurrentRun | 完整 | 正确 |
| Binary序列化 | 自定义BinaryReader/Writer | **偏离设计（应为JSON）** |
| 版本兼容 | SaveVersion=1，旧存档兼容 | 正确（阶段3修复#2） |
| 跨平台路径 | Application.persistentDataPath | **阶段3修复#3** |

**问题发现**：
1. 存档使用Binary格式而非JSON，可读性差，版本迁移困难（阶段3笔记P2问题#3）
2. 无存档版本迁移系统（MigrationManager）
3. `RunSaveSerializer.Restore` 在恢复房间时硬编码 fallback 到 `PrototypeAct`

### 6.6 RunFlow表现层

**PrototypeRunController**（~318行，阶段3新增）

| 功能 | 实现 | 评价 |
|------|------|------|
| 开始新Run/读档继续 | 完整 | 正确 |
| 地图显示 | PrototypeRunMapView | 可用 |
| 房间面板 | PrototypeRunRoomPanel | 可用 |
| 奖励面板 | PrototypeRewardPanel | 可用 |
| 战斗启动 | StartCombat方法 | 可用 |
| 奖励领取 | OnRewardSelected/OnCardSelected | 可用 |

**问题发现**：
1. `PrototypeRunController.BuildUi` 大量使用反射设置私有字段（`_contentRoot`, `_nodeButtonPrefab`, `_legendText`），这是**脆弱的实现**。阶段5应重构为公开字段或Prefab序列化。
2. `CanSelectPoint` 与 `RunManager.CanEnterPoint` 存在**逻辑重复**，且都使用 `ReferenceEquals`。

---

## 七、架构合规性详细检查

### 7.1 禁止引用检查

| 禁止项 | Core层 | Content层 | 结果 |
|--------|--------|----------|------|
| GameObject | 未引用 | 未引用 | 合规 |
| MonoBehaviour | 未引用 | 未引用 | 合规 |
| Transform | 未引用 | 未引用 | 合规 |
| Coroutine | 未引用 | 未引用 | 合规 |
| AudioSource | 未引用 | 未引用 | 合规 |
| ParticleSystem | 未引用 | 未引用 | 合规 |
| SceneManager | 未引用 | 未引用 | 合规 |

### 7.2 依赖方向检查

```
Game.Core ← 无依赖（纯净C#）
Game.Content ← Game.Core
Game.Presentation ← Game.Core + Game.Content + Unity.TextMeshPro
Game.Editor ← 空壳
```

**结论**：依赖方向100%合规。

### 7.3 命名空间检查

| 设计方案命名空间 | 实际命名空间 | 对齐 |
|-----------------|-------------|------|
| Project.Core | Game.Core | 基本一致 |
| Project.Core.Models | Game.Core.Models | 一致 |
| Project.Core.Combat | Game.Core.Combat | 一致 |
| Project.Content.Cards | Game.Content（扁平） | 略有简化 |
| Project.Presentation.Combat | Game.Presentation.Combat | 一致 |

### 7.4 表现层规则检查

| 规则 | 检查 | 结果 |
|------|------|------|
| UI不能直接修改HP | CardView/EnemyView均通过事件订阅 | 合规 |
| UI不能直接改牌堆 | 通过CardPlayRequest提交 | 合规 |
| UI不能直接改能量 | 通过CombatManager.SubmitCardPlayRequestAsync | 合规 |
| 输入先变Action | ImmediatePlayCardAction入队执行 | 合规 |

**例外**：`EventRoomPlaceholder.TakeRisk` 直接调用 `player.Creature.SetCurrentHp`，**违规**。阶段3笔记已标记为P2问题。

---

## 八、代码质量评估

### 8.1 代码风格一致性

| 维度 | 评价 |
|------|------|
| 命名规范 | 统一使用PascalCase，符合C#惯例 |
| 文件组织 | 按功能域分目录，清晰 |
| 命名空间 | 与目录结构一致 |
| 访问修饰符 | 核心类多为public sealed，接口明确 |

### 8.2 潜在代码异味

| 位置 | 异味 | 严重程度 | 建议 |
|------|------|---------|------|
| `CombatPrototypeController` | 单类负责创建所有场景对象（上帝类） | 中等 | 阶段3笔记已标记，阶段5拆分 |
| `PrototypeRunController.BuildUi` | 反射设置私有字段 | 中等 | 阶段5改为公开字段/Prefab |
| `PrototypeRunController.CanSelectPoint` | 与RunManager逻辑重复 | 低 | 阶段4统一 |
| `EventRoomPlaceholder.TakeRisk` | 直接修改Creature.HP | 中等 | 阶段4引入事件系统时修复 |
| `StandardActMapGenerator` | 整行统一随机类型 | 低 | 阶段5优化 |
| `SaveManager` | Binary格式而非JSON | 低 | 阶段5迁移 |
| `ModelDb.Clear()` | 生产代码无保护 | 低 | 添加`#if UNITY_EDITOR`保护 |

### 8.3 异步代码安全性

| 检查项 | 状态 |
|--------|------|
| async void在Unity事件回调中 | **阶段2.5已全部修复** |
| Task未等待 | CardPile.Add中的Hook调用使用`_ =`丢弃，当前安全（空壳） |
| CancellationToken | 未使用（CombatManager有`_combatCts`字段但未实际使用） |

---

## 九、与STS2源码参考的对比

### 9.1 架构思想保留情况

| STS2架构 | 本项目实现 | 保留度 |
|---------|----------|--------|
| 三层分离（Presentation/Logic/Data） | Core/Content/Presentation | 100% |
| ModelDb反射注册 | 手动注册（StarterContentRegistry） | 70%（未用反射） |
| Canonical/Mutable克隆 | AbstractModel.CloneMutable | 100% |
| Hook系统 | 18个空壳Hook | 100%（初版范围） |
| Action队列+执行器 | ActionQueueSet+ActionExecutor | 100% |
| CombatManager阶段流 | 完整回合循环 | 100% |
| 地图DAG | ActMap+MapPoint(parents/children) | 100% |
| 五大牌堆+PlayPile | 已实现 | 100% |
| DamageInfo/DamageResult | 已实现 | 100% |
| Power堆叠（Counter/Duration） | 仅Counter实现 | 80% |
| 存档分层（Settings/Progress/Run） | 仅Run存档 | 60% |
| 多人同步架构 | 未实现 | 0% |
| 代码即数据（无外部配置） | 纯C#类定义 | 100% |
| 源码生成（SourceGenerator） | 未实现 | 0% |
| FMOD音频 | UnityAudioService占位 | 20% |
| Spine动画 | 无 | 0% |
| 本地化系统 | 无 | 0% |

### 9.2 关键差异说明

1. **ModelDb注册方式**：STS2使用反射自动注册所有AbstractModel子类。本项目使用手动 `StarterContentRegistry.RegisterAll()`，更可控但维护成本略高。
2. **存档格式**：STS2使用 `System.Text.Json` + 自定义 `JsonSerializerContext`。本项目使用自定义Binary格式，更紧凑但可读性和版本兼容性差。
3. **卡牌效果**：STS2使用Hook+虚方法（`OnPlay`）。本项目采用相同模式，但效果实现更简化。
4. **地图生成**：STS2有复杂的路径修剪、后处理、类型约束。本项目基础版本缺少这些优化。

---

## 十、问题与风险清单

### 10.1 阻塞性问题（必须修复才能进入阶段4）

| # | 问题 | 影响 | 修复建议 | 工作量 |
|---|------|------|---------|--------|
| 1 | `CanSelectPoint`使用ReferenceEquals | 地图节点选择可能不工作 | 改为coord.Equals比较 | 10分钟 |
| 2 | `EventRoomPlaceholder`直接修改HP | 架构违规，绕过Command层 | 改为使用CreatureCmd.DealDamage | 30分钟 |
| 3 | 无存档版本迁移 | 阶段4改存档结构后旧存档崩溃 | 添加MigrationManager或改用JSON | 2-4小时 |

### 10.2 高风险问题（阶段4可能放大）

| # | 问题 | 影响 | 建议处理时机 |
|---|------|------|------------|
| 4 | `PlayTarget`未预留BufferSlot | 阶段4需大改PlayTarget结构 | 阶段4开始时重构 |
| 5 | Hook为空壳（无实际调用链） | 遗物/Power效果无法真正Hook | 阶段4配合内容一起实现 |
| 6 | CombatManager流程硬编码 | 阶段4需改为Planning→LockIn→Resolve | 阶段4引入CombatPhaseMachine |
| 7 | 无CombatHistory | 无法回放/调试 | 阶段5补充 |
| 8 | 阶段3零自动化测试 | 回归风险高 | 阶段4开头补充 |

### 10.3 中低风险问题（可延后）

| # | 问题 | 建议处理时机 |
|---|------|------------|
| 9 | `CombatPrototypeController`过重 | 阶段5拆分 |
| 10 | 反射设置UI字段 | 阶段5改为Prefab |
| 11 | Binary存档格式 | 阶段5迁移JSON |
| 12 | 地图生成缺少修剪/后处理 | 阶段5优化 |
| 13 | 卡牌World Space Canvas字号偏小 | 阶段5调优 |
| 14 | 无PlayMode测试 | 阶段5补充 |
| 15 | CardKeyword.Ethereal/Retain未处理 | 阶段5（当前无相关卡牌） |

---

## 十一、综合建议

### 11.1 总体判断：继续开发（阶段4），但需先完成3项前置修复

**理由**：
1. 阶段1-3的核心闭环已经打通（StartRun→Map→Room→Combat→Reward→Save→Load）
2. 架构骨架完整且合规，阶段4有稳定的改造基础
3. 问题清单中无阻塞阶段4架构改造的问题
4. 修复工作量小（3项阻塞问题总计<半天）

### 11.2 进入阶段4的前置Checklist

- [ ] 修复 `CanSelectPoint` 的 ReferenceEquals → coord.Equals（10分钟）
- [ ] 修复 `EventRoomPlaceholder.TakeRisk` 直接改HP → 走Command层（30分钟）
- [ ] 决定存档格式：保留Binary或迁移JSON（影响阶段4存档设计，建议保留Binary，阶段5再迁）
- [ ] 验证手动PlayMode：开始Run→打赢3场战斗→领取奖励→到达Boss→胜利（1-2小时）
- [ ] 阅读《类杀戮尖塔2原型落地方案.md》第8章（微创新插入方案）
- [ ] 阅读阶段3笔记中的"阶段4最低准入条件"

### 11.3 阶段4开发建议

**优先级排序**：

1. **CombatPhaseMachine状态机**（最高优先级）
   - 将当前硬编码流程改为：Setup → PlayerPlanning → LockIn → BuildTimeline → Resolve → Cleanup
   - 这是阶段4所有机制改造的基础

2. **PlayTarget扩展**（高优先级）
   - 添加 `BufferSlotRef` 字段
   - 保留现有Creature兼容性
   - 新增 `PlayDestination` 概念

3. **CombatPlanBuffer核心数据**（高优先级）
   - BufferSlot / BufferSlotRef / BufferedCardEntry
   - 放在CombatState中（非UI）

4. **CommitCardToBufferAction**（高优先级）
   - 替代大部分ImmediatePlayCardAction
   - 入槽扣能量但不结算

5. **ResolutionTimeline**（高优先级）
   - ResolutionEntry / BufferedCardResolutionEntry / EnemyIntentResolutionEntry
   - IResolutionOrderPolicy（先实现玩家先敌人后）

6. **地图两步窥探**（中优先级）
   - MapKnowledgeState / IMapVisibilityPolicy
   - 与阶段3完整地图显示并存（通过Policy切换）

7. **Hook补全**（中优先级）
   - 新增6-8个缓冲区相关Hook
   - 开始实现部分Hook的实际调用链（如遗物效果）

8. **测试补充**（中优先级）
   - 地图生成测试
   - 存档/读档测试
   - 至少3个PlayMode集成测试

### 11.4 架构保护建议

阶段4是最容易引入架构债的阶段，需严格遵循：

1. **不要删除Targeting概念**——扩展为Destination，保留现有Creature目标
2. **不要让缓冲区只存在于UI**——必须是CombatState核心数据
3. **不要把AfterCardPlayed用作入槽事件**——入槽用AfterCardQueuedToBuffer
4. **不要实现真正同时结算**——先做确定性顺序结算（方案A），UI表达为共同回合
5. **不要一口气改所有卡**——先保证5-8张原型卡在缓冲模式下工作

---

## 十二、CodeGraph使用感受（调研辅助说明）

本次调研使用了项目目录下的 `.codegraph/codegraph.db` 进行辅助分析：

| 维度 | 评价 |
|------|------|
| 数据库规模 | 约55MB，索引了101个文件、107个类、502个方法 |
| 类定位 | **非常有用**——快速定位类文件路径（如CombatManager→Assets/Game/Core/Runtime/Combat/CombatManager.cs） |
| 方法统计 | 有用——可快速了解项目方法规模 |
| 依赖分析 | **受限**——edges表似乎未正确填充或关系未建立，无法进行调用图/依赖链分析 |
| 全文搜索 | 未测试（nodes_fts表存在但本次未使用） |

**建议**：如果CodeGraph的edges关系能正确建立，将极大提升跨文件依赖分析能力。当前阶段，CodeGraph主要作为"类文件索引器"使用，效果良好。

---

## 附录：关键文件速查表

| 功能 | 文件路径 |
|------|---------|
| 调试入口 | `Assets/Game/Presentation/Runtime/Bootstrap/DebugCombatBootstrap.cs` |
| Run流程整合 | `Assets/Game/Presentation/Runtime/RunFlow/PrototypeRunController.cs` |
| 战斗管理器 | `Assets/Game/Core/Runtime/Combat/CombatManager.cs` |
| Run管理器 | `Assets/Game/Core/Runtime/Runs/RunManager.cs` |
| 地图生成 | `Assets/Game/Core/Runtime/Map/StandardActMapGenerator.cs` |
| 存档管理 | `Assets/Game/Core/Runtime/Saves/SaveManager.cs` |
| 模型数据库 | `Assets/Game/Core/Runtime/Models/ModelDb.cs` |
| 内容注册 | `Assets/Game/Content/Runtime/StarterContentRegistry.cs` |
| 核心测试 | `Assets/Game/Core/Tests/CoreLogicTests.cs` |
| 阶段1笔记 | `Assets/Notes/杀戮尖塔2unity移植/阶段1-核心逻辑骨架与内容注册.md` |
| 阶段2笔记 | `Assets/Notes/杀戮尖塔2unity移植/阶段2-标准战斗MVP与3D卡牌交互.md` |
| 阶段2.5修复 | `Assets/Notes/杀戮尖塔2unity移植/阶段2.5—中途优化、修复文档.md` |
| 阶段3笔记 | `Assets/Notes/杀戮尖塔2unity移植/阶段3-Run地图房间奖励存档闭环.md` |

---

*报告结束。本报告基于对Assets/Game目录下101个C#文件的完整代码阅读、4份笔记文档分析、CodeGraph数据库辅助查询，以及与3份参考设计文档的逐项对比。*
