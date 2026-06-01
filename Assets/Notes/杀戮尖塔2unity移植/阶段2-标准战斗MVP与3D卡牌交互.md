# 阶段 2 交接文档：标准 STS-like 战斗 MVP + 3D 卡牌交互

> 完成日期：2026-06-01
> 负责人：AI Agent
> 状态：**核心闭环已完成，可运行单场战斗**

---

## 1. 阶段目标回顾

根据《五个大阶段落地计划.md》，阶段 2 的目标：

```text
完成一个可玩的单场战斗：
- 进入调试战斗
- 玩家看到 3D 手牌
- 拖卡到敌人/桌面
- 卡牌立即结算
- 能量扣除
- 结束回合
- 敌人行动
- 胜利/失败
```

**验收结果：**
- [x] 运行 `CombatPrototypeScene`（即 `New Scene.unity`）可进入战斗
- [x] 开局抽 5 张牌
- [x] Strike 拖到敌人身上扣能量并造成伤害
- [x] Defend 拖到桌面获得格挡
- [x] 能量不足时卡牌变灰/不可交互
- [x] 点击结束回合，手牌弃掉，敌人行动，下一回合重新抽牌
- [x] 敌人死亡后触发胜利弹窗
- [ ] 敌人意图显示（IntentView 已创建但当前不显示，见已知问题）

---

## 2. 新增/修改文件清单

### Core 层修复
| 文件 | 说明 |
|------|------|
| `Assets/Game/Core/Runtime/Compatibility/IsExternalInit.cs` | 修复 C# `init` accessor 编译错误 |
| `Assets/Game/Core/Tests/CoreLogicTests.cs` | 将 `async Task` 测试改为 `void` + `.GetAwaiter().GetResult()`，适配 Unity NUnit |

### Presentation 服务层
| 文件 | 说明 |
|------|------|
| `Assets/Game/Presentation/Runtime/Services/GameServices.cs` | 静态服务入口，含 EnsureInitialized |
| `Assets/Game/Presentation/Runtime/Services/ITweenService.cs` | Tween 接口 + EaseType 枚举 |
| `Assets/Game/Presentation/Runtime/Services/CoroutineTweenService.cs` | 基于 Coroutine 的 Tween 实现 |
| `Assets/Game/Presentation/Runtime/Services/IAudioService.cs` | 音频接口 + AudioEventId/AudioParams |
| `Assets/Game/Presentation/Runtime/Services/UnityAudioService.cs` | 占位音频服务（Debug.Log） |
| `Assets/Game/Presentation/Runtime/Services/IVfxService.cs` | 特效接口 + VfxEventId/VfxContext |
| `Assets/Game/Presentation/Runtime/Services/SimpleVfxService.cs` | 占位 VFX 服务 |
| `Assets/Game/Presentation/Runtime/Services/IFloatingTextService.cs` | 飘字接口 |
| `Assets/Game/Presentation/Runtime/Services/FloatingTextService.cs` | 占位飘字服务 |

### 卡牌与手牌视图
| 文件 | 说明 |
|------|------|
| `Assets/Game/Presentation/Runtime/Combat/Cards/CardView.cs` | World Space Canvas 卡牌视图，Bind/Refresh/PlayHover/PlayMoveTo |
| `Assets/Game/Presentation/Runtime/Combat/Cards/CardViewPool.cs` | 对象池（简化版） |
| `Assets/Game/Presentation/Runtime/Combat/Cards/ArcHandLayout.cs` | 扇形手牌布局算法 |
| `Assets/Game/Presentation/Runtime/Combat/Cards/PlayerHandView.cs` | 手牌视图，订阅 Hand 事件，同步 CardView 列表 |

### 敌人与生物视图
| 文件 | 说明 |
|------|------|
| `Assets/Game/Presentation/Runtime/Combat/Creatures/EnemyView.cs` | 敌人视图，绑定 Creature，订阅 HP/Block/Power/Died 事件 |
| `Assets/Game/Presentation/Runtime/Combat/Creatures/CreatureHealthBar.cs` | 血条、Block、Power 显示（World Space Canvas） |
| `Assets/Game/Presentation/Runtime/Combat/Creatures/IntentView.cs` | 敌人意图显示（World Space Canvas） |

### 战斗 UI
| 文件 | 说明 |
|------|------|
| `Assets/Game/Presentation/Runtime/Combat/UI/EnergyPanel.cs` | 能量显示 "当前 / 最大" |
| `Assets/Game/Presentation/Runtime/Combat/UI/EndTurnButton.cs` | 结束回合按钮，绑定 CombatManager |
| `Assets/Game/Presentation/Runtime/Combat/UI/PileButtonsView.cs` | 抽牌堆/弃牌堆/消耗堆数量显示 |

### 输入与拖拽
| 文件 | 说明 |
|------|------|
| `Assets/Game/Presentation/Runtime/Input/CombatRaycastService.cs` | 射线检测：Card/Enemy/PlayArea |
| `Assets/Game/Presentation/Runtime/Input/CardDragController.cs` | 卡牌拖拽逻辑：悬停、拖拽、目标判定、回弹 |
| `Assets/Game/Presentation/Runtime/Input/CombatInputController.cs` | 输入控制器：提交 CardPlayRequest、结束回合 |

### 场景与调试入口
| 文件 | 说明 |
|------|------|
| `Assets/Game/Presentation/Runtime/Combat/CombatPrototypeController.cs` | **核心整合器**：运行时动态创建相机/灯光/UI/敌人/卡牌模板，绑定所有视图，处理战斗事件 |
| `Assets/Game/Presentation/Runtime/Bootstrap/DebugCombatBootstrap.cs` | 调试入口：注册内容 → 创建玩家/敌人/CombatState → 启动战斗 |
| `Assets/Scenes/New Scene.unity` | 调试战斗场景（含 DebugBootstrap GameObject） |

### asmdef 修改
| 文件 | 说明 |
|------|------|
| `Assets/Game/Presentation/Game.Presentation.asmdef` | 添加 `Unity.TextMeshPro` 引用 |

---

## 3. 架构要点

### 3.1 运行时动态创建策略

阶段 2 没有制作 Prefab 资源，所有视觉元素由 `CombatPrototypeController` 在运行时通过代码创建：

- **CardView 模板**：空 GameObject + `CardView` 组件，`Awake` 自动调用 `EnsureVisuals()` 创建 Canvas/Image/Text/Collider
- **EnemyView**：空 GameObject + SpriteRenderer（占位）+ HealthBar Canvas + IntentView Canvas
- **UI Overlay**：Screen Space Overlay Canvas + Image/Text 组件
- **相机/灯光**：若场景中不存在则自动创建

**优点**：零 Prefab 依赖，Agent 可完全通过代码迭代。
**缺点**： Inspector 不可调，美术替换时需要重构。

### 3.2 输入流程

```text
PointerDown on CardView
  → CardDragController.BeginDrag
  → 卡牌放大 + 抬起
PointerDrag
  → 卡牌跟随鼠标（射线到桌面平面）
  → 悬停 EnemyView 时高亮
PointerUp
  → 判定目标有效性（根据 CardTargeting）
  → 有效：CombatInputController.SubmitCardPlayRequest
          → CombatManager.SubmitCardPlayRequestAsync
          → ImmediatePlayCardAction 执行
  → 无效：Tween 回弹到手牌
```

### 3.3 CombatPrototypeController 事件链

```text
Bind(CombatManager)
  ├─ BuildSceneObjects()     创建所有 GameObject
  ├─ BindViews()             绑定 PlayerHandView / EnemyViews / UI
  └─ SubscribeEvents()       订阅 CombatManager 事件

TurnStarted
  → Refresh EnergyPanel / PileButtonsView / ArrangeCards

EnemyIntentRolled
  → 找到对应 EnemyView，刷新 IntentView

CreaturesChanged
  → Refresh EnergyPanel / PileButtonsView

CombatWon / CombatEnded
  → 显示胜利/失败弹窗
```

---

## 4. 已知问题与 TODO

### 4.1 IntentView 不显示（P1）
**现象**：敌人上方没有意图图标和文字。
**可能原因**：
- `IntentView.FadeInAsync` 的 CanvasGroup 动画未正确执行
- `CombatPrototypeController.CreateEnemyView` 中 IntentView 的 Canvas `worldCamera` 未设置
- `IntentView` 的 `Awake` 中 `gameObject.SetActive(false)` 导致初始状态不可见，但 `ShowIntent` 已重新激活

**建议排查**：
1. 在 `IntentView.ShowIntent` 开头加 `Debug.Log($"ShowIntent: {intent.Description}");`
2. 检查 `OnEnemyIntentRolled` 是否被调用
3. 检查 `IntentView` 的 Canvas 是否在相机视野内，renderMode 和 sortingOrder 是否正确

### 4.2 敌人视觉占位简陋（P2）
**现象**：敌人是纯色方块，无动画。
**方案**：阶段 5 替换为 Spine/Animator 或 3D 模型。

### 4.3 卡牌 World Space Canvas 字号偏小（P2）
**现象**：卡牌文字在部分分辨率下模糊。
**方案**：调整 `CardView.EnsureVisuals` 中 Canvas 的 `referencePixelsPerUnit` 或增大字体。

### 4.4 无牌库预览/展开功能（P3）
**现象**：点击 Draw/Discard/Exhaust 按钮无反应。
**方案**：阶段 3 或 5 实现牌库预览面板。

### 4.5 CombatPrototypeController 过重（P3）
**现象**：单个类负责创建所有场景对象。
**方案**：阶段 3 拆分为 `CombatSceneBuilder` + `CombatViewBinder`。

### 4.6 缺少 PlayMode 自动化测试（P2）
**现状**：仅有 EditMode 测试（7 个通过）。
**建议**：阶段 3 补充 PlayMode 测试，验证拖拽、回合切换、胜利条件。

---

## 5. 下一阶段（阶段 3）建议

阶段 3 目标是 **Run、地图、房间、奖励、存档闭环**。

建议优先复用阶段 2 的 Presentation 服务层（Tween/Audio/Vfx），并注意：

1. **CombatManager 保持不变**：阶段 3 的战斗仍然使用即时结算（标准 STS-like），不要提前插入缓冲区逻辑。
2. **CardView / EnemyView 可直接复用**：只需在地图场景和奖励场景中重新实例化或切换显示。
3. **新增 RunManager**：管理 RunState、ActMap、房间切换。
4. **新增 SaveManager**：序列化 RunState、Player、CardPile、RngState。
5. **新增 MapView**：显示 ActMap 节点和路径，点击后调用 `RunManager.EnterMapCoord`。
6. **新增 RewardScreenView**：战斗胜利后显示金币/卡牌奖励，选择后写入 `Player.Deck`。

---

## 6. 测试状态

| 测试类型 | 数量 | 状态 |
|---------|------|------|
| EditMode 核心逻辑测试 | 7 | **全部通过** |
| PlayMode 自动化测试 | 0 | 未实现 |
| 手动场景测试 | 1 | **通过**（截图验证） |

### EditMode 测试列表
- `ModelDb_RegisterAndCloneMutable`
- `CardPile_Draw_ShuffleDiscardWhenDrawEmpty`
- `CreatureCmd_Damage_BlockAbsorbsBeforeHp`
- `CreatureCmd_StrengthModifiesAttackDamage`
- `Card_Strike_DealsDamage`
- `Card_Defend_GainsBlock`
- `ActionQueue_ExecutesInOrder`

---

## 7. 快速启动指南

1. 打开 Unity 项目
2. 打开场景 `Assets/Scenes/New Scene.unity`
3. 点击 Play
4. 观察：
   - 5 张卡牌扇形出现在上方
   - 敌人出现在中上方（红色方块）
   - 左下角能量 3/3
   - 右下角 End Turn 按钮
   - 底部 Draw(6) / Discard(0) / Exhaust(0)
5. 操作：
   - 鼠标左键按住卡牌拖拽到敌人身上 → 松手 → 卡牌消失，敌人扣血
   - 点击 End Turn → 手牌弃掉 → 敌人行动 → 下一回合抽牌
   - 将敌人 HP 打到 0 → 显示 "VICTORY" 弹窗

---

## 8. 关键代码入口

| 入口 | 文件 | 方法 |
|------|------|------|
| 调试启动 | `DebugCombatBootstrap.cs` | `StartPrototypeCombatAsync()` |
| 场景构建 | `CombatPrototypeController.cs` | `Bind(CombatManager)` |
| 卡牌打出 | `CombatInputController.cs` | `SubmitCardPlayRequest()` |
| 战斗流程 | `CombatManager.cs` | `StartPlayerTurnAsync()` / `ExecuteEnemyTurnAsync()` |
| 伤害计算 | `CreatureCmd.cs` | `DealDamage()` |
