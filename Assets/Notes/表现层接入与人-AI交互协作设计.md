# question_2：表现层接入与人-AI交互协作设计

> 目标：解决“人类做好表现后，AI 如何稳定、集中、可检查、低耦合地使用这些表现”的问题。  
> 核心结论：不要让 AI 直接调用动画、VFX、Audio、Tween，也不要让表现触发散落在各个业务类里。应该建立三条稳定接缝：**输入意图 PlayerIntent、领域事件 DomainEvent、表现清单 Presentation Manifest**。

---

## 0. 一句话结论

推荐采用：

```text
人类输入/表现层
    ↓ 只提交 PlayerIntent
领域层 / 业务逻辑层
    ↓ 只产出 DomainEventBatch
表现编排层 Presentation Orchestrator
    ↓ 查 Presentation Manifest
具体 UI / 动画 / VFX / Audio / 镜头 / 手感
```

AI 不应该“使用某个动画实现”，而应该让规则层准确地产生“发生了什么”的领域事件。表现层由人类维护一张“表现清单”，把这些领域事件映射到具体动画、特效、音效、镜头和 UI。这样 AI 好写，人类好查，表现不会散落到项目各处。

---

## 1. 你现在纠结的问题，本质是什么

你想要的协作方式是：

1. 人类把底层和表现做好；
2. AI 根据完整设计和业务需求写规则；
3. AI 必须用上人类已经做好的表现；
4. 如果 AI 不知道怎么用某个表现，就说明某个契约没有落地到位；
5. 整个接入方式要集中、可控、方便检查，不能到处散落。

这个目标非常正确，但要避免一个陷阱：

> 不要把“表现已经做好”理解成“AI 要在业务代码里手动调用这些表现”。

如果 AI 在每个业务类里写：

```csharp
GameServices.Vfx.Play("trap_crossbow_fire");
GameServices.Audio.Play("card_hit");
GameServices.Tween.MoveTo(...);
```

短期看起来很快，长期会出现：

- 表现 ID 到处散落；
- 同一事件在不同业务类里表现不一致；
- 人类改一个动画名会影响几十处 AI 代码；
- AI 会发明不存在的 VFX 字符串；
- 业务逻辑开始依赖动画时长；
- 表现层无法统一节奏和风格；
- 测试时没有表现层就跑不动业务。

正确做法是：**AI 只负责让规则层说清楚“发生了什么”，人类负责决定“这件事怎么表现”。**

---

## 2. 三条稳定接缝

### 2.1 输入接缝：`PlayerIntent`

表现层把鼠标、触摸、拖动、点击转换为领域意图。

```text
拖动玩家卡到空格      → MovePlayerIntent
拖动玩家卡到怪物      → InteractWithCardIntent
拖动道具卡到道具格    → StoreItemIntent
从道具格拖出道具      → BeginUseItemIntent
选择勾绳方向          → ChooseOptionIntent / ChooseDirectionIntent
选择遗物              → ChooseOptionIntent
选择路线              → ChooseRouteIntent
主动遗物使用          → ActivateRelicIntent
```

表现层只提交意图，不判断规则结果。是否合法由领域层判断。

### 2.2 输出接缝：`DomainEventBatch`

领域层和业务逻辑层执行动作后，只产出领域事件。

```text
CardMoved
CardFlipped
DamageApplied
CardRemoved
TrapTriggered
GoldChanged
RelicAcquired
ChoiceOpened
RoomCleared
RouteChoicesGenerated
```

这些事件是“事实”，不是“表现指令”。

### 2.3 表现清单接缝：`Presentation Manifest`

人类维护一份机器可读 + 人类可读的表现清单。

它回答：

- 有哪些表现 Cue？
- 每个 Cue 对应什么语义？
- 它监听哪些 DomainEvent？
- 需要哪些 payload？
- 是否阻塞后续动作？
- 是否可以并行动画？
- 适用于哪些卡牌类型/标签？
- 如果缺失资源，用什么 fallback？

AI 读这份清单，知道哪些表现语义已经存在；但 AI 不直接实现或播放这些表现。

---

## 3. 总体架构图

```text
┌────────────────────────────────────────────────────────────┐
│ Presentation：人类控制                                      │
│                                                            │
│  CardView / GridView / InventoryView / RelicView / UI       │
│          │                                                 │
│          ▼                                                 │
│  GridInputAdapter                                          │
│          │ 生成 PlayerIntent                               │
└──────────┼─────────────────────────────────────────────────┘
           │
           ▼
┌────────────────────────────────────────────────────────────┐
│ DomainFacade：唯一规则入口                                  │
│                                                            │
│  PreviewIntent(intent) → IntentPreview                     │
│  SubmitIntent(intent)  → ActionQueue                       │
│                                                            │
│  Action 执行后产出 DomainEventBatch                         │
└──────────┼─────────────────────────────────────────────────┘
           │
           ▼
┌────────────────────────────────────────────────────────────┐
│ Presentation Orchestrator：表现编排层，人类控制               │
│                                                            │
│  读取 DomainEventBatch                                     │
│  查询 Presentation Manifest                                │
│  分派给 GridPresenter / CardPresenter / FeedbackPresenter   │
│  播放动画、音效、特效、镜头、UI                              │
└────────────────────────────────────────────────────────────┘
```

这套结构中，所有交互只走一个入口，所有表现只走一个出口。

---

## 4. 输入设计：表现层如何把玩家操作交给领域层

### 4.1 不要让 View 自己做规则判断

错误方向：

```text
CardView.OnDrop
  if target is MonsterView:
      monster.TakeDamage(...)
      PlayHitVfx(...)
```

正确方向：

```text
CardView.OnDrop
  → GridInputAdapter.BuildIntent(pointerData)
  → DomainFacade.SubmitIntent(intent)
```

`CardView` 只知道自己代表哪个 `CardInstanceId`。它不应该知道：

- 这张怪物有多少防御；
- 这次是否计入行动；
- 是否触发伏击；
- 要不要翻开下方卡牌；
- 播什么 VFX。

### 4.2 `IntentPreview` 支持拖动高亮

拖动过程中表现层需要实时反馈合法目标。不要让表现层自己算，应该向领域层请求预览。

```csharp
public sealed class IntentPreview
{
    public bool IsValid { get; }
    public IntentKind Kind { get; }
    public string InvalidReasonKey { get; }
    public IReadOnlyList<GridCoord> HighlightCells { get; }
    public IReadOnlyList<CardInstanceId> HighlightCards { get; }
    public PreviewAffordance Affordance { get; }
}
```

`PreviewAffordance` 只给表现层语义：

```csharp
public enum PreviewAffordance
{
    None,
    ValidMove,
    ValidInteraction,
    ValidItemStore,
    ValidItemTarget,
    InvalidBlocked,
    InvalidFaceDown,
    InvalidNotTopCard,
    InvalidOutOfRange,
    InvalidNoGold
}
```

表现层根据这些语义表现：高亮、红框、音效、提示文案。AI 不参与。

### 4.3 拖动玩家卡

```text
PointerDown(PlayerCardView)
  → BeginDrag
  → Preview MovePlayerIntent / InteractWithCardIntent
  → PointerUp
      if target empty cell:
          Submit MovePlayerIntent
      if target top face-up card:
          Submit InteractWithCardIntent
      else:
          Submit canceled / invalid feedback
```

领域层负责判断：

- 是否空白格；
- 是否最上方卡牌；
- 是否正面朝上；
- 是否允许互动；
- 是否会计入玩家行动。

### 4.4 拖动道具卡

道具交互分两类：

1. 场上道具卡收入道具栏；
2. 道具栏中的道具使用到目标。

```text
场上道具卡拖到道具格
  → StoreItemIntent
  → 不计玩家行动

道具格中拖出道具
  → BeginUseItemIntent
  → Domain 返回 TargetingSession
  → 表现层根据 TargetingSession 高亮合法目标
  → UseItemOnTargetIntent
  → 不计玩家行动
```

`TargetingSession` 示例：

```csharp
public sealed class TargetingSession
{
    public TargetingSessionId Id { get; }
    public ItemInstanceId SourceItem { get; }
    public TargetMode TargetMode { get; }
    public IReadOnlyList<TargetDescriptor> ValidTargets { get; }
    public string PromptKey { get; }
}
```

### 4.5 多步骤操作统一走 `ChoiceSession`

以下都属于“选择会话”：

- 宝箱三选一遗物；
- 属性提升三选一；
- 导师三选一技能；
- 勾绳选择方向；
- 商店购买确认；
- 路线选择；
- 法则魔杖选择任意牌和任意格。

```csharp
public sealed class ChoiceSession
{
    public ChoiceSessionId Id { get; }
    public ChoiceKind Kind { get; }
    public string PromptKey { get; }
    public IReadOnlyList<ChoiceOption> Options { get; }
    public bool BlocksOtherInput { get; }
}
```

表现层只负责把 `ChoiceSession` 渲染成 UI 或棋盘上的可交互对象。选择结果通过 `ChooseOptionIntent` 回到领域层。

---

## 5. 输出设计：领域事件如何驱动表现

### 5.1 `DomainEvent` 是事实，不是表现命令

例如玩家攻击怪物后，领域层可能产出：

```text
PlayerActionCommitted(actionIndex=12)
DamageApplied(source=PlayerCard, target=MonsterCard#21, amount=2)
DamageApplied(source=MonsterCard#21, target=PlayerCard, amount=1)
CardRemoved(card=MonsterCard#21, reason=Defeated)
GoldChanged(delta=10, newValue=40)
CardFlipped(card=Card#33, reason=RevealAfterTopRemoved)
RoomCleared
RouteChoicesGenerated([...])
```

表现层根据这些事实决定：

- 玩家卡是否轻微前冲；
- 怪物卡是否抖动；
- 飘字怎么显示；
- 金币 UI 怎么跳；
- 被移除卡怎么消散；
- 新翻开的卡怎么翻面；
- 清场后路线选择怎么进入。

### 5.2 事件批次和顺序

同一次 Action 产生的事件应组成 `DomainEventBatch`：

```csharp
public sealed class DomainEventBatch
{
    public uint ActionId { get; }
    public PlayerIntent SourceIntent { get; }
    public IReadOnlyList<DomainEvent> Events { get; }
    public PresentationGatePolicy GatePolicy { get; }
}
```

`PresentationGatePolicy`：

```csharp
public enum PresentationGatePolicy
{
    None,              // 逻辑不等表现
    WaitCriticalOnly,  // 等移动/翻牌/移除等关键表现
    WaitAll            // 少用，只在教程或重大演出使用
}
```

一般推荐：

- 卡牌移动、翻面、移除：Critical；
- 伤害飘字、音效、粒子：NonBlocking；
- 选择 UI：Blocking input，但不阻塞规则线程；
- Boss 重大演出：可单独配置 WaitAll。

### 5.3 表现层等待不能影响规则正确性

领域层状态应在 Action 中完成变更，表现层只是播放结果。如果表现失败，规则不应该失败。

可以通过接口隔离：

```csharp
public interface IPresentationBridge
{
    Task PlayAsync(DomainEventBatch batch, PresentationGatePolicy policy);
}
```

在单元测试中使用 No-op 实现。

```csharp
public sealed class NullPresentationBridge : IPresentationBridge
{
    public Task PlayAsync(DomainEventBatch batch, PresentationGatePolicy policy)
        => Task.CompletedTask;
}
```

这样业务和领域测试不依赖 Unity 场景。

---

## 6. 表现清单 Presentation Manifest

### 6.1 它是什么

`Presentation Manifest` 是表现层的“可用能力说明书”。它同时服务人类和 AI。

建议有两份产物：

```text
Assets/Scripts/Game/Presentation/Contract/PresentationManifest.asset 或 .json
Docs/PRESENTATION_MANIFEST.md
```

机器可读文件用于运行时和测试；Markdown 用于 AI 阅读。

### 6.2 Manifest 字段

```yaml
cueId: cue.grid.card.flip.default
label: 默认翻牌表现
owner: human
status: ready
semanticEvents:
  - CardFlipped
requiredPayload:
  - cardInstanceId
  - fromFaceUp
  - toFaceUp
  - reason
blockingPolicy: critical
fallbackCueId: cue.grid.card.flip.fallback
allowedCardTypes:
  - Monster
  - Trap
  - Item
  - Gold
  - Chest
notes: 所有非玩家卡从背面到正面的默认翻牌动画。AI 不得直接调用，只需确保领域层发出 CardFlipped。
```

推荐字段：

| 字段 | 作用 |
|---|---|
| `cueId` | 稳定 ID，不允许随便改名 |
| `label` | 人类可读名称 |
| `semanticEvents` | 监听哪些领域事件 |
| `requiredPayload` | 事件必须提供哪些字段 |
| `blockingPolicy` | 是否阻塞后续关键动作 |
| `targetResolver` | 如何找到表现对象，如 CardInstanceId / GridCoord / UI Slot |
| `variantRules` | 不同卡牌类型、品质、伤害类型使用不同变体 |
| `fallbackCueId` | 资源缺失时兜底表现 |
| `status` | ready / wip / deprecated |
| `usagePolicy` | required / optional / debugOnly / contentSpecific |
| `owner` | human / system |
| `notes` | 给 AI 和人类看的使用说明 |

### 6.3 不要用裸字符串

表现 ID 应生成强类型常量：

```csharp
public static class PresentationCueIds
{
    public static readonly PresentationCueId GridCardFlipDefault = new("cue.grid.card.flip.default");
    public static readonly PresentationCueId CombatDamageHit = new("cue.combat.damage.hit");
    public static readonly PresentationCueId TrapCrossbowFireLine = new("cue.trap.crossbow.fire_line");
}
```

AI 不能发明：

```csharp
Play("cool_big_hit_vfx"); // 禁止
```

如果确实需要新表现，AI 只能在 PR 说明中提出：

```text
需要新增表现 Cue：cue.trap.teleport.shuffle
原因：传送机关洗牌重排时需要统一演出
所需 payload：originCardId, movedCards, playerFrom, playerTo, redistributedCards
建议阻塞策略：WaitCriticalOnly
```

由人类决定是否实现。

---

## 7. 表现编排层 Presentation Orchestrator

### 7.1 为什么需要 Orchestrator

如果每个 `CardView`、`MonsterView`、`TrapView` 都自己订阅领域事件，很快会散。

推荐集中入口：

```csharp
public sealed class GamePresentationOrchestrator : MonoBehaviour
{
    [SerializeField] private GridPresenter _gridPresenter;
    [SerializeField] private CardPresenter _cardPresenter;
    [SerializeField] private FeedbackPresenter _feedbackPresenter;
    [SerializeField] private ChoicePresenter _choicePresenter;
    [SerializeField] private RoutePresenter _routePresenter;
    [SerializeField] private PlayerHudPresenter _playerHudPresenter;

    public async Task PlayAsync(DomainEventBatch batch, PresentationGatePolicy policy)
    {
        PresentationTimeline timeline = _timelineBuilder.Build(batch);
        await timeline.PlayAsync(policy);
    }
}
```

Orchestrator 负责：

- 读取事件批次；
- 查询 Manifest；
- 把事件转成表现时间线；
- 找到相关 View；
- 决定哪些并行、哪些串行；
- 统一处理缺失表现；
- 统一记录表现覆盖日志。

### 7.2 Presenter 分工

| Presenter | 责任 |
|---|---|
| `GridPresenter` | 格子布局、卡牌位置、移动路径、高亮 |
| `CardPresenter` | 单张卡翻面、正反面、数值显示、受击、移除 |
| `FeedbackPresenter` | 飘字、屏幕震动、命中暂停、通用音效 |
| `ChoicePresenter` | 三选一、方向选择、目标选择、商店购买 |
| `RoutePresenter` | 清场后路线选择 |
| `InventoryPresenter` | 道具格、主动遗物格、拖出使用 |
| `RelicPresenter` | 遗物获得、遗物闪光、冷却状态 |
| `PlayerHudPresenter` | 血量、攻击、防御、金币、技能词条 |

### 7.3 View 保持“哑对象”

`CardView` 应该暴露表现方法，但不做业务判断：

```csharp
public sealed class CardView : MonoBehaviour
{
    public CardInstanceId InstanceId { get; private set; }

    public Task PlayFlipAsync(CardFlipViewData data);
    public Task PlayMoveAsync(CardMoveViewData data);
    public Task PlayHitAsync(CardHitViewData data);
    public Task PlayRemoveAsync(CardRemoveViewData data);
    public void SetFaceUpImmediate(bool isFaceUp);
    public void RefreshStats(CardStatsViewData data);
}
```

它不知道“为什么翻面”，只拿到 ViewData。

---

## 8. 表现事件与 Cue 的推荐清单

以下清单适配当前 playtest 体量。人类可以先实现默认版本，后续逐步替换为精细表现。

### 8.1 九宫格基础表现

| CueId | 触发事件 | 用途 | 阻塞策略 |
|---|---|---|---|
| `cue.grid.card.spawn_face_down` | `CardAddedToGrid` | 房间初始化发牌 | Critical |
| `cue.grid.card.flip.default` | `CardFlipped` | 普通翻牌 | Critical |
| `cue.grid.card.move.default` | `CardMoved` | 卡牌移动 | Critical |
| `cue.grid.card.swap.default` | `CardsSwapped` | 翻转卡交换 | Critical |
| `cue.grid.card.cover.default` | `CardCovered` | 召唤覆盖/压牌 | Critical |
| `cue.grid.card.remove.default` | `CardRemoved` | 卡牌移除 | Critical |
| `cue.grid.cell.highlight.valid` | `IntentPreview` | 合法目标高亮 | NonBlocking |
| `cue.grid.cell.highlight.invalid` | `IntentPreview` | 非法目标提示 | NonBlocking |

### 8.2 战斗反馈

| CueId | 触发事件 | 用途 | 阻塞策略 |
|---|---|---|---|
| `cue.combat.player.lunge` | `PlayerInteracted` | 玩家互动前冲 | Critical |
| `cue.combat.damage.hit` | `DamageApplied` | 普通受击 | NonBlocking |
| `cue.combat.damage.zero` | `DamageApplied(amount=0)` | 0 伤害反馈 | NonBlocking |
| `cue.combat.damage.prevented` | `DamagePrevented` | 庇佑免疫 | NonBlocking |
| `cue.combat.death.monster` | `MonsterDefeated` | 怪物死亡 | Critical |
| `cue.combat.gold.gain` | `GoldChanged(delta>0)` | 金币获得 | NonBlocking |
| `cue.combat.stat.change` | `StatChanged` | 属性变化 | NonBlocking |

### 8.3 机关表现

| CueId | 触发事件 | 用途 | 阻塞策略 |
|---|---|---|---|
| `cue.trap.crossbow.armed` | `CardFlipped(crossbow)` | 弩箭机关翻开待触发 | NonBlocking |
| `cue.trap.crossbow.fire_line` | `TrapTriggered(crossbow)` | 同列上方发射 | Critical |
| `cue.trap.spike.arm` | `CardFlipped(spike)` | 尖刺机关蓄势 | NonBlocking |
| `cue.trap.spike.burst` | `TrapTriggered(spike)` | 正交范围爆发 | Critical |
| `cue.trap.teleport.shuffle` | `TrapTriggered(teleport)` | 洗牌、玩家随机移动、重新分布 | Critical |

### 8.4 道具表现

| CueId | 触发事件 | 用途 | 阻塞策略 |
|---|---|---|---|
| `cue.item.store` | `ItemStored` | 收入道具格 | Critical |
| `cue.item.use.generic` | `ItemUsed` | 默认道具使用 | NonBlocking |
| `cue.item.hook_rope.select_direction` | `ChoiceOpened(direction)` | 勾绳方向选择 UI | BlockingInput |
| `cue.item.hook_rope.pull` | `CardMoved(reason=HookRope)` | 勾绳移动 | Critical |
| `cue.item.potion.heal` | `HealingApplied(reason=Potion)` | 恢复药水 | NonBlocking |
| `cue.item.knife.hit` | `DamageApplied(source=ThrowingKnife)` | 飞刀命中 | NonBlocking |
| `cue.item.blessing.shield` | `DamagePrevented` | 庇佑免伤 | NonBlocking |
| `cue.item.light.reveal` | `CardFlipped(reason=LightCard)` | 照明卡揭示 | Critical |
| `cue.item.brutality.buff` | `StatModifierAdded(brutality)` | 暴力卡加成 | NonBlocking |

### 8.5 房间衍生卡与选择

| CueId | 触发事件 | 用途 | 阻塞策略 |
|---|---|---|---|
| `cue.room.gold_card.collect` | `GoldChanged(reason=GoldCard)` | 金币卡收集 | NonBlocking |
| `cue.room.stat_choice.open` | `ChoiceOpened(statUpgrade)` | 属性三选一 | BlockingInput |
| `cue.room.chest.open` | `ChoiceOpened(relicChoice)` | 宝箱打开 | BlockingInput |
| `cue.room.food.heal_full` | `HealingApplied(reason=Food)` | 食品回血 | NonBlocking |
| `cue.room.mentor.teach` | `TraitAcquired` | 导师教授技能 | NonBlocking |
| `cue.room.shop.purchase` | `ShopPurchased` | 商品购买 | NonBlocking |
| `cue.room.shop.no_gold` | `IntentRejected(noGold)` | 金币不足 | NonBlocking |

### 8.6 遗物表现

| CueId | 触发事件 | 用途 | 阻塞策略 |
|---|---|---|---|
| `cue.relic.acquire` | `RelicAcquired` | 获得遗物 | NonBlocking |
| `cue.relic.flash` | `RelicTriggered` | 被动遗物触发闪光 | NonBlocking |
| `cue.relic.active.ready` | `ActiveRelicChargeChanged` | 主动遗物可用 | NonBlocking |
| `cue.relic.active.use` | `RelicActivated` | 主动遗物使用 | Critical |
| `cue.relic.cooldown.reset` | `RoomEntered` | 每房间次数重置 | NonBlocking |

### 8.7 房间流程表现

| CueId | 触发事件 | 用途 | 阻塞策略 |
|---|---|---|---|
| `cue.room.enter` | `RoomEntered` | 进入房间 | Critical |
| `cue.room.clear` | `RoomCleared` | 清场提示 | Critical |
| `cue.route.choices.show` | `RouteChoicesGenerated` | 路线选择出现 | BlockingInput |
| `cue.run.game_over` | `RunEnded(loss)` | 游戏失败 | BlockingInput |
| `cue.run.victory` | `RunEnded(win)` | 通关 | BlockingInput |

---

## 9. AI 如何“用上”表现，但不越界

### 9.1 AI 不直接调用表现，而是保证规则事件完整

以弩箭机关为例，AI 不应该写：

```csharp
Vfx.Play("crossbow_fire_line");
Audio.Play("arrow_shot");
```

AI 应该写：

```text
OnDestroyed:
  找到同列上方所有翻开的卡
  Emit TrapTriggered(crossbow, affectedCards)
  对 affectedCards 逐一 ApplyDamage(6)
```

表现层看到 `TrapTriggered(crossbow)`，Manifest 自动映射到：

```text
cue.trap.crossbow.fire_line
```

如果表现没播，说明 Manifest 或 Orchestrator 问题，不是 AI 业务逻辑应该补丁解决。

### 9.2 AI 可以使用表现标签，但只能是数据

某些内容可能需要变体，例如怪物死亡有不同消散风格。可以允许内容模型暴露标签：

```csharp
public virtual PresentationTagSet PresentationTags => PresentationTagSet.Empty;
```

例如：

```csharp
PresentationTags = ["skeleton", "bone", "act1"]
```

表现层根据标签选择具体变体。AI 不能写播放代码，只能声明语义标签。

### 9.3 AI 新增内容时必须提供表现需求说明

每个 AI 新内容 PR 模板增加一节：

```markdown
## Presentation Contract
- 会产生哪些 DomainEvent：
  - CardFlipped
  - DamageApplied
  - CardRemoved
- 是否需要新增表现 Cue：否
- 使用的表现标签：skeleton, act1
- 是否有特殊演出需求：无
```

如果需要新增 Cue：

```markdown
## Presentation Cue Request
- cueId: cue.boss.skeleton_lord.summon
- 触发事件: CardSummoned(reason=SkeletonLordScatter)
- payload: bossCardId, summonedCardId, targetCoord, coveredCardId?
- blockingPolicy: Critical
- fallback: cue.grid.card.cover.default
```

人类审核后决定是否实现。

---

## 10. 表现覆盖验证

你说“如果有表现 AI 不知道怎么用，就说明这里没落地到位”，这个想法可以自动化。

### 10.1 Manifest 覆盖测试

```text
测试 1：所有 ready + required 的 Cue 必须至少绑定一个 DomainEvent。
测试 2：所有 DomainEventType 必须有默认表现或明确标记为 silent。
测试 3：所有内容模型引用的 PresentationTag 必须在 Manifest 中注册。
测试 4：所有业务逻辑中出现的 PresentationCueId 必须来自生成常量。
测试 5：GameLogic 中禁止出现 Vfx/Audio/Tween/GameServices 调用。
```

### 10.2 事件覆盖测试

为每个核心内容写事件期望：

```text
弩箭机关摧毁：
  必须产生 TrapTriggered(crossbow)
  必须产生 DamageApplied × N
  必须产生 CardRemoved(crossbow)

传送机关翻开：
  必须产生 TrapTriggered(teleport)
  必须产生 CardZoneChanged(nonPlayerCards -> DungeonDeck)
  必须产生 CardMoved(player)
  必须产生 CardAddedToGrid / CardMoved / CardZoneChanged(redistributedCards)

宝箱卡互动：
  必须产生 ChoiceOpened(relicChoice)
```

只要事件完整，表现层就能接上。

### 10.3 未使用表现报告

构建一个工具：

```text
Presentation Coverage Report
────────────────────────────
Ready cues: 42
Required cues: 31
Used by manifest mapping: 29
Unused required cues: 2
  - cue.item.blessing.shield
  - cue.relic.active.ready
Unknown event without cue: 1
  - CardCovered
Content references unknown tags: 0
GameLogic direct presentation calls: 0
```

这比靠人工看代码可靠得多。

### 10.4 领域事件检查器

在 Debug UI 做一个 Event Inspector：

```text
Action #102 InteractWithCardIntent(target=CrossbowTrap#45)
  01 PlayerActionCommitted(index=12)        → silent
  02 DamageApplied(player -> crossbow, 3)   → cue.combat.damage.hit
  03 CardRemoved(crossbow)                  → cue.grid.card.remove.default
  04 TrapTriggered(crossbow)                → cue.trap.crossbow.fire_line
  05 DamageApplied(crossbow -> monster#12)  → cue.combat.damage.hit
  06 CardRemoved(monster#12)                → cue.combat.death.monster
  07 GoldChanged(+10)                       → cue.combat.gold.gain
```

人类可以一眼看出：规则事件是否正确、表现 Cue 是否匹配、有没有 silent 或 missing。

---

## 11. 典型交互流程

### 11.1 玩家移动

```text
玩家拖动玩家卡到空格
  → Presentation: MovePlayerIntent(to)
  → Domain: 校验空格
  → Action: 移动玩家卡
  → DomainEventBatch:
       CardMoved(player, from, to)
       PlayerActionCommitted(index)
       CardFlipped(adjacentTopCardA)
       CardFlipped(adjacentTopCardB)
  → Orchestrator:
       cue.grid.card.move.default
       cue.grid.card.flip.default × 2
```

### 11.2 玩家攻击怪物

```text
玩家拖动玩家卡到正面怪物卡
  → InteractWithCardIntent(monster)
  → Domain: 计算玩家伤害、怪物反击
  → DomainEventBatch:
       PlayerActionCommitted
       DamageApplied(player -> monster)
       DamageApplied(monster -> player)
       CardRemoved(monster) 如果死亡
       GoldChanged(+10) 如果移除怪物
       CardFlipped(underlyingCard) 如果堆叠下方有牌
  → Orchestrator:
       cue.combat.player.lunge
       cue.combat.damage.hit
       cue.combat.death.monster
       cue.combat.gold.gain
       cue.grid.card.flip.default
```

### 11.3 宝箱卡

```text
玩家拖动玩家卡到宝箱卡
  → InteractWithCardIntent(chest)
  → Domain:
       按品质概率生成 3 个遗物候选
       打开 ChoiceSession(relicChoice)
  → DomainEventBatch:
       PlayerActionCommitted
       ChoiceOpened(relicChoice)
  → Orchestrator:
       cue.room.chest.open
       ChoicePresenter 显示三选一

玩家选择遗物
  → ChooseOptionIntent(session, option)
  → Domain:
       AddRelic
       RemoveCard(chest)
  → DomainEventBatch:
       RelicAcquired
       CardRemoved(chest)
  → Orchestrator:
       cue.relic.acquire
       cue.grid.card.remove.default
```

### 11.4 勾绳

```text
玩家从道具格拖出勾绳
  → BeginUseItemIntent(slot)
  → Domain: TargetingSession(CardThenDirection)
  → Presentation: 高亮所有可被移动的卡牌

玩家选择目标卡
  → ChooseTargetIntent(card)
  → Domain: ChoiceSession(direction: 上下左右中合法方向)
  → Presentation:
       cue.item.hook_rope.select_direction

玩家选择方向
  → ChooseOptionIntent(direction)
  → Domain:
       MoveCard(target, adjacentCoord)
       ConsumeItem(hookRope)
  → DomainEventBatch:
       ItemUsed(hookRope)
       CardMoved(reason=HookRope)
  → Orchestrator:
       cue.item.hook_rope.pull
```

### 11.5 尖刺机关

```text
尖刺机关被翻开
  → Domain:
       card.State.triggerAfterActionIndex = currentActionIndex + 1
  → DomainEventBatch:
       CardFlipped(spike)
  → Orchestrator:
       cue.grid.card.flip.default
       cue.trap.spike.arm

下一次玩家行动后
  → Domain:
       TrapTriggered(spike)
       DamageApplied(spike -> orthogonal cards)
  → Orchestrator:
       cue.trap.spike.burst
       cue.combat.damage.hit × N
```

### 11.6 传送机关

```text
传送机关被翻开
  → Domain:
       Emit TrapTriggered(teleport)
       ShuffleNonPlayerGridCardsIntoDeck
       MovePlayer(randomCoord)
       RedistributeDeck(excluding player coord)
  → DomainEventBatch:
       CardFlipped(teleport)
       TrapTriggered(teleport)
       CardZoneChanged(nonPlayerCards -> DungeonDeck)
       CardMoved(player, old, new)
       CardAddedToGrid(redistributed cards)
  → Orchestrator:
       cue.trap.teleport.shuffle
```

这个复杂演出必须由一个统一 Cue 编排，不能让 AI 在业务里一步一步播 Tween。

### 11.7 主动遗物：法则魔杖

```text
玩家点击主动遗物格
  → ActivateRelicIntent(lawWand)
  → Domain:
       检查本房间次数
       打开 TargetingSession(anyCardThenAnyCell)
  → Presentation:
       高亮所有卡牌和所有可放置格

玩家选择卡和目标格
  → ChooseOptionIntent / UseRelicTargetIntent
  → Domain:
       MoveCard(card, targetCell, reason=LawWand)
       MarkRelicUsedThisRoom
  → DomainEventBatch:
       RelicActivated(lawWand)
       CardMoved(reason=LawWand)
       ActiveRelicChargeChanged
  → Orchestrator:
       cue.relic.active.use
       cue.grid.card.move.default 或 cue.relic.law_wand.move_variant
```

---

## 12. 表现层文件组织建议

```text
Assets/Scripts/Game/Presentation/
├─ Contract/
│  ├─ PresentationCueId.cs
│  ├─ PresentationCueIds.g.cs
│  ├─ PresentationManifest.asset 或 presentation_manifest.json
│  ├─ PresentationPayloadSchema.cs
│  └─ PRESENTATION_MANIFEST.md
├─ Runtime/
│  ├─ GamePresentationOrchestrator.cs
│  ├─ PresentationTimelineBuilder.cs
│  ├─ PresentationCueResolver.cs
│  ├─ PresentationCoverageReporter.cs
│  └─ NullPresentationBridge.cs
├─ Input/
│  ├─ GridInputAdapter.cs
│  ├─ DragIntentBuilder.cs
│  ├─ TargetingInputController.cs
│  └─ ChoiceInputController.cs
├─ Presenters/
│  ├─ GridPresenter.cs
│  ├─ CardPresenter.cs
│  ├─ FeedbackPresenter.cs
│  ├─ ChoicePresenter.cs
│  ├─ InventoryPresenter.cs
│  ├─ RelicPresenter.cs
│  ├─ RoutePresenter.cs
│  └─ PlayerHudPresenter.cs
├─ Views/
│  ├─ CardView.cs
│  ├─ GridCellView.cs
│  ├─ InventorySlotView.cs
│  ├─ RelicSlotView.cs
│  └─ ChoiceOptionView.cs
└─ Recipes/
   ├─ CardFlipRecipe.asset
   ├─ DamageHitRecipe.asset
   ├─ TrapCrossbowRecipe.asset
   ├─ TrapTeleportRecipe.asset
   └─ ...
```

`Recipes` 是人类做手感的地方。AI 不改。

---

## 13. 表现配方 Recipe

一个 Cue 可以映射到一个 Recipe。

```csharp
public abstract class PresentationRecipe : ScriptableObject
{
    public PresentationCueId CueId;
    public PresentationBlockingPolicy BlockingPolicy;
    public abstract Task PlayAsync(PresentationContext ctx);
}
```

示例：`TrapCrossbowRecipe`

```text
输入 payload：
  - trapCardId
  - originCoord
  - affectedCardIds
  - affectedCoords
表现：
  1. 弩箭卡轻微蓄力
  2. 同列上方画一条箭矢轨迹
  3. 命中每个 affectedCard
  4. 播放统一音效
  5. 非阻塞飘字由 DamageApplied 自己触发
阻塞：Critical，箭矢轨迹完成后允许后续关键动画
```

这样人类可以把复杂表现做在一个地方，AI 只需要保证事件 payload 完整。

---

## 14. 状态读取：ReadModel，而不是表现层偷看内部对象

表现层需要刷新 UI，但不要直接读可变内部结构。建议领域层提供只读快照：

```csharp
public sealed class RoomReadModel
{
    public IReadOnlyList<GridCellReadModel> Cells { get; }
    public PlayerReadModel Player { get; }
    public IReadOnlyList<ItemSlotReadModel> ItemSlots { get; }
    public IReadOnlyList<RelicReadModel> Relics { get; }
    public IReadOnlyList<ChoiceReadModel> ActiveChoices { get; }
}
```

表现层在以下时机刷新：

- 房间进入；
- 事件批次播放完成；
- ChoiceSession 打开/关闭；
- 存档恢复；
- Debug 强制刷新。

`ReadModel` 可以避免表现层到处访问 `GridState.Cells[x,y]._stack`。

---

## 15. 如何防止接入散落

### 15.1 禁止的散落点

以下地方不应该出现 VFX、Audio、Tween 或具体表现 ID：

- `MonsterCardModel` 子类；
- `TrapCardModel` 子类；
- `ItemCardModel` 子类；
- `RelicModel` 子类；
- `TraitModel` 子类；
- `RoomContentGenerator`；
- `DamageResolution`；
- `GridState`；
- `PlayerInteractAction`。

它们只能发领域事件或返回效果。

### 15.2 唯一允许播放表现的位置

- `GamePresentationOrchestrator`；
- 各类 `Presenter`；
- `PresentationRecipe`；
- `GameServices` 的具体实现；
- 人类写的 View。

### 15.3 代码扫描规则

```text
GameLogic/ 目录中禁止：
  using UnityEngine;
  using DG.Tweening;
  GameServices.
  IVfxService
  IAudioService
  ITweenService
  PresentationCueId("裸字符串")

Core/Domain/ 目录中禁止：
  using UnityEngine;
  具体表现 Cue 播放
  具体 prefab / Transform / AudioClip 引用

Presentation/ 目录允许：
  UnityEngine
  Tween
  Audio
  VFX
但禁止：
  修改领域状态
  自己结算伤害
  自己改变金币/血量/卡牌 Zone
```

---

## 16. 人类制作表现后的标准流程

### Step 1：人类实现表现资源

例如实现“传送机关洗牌重排”的演出。

产出：

- VFX prefab；
- 音效；
- 卡牌移动/缩放/消失/再出现的 Recipe；
- 必要的 UI 提示。

### Step 2：登记到 Manifest

```yaml
cueId: cue.trap.teleport.shuffle
status: ready
semanticEvents:
  - TrapTriggered
  - CardZoneChanged
  - CardMoved
  - CardAddedToGrid
requiredPayload:
  - trapCardId
  - playerFrom
  - playerTo
  - returnedCardIds
  - redistributedCards
blockingPolicy: critical
usagePolicy: required
fallbackCueId: cue.grid.card.move.default
```

### Step 3：生成或更新文档

`Docs/PRESENTATION_MANIFEST.md` 自动包含该 Cue。

### Step 4：AI 写或修改业务逻辑

AI 看到 Manifest 后，知道传送机关必须产生完整事件 payload，而不是自己播 VFX。

### Step 5：运行覆盖测试

如果传送机关没有产生 `TrapTriggered(teleport)` 或 payload 不完整，测试失败。

### Step 6：人类在 Event Inspector 检查

确认事件和表现映射正确。

---

## 17. 让 AI “必须用上全部表现”的正确方式

你可以把 Manifest 中的表现分为四类：

| usagePolicy | 含义 | 测试策略 |
|---|---|---|
| `required` | 对应语义发生时必须使用 | 未绑定事件或事件缺 payload 则测试失败 |
| `optional` | 有则增强，无也可 | 只在报告中提示 |
| `contentSpecific` | 只有特定卡/怪/遗物使用 | 内容模型必须声明标签或事件 reason |
| `debugOnly` | 只给开发调试使用 | 不参与正式覆盖 |

这样就能做到：

- 人类实现的关键表现不会被闲置；
- AI 不需要猜怎么用；
- 没有业务需求的表现不会强迫规则乱触发；
- 检查可以自动化。

示例报告：

```text
Required cue coverage:
  ✓ cue.grid.card.flip.default       bound to CardFlipped
  ✓ cue.combat.damage.hit            bound to DamageApplied
  ✓ cue.trap.crossbow.fire_line      bound to TrapTriggered(crossbow)
  ✗ cue.item.blessing.shield         no event scenario found

Action required:
  添加庇佑魔法卡免伤测试，确保 DamagePrevented 事件产生。
```

---

## 18. AI 可读的表现文档模板

建议每个 Cue 在 Markdown 中长这样：

```markdown
## cue.trap.crossbow.fire_line

- 状态：ready
- 语义：弩箭机关触发，对同列上方翻开的卡牌发射
- 由谁触发：Presentation Orchestrator 监听 TrapTriggered(crossbow)
- AI 是否可直接调用：否
- 业务层需要保证：
  - 触发时产生 TrapTriggered 事件
  - payload 包含 trapCardId、originCoord、affectedCardIds、affectedCoords
  - 后续每个受影响目标产生 DamageApplied
- 阻塞策略：Critical
- Fallback：cue.combat.damage.hit
- 常见错误：
  - 只造成伤害但没有 TrapTriggered，导致弩箭演出不播放
  - affectedCards 包含背面卡，违反设计
```

这类文档非常适合给本地 AI 当上下文。

---

## 19. 表现与逻辑同步策略

### 19.1 状态先变，表现后播

推荐规则状态先完成，再播放表现。这样存档、测试、回放都稳定。

但表现需要看到“变化前后”，因此事件必须携带 from/to：

```csharp
public sealed record CardMovedEvent(
    CardInstanceId CardId,
    GridCoord From,
    GridCoord To,
    MoveReason Reason
) : DomainEvent;

public sealed record DamageAppliedEvent(
    DamageSource Source,
    DamageTarget Target,
    int Amount,
    int HpBefore,
    int HpAfter,
    DamageKind Kind
) : DomainEvent;
```

### 19.2 关键表现可阻塞输入，不阻塞规则完整性

动作执行完成后，可以让表现层播放关键动画，播放期间锁输入。

```text
Action 完成状态变更
  → DomainEventBatch
  → Orchestrator 播放 Critical 动画
  → 解锁下一次玩家输入
```

这能保证玩家看到翻牌/移除/重排，不会在动画中途继续操作。

### 19.3 连锁事件按批次编排

例如玩家击杀怪物后翻出伏击者并触发伏击，可能形成多个 Action：

```text
Action #1 玩家攻击怪物
  - DamageApplied
  - CardRemoved
  - GoldChanged
  - CardFlipped(伏击者)

Follow-up Action #2 伏击触发
  - MonsterAmbushTriggered
  - DamageApplied(monster -> player)
```

表现层按 Action 批次播放，而不是把所有事件混在一个无序列表里。

---

## 20. 对现有服务接口的调整建议

你已有 Tween/Audio/Vfx/FloatingText 接口，可以保留，但不要让 AI 直接调用。

建议新增更语义化的上层接口：

```csharp
public interface IPresentationBridge
{
    Task PlayAsync(DomainEventBatch batch, PresentationGatePolicy policy);
}

public interface IIntentFeedbackService
{
    void ShowPreview(IntentPreview preview);
    void ClearPreview();
    void ShowInvalidReason(string reasonKey);
}
```

底层服务仍然由人类实现：

```text
PresentationRecipe
  → ITweenService
  → IAudioService
  → IVfxService
  → IFloatingTextService
  → IScreenShakeService
  → IHitStopService
```

AI 只接触：

```text
DomainEvent
PlayerIntent
ChoiceSession
PresentationTag
```

---

## 21. 最小可行版本

不要一开始做过大的表现系统。MVP 只需要：

1. `PlayerIntent` 输入管线。
2. `DomainEventBatch` 输出管线。
3. `GamePresentationOrchestrator` 一个集中入口。
4. Manifest 中登记 15 个核心 Cue：
   - 发牌；
   - 翻牌；
   - 移动；
   - 移除；
   - 受击；
   - 0 伤害；
   - 金币变化；
   - 属性变化；
   - 选择窗口；
   - 道具收入；
   - 道具使用；
   - 机关触发；
   - 遗物获得；
   - 清场；
   - 路线选择。
5. 一个 Coverage Report。
6. 一个 Debug Event Inspector。

等这些稳定后，再慢慢增加精细 Cue。

---

## 22. 本地 AI 落地任务拆分

```text
PR-01 Presentation Contract
  PresentationCueId / PresentationManifest schema / generated IDs

PR-02 Domain Event Bridge
  DomainEventBatch → IPresentationBridge → NullPresentationBridge

PR-03 Input Intent Bridge
  GridInputAdapter / IntentPreview / SubmitIntent

PR-04 Orchestrator Skeleton
  GamePresentationOrchestrator / TimelineBuilder / CueResolver

PR-05 Core Cue Recipes
  flip / move / remove / damage / gold / choice / route

PR-06 Coverage Tools
  ManifestCoverageReporter / GameLogic forbidden reference scan

PR-07 Event Inspector
  Debug UI 显示 ActionId、DomainEvent、CueId、missing/fallback

PR-08 Content Scenario Tests
  弩箭、尖刺、传送、宝箱、勾绳、庇佑、法则魔杖
```

每个 PR 的验收条件：

- 业务逻辑测试可在 NullPresentationBridge 下运行；
- 表现层缺资源时有 fallback，不崩溃；
- Manifest 覆盖报告没有 required missing；
- GameLogic 不引用 Presentation；
- 所有新增 Cue 都有文档。

---

## 23. 你该如何和 AI 交互

以后你给本地 AI 的任务不要说：

```text
实现弩箭机关，并播放弩箭特效。
```

应该说：

```text
实现弩箭机关业务逻辑。
必须遵守：
1. 不引用 Presentation / UnityEngine。
2. 摧毁后查询同列上方所有正面朝上的卡牌。
3. 产生 TrapTriggered(crossbow) 事件，payload 包含 affectedCardIds。
4. 对 affectedCardIds 造成 6 点伤害。
5. 相关表现由 cue.trap.crossbow.fire_line 负责，不得直接播放。
6. 添加测试：弩箭不伤害背面卡、不伤害下方卡、不伤害不同列卡。
```

对于表现任务，你对人类或表现 AI 说：

```text
实现 cue.trap.crossbow.fire_line 的 PresentationRecipe。
输入来自 TrapTriggered(crossbow)。
不得改变领域状态。
缺少 affectedCardView 时使用 fallback 并记录 warning。
```

这样业务 AI 和表现实现彻底分离。

---

## 24. 最终判断

你真正需要的不是“让 AI 知道每个表现资源在哪个 prefab 里”，而是建立一份稳定的 **表现契约**。

这份契约应该做到：

- AI 看得懂：有哪些事件、哪些 Cue、需要什么 payload；
- 人类查得清：每个表现由谁触发，是否被使用；
- 系统测得出：缺事件、缺 payload、未使用表现、非法调用都能自动报告；
- 架构隔得开：业务逻辑不依赖表现实现，表现层不结算业务规则；
- 长期可维护：新增 100 张卡、50 个遗物后，表现仍然通过事件和 Manifest 集中管理。

所以推荐的最终交互范式是：

```text
人类实现表现资源
  → 登记 Presentation Manifest
  → 自动生成 AI 可读文档和强类型 CueId
  → AI 写业务逻辑，只产出 DomainEvent
  → Orchestrator 根据 Manifest 播放表现
  → Coverage Report 检查表现是否被正确覆盖
```

这套方式会比“AI 手动调用表现”慢一点点起步，但它能显著降低长期维护成本，也最符合你“人类控制表现、AI 负责业务”的原始目标。
