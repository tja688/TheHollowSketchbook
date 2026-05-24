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
updatedAt: 1779606871344
---

# rat-paper-cutout-prototype

## Summary
TA prototype setup for rat paper-cutout tests, hard-cardboard cutout workflow, retro render validation scene wiring, Phase 05/07/08 render assets, and the editor render control console including archived Phase 09 virtual-resolution workflow and preset paths.

<!-- locus:maintain-rules:start -->
- Record only Unity project structure knowledge and lookup info that reduce repeated exploration
- Maintain only project-derived engineering understanding, including directory responsibilities, system entry points, config mappings, key assets, and durable tool entry points
- Write user-supplied design goals and agreed solution decisions into Design, not Memory
- Prioritize stable lookup paths, renderer/material wiring, scene mappings, and reusable editor/runtime tooling notes
- Remove temporary investigation traces, one-off guesses, and stale observations when contradicted by current project state
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
- `Assets/Scenes/SampleScene.unity` contains a TA prototype object `PaperCutout_Rat_Prototype` for the rat cutout look test.
- Current verified asset root is `Assets/tests/Pictures/Monsters/` (not the older `Assets/Arts/Pictures/Monsters/` path).
- Source image: `Assets/tests/Pictures/Monsters/老鼠.png`; processed paper/noise/posterized texture generated at `Assets/tests/Pictures/Monsters/Prototype/老鼠_PaperProcessed.png`.
- Main material `Assets/tests/Pictures/Monsters/Materials/老鼠_PaperCutout.mat` uses URP Lit alpha cutout (`_Cutoff` 0.38), double-sided rendering, low smoothness, and the processed texture.
- Fake paper thickness is made from an offset dark back quad using `Assets/tests/Pictures/Monsters/Materials/老鼠_PaperBack_Edge.mat`, plus a thin bottom edge quad and a painted contact shadow.
- Runtime feedback script `Assets/tests/Pictures/Monsters/PaperCutoutFeedback.cs` adds simple idle sway and pop motion in Play Mode.
- Paper effect comparison objects are in the same scene: `Rat_Paper_SchemeA_VertexDeform` is a duplicated subdivided 12x16 mesh with `PaperVertexSpringDeformer`; `Rat_Paper_SchemeB_ControlPoints` is a duplicated segmented/control-point spring setup using `PaperControlPointSpring` plus per-segment `PaperVertexSpringDeformer`.
- Builder utility: `Assets/tests/Pictures/Monsters/RatPaperExperimentBuilder.cs` can rebuild the comparison objects and generated meshes/materials from Unity menu `Tools/Prototype/Build Rat Paper Experiments`.
- Hard-cardboard generator: `Assets/tests/Pictures/Monsters/CardboardCutoutGenerator.cs` reads `Assets/tests/Pictures/Monsters/硬纸板老鼠.png` in manual Alpha mode, uses the source PNG alpha as the final hand-cut silhouette, lightly simplifies/resamples it, then generates an extruded 3-submesh URP Lit mesh, materials, edge repeat texture, prefab, and places `Cardboard_Rat_Generated` in `Assets/Scenes/SampleScene.unity`.
- Manual Alpha mode requires `Assets/tests/Pictures/Monsters/硬纸板老鼠.png` to contain transparent pixels; there is no automatic rough crop fallback in the generator now.
- Generated hard-cardboard outputs live under `Assets/CardboardCutout/Generated/`: `Meshes/Rat_Cardboard.asset`, `Materials/Rat_Cardboard_Front.mat`, `Materials/Rat_Cardboard_Back.mat`, `Materials/Rat_Cardboard_Edge.mat`, `Textures/Cardboard_Edge_Repeat.png`, `Textures/Rat_Cardboard_CutMask.png`, and `Prefabs/Rat_Cardboard.prefab`.
- Rebuild hard-cardboard via Unity menu `Tools/Cardboard Cutout/Build Rat Cardboard Cutout`; it replaces the scene instance named `Cardboard_Rat_Generated` and adjusts camera/light for inspection.
- Imported fantasy hero asset root: `Assets/PolygonFantasyHeroCharacters/`. In URP project, `Assets/PolygonFantasyHeroCharacters/Shaders/POLYGON_CustomCharacters.shader` was converted from built-in surface shader to a URP-compatible custom shader named `SyntyStudios/CustomCharacter`.
- Fantasy hero material repair status: 21 custom materials under `Assets/PolygonFantasyHeroCharacters/Materials/CustomMaterials/` plus `Assets/PolygonFantasyHeroCharacters/Materials/FantasyHero.mat` use `SyntyStudios/CustomCharacter`; 12 StandardMaterials under `Assets/PolygonFantasyHeroCharacters/Materials/StandardMaterials/` use `Universal Render Pipeline/Lit`.
- Inscryption-style render pipeline validation was previously built in `Assets/Scenes/SampleScene.unity/Inscryption_RenderPipeline_Validation`: dark table block, black crush room shell, 5 physical multiplier slots, glowing labels/grooves, physical cards, heart candles, soul jar, red eyes, and one placed prefab instance from `Assets/PolygonFantasyHeroCharacters/Prefabs/Characters_Presets/Chr_FantasyHero_Preset_1.prefab` hidden in darkness.
- Current active render validation scene inspected on 2026-05-23 is `Assets/Tests/render.unity`. It contains a table, 3 card mesh instances, a bird with eye objects, dark scene/background meshes, statues/chest/candleholder props, one spot `TableWarmLight`, three point `CandleLight_*`, and a zero-intensity directional light.
- `Assets/Tests/render.unity` uses `Assets/Settings/URP-HighFidelity.asset` with render scale 0.5, MSAA disabled, HDR enabled, depth texture enabled, additional lights per object 4, and active renderer `Assets/Settings/URP-HighFidelity-Renderer.asset`.
- `Assets/Settings/URP-HighFidelity-Renderer.asset` includes SSAO and a FullScreenPassRendererFeature named `CardDungeon Retro Composite`, injected After Rendering Post Processing with Color requirement and using `Assets/VisualPrototypes/InscryptionRetro/Materials/M_RetroComposite_Inscryption.mat`.
- Retro full-screen shader lives at `Assets/Shaders/CardDungeon_RetroComposite.shader`; material preset lives at `Assets/VisualPrototypes/InscryptionRetro/Materials/M_RetroComposite_Inscryption.mat`.
- The retro shader includes barrel curvature, soft edge mask, horizontal jitter, scanlines, highlight bleed, chromatic aberration, noise, palette/posterization, black crush, and vignette. In `render.unity` the material is set to virtual 960x540, pixelate 1, posterize strength 0.42, palette strength 0.34, black crush 0.10, scanline 0.12, chromatic aberration 0.45, vignette 0.62.
- `Assets/Tests/render.unity` has no Volume component and `Main Camera` has Render Post Processing disabled, so URP Volume Bloom/Tonemapping/Vignette are not active there; the visible retro look comes from the renderer feature composite rather than camera post-processing.
- On 2026-05-23, Phase 05 RetroFakeLit lives at `Assets/_Project/Rendering/Shaders/RetroFakeLit.shader`; default `_LightWrap` is 0 because visual review found wrap 0 looks better than wrapped lighting. `Assets/Tests/render.unity` ordinary props/table/bird/statues/chest/candleholder/background characters were converted to generated materials under `Assets/_Project/Rendering/Materials/`. Cards, eye emission spheres, flame/soul emission meshes, light gizmo meshes, and inactive tree test assets were intentionally excluded.
- The reusable conversion tool is `Assets/_Project/Scripts/RetroFakeLitMaterialConverter.cs` with menu items `Tools/CardDungeon Rendering/Convert Scene Ordinary Objects To RetroFakeLit` and `Tools/CardDungeon Rendering/Convert Selection To RetroFakeLit`. It converts source material base texture/color into generated `M_RetroFakeLit_*` materials, sets `_LightWrap = 0`, and skips obvious cards/lights/eyes for the active scene pipeline.
- Phase 07 PosterizeWithThreshold was implemented on 2026-05-23 as `Assets/_Project/Rendering/Shaders/RetroPosterizeThreshold.shader`, material `Assets/_Project/Rendering/Materials/M_RetroPosterizeThreshold_Phase07.mat`, and three generated 256x1 LUT assets under `Assets/_Project/Rendering/Textures/PosterizeLUT/`: `T_LUT_DirtyBrown.asset`, `T_LUT_DarkGreen.asset`, `T_LUT_CandleRed.asset`.
- Phase 07 is active by default in `Assets/Settings/URP-HighFidelity-Renderer.asset` as FullScreenPassRendererFeature `RetroPosterizeThreshold`, ordered after SSAO and before `CardDungeon Retro Composite`, injected After Rendering Post Processing with Color requirement. Default params: `_Threshold=0.50`, `_ThresholdSharpness=12`, `_Contribution=0.85`, `_LutStrength=1`, LUT DirtyBrown, `_CompareDebug=0`, `_DebugMask=0`. Toggle comparison by disabling the `RetroPosterizeThreshold` renderer feature, or set `_CompareDebug=1` / `_DebugMask=1` on the material.
- `Assets/Settings/SampleSceneProfile.asset` is used by the older `Global Volume` setup in `Assets/Scenes/SampleScene.unity`, not by `Assets/Tests/render.unity` unless a volume is added.
- Creating TextMeshPro 3D labels caused Unity to import TMP essentials under `Assets/TextMesh Pro/`; keep this if the scene labels or future card text use TextMeshPro.
- On 2026-05-23, a dedicated editor window control panel was added at `Assets/_Project/Editor/Rendering/CardDungeonRenderPipelineConsoleWindow.cs` with config asset `Assets/_Project/Editor/Rendering/CardDungeonRenderPipelineConsoleConfig.asset`, opened from menu `Tools/CardDungeon Rendering/项目渲染管线综合控制台`. It directly manages `Assets/Settings/URP-HighFidelity.asset`, `Assets/Settings/URP-HighFidelity-Renderer.asset`, `Assets/_Project/Rendering/Materials/M_RetroPosterizeThreshold_Phase07.mat`, `Assets/VisualPrototypes/InscryptionRetro/Materials/M_RetroComposite_Inscryption.mat`, `Assets/Settings/SampleSceneProfile.asset`, and batch-shared Phase 05 values for all `CardDungeon/RetroFakeLit` materials under `Assets/_Project/Rendering/Materials/`.
- The render console is organized into pages: Overview, Render Scale/URP baseline, Phase 05 RetroFakeLit, Phase 07 Posterize LUT, Phase 08 Retro Composite, and Presets. Each control includes user-facing explanation text about what increasing or decreasing the parameter changes.
- Important project quirk: `Assets/Settings/URP-HighFidelity.asset` currently serializes `m_UpscalingFilter = 4`, while the runtime enum probe for `UpscalingFilterSelection` reported values 0=Auto, 1=Linear, 2=Point, 3=FSR. The control panel keeps raw option 4 exposed as a special “current project value” choice instead of assuming a semantic mapping.
- On 2026-05-23, Phase 09 was archived as a control-console-driven fixed virtual resolution workflow rather than a new runtime `FixedVirtualResolutionController`: the authoritative virtual pixel size now lives in `Assets/VisualPrototypes/InscryptionRetro/Materials/M_RetroComposite_Inscryption.mat` (`_VirtualWidth`, `_VirtualHeight`, `_Pixelate`), with quick switches for 640x360 / 960x540 / 1280x720 in the Render Scale page.
- `Assets/Tests/render.unity/Main Camera` currently renders directly to screen (`Target Texture = None`) and has no runtime letterbox/pillarbox or mouse-coordinate remap logic; this project state is intentional for the current archive because card interaction and later runtime systems were explicitly deferred.
- Preset assets are managed under `Assets/_Project/Rendering/Presets/`. The console now auto-syncs preset references into `Assets/_Project/Editor/Rendering/CardDungeonRenderPipelineConsoleConfig.asset`, supports default preset regeneration, and uses `Assets/_Project/Rendering/Presets/Retro_Archive_Current.asset` as the archive snapshot of the currently approved core look.
- Current archive snapshot values captured from project assets are: Phase07 LUT DirtyBrown, `threshold=0.97`, `contribution=0.51`, `thresholdSharpness=11.45`; Composite virtual resolution `960x540`; Bloom intensity `1.35` from `Assets/Settings/SampleSceneProfile.asset`.
<!-- locus:body:end -->
