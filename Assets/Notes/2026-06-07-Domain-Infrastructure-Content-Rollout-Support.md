# Domain-Infrastructure Support For First Content Rollout

更新时间：2026-06-07

本次任务主目标是首批 `Game.Content` 落地，但为了不把规则缺口硬编码到内容层，补了以下 Domain-Infrastructure 支撑面。

## 变更范围

- `Assets/Scripts/Game/Core/Runtime/Domain/Cards/CardModel.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/ContentContracts/CardContexts.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/ContentContracts/ContentContractModels.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Combat/CombatResolution.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Combat/DamageContext.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Interaction/ChoiceSession.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Interaction/PlayerIntent.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Interaction/IntentValidator.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Inventory/Inventories.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Progression/PlayerRunState.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/DomainActionContext.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/DomainFacade.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Actions/*.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Deck/DungeonDeckBuilder.cs`
- `Assets/Scripts/Game/Core/Runtime/Domain/Rooms/RoomTransitionService.cs`
- `Assets/Scripts/Game/Core/Runtime/Saves/DomainSaveDto.cs`
- `Assets/Scripts/Game/Core/Runtime/Saves/DomainSaveAdapter.cs`

## 新增/扩展的规则能力

### 1. 选择会话从“只记索引”扩成“可回调源卡牌”

补充了：

- `ChoiceSession.SourceCardId`
- `ChoiceSession.OptionKeys`
- `ChoiceResolutionContext`
- `CardModel.OnChoiceResolvedAsync(...)`
- `DomainActionContext.OpenChoiceSession(...)`
- `DomainActionContext.ResolveChoiceSessionAsync(...)`

结果：

- 内容层可以安全地打开 choice，再通过 `ChooseOptionIntent` 回到原始 card model 结算，而不是把选择结果散落到 Presentation 或外部 UI 逻辑里。

### 2. 玩家运行态新增 trait 集合与按 source 移除能力

`PlayerRunState` 新增：

- `PlayerTraitState`
- `AddTrait(...)`
- `RemoveTrait(...)`
- `RemoveTraitsBySource(...)`
- `RemoveModifiersBySource(...)`
- trait 存档恢复

结果：

- 玩家导师词条、临时暴力卡效果、房间生命周期型 trait 可以通过统一运行态参与保存与结算。

### 3. 主动遗物支持 target 选择

`ActivateRelicIntent` 现在带 `ItemTargetSelection Target`，并且 `RelicModel` 新增 `TargetMode`。

结果：

- 主动遗物不再只能“无目标点击触发”。
- 像法则魔杖这类“选卡 + 选格”的遗物可以直接走 intent/validator/action 流。

### 4. 房间 / 怪物生命周期上下文

新增：

- `RoomLifecycleContext`
- `MonsterDefeatedContext`
- `RelicModel.OnRoomEnteredAsync(...)`
- `RelicModel.OnRoomClearedAsync(...)`
- `RelicModel.OnMonsterDefeatedAsync(...)`
- `RelicModel.OnEliteOrBossDefeatedAsync(...)`
- 对应的 `TraitModel` 房间/怪物生命周期钩子

结果：

- 内容层可以实现“每进三房加攻”“清房回血”“击败精英/层主加攻”“怪物死亡刷新主动遗物次数”等规则，而不需要把 relic id 硬编码到 Domain 主流程。

### 5. DamageContext 补入当前事件集合

`DamageContext` 现在携带 `Events`。

结果：

- `OnBeforeDamageAsync` / `OnAfterDamageAsync` 这类内容钩子不再只能读状态，也可以在合法上下文里追加新的 `DomainEvent` 或再发起一次规则伤害。
- 这为 `刺皮` 这类“受伤后反伤”提供了规则出口。

### 6. 房间牌分类键拆分

`DungeonDeckBuilder` 不再把金币卡 / 宝箱卡 / 属性卡 / 食品卡 / 导师卡 / 商品卡混用同一个 `room` 分类，而是明确按：

- `gold`
- `stat`
- `chest`
- `food`
- `mentor`
- `shop-product`

结果：

- `RoomContentCatalog` 可以真正成为内容分发表，而不是抽到错误房间牌类型。

### 7. 玩家道具槽容量规则补齐为 2

`PlayerInventory` 新增固定容量 `2` 与 `HasSpace`。
`IntentValidator` 也补了 `InventoryFull` 校验。

结果：

- 现在“道具牌格上限 2 格，满时不能再拿新道具”是 Domain 规则，而不是内容层或 UI 约定。

### 8. DomainFacade 对连续 intent 的队列回落做了兜底

在一次 submit 后，如果 action queue 里仍有残留动作，会继续 drain，避免连续 intent 在极短窗口里拿到空 batch。

结果：

- choice 之后立刻继续 interact / activate 的内容流程更稳，不需要内容层自带额外重试。

## 存档影响

本次 Domain 变更新增了以下可存档字段：

- 玩家 trait 集合
- choice session 的 `SourceCardId`
- choice session 的 `OptionKeys`

未改动 Foundation save 入口，只扩了 Domain DTO 与 adapter。

## 未继续下沉的内容点

- 怪物词条的“持有者实例感知”仍未抽成通用 Domain trait-owner 机制。
- 这次首落选择把 `鼓舞` / `复仇` / `好战` / `伏击` / `破甲` / `散子` 主要固化在对应 monster model 内，避免为了首批内容额外扩大 Domain trait 语义面。
