# 卡牌印制管线 Same Target 尝试记录（2026-05-25）

## 本次落地对象

- 场景：`Assets/Tests/render.unity`
- 目标对象：`Assets/Tests/render.unity/卡牌-原版材质3`
- 卡面插画源：`Assets/Arts/Pictures/卡面测试素材.png`
- 标题字体：`Assets/Arts/Fronts/Rock_Salt/RockSalt-Regular SDF.asset`
- 标题文本：`Same Target`
- 图标源：`Assets/Arts/Pictures/people.png`

## 产物

- 编辑器烘焙工具：`Assets/_Project/Editor/CardPrinting/CardPrintBakeUtility.cs`
- 烘焙卡面贴图：`Assets/_Project/Rendering/CardPrints/T_CardPrint_SameTarget.png`
- 卡牌材质：`Assets/_Project/Rendering/Materials/CardPrints/M_CardPrint_SameTarget.mat`

工具菜单入口：`Tools/CardDungeon Rendering/Bake Same Target Card Print`。

## 当前实现方式

这次没有把文字、图标做成独立 Canvas 或 3D Text，而是先在编辑器里生成一张 `1024x1024` 卡牌 BaseMap，再把它接到 `CardDungeon/RetroFakeLit` 材质的 `_BaseMap`。这样标题、插画、图标会和卡牌本体一起接受当前 URP 光照、RetroFakeLit 假光照分层、雾色、全屏 Posterize/Composite/Bloom 等项目渲染管线影响。

卡牌模型 UV 经检查正面区域在贴图右半边，大致使用 `x=0.5..1.0, y=0..0.74`，所以本次烘焙只在这块正面 UV 上叠加涂鸦层，尽量保留原始卡牌底图、边缘和旧材质纹理。

## 视觉处理

### 当前方案：无边界涂鸦覆盖

- 不再重铺整张脏棕黄底纸，也不再画外框、插画框或图标框，避免把原本扑克牌质感改没。
- `卡面测试素材.png` 现在按 contain 方式完整映射到整张卡面正面附近，超出只被卡牌正面 UV 裁掉，不再被内部矩形框限制。
- 插画和 `people.png` 都按 alpha + luma 转成深褐墨迹/涂鸦遮罩：白色或透明区域基本不影响卡面，深色笔画会直接压到原卡面上。
- `Same Target` 仍由 TextMeshPro + `RockSalt-Regular SDF.asset` 离屏生成 mask，但标题区域故意跨出卡面正面边界，由正面 UV 裁切，形成“直接写上去”的感觉。
- 涂鸦层只加轻微噪声侵蚀和脏化，重点是让图像可见，而不是做强破损。

### 这次定位出的旧问题

- 旧版把正面区域整体 `FillPaper` 覆盖成新底纸，又额外画了外框/插画框/图标框，所以牌面本体被改得太多。
- 旧版插画被限制在中间小框，并且 `paperBlend` 与高亮白区处理过强；实测旧插画区域亮度标准差约 `0.011`，几乎被揉成均匀纸色，所以远景基本看不到图。
- `M_CardPrint_SameTarget` 的 `_BaseColor=(0.86,0.76,0.60)` 会二次压暗/染色烘焙贴图；现在改成白色，避免材质再把涂鸦吃掉。

## 当前布局参数

- 卡牌正面 UV 对应贴图区域：`RectInt(512, 22, 512, 738)`。
- 插画涂鸦层：`RectInt(front.x + 6, front.y - 10, front.width - 12, front.height + 20)`，接近覆盖整张正面。
- 标题层：mask `704x176`，目标区域 `RectInt(front.x - 24, front.y + 560, front.width + 48, 156)`，允许左右越界后由卡面裁掉。
- 图标层：`RectInt(front.x + 176, front.y + 72, 160, 160)`。


## 材质参数

`M_CardPrint_SameTarget` 使用 `CardDungeon/RetroFakeLit`：

- `_BaseMap`：`T_CardPrint_SameTarget.png`
- `_BaseColor`：`(1, 1, 1, 1)`，避免二次染色压暗烘焙贴图。
- `_AmbientStrength`：`0.18`
- `_RampSteps`：`4`
- `_RampStrength`：`0.28`
- `_SpecStrength`：`0.025`
- `_SpecPower`：`18`
- `_FogStart`：`2.2`
- `_FogEnd`：`5.5`
- `_EmissionStrength`：`0`，本次卡面是“印制墨迹”，不是发光符文。

## 可复用方向

`CardPrintBakeUtility.BakeAndApply(Request request)` 已经把标题、目标对象、插画、图标、字体、输出贴图、输出材质做成请求参数。后续可以扩展成：

1. 卡牌数据驱动：由 CardData 传入标题、插画、图标和输出路径。
2. 缓存：按标题/图标/插画路径和参数 hash 生成贴图，避免重复烘焙。
3. 三条独立层级：标题层、图标/logo 层、插画层各自有 rect、opacity、erosion、saturation、paperBlend 参数。
4. 运行时版本：当前是编辑器烘焙到 PNG；如果卡牌运行时动态变化，可以改为 RenderTexture / Texture2D 缓存。

## 后续调参建议

- 如果插画仍不够清楚：先提高 `DrawDoodleImage` 的 `opacity`，或降低 luma mask 的门槛；不要再加内部画框。
- 如果涂鸦太重：降低插画层 `opacity=0.96` 或 `sourceInfluence=0.16`，但保留 `_BaseColor=Color.white`。
- 如果标题太规整：略增标题 `erosion`；如果标题太断，降低到 `0.03..0.04`。
- 如果想更像孩子乱画：下一步应加入每层的旋转/偏移/缩放扰动，而不是恢复卡牌 UI 框。
- 如果远景低清导致字读不出：保持卡面烘焙 1024，优先调整标题 mask 尺寸和侵蚀。

## 当前结论

当前更适合的方向是“保留扑克牌本体，把字、图、图标当作无边界深褐涂鸦直接压到卡面上”。它比旧版框选印刷更接近孩子在扑克牌上乱画，也不会把原卡牌材质整体改没。后续正式化时应把 layer rect、luma mask、opacity、erosion 和随机偏移做成 ScriptableObject 或 CardData 参数。
