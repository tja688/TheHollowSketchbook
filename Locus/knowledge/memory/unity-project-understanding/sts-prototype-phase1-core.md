---
id: kd_b95cb475-ada2-4bc0-baf0-00eb2fc1abd9
type: memory
path: unity-project-understanding/sts-prototype-phase1-core.md
title: sts-prototype-phase1-core
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1780290415998
updatedAt: 1780648463967
---

# sts-prototype-phase1-core

## Summary
当前 Unity 项目 Core/Presentation 结构与 P0 领域层入口、命名空间、关键规则、测试位置与最新 SubmitIntent 串行化约束速查。

<!-- locus:maintain-rules:start -->
- Record only Unity project structure knowledge and lookup info that reduce repeated exploration
- Maintain only project-specific, durable understanding; avoid task-by-task scratch notes
- Prefer concise bullets naming directories, entry points, invariants, and important asmdef boundaries
- Update when project structure or core architectural boundaries change
- If later observations conflict with this cache, correct or remove stale bullets promptly
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
#### 当前 Core 骨架

- 当前脚本根目录是 `Assets/Scripts/Game`，不是旧记忆中的 `Assets/Game`。
- `Assets/Scripts/Game/Core/Game.Core.asmdef` 是纯 C# Core 程序集，`noEngineReferences: true`，不得引用 UnityEngine。
- `Assets/Scripts/Game/Presentation/Game.Presentation.asmdef` 引用 `Game.Core` 与 TextMeshPro，允许 UnityEngine。
- 通用基础设施保留在 `Assets/Scripts/Game/Core/Runtime`：`Models`、`Actions`、`Random`、`Map`、`Entities`、`Combat`、`Hooks`、`Rewards`、`Rooms`、`Runs`、`Saves` 等。
- `Game.Core.Actions.GameActionExecutionContext` 当前为空；领域动作通过构造参数持有 `DomainActionContext`。
- 旧 `Game.Core.Combat.DamageInfo/DamageResult` 基于 `Creature`，实体卡 P0 使用 `Game.Core.Domain.Combat` 下的泛化伤害类型，避免破坏旧骨架。

#### 项目级领域基础设施 P0

- P0 九宫格领域层位于 `Assets/Scripts/Game/Core/Runtime/Domain`，入口是 `Game.Core.Domain.DomainFacade` 与 `DomainActionContext`。
- 主要命名空间：`Domain.Grid`（`GridCoord`、`GridQueries`、`GridCell`、`GridState`、`GridOperationResult`）、`Domain.Cards`（`CardInstanceId`、`CardType`、`CardZone`、`CardModel`、`CardInstance`）、`Domain.Deck`（`DungeonDeck`）、`Domain.Interaction`（`PlayerIntent`、`IntentPreview`、`IntentValidator`）、`Domain.Actions`、`Domain.Events`、`Domain.Combat`、`Domain.Rooms`（`RoomClearChecker`）、`Domain.Validation`。
- P0 规则：九宫格 1~9 行优先；玩家默认格 8；玩家移动到空格计行动并揭示正交相邻顶牌；互动正面顶牌计行动且玩家位置不变；移除顶牌后下方顶牌自动翻开；怪物移除给 10 金币；机关接触伤害忽略玩家防御；怪物清空后发出 `RoomCleared`。
- `DomainFacade.SubmitIntentAsync()` 现已串行化：外部并发提交会排队等待同一 `SemaphoreSlim` 通道，Hook/生命周期中的同提交链重入会返回 `IntentRejected("SubmitIntentReentrant")`，`ActionExecutor.ExecuteAllAsync()` 会复用同一个运行中的 drain task，避免多个 executor 并发消费同一 `ActionQueueSet`。
- P0 测试程序集是 `Assets/Scripts/Game/Core/Tests/Game.Core.Tests.asmdef`，测试文件是 `Assets/Scripts/Game/Core/Tests/DomainP0Tests.cs`，覆盖坐标、堆叠、移动/揭示、互动、非法意图、移除级联、伤害、清场、不变量。
- 项目开发规范文档：`Assets/Notes/项目开发规范.md`；L1 AI 执行手册：`Assets/Notes/L1开发AI快速执行手册.md`。
<!-- locus:body:end -->
