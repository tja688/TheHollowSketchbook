---
id: kd_8c77f5ec-cb9b-4166-9065-2c8095f94a45
type: memory
path: unity-project-understanding/rendering-notes-summary.md
title: rendering-notes-summary
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779672335148
updatedAt: 1779672335149
---

# rendering-notes-summary

## Summary
Concise lookup cache for current render validation scene, comfort tuning, and the render console entry/assets.

<!-- locus:body:start -->
## Render Validation / Comfort Tuning

- Current validation scene: `Assets/Tests/render.unity`.
- Main stable tuning reference: `Assets/Notes/类邪恶冥刻舒适画面调参记录_2026-05-24.md`.
- Current comfortable target: darker, warmer table scene with restrained Bloom and only small highlight glows.

## Render Console

- Main entry: `Tools/CardDungeon Rendering/项目渲染管线综合控制台`.
- It manages URP baseline, Phase 05 RetroFakeLit, Phase 07 Posterize LUT, Phase 08 Retro Composite, Bloom, and presets.
- Key editable assets remain the URP HighFidelity settings, renderer feature asset, SampleSceneProfile, Phase 07/08 materials, and preset assets.

## Practical Maintenance Note

- When the look drifts, restore from the archive preset first, then tune in this order: URP baseline -> Phase 05 -> Phase 07 -> Phase 08 -> Bloom.
<!-- locus:body:end -->
