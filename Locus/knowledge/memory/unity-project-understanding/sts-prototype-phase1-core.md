---
id: kd_b95cb475-ada2-4bc0-baf0-00eb2fc1abd9
type: memory
path: unity-project-understanding/sts-prototype-phase1-core.md
title: sts-prototype-phase1-core
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1780290415998
updatedAt: 1780290415999
---

# sts-prototype-phase1-core

## Summary
STS-like 原型第一阶段已在 `Assets/Game` 建立纯 C# 核心骨架与基础测试。

<!-- locus:body:start -->
#### 阶段 1 核心骨架

- 新增纯 C# 程序集：`Assets/Game/Core/Game.Core.asmdef` 与 `Assets/Game/Content/Game.Content.asmdef`，两者都不依赖 UnityEngine。
- 新增占位程序集：`Assets/Game/Presentation/Game.Presentation.asmdef`、`Assets/Game/Editor/Game.Editor.asmdef`。
- 新增测试程序集：`Assets/Game/Core/Tests/Game.Core.Tests.asmdef`，当前用于 EditMode 核心逻辑测试。
- `Assets/Game/Core/Runtime` 已实现：`ModelId`、`AbstractModel`、`ModelDb`、`IRng/DeterministicRng`、`Player`、`Creature`、`PlayerCombatState`、`CardModel`、`CardPile`、`CombatState`、`DamageInfo`、`DamageResult`、`CardPileCmd`、`CreatureCmd`、`PlayerCmd`、`Hook` 空壳、`GameAction/ActionQueueSet/ActionExecutor`、`RunSaveDto`。
- `Assets/Game/Content/Runtime` 已实现最小可玩内容注册：`PrototypeHero`、`Strike`、`Defend`、`Bash`、`ZapDebug`、`GuardDebug`、`Strength`、`Vulnerable`、`Weak`、`DebugCultist`、`DebugSlime`、`PrototypeCultistEncounter`、`PrototypeAct`。
- `Player` 当前通过牌库克隆生成战斗抽牌堆；战斗中使用的卡牌实例与牌库实例分离。
- 当前 `Hook` 仅保留接口占位，尚未承载复杂遗物/Power/事件插入逻辑。
- 当前没有 `CombatManager` 与场景表现绑定；第二阶段应先接即时出牌 MVP，不要提前混入缓冲区逻辑。
- 核心测试位于 `Assets/Game/Core/Tests/CoreLogicTests.cs`，已覆盖 ModelDb、抽牌洗牌、伤害与格挡、Strength 修正、Strike、Defend、ActionQueue 顺序。
<!-- locus:body:end -->
