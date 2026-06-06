# Game-LogicAndContent Authority

更新时间：2026-06-07

## 层目标

`Game.Content` 是九宫格地牢规则之上的首批具体内容层，负责：

- 具体怪物、机关、道具、遗物、房间衍生卡、商店商品、导师卡、路线卡的 `ModelId` 与 `CardModel`/`RelicModel`/`TraitModel` 实现。
- `ModelDb` 注册与 `RoomContentCatalog` 分类供给。
- 在不触碰 Presentation 的前提下，通过 `DomainEvent`、选择会话、规则状态修改，把设计内容落到 Domain 事实流。

本层仍遵守：

- 只依赖 `Game.Core`。
- 不使用 `UnityEngine`。
- 不直接播放视觉效果；所有可视化都依赖已有 `DomainEventType` 和事件载荷。

## 当前入口

主入口为 `Game.Content.Runtime.StarterContentRegistry`：

- `RegisterAll()`：注册全部首批内容模型，并返回配套 `RoomContentCatalog`。
- `CreateDomainRunFlow(...)`：用内容 catalog 构造带内容分发表的 `DomainRunFlow`。
- `StartNewRun(seed)`：注册内容、创建新 run、写入 `ContentCatalog`、给玩家装备初始遗物 `VillageGoodSword`。

## 当前内容清单

### 玩家与兼容模型

- `player:hero`：首发玩家卡模板，数值为 8/3/1。
- `Character:PrototypeHero`、`Act:PrototypeAct`：给旧 `PrototypeRunController` / `RunManager` 路径保底的兼容模型。

### 怪物

- 等级 1：`monster:skeleton`
- 等级 2：`monster:armored-skeleton`
- 等级 3：`monster:banner-skeleton`、`monster:revenge-skeleton`
- 等级 4：`monster:tracker-skeleton`、`monster:ambusher-skeleton`、`monster:war-skeleton`
- 层主：`monster:big-skeleton-lord`

### 机关

- `trap:crossbow`
- `trap:spike`
- `trap:teleport`

### 道具

- `item:hook-rope`
- `item:healing-potion`
- `item:throwing-knife`
- `item:protection-spell`
- `item:flip-card`
- `item:light-card`
- `item:violence-card`
- `item:first-strike-card`

### 房间卡 / 商品 / 导师 / 主动遗物拾取卡

- 功能卡：`room:gold`、`room:stat-upgrade`、`room:food`
- 宝箱：`room:ordinary-chest`、`room:blue-chest`、`room:gold-chest`
- 导师：`room:mentor-thorn-skin`、`room:mentor-iron-skin`、`room:mentor-veteran`
- 商店商品：`room:shop-attack`、`room:shop-defense`、`room:shop-max-hp`、`room:shop-random-item`、`room:shop-ordinary-chest`
- 主动遗物拾取卡：`room:pickup-law-wand`、`room:pickup-endless-water-bag`、`room:pickup-blood-shield`

### 遗物

- 白色被动：`relic:living-flesh`、`relic:wood-shield`、`relic:wood-sword`
- 白色主动：`relic:law-wand`、`relic:endless-water-bag`
- 蓝色被动：`relic:item-stockpile`
- 蓝色主动：`relic:blood-shield`
- 初始遗物：`relic:village-good-sword`

### 路线卡

- 为 `RoomType` 的每个有效分支注册 `route:*` 模型，当前用 `GenericRouteChoiceModel` 承载。

## 注册与分发表约定

`RoomContentCatalog` 当前使用以下分类键：

- 怪物：`monster-1`、`monster-2`、`monster-3`、`monster-4`
- 层主：`boss`
- 机关：`trap`
- 道具：`item`
- 金币卡：`gold`
- 属性提升卡：`stat`
- 宝箱卡：`chest`
- 食品卡：`food`
- 导师卡：`mentor`
- 商店商品：`shop-product`

这次首落明确不再把所有房间牌混注册到一个 `room` 分类里；`DungeonDeckBuilder` 已按房间牌类型分别取样。

## 内容实现约定

### 怪物词条落点

首批怪物词条没有把怪物行为硬塞进通用 `TraitModel` 运行时，而是采用两层结构：

- `TraitIds` 继续作为内容事实与展示元数据来源。
- 具体行为优先写进对应怪物模型本身，避免当前 Domain trait 回调缺少“持有者实例”语义时带来的歧义。

这适用于：

- `鼓舞`：通过怪物战斗前的攻击修正实现。
- `复仇`：通过其他怪物被移除时，对场上复仇骷髅直接加攻实现。
- `好战`：通过每 3 次玩家行动后的位移/追击实现。
- `伏击`：通过翻开时给自己赋先攻并立刻结算互动实现。
- `破甲`：通过战斗后检查怪物是否真的对玩家造成过一次攻击事件，再施加房间防御减益实现。
- `散子`：通过层主受伤后的阈值检测与相邻格召唤实现。

### 玩家词条落点

玩家词条继续使用 `TraitModel`，并通过 `PlayerRunState` 的 trait 集合参与：

- `thorn-skin`
- `iron-skin`
- `veteran`
- 临时 `violence`

简单标记型能力仍优先用 `PlayerRunState` keyword，例如 `firstStrike`。

## Presentation 事实约定

当前内容只复用已有 `DomainEventType`，未新增新的 Presentation cue id。

### 常见事件输出

- 标准怪物/机关/玩家战斗：`DamageApplied`、`CardRemoved`、`MonsterDefeated`、`GoldChanged`
- 翻开型内容：`CardFlipped`
- 机关延迟触发：`TrapTriggered`
- 道具入包与使用：`ItemStored`、`ItemUsed`
- 房间清空与路线：`RoomCleared`、`RouteChoicesGenerated`
- 属性/词条/遗物获取：`StatChanged`、`TraitAcquired`、`RelicAcquired`
- 选择型卡牌：`ChoiceOpened`、`ChoiceResolved`
- 主动遗物：`RelicActivated`

### 内容家族与事件载荷

- 金币卡：`GoldChanged.Amount=50`，`Reason=GoldCard`
- 属性提升卡：`ChoiceOpened.Reason=<sessionId>`，结算后 `StatChanged.Reason=player:attack|player:defense|player:max-hp`
- 宝箱/商店宝箱：`ChoiceOpened` + `ChoiceResolved`，结算后 `RelicAcquired.Reason=<relicId>`
- 导师卡：`TraitAcquired.Reason=<traitId>`
- 尖刺机关：延迟动作出队时先发 `TrapTriggered.Reason=spike`，随后对相邻翻开卡发 `DamageApplied`
- 传送机关：重洗/重发本身只复用 `CardZoneChanged`、`CardAddedToGrid`、`CardMoved`、`CardFlipped`

## 测试策略

当前内容层测试放在 `Assets/Scripts/Game/Content/Tests/StarterContentRegistryTests.cs`，覆盖重点为：

- 内容注册与 catalog 分类是否完整。
- 选择型房间卡是否能通过 `ChoiceSession` 闭环结算。
- 首批怪物/机关代表机制是否通过 Domain pipeline 生效。
- 道具与主动遗物的目标选择、非法目标拒绝、效果结算。

配套的 Domain 支撑测试放在 `Assets/Scripts/Game/Core/Tests/DomainBatch4Tests.cs`，用于保证：

- 选择会话能回调到源 card model。
- 玩家 trait 状态能存档恢复并参与房间生命周期。
- 主动遗物 target 会通过 intent 进入 relic model。

## 已知边界

- 精英怪具体内容设计文档仍为空，因此 `elite` 分类未落具体内容；精英房仍会走 Domain fallback 怪。
- 金色遗物池当前无具体设计条目，因此金宝箱会退化为只在现有可用池里抽取。
- 怪物词条虽然都保留了 `TraitIds`，但首批行为主要由怪物模型本身承载，而不是依赖通用怪物 trait 执行器。

## Change Memory

- 2026-06-07：首次全量落地 `Game.Content`，补齐首批怪物、机关、道具、遗物、房间牌、路线牌、兼容角色/Act 模型与注册表，并配套补充内容测试与内容事实文档。
