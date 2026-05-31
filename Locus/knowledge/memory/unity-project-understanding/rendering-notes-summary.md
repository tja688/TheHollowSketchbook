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
updatedAt: 1780234352438
---

# rendering-notes-summary

## Summary
Concise lookup cache for current render validation scene, comfort tuning, render console entry/assets, card-face printing prototypes, and the current RetroFakeLit card/text rendering constraints.

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
- Practical direction for stable “text glued to card face”: prefer baking card title/graphics into the card face texture/material, or render them on a dedicated front-face mesh/quad using an opaque or alpha-clipped shader with a tiny normal offset. Avoid relying on world-space UGUI sitting almost coplanar over a transparent card surface.

## Practical Maintenance Note

- When the look drifts, restore from the archive preset first, then tune in this order: URP baseline -> Phase 05 -> Phase 07 -> Phase 08 -> Bloom.
<!-- locus:body:end -->
