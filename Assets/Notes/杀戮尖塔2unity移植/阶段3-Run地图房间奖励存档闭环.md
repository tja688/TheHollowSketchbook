# 阶段 3 — Run、地图、房间、奖励、存档闭环

> 日期：2026-06-01（修复补全）  
> 原始阶段3由前序AI落地，但因死机导致笔记缺失。本次由修复者根据《杀戮尖塔2原型第三阶段落地情况_深度调研报告.md》进行修复补全。  
> 依据：《类杀戮尖塔 2 原型落地方案.md》第7章 + 《五个大阶段落地计划.md》阶段3任务清单  

---

## 一、阶段概述

阶段3将项目从"单场战斗Demo"升级为"可跑一个小型Run"的完整闭环：

```
开始新Run → 地图生成 → 选择节点 → 进入房间(战斗/事件/宝箱/休息/Boss)
→ 完成房间 → 领取奖励 → 返回地图 → 循环直到Boss或失败
```

核心特性：确定性种子地图、自动存档/读档、奖励卡入牌库、跨房间状态继承。

---

## 二、已实现模块清单

### Core层（纯C#，不依赖Unity）

| 模块 | 文件 | 状态 |
|------|------|------|
| RunManager | `Assets/Game/Core/Runtime/Runs/RunManager.cs` | ✅ 完整 |
| RunState | `Assets/Game/Core/Runtime/Runs/RunState.cs` | ✅ 完整 |
| 地图生成 | `Assets/Game/Core/Runtime/Map/StandardActMapGenerator.cs` | ✅ 完整 |
| 地图数据 | `Assets/Game/Core/Runtime/Map/ActMap.cs` | ✅ 完整 |
| 房间工厂 | `Assets/Game/Core/Runtime/Rooms/RoomFactory.cs` | ✅ 完整 |
| 战斗房间 | `Assets/Game/Core/Runtime/Rooms/CombatRoom.cs` | ✅ 完整 |
| Boss房间 | `Assets/Game/Core/Runtime/Rooms/BossRoom.cs` | ✅ 完整 |
| 事件占位 | `Assets/Game/Core/Runtime/Rooms/EventRoomPlaceholder.cs` | ✅ 占位 |
| 休息占位 | `Assets/Game/Core/Runtime/Rooms/RestSiteRoomPlaceholder.cs` | ✅ 占位 |
| 宝箱房间 | `Assets/Game/Core/Runtime/Rooms/TreasureRoom.cs` | ✅ 完整 |
| 商店占位 | `Assets/Game/Core/Runtime/Rooms/ShopRoomPlaceholder.cs` | ✅ 占位 |
| 奖励生成器 | `Assets/Game/Core/Runtime/Rewards/RewardGenerator.cs` | ✅ 完整 |
| 金币奖励 | `Assets/Game/Core/Runtime/Rewards/GoldReward.cs` | ✅ 完整 |
| 选卡奖励 | `Assets/Game/Core/Runtime/Rewards/CardRewardChoice.cs` | ✅ 完整 |
| 存档管理 | `Assets/Game/Core/Runtime/Saves/SaveManager.cs` | ✅ 完整 |
| 存档DTO | `Assets/Game/Core/Runtime/Saves/RunSaveDto.cs` | ✅ 完整 |

### Presentation层（Unity表现）

| 模块 | 文件 | 状态 |
|------|------|------|
| Run流程整合器 | `Assets/Game/Presentation/Runtime/RunFlow/PrototypeRunController.cs` | ✅ 完整 |
| 地图UI | `Assets/Game/Presentation/Runtime/RunFlow/PrototypeRunMapView.cs` | ✅ 完整 |
| 房间面板 | `Assets/Game/Presentation/Runtime/RunFlow/PrototypeRunRoomPanel.cs` | ✅ 完整 |
| 奖励面板 | `Assets/Game/Presentation/Runtime/RunFlow/PrototypeRewardPanel.cs` | ✅ 完整 |
| 调试入口 | `Assets/Game/Presentation/Runtime/Bootstrap/DebugCombatBootstrap.cs` | ✅ 完整 |

---

## 三、本次修复记录（修复者工作日志）

前序AI落地阶段3后因死机中断，遗留以下问题，本次一并修复：

### P1：阻塞编译或运行

| # | 问题 | 修复方式 | 修改文件 |
|---|------|---------|---------|
| 1 | **DebugCombatBootstrap编译错误** | `PrototypeRunController.StartPrototypeRun` 新增重载 `StartPrototypeRun(int seed, bool continueSavedRunIfPresent)`，内部实现读档优先逻辑；原单参数方法转发到新重载 | `PrototypeRunController.cs` |
| 2 | **读档后acts丢失** | ① `RunSaveDto` 新增 `ActIds` 列表 + `ActIdSaveDto`；② `RunSaveBinarySerializer` 在 Players 后追加写入/读取 ActIds（向后兼容旧存档）；③ `RunSaveSerializer.Capture` 记录acts；④ `Restore` 优先从 `dto.ActIds` 恢复，fallback到硬编码 | `RunSaveDto.cs`<br>`SaveManager.cs` |
| 3 | **存档路径跨平台问题** | `SaveManager` 构造函数增加可选 `saveDirectory` 参数，默认保持 `LocalApplicationData`；`PrototypeRunController` 和 `DebugCombatBootstrap` 传入 `Application.persistentDataPath` | `SaveManager.cs`<br>`PrototypeRunController.cs`<br>`DebugCombatBootstrap.cs` |

### P2：影响体验或扩展性

| # | 问题 | 修复方式 | 修改文件 |
|---|------|---------|---------|
| 4 | **StartNewRun双重签名歧义** | 删除单参数 `StartNewRun(CharacterModel, int)`，仅保留带acts的三参数版本 | `RunManager.cs` |
| 5 | **奖励卡池硬编码** | `RewardGenerator.GenerateCardChoices` 改为从 `ModelDb.All<CardModel>()` 动态筛选，排除 `Rarity == Basic` 的卡 | `RewardGenerator.cs` |

### 编译与测试验证

- Unity 2022 LTS `refresh_unity(force compile)` ✅ 通过
- EditMode 核心测试（7项）✅ 全部通过

---

## 四、关键接口说明

### 4.1 开始一个新Run

```csharp
var saveManager = new SaveManager(Application.persistentDataPath);
var runManager = new RunManager(saveManager: saveManager);

CharacterModel character = ModelDb.Get<CharacterModel>(new ModelId("Character", "PrototypeHero"));
IReadOnlyList<ActModel> acts = new[] { ModelDb.Get<ActModel>(new ModelId("Act", "PrototypeAct")) };
runManager.StartNewRun(character, seed: 12345, acts);
```

### 4.2 进入地图节点

```csharp
// 由地图UI点击触发
runManager.EnterMapCoord(new MapCoord(column, row));
// 触发 RoomEntered 事件，返回 AbstractRoom
```

### 4.3 完成房间

```csharp
runManager.CompleteCurrentRoom();
// 生成奖励 → 触发 RoomCompleted 事件
```

### 4.4 领取奖励后返回地图

```csharp
// UI层调用 reward.Resolve(state, player) 后
runManager.SaveRun(); // 手动保存（已自动在CompleteCurrentRoom中保存一次）
runManager.ProceedToMap(); // 检查无待领奖励后清理CurrentRoom，触发MapChanged
```

### 4.5 存档/读档

```csharp
// 自动触发点：StartNewRun、EnterMapCoord、CompleteCurrentRoom、奖励Resolve后
runManager.SaveRun();

// 显式读档
RunState loaded = runManager.LoadRun();
// 读档成功会自动触发 MapChanged 事件
```

---

## 五、已知问题与遗留

| 优先级 | 问题 | 说明 | 建议处理时机 |
|--------|------|------|------------|
| P2 | **EventRoomPlaceholder直接改数据** | `TakeRisk` 直接调用 `player.Creature.SetCurrentHp`，未通过伤害Command层 | 阶段4引入事件系统时统一改 |
| P2 | **无存档版本迁移** | `SaveVersion` 字段存在（当前=1，修复后旧存档仍兼容），但没有 `MigrationManager` | 阶段5稳定后引入 |
| P2 | **BinarySerializer而非JSON** | 存档使用自定义Binary格式，可读性差、版本兼容难处理 | 阶段5建议迁移到 `System.Text.Json` |
| P2 | **中间行类型分布极端** | `StandardActMapGenerator` 对整行所有节点统一随机类型，可能出现全Monster或全Shop行 | 阶段5优化地图生成算法 |
| P3 | **阶段3零测试** | 没有地图生成、存档/读档、RunFlow的自动化测试 | 阶段3.5或阶段4开头补 |
| P3 | **无PlayMode测试** | 验收标准"连续完成3个节点"无自动化验证 | 阶段4引入 |

---

## 六、给阶段4开发者的指引

阶段4是最关键的架构改造期（两步地图 + 缓冲区 + 共同结算）。在开始前，请确保：

1. **CombatManager流程必须可替换**：当前 `StartPlayerTurnAsync → ExecuteEnemyTurnAsync` 是硬编码的。阶段4需要改为 `Planning → LockIn → BuildTimeline → Resolve → Cleanup`。建议先引入 `CombatPhaseMachine` 状态机。

2. **PlayTarget已预留BufferSlot**：当前 `PlayTarget` 只有 `Creature` 字段，阶段4需要扩展 `BufferSlotRef`。注意保持向后兼容。

3. **Hook需要新增缓冲区相关**：`BeforeCardQueuedToBuffer`、`AfterCardQueuedToBuffer`、`BeforeBufferedCardResolved` 等。当前Hook.cs有18个，阶段4需要再增6-8个。

4. **MapKnowledgeState尚未存在**：阶段4需要新增 `IMapVisibilityPolicy`、`TwoStepPeekVisibilityPolicy`、`MapVisibilitySnapshot` 等。当前地图是完全可见的。

5. **不要提前删除ImmediatePlayCardAction**：阶段4初期仍需要它来处理少数"即时卡"。应该新增 `CommitCardToBufferAction`，而非替换。

### 阶段4最低准入条件（建议 checklist）

- [ ] 本笔记已阅读完毕
- [ ] 能开始新Run、打赢一场战斗、领取奖励、返回地图（手动PlayMode验证）
- [ ] 保存后退出、重新打开能正确读档继续
- [ ] 已阅读《类杀戮尖塔 2 原型落地方案.md》第8章（微创新插入方案）

---

*本阶段修复者：落地修复AI（基于深度调研报告执行修复）*  
*如有疑问，参考原始方案文档和调研报告。*
