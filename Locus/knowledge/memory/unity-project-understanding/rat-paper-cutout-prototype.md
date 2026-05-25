---
id: kd_f2d82562-b735-4f74-802b-df83d8a786ce
type: memory
path: unity-project-understanding/rat-paper-cutout-prototype.md
title: rat-paper-cutout-prototype
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1779261693245
updatedAt: 1779671686984
---

# rat-paper-cutout-prototype

## Summary
Unity project lookup cache for rat paper-cutout/cardboard prototypes, imported character asset repairs, and the Inscryption-style render validation pipeline. Updated with the 2026-05-24 comfortable render tuning: active Volume in `Assets/Tests/render.unity`, Phase 07/08 current values, restrained Bloom/light/emission settings, and the `Retro_ComfortInscryption` preset.

<!-- locus:maintain-rules:start -->
- Record only Unity project structure knowledge and lookup info that reduce repeated exploration
- Maintain only project-derived engineering understanding, including directory responsibilities, system entry points, config mappings, key assets, and durable tool entry points
- Write user-supplied design goals and agreed solution decisions into Design, not Memory
- Prioritize stable lookup paths, renderer/material wiring, scene mappings, and reusable editor/runtime tooling notes
- Remove temporary investigation traces, one-off guesses, and stale observations when contradicted by current project state
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
## Rat / Paper Cutout Prototype

- `Assets/Scenes/SampleScene.unity` contains `PaperCutout_Rat_Prototype` and earlier rat paper-cutout validation objects.
- Source rat art and generated paper/noise/posterized outputs live under `Assets/tests/Pictures/Monsters/`; the old `Assets/Arts/Pictures/Monsters/` path should not be assumed.
- Rebuild rat paper experiments via `Assets/tests/Pictures/Monsters/RatPaperExperimentBuilder.cs` menu `Tools/Prototype/Build Rat Paper Experiments`.
- Hard-cardboard cutout generation uses `Assets/tests/Pictures/Monsters/CardboardCutoutGenerator.cs`; generated meshes/materials/textures/prefabs live under `Assets/CardboardCutout/Generated/`, rebuilt from menu `Tools/Cardboard Cutout/Build Rat Cardboard Cutout`.
- Manual Alpha mode for `Assets/tests/Pictures/Monsters/硬纸板老鼠.png` requires actual transparent pixels; there is no automatic rough-crop fallback.

## Imported Character Asset Notes

- Imported fantasy hero assets are under `Assets/PolygonFantasyHeroCharacters/` or `Assets/Arts/Models/PolygonFantasyHeroCharacters/` depending on import path used by the scene.
- The Synty custom shader was converted for URP as `SyntyStudios/CustomCharacter`; repaired custom materials use that shader, and StandardMaterials use `Universal Render Pipeline/Lit`.

## Render Validation Scene and Core Assets

- Current render validation scene is `Assets/Tests/render.unity`. It contains a table, cards, statue/chest/candle props, tree tests, one spot `Lights/TableWarmLight`, point lights `Lights/CandleLight_*`, point light `Lights/SoulBottle_GlowHint`, and a zero-intensity directional light.
- `Assets/Tests/render.unity` uses `Assets/Settings/URP-HighFidelity.asset`: render scale `0.5`, HDR enabled, MSAA disabled, depth texture enabled, additional lights per object `4`, active renderer `Assets/Settings/URP-HighFidelity-Renderer.asset`.
- Project quirk: `Assets/Settings/URP-HighFidelity.asset` serializes `m_UpscalingFilter = 4`; keep the render console's special raw value instead of assuming a standard URP enum semantic.
- `Assets/Settings/URP-HighFidelity-Renderer.asset` includes SSAO plus FullScreenPassRendererFeatures `RetroPosterizeThreshold` and `CardDungeon Retro Composite`, both injected After Rendering Post Processing with Color requirement.

## Phase 05 / RetroFakeLit

- `Assets/_Project/Rendering/Shaders/RetroFakeLit.shader` is the project non-PBR fake-lit shader for ordinary props; `_LightWrap` was intentionally left at `0` after visual review.
- Generated/converted RetroFakeLit materials live under `Assets/_Project/Rendering/Materials/` and `Assets/_Project/Rendering/Materials/RetroFakeLitGenerated/`.
- `RetroFakeLitConvert` is implemented in `Assets/_Project/Editor/Rendering/RetroFakeLitConversionService.cs` and exposed in `Assets/_Project/Editor/Rendering/CardDungeonRenderPipelineConsoleWindow.cs`. It writes prefabs to `Assets/Arts/Prefabs/RetroFakeLits/` and generated materials to `Assets/_Project/Rendering/Materials/RetroFakeLitGenerated/`.
- Verified conversion outputs include `Assets/Arts/Prefabs/RetroFakeLits/gezinkte_spielkarte_km_o_811_RetroFakeLit.prefab` and `Assets/Arts/Prefabs/RetroFakeLits/PineTree_RetroFakeLit.prefab`.

## Phase 07 / Posterize Threshold

- Phase 07 shader/material: `Assets/_Project/Rendering/Shaders/RetroPosterizeThreshold.shader` and `Assets/_Project/Rendering/Materials/M_RetroPosterizeThreshold_Phase07.mat`.
- Phase 07 LUT assets: `Assets/_Project/Rendering/Textures/PosterizeLUT/T_LUT_DirtyBrown.asset`, `T_LUT_DarkGreen.asset`, and `T_LUT_CandleRed.asset`.
- As of the 2026-05-24 comfortable Inscryption tuning, current Phase 07 values are DirtyBrown LUT, `_Contribution=0.62`, `_Threshold=0.50`, `_ThresholdSharpness=8`, `_LutStrength=0.80`, debug values `0`.
- The earlier aggressive archive had `_Threshold=0.97`, which caused too much midtone LUT pollution. Avoid returning to that unless intentionally testing a harsh look.

## Phase 08 / Retro Composite

- Retro composite shader/material: `Assets/Shaders/CardDungeon_RetroComposite.shader` and `Assets/VisualPrototypes/InscryptionRetro/Materials/M_RetroComposite_Inscryption.mat`.
- The shader includes virtual pixel sampling, palette/posterization, black crush, vignette, scanlines, noise, chromatic aberration, CRT curvature/edge softness, glow bleed, and horizontal jitter.
- As of the 2026-05-24 comfortable Inscryption tuning, current Composite values are `1280x720`, `_Pixelate=0.88`, `_PosterizeLevels=10`, `_PosterizeStrength=0.32`, `_PaletteStrength=0.20`, `_PaletteDarkThreshold=0.38`, `_BlackCrush=0.06`, `_Contrast=1.22`, `_Saturation=0.86`, `_VignetteStrength=0.58`, `_VignetteRadius=0.74`, `_ScanlineStrength=0.03`, `_ChromaticAberration=0.15`, `_NoiseStrength=0.02`, `_CrtCurvature=0.012`, `_CrtEdgeSoftness=0.012`, `_CrtGlowBleed=0.10`, `_HorizontalJitter=0.016`, warm tint `(1.06, 0.84, 0.58)`, cold tint `(0.12, 0.26, 0.23)`.
- If the look becomes too clear, first raise `_Pixelate` toward `0.95`; do not immediately return to `960x540 + Pixelate=1` because that was part of the over-blurred look.

## Post Processing / Volume State

- As of 2026-05-24, `Assets/Tests/render.unity` now has `Global Post Process Volume` using `Assets/Settings/SampleSceneProfile.asset`, and `Assets/Tests/render.unity/Main Camera` has URP `Render Post Processing` enabled.
- `Assets/Settings/SampleSceneProfile.asset` is now actually active in `Assets/Tests/render.unity`; older memory saying the render scene had no Volume is stale.
- Comfortable Bloom values: threshold `1.05`, intensity `0.28`, scatter `0.32`, tint `(1.0, 0.78, 0.48)`. This intentionally keeps Bloom as small information-point glow rather than lighting the table.
- Comfortable Vignette values in the shared profile: intensity `0.26`, smoothness `0.62`, black color. The custom Composite pass also has its own vignette, so avoid stacking both too high.

## Current Render Scene Lighting Tuning

- Comfortable tuning in `Assets/Tests/render.unity`: `Lights/TableWarmLight` spot color `(1, 0.58, 0.33)`, intensity `165`, range `3.3`, spot angle `52`, inner angle `30`.
- `Lights/CandleLight_2`: color `(1, 0.56, 0.30)`, intensity `1.15`, range `2.25`.
- `Lights/SoulBottle_GlowHint`: color `(0.22, 0.80, 0.72)`, intensity `0.55`, range `1.45`.
- `Assets/tests/Render/M_Emission_Flame.mat` was reduced to emission `(2.2, 0.9, 0.22)`; `Assets/tests/Render/M_Emission_SoulBottle.mat` was reduced to emission `(0.08, 1.35, 1.05)`.

## Render Console and Presets

- Render console entry: `Tools/CardDungeon Rendering/项目渲染管线综合控制台`.
- Console files: `Assets/_Project/Editor/Rendering/CardDungeonRenderPipelineConsoleWindow.cs`, `Assets/_Project/Editor/Rendering/CardDungeonRenderPipelineConsoleConfig.cs`, and config asset `Assets/_Project/Editor/Rendering/CardDungeonRenderPipelineConsoleConfig.asset`.
- The console manages URP baseline, Phase 05 RetroFakeLit shared params/conversion, Phase 07, Phase 08, Bloom, and preset application/capture.
- Presets live under `Assets/_Project/Rendering/Presets/`. `Retro_Archive_Current.asset` preserves the prior harsher 960x540/strong-Bloom archive. `Retro_ComfortInscryption.asset` captures the 2026-05-24 more comfortable Inscryption-inspired tuning and is referenced by the console config.
- Current task note: `Assets/Notes/类邪恶冥刻舒适画面调参记录_2026-05-24.md` summarizes the comfortable tuning rationale and key values.
<!-- locus:body:end -->
