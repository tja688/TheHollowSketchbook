---
id: kd_8c77f5ec-cb9b-4166-9065-2c8095f94a45
type: memory
path: unity-project-understanding/rendering-notes-summary.md
title: rendering-notes-summary
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1779672335148
updatedAt: 1780755305499
---

# rendering-notes-summary

## Summary
Concise lookup cache for current render validation scene, comfort tuning, render console entry/assets, shadow behavior of RetroFakeLit, card-face printing prototypes, the TestPlane TMP surface prototype, and current RetroFakeLit/world-space card text rendering constraints.

<!-- locus:maintain-rules:start -->
- Record only Unity project structure knowledge and lookup info that reduce repeated exploration
- Maintain only project-derived engineering understanding, including directory responsibilities, system entry points, asset relationships, runtime entry points, and config mappings
- Write user-supplied design goals, gameplay intent, product direction, and solution decisions into Design
- Prioritize directory responsibilities, core system entry points, key scenes, prefabs, ScriptableObjects, assemblies, and config mappings
- Record verified asset relationships, runtime entry points, key dependencies, and common lookup paths
- Remove temporary investigation traces, one-off task residue, unverified guesses, and expired cache
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
## Render Validation / Comfort Tuning

- Current validation scene: `Assets/Tests/render.unity`.
- Main stable tuning reference: `Assets/Notes/类邪恶冥刻舒适画面调参记录_2026-05-24.md`.
- Current comfortable target: darker, warmer table scene with restrained Bloom and only small highlight glows.

## Render Console

- Main entry: `Tools/CardDungeon Rendering/项目渲染管线综合控制台`.
- It manages URP baseline, Phase 05 RetroFakeLit, Phase 07 Posterize LUT, Phase 08 Retro Composite, Bloom, and presets.
- Key editable assets remain the URP HighFidelity settings, renderer feature asset, SampleSceneProfile, Phase 07/08 materials, and preset assets.

## Shadow / RetroFakeLit Findings

- `Assets/Settings/URP-HighFidelity.asset` currently keeps main-light shadows on, but disables additional/local light shadows (`m_AdditionalLightShadowsSupported = 0`, `m_LocalShadowsSupported = 0`). So spot/point lights in `Assets/Tests/render.unity` can light objects but will not cast realtime shadows.
- `Assets/_Project/Rendering/Shaders/RetroFakeLit.shader` samples main-light shadow attenuation, so RetroFakeLit materials can receive main directional-light shadows, but the shader currently has no `ShadowCaster` pass. Objects using this shader therefore do not cast shadows into URP shadow maps.
- Practical consequence in `Assets/Tests/render.unity`: with most visible props using RetroFakeLit, the scene can look like “lighting works but nothing really throws shadows,” especially under local lights.

## Card Face Printing Prototype

- Editor bake entry: `Tools/CardDungeon Rendering/Bake Same Target Card Print` in `Assets/_Project/Editor/CardPrinting/CardPrintBakeUtility.cs`.
- Prototype output paths: `Assets/_Project/Rendering/CardPrints/` for baked card face textures and `Assets/_Project/Rendering/Materials/CardPrints/` for RetroFakeLit card print materials.
- `Assets/Tests/render.unity/卡牌-原版材质3` currently uses `Assets/_Project/Rendering/Materials/CardPrints/M_CardPrint_SameTarget.mat`, whose `_BaseMap` is the baked texture `Assets/_Project/Rendering/CardPrints/T_CardPrint_SameTarget.png`.
- `Assets/Tests/render.unity/NewCard/render/CardFace_InscriptionPrototype` is a scene-level Quad overlay prototype for an Inscryption-like ink/cardboard print; it uses `Assets/_Project/Rendering/Materials/CardPrints/M_CardPrint_InscriptionPrototype.mat` and `Assets/_Project/Rendering/CardPrints/T_CardPrint_InscriptionPrototype.png`.
- Workflow note: card title/icon/illustration are baked into the material base texture as paper/ink content so they receive scene lighting, RetroFakeLit, and full-screen render styling, rather than being displayed as UI overlays.
- Current visual direction after the 2026-05-25 revision: preserve the original playing-card base texture and overlay borderless dark-brown doodle masks across the card face. Avoid re-filling the face with a new paper color or adding UI-like illustration/icon frames.
- `Assets/_Project/Rendering/Shaders/RetroFakeLit.shader` now includes optional UV-based rounded card clipping and edge darkening controls (`_UseRoundedClip`, `_CardAspect`, `_CornerRadius`, `_EdgeSoftness`, `_EdgeDarkenWidth`, `_EdgeDarkenStrength`) so card prefabs can keep RetroFakeLit lighting while regaining rounded silhouettes without extra border geometry.
- Current rounded-card test material: `Assets/_Project/Rendering/Materials/RetroFakeLitGenerated/M_RetroFakeLit_scene_card_colour_12f771db_Rounded.mat`, applied to `Assets/Arts/Prefabs/RetroFakeLits/卡牌-RetroFakeLit (1).prefab` and its scene instance in `Assets/Tests/render.unity`.
- `Assets/Tests/render.unity/最终版卡牌-RetroFakeLit` currently tests a child world-space Canvas + `TextMeshProUGUI` title overlay on top of the card mesh, while the card itself uses `Assets/_Project/Rendering/Materials/RetroFakeLitTransparentCard.mat` (`Transparent`, queue 3000, `ZWrite Off`). This setup is angle-fragile: the card and the TMP UI both render in transparent queue without depth writes, so ordering becomes camera-dependent and the text can appear to pop behind the card or be swallowed when the viewing angle changes.
- `Assets/Tests/render.unity/桌子/九宫场地格/格7` and `格9` previously had world-space `Canvas` card faces whose text children were plain `TextMeshPro` (3D mesh renderer), not `TextMeshProUGUI`; those text objects therefore did not participate in Canvas draw order and could sort against the card face by camera/depth. They have been converted in-scene to `TextMeshProUGUI` so the card-face text now renders through the parent Canvas.
- Practical direction for stable “text glued to card face”: prefer baking card title/graphics into the card face texture/material, or render them on a dedicated front-face mesh/quad using an opaque or alpha-clipped shader with a tiny normal offset. Avoid relying on world-space UGUI sitting almost coplanar over a transparent card surface.

## Plane TMP Surface Prototype

- `Assets/Tests/render.unity/TestPlane` is a transparent text-surface prototype: its own `MeshRenderer` is disabled, while child `TestPlane/PlaneText` renders world-space `TextMeshPro` only.
- Runtime controller script: `Assets/Scripts/StrayPathCore/UI/PlaneTextDisplay.cs`. It exposes `SetText`, `AppendText`, `DeleteLastCharacter`, `ClearText`, `FadeIn`, `FadeOut`, `FadeTo`, and optional Legacy Input `Input.inputString` capture for dynamic typing/deleting.
- Chinese TMP font asset for this prototype: `Assets/TextMesh Pro/Resources/Fonts & Materials/MaShanZheng SDF.asset`, generated from `Assets/Arts/Fronts/Ma_Shan_Zheng/MaShanZheng-Regular.ttf` and set to dynamic atlas population.
- Implementation note: this is good for a transparent “floating text on plane bounds” test. For final card/desk printing that must receive scene lighting like ink, prefer texture/material baking or a dedicated alpha-clipped mesh/quad.

## RetroFakeLit Desk Print Constraint

- `Assets/Tests/render.unity/桌子` uses `Assets/_Project/Rendering/Materials/M_RetroFakeLit_wooden_table_02.mat`, which only drives `CardDungeon/RetroFakeLit` through `_BaseMap` plus a brown `_BaseColor` tint `(0.78, 0.62, 0.42)`.
- Current `Assets/_Project/Rendering/Shaders/RetroFakeLit.shader` has no second print/decal overlay texture slot, no alpha-only print blend path, and no UV rotation property. So “insert an external print” on RetroFakeLit currently means either replacing/rebaking the whole `_BaseMap` or extending the shader.
- The wooden table top UVs are part of a shared atlas with multiple top-surface UV islands, not one clean dedicated rectangle. So rough atlas alignment is possible, but in-material 90° rotation/placement control is not available with the current single-texture setup.

## Practical Maintenance Note

- When the look drifts, restore from the archive preset first, then tune in this order: URP baseline -> Phase 05 -> Phase 07 -> Phase 08 -> Bloom.
<!-- locus:body:end -->
