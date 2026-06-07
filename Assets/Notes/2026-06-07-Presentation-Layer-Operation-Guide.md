## Presentation 层初版落地指南

本文档描述 Presentation 层初版实现的结构、装配关系与操作方式。

### 架构概览

Presentation 层遵循四层模型的契约模式：

`Presentation 输入 → PlayerIntent → DomainFacade → DomainEventBatch → Presentation Controller → CardView/ChoiceButtonView`

核心类是 `DomainPresentationController`（MonoBehaviour），它负责：
- 监听 Domain 事件批次（DomainEventBatch）
- 将事件映射为 DOTween 动效（移动、翻转、淡入淡出、打击反馈）
- 管理卡牌视图（CardView）的创建、定位、同步与销毁
- 处理玩家点击（卡牌点击、格子点击、遗物点击）并转换为 PlayerIntent
- 维护 HUD 文本（生命、金币、角色）和详情面板

### 装配边界

| 程序集 | 引用 | noEngineReferences |
|--------|------|-------------------|
| Game.Core | 无 | true |
| Game.Content | Game.Core | true |
| Game.Presentation | Game.Core, Unity.TextMeshPro, Sirenix.OdinInspector.Attributes | false |

Presentation 不引用 Game.Content。启动时的 `StarterContentRegistry.StartNewRun(seed)` 调用通过委托注入模式解耦。

### 委托注入：Content 启动桥接

`DomainPresentationController` 暴露一个公共委托属性：

```csharp
public Func<int, DomainActionContext> CreateRunContext { get; set; }
```

`DomainPresentationBootstrap`（位于 Assembly-CSharp 的 `Assets/Scripts/Bridge/`）在 Awake 中完成接线：

```csharp
_controller.CreateRunContext = seed => StarterContentRegistry.StartNewRun(seed);
```

场景中的接线步骤：
1. 在场景根对象上挂载 `DomainPresentationController`
2. 在同一对象上挂载 `DomainPresentationBootstrap`（它会自动 GetComponent 关联）
3. Play 模式下 Bootstrap 在 Awake 阶段完成委托注入

### Odin 效果面板（PresentationEffectPanel）

这是一个 Odin Inspector 增强的 ScriptableObject，通过 Tab Group 组织为四个标签页：

**操控 Tab**
- 启动设置（autoStart、seed）
- 关联状态（只读显示当前绑定的 Controller）
- 快捷操作按钮（"开始演出"、"重置为默认配置"）

**动效 Tab**
- 动画时长调节（moveDuration、flipDuration、fadeDuration、hitPunchDuration）
- 力度参数（hitPunchStrength、hoverScale）

**配色 Tab**
- 基础配色（背面色、玩家色、怪物色、机关色、道具色、金币色）
- 扩展配色（路线色、特殊色、遗物色）

**交互 Tab**
- 预览高亮色（合法/非法）
- 描边空闲色

创建方式：在 Project 窗口右键 → Create → CardDungeon → Presentation → Effect Panel。
将生成的 .asset 拖入 `DomainPresentationController` 的 Effect Panel 字段即可。

面板也可以放在 `Assets/Resources/Presentation/` 下，Controller 启动时会自动从 Resources 加载。

### PlaytestConfig（旧版兼容）

`PresentationPlaytestConfig` 仍然可用。当 EffectPanel 存在时，EffectPanel 的值会覆盖 PlaytestConfig。优先级为：Inspector 序列化值 < PlaytestConfig < EffectPanel。

### DOTween 兼容性

DOTween 的 `CanvasGroup.DOFade` 和 `Tween.AsyncWaitForCompletion` 扩展方法定义在 `DOTween/Modules/` 的源码文件中，这些文件属于 Assembly-CSharp。由于 Game.Presentation 是独立程序集，无法访问 Assembly-CSharp 中的扩展方法。

Controller 内置了两个替代方法：
- `AwaitTween(Tween)` — 使用 TaskCompletionSource 替代 AsyncWaitForCompletion
- `FadeCanvasGroupAlpha(CanvasGroup, float, float)` — 使用 DOTween.To 替代 CanvasGroup.DOFade

### 卡牌视图（CardView）

CardView 是 MonoBehaviour，实现了 IPointerClickHandler/IPointerEnterHandler/IPointerExitHandler。它通过名称匹配查找子物体中的 TextMeshProUGUI 组件（"名字"、"词条/简要描述"、"攻击"、"防御"）。

如果场景中的卡面预制体使用不同的子物体名称，需要更新 CardView.Initialize 中的 switch 分支。

### 已知限制与后续工作

1. `TranslateFailureReason` 目前只覆盖常见的 Domain 拒绝原因。新增的 Domain 拒绝码需要同步更新。
2. `HandleRelicTargetSelectionClickAsync` 目前只处理 AnyCardThenAnyCell 的第一阶段。第二阶段（选格子后提交遗物意图）需要后续实现。
3. DOTween Modules 目前仍在 Assembly-CSharp 中。如果未来其他 asmdef 也需要 DOTween UI 扩展，可以考虑为 `DOTween/Modules/` 创建独立 asmdef。
4. `ClearChildrenExceptCardViews` 方法名暗示不清理 CardView，但实际会销毁 CardView。方法名可能需要修正。
