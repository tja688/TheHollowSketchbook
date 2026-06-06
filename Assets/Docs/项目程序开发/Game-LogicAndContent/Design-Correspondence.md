# Game-LogicAndContent Design Correspondence

更新时间：2026-06-07

## 对照范围

本文件对应以下现行设计文档：

- `Assets/Docs/深入地牢/第二版设计.md`
- `Assets/Docs/深入地牢/互动规则.md`
- `Assets/Docs/深入地牢/每层流程.md`
- `Assets/Docs/深入地牢/怪物设计.md`
- `Assets/Docs/深入地牢/机关卡.md`
- `Assets/Docs/深入地牢/道具卡.md`
- `Assets/Docs/深入地牢/遗物.md`
- `Assets/Docs/深入地牢/词条.md`
- `Assets/Docs/深入地牢/功能卡.md`
- `Assets/Docs/深入地牢/房间衍生卡.md`
- `Assets/Docs/深入地牢/职业.md`

## 已落地内容

### 玩家与初始配置

- 兵大哥 8/3/1：`StarterContentRegistry.StartNewRun`、`PlayerHeroCardModel`
- 初始遗物村好剑：`VillageGoodSwordRelicModel`

### 怪物设计

- 骷髅、带甲骷髅、旗兵骷髅、复仇骷髅、追踪者骷髅、伏击者骷髅、武装骷髅：`StarterContentModels.cs` 对应 monster model
- 大骷髅老爷：`BigSkeletonLordModel`
- 每移除一张怪物卡获得 10 金币：沿用 Domain 统一 `GoldOnRemoved`
- 层主额外奖励（金箱 + 2 金币卡 + 属性卡）：`BigSkeletonLordModel.OnDestroyedAsync`

### 机关卡

- 弩箭机关：`CrossbowTrapModel`
- 尖刺机关：`SpikeTrapModel`
- 传送机关：`TeleportTrapModel`

### 道具卡

- 勾绳：`HookRopeItemModel`
- 恢复药水：`HealingPotionItemModel`
- 飞刀：`ThrowingKnifeItemModel`
- 庇佑魔法卡：`ProtectionSpellItemModel`
- 翻转卡：`FlipCardItemModel`
- 照明卡：`LightCardItemModel`
- 暴力卡：`ViolenceCardItemModel`
- 先攻卡：`FirstStrikeCardItemModel`

### 遗物

- 活着的肉：`LivingFleshRelicModel`
- 木盾：`WoodShieldRelicModel`
- 木剑：`WoodSwordRelicModel`
- 法则魔杖：`LawWandRelicModel`
- 无尽水袋：`EndlessWaterBagRelicModel`
- 道具储备：`ItemStockpileRelicModel`
- 血盾：`BloodShieldRelicModel`
- 村好剑：`VillageGoodSwordRelicModel`

### 功能卡 / 房间衍生卡

- 金币卡：`GoldCardModel`
- 属性提升卡：`StatUpgradeCardModel`
- 食品卡：`FoodCardModel`
- 普通/蓝色/金色宝箱卡：`ChestCardModel`
- 导师刺皮 / 硬皮 / 历战：`MentorCardModel`
- 商店五类商品：`ShopProductCardModel`

### 路线与房间分发

- `RoomContentCatalog` 已按 `gold/stat/chest/food/mentor/shop-product/item/trap/monster-N/boss` 分类注册。
- `RoomType` 到 `route:*` 模型已补齐，可被 `RoomTransitionService` 直接使用。

## 与设计的有意识偏差

### 1. 精英怪未做具体内容化

原因：`怪物设计.md` 只写了“精英怪具体设计”标题，未给出任何精英怪条目。

当前落地：

- `elite` 分类没有首批具体内容。
- 精英房仍由 `DungeonDeckBuilder` 的 fallback elite 生成逻辑兜底。

### 2. 金色遗物池暂未补具体条目

原因：`遗物.md` 的“金色被动遗物 / 金色主动遗物”为空。

当前落地：

- 金宝箱仍会发起 relic 选择流程。
- 但在没有金色条目可用时，会退回当前可抽的现有池。

### 3. 导师卡采用“三张显式导师牌直接选择”而非二级弹窗

原因：`第二版设计.md` 说明餐厅会出现 3 张导师卡，`房间衍生卡.md` 又写了“弹出选项框选择一张导师卡”。

当前落地选择：

- 房间直接生成三张具体导师牌。
- 玩家与其中一张互动后立刻获得对应词条，并移除其他导师牌。

这样仍满足“从三种导师中选一”的规则，但不再套二级 choice UI。

### 4. 怪物词条行为目前主要固化在怪物模型内

原因：现阶段通用 monster trait 回调仍缺“trait 持有者实例”语义，像 `鼓舞` / `复仇` 这类观察者逻辑若强行塞到共享 `TraitModel` 会丢失持有者上下文。

当前落地：

- `TraitIds` 仍保留，供事实文档与未来展示使用。
- 具体规则优先由对应 monster model 实现。

## 设计到文件映射

- 战场内具体内容实现：`Assets/Scripts/Game/Content/Runtime/StarterContentModels.cs`
- ModelId 与内容清单：`Assets/Scripts/Game/Content/Runtime/StarterContentIds.cs`
- 注册、内容池、catalog 入口：`Assets/Scripts/Game/Content/Runtime/StarterContentRegistry.cs`
- 内容验证：`Assets/Scripts/Game/Content/Tests/StarterContentRegistryTests.cs`
- 支撑 Choice / 玩家 trait / relic target 的 Domain 测试：`Assets/Scripts/Game/Core/Tests/DomainBatch4Tests.cs`

## 仍待后续设计补完的缺口

- 精英怪具体名单与数值
- 第二层 / 第三层怪物内容
- 金色遗物内容
- 若未来需要真正把怪物词条完全抽回共享 `TraitModel`，需要额外的 Domain trait-owner 上下文支持
