# L1 开发 AI 快速执行手册

> 读者：只负责具体内容或小功能落地的 L1 开发 AI。目标：让你安全调用现有领域层，不破坏核心架构。  
> **更新日期**: 2026-06-05 — 已同步 CardModel 生命周期接口、虚方法分派、先攻/伤害免疫、Inventory 系统、存档 DTO 扩展。

---

## 0. 先读这三份

1. `Assets/Notes/项目开发规范.md`
2. `Assets/Notes/项目级领域基础设施顶层设计.md`
3. `Assets/Notes/表现层接入与人-AI交互协作设计.md`

只做具体内容时，不要重构基础设施。

---

## 1. 你的默认工作流

```text
确认需求
  ↓
查现有 Domain API / 现有测试
  ↓
写或改业务内容
  ↓
补 Core 测试
  ↓
Unity 编译
  ↓
汇报改动、测试、未决规则
```

如果需求无法用现有 Domain API 表达，停止绕路，向上级提出“需要新增领域 API”。

---

## 2. 绝对禁止

- 不要在 Core / GameLogic 中 `using UnityEngine`。
- 不要直接设置：`card.Zone`、`card.Coord`、`card.StackIndex`、`card.IsFaceUp`。
- 不要直接改 `GridCell` 内部卡堆。
- 不要自己加玩家行动计数。
- 不要自己写随机数：禁止 `System.Random`、`UnityEngine.Random`、时间戳。
- 不要在业务代码里播放 VFX / Audio / Tween。
- 不要改 `ModelDb`、`DeterministicRng`、`ActionSystem`、存档底层，除非任务明确要求。

---

## 3. P0 API 速查

| 需求 | 使用 |
|---|---|
| 格位编号 1~9 转坐标 | `GridCoord.FromCellIndex(index)` |
| 正交相邻查询 | `GridQueries.OrthogonalNeighbors(coord)` |
| 同列上方查询 | `GridQueries.CoordsAboveSameColumn(coord)` |
| 放卡到格子 | `GridState.AddCardToGrid(card, coord, faceUp)` |
| 移动顶牌到空格 | `GridState.MoveCardToEmptyCell(card, coord)` |
| 翻开顶牌 | `GridState.FlipTopCard(coord, reason)` |
| 移除卡牌 | `GridState.RemoveCard(card, reason)` |
| 玩家输入预览 | `DomainFacade.PreviewIntent(intent)` |
| 提交玩家意图 | `DomainFacade.SubmitIntentAsync(intent)` |
| 基础伤害 | `CombatResolution.ApplyDamage(info, events)` |
| 玩家行动计数 | `PlayerActionCounter.Value`，只能由领域动作递增 |
| 检查不变量 | `DomainInvariantValidator.Validate(grid)` |
| 收入道具栏 | `StoreItemIntent(cardId)` → `DomainFacade.SubmitIntentAsync` |
| 使用道具 | `UseItemIntent(slot)` → `DomainFacade.SubmitIntentAsync` |
| 激活主动遗物 | `ActivateRelicIntent(relicId)` → `DomainFacade.SubmitIntentAsync` |
| 选择选项 | `ChooseOptionIntent(sessionId, optionIndex)` → `DomainFacade.SubmitIntentAsync` |

---

## 4. 玩家意图规则

表现层只提交意图：

- 移动：`MovePlayerIntent(to)`
- 互动：`InteractWithCardIntent(targetCardId)`
- 收入道具栏：`StoreItemIntent(itemCardId)`
- 使用道具：`UseItemIntent(inventorySlot)`
- 激活遗物：`ActivateRelicIntent(relicId)`
- 选择选项：`ChooseOptionIntent(sessionId, optionIndex)`

领域层会自动处理：

- 合法性校验；
- 计入玩家行动（**道具/遗物/选择不计行动**）；
- 玩家移动或保持位置；
- 自动翻开相邻顶牌；
- 基础怪物/机关伤害（含先攻判定、伤害免疫、同时死亡）；
- 怪物死亡移除与金币；
- `AfterPlayerActionCommitted` 回调（好战/尖刺等）；
- 翻牌/移除连锁回调（伏击/传送等）；
- 玩家死亡检测；
- 领域事件输出。

不要在表现层或内容类重复这些逻辑。

---

## 5. 内容回调速查（写 Monster/Trap/Item 时必须覆盖的虚方法）

```csharp
// 所有 CardModel 子类可用
public virtual bool CanInteractWithPlayer(CardInteractionContext ctx) => false;
public virtual Task OnPlayerInteractAsync(CardInteractionContext ctx) => Task.CompletedTask;
public virtual Task OnRevealedAsync(CardRevealContext ctx) => Task.CompletedTask;
public virtual Task OnDestroyedAsync(CardDestroyedContext ctx) => Task.CompletedTask;
public virtual Task OnAfterPlayerActionCommittedAsync(PlayerActionContext ctx) => Task.CompletedTask;

// MonsterCardModel 已默认实现 OnPlayerInteractAsync = ResolvePlayerVsMonster
// TrapCardModel 已默认实现 OnPlayerInteractAsync = ResolvePlayerVsTrap
// ItemCardModel 已默认 CanInteractWithPlayer = false，需实现 UseAsync
```

**时序提示**：
- `OnRevealedAsync`：卡牌被翻开**后**立即调用（含相邻翻开、移除后下方翻开）。
- `OnDestroyedAsync`：卡牌被移除**后**立即调用。
- `OnAfterPlayerActionCommittedAsync`：玩家移动/互动**成功并计行动后**调用。可用于"好战（每三次行动）""尖刺（下一次行动后）"。
- **注意**：新翻开的卡不 retroactively 响应导致它翻开的本次玩家行动。`ActionIndex` 参数供你判断。

---

## 6. 新增内容时必须说明

在交付说明中写：

```markdown
## 规则事件
- 会产生哪些 DomainEvent：
- 是否需要新增 DomainEventType：

## 表现契约
- 是否需要新增表现 Cue：是/否
- 需要的 payload：

## 测试
- 覆盖了哪些规则：
- 使用的 seed 或测试类：
```

如果没有新增表现需求，明确写“复用默认 DomainEvent 表现”。

---

## 7. 什么时候必须升级给上级

- 需要新增或修改领域公共 API（GridState、CombatResolution、IntentValidator 等）。
- 需要改变行动计数、翻牌、移除、金币、清场语义。
- 内容需求依赖尚未定义的基础设施，如"房间生成管线""存档完整 Restore"。
- 需要新增表现 Cue 或修改 Manifest。
- 测试发现设计文档与当前实现冲突。
- 需要修改存档、RNG、ActionQueue、ModelDb。

---

## 8. 最小交付标准

- 代码能编译。
- Core 规则有测试。
- 没有越界引用 Presentation / UnityEngine。
- 没有直接篡改领域内部状态。
- 汇报包含改动路径、验证结果、未决问题。
