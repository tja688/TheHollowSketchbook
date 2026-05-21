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
updatedAt: 1779347708946
---

# rat-paper-cutout-prototype

## Summary
TA prototype setup for rat paper-cutout tests, hard-cardboard cutout workflow, PolygonFantasyHeroCharacters URP material repair notes, and SampleScene Inscryption-style retro render pipeline/CRT full-screen filter setup.

<!-- locus:maintain-rules:start -->
- Record only Unity project structure knowledge and lookup info that reduce repeated exploration
- Maintain only project-derived engineering understanding, including directory responsibilities, system entry points, asset relationships, runtime entry points, and config mappings
- Write user-supplied design goals, gameplay intent, product direction, and solution decisions into Design
- Prioritize directory responsibilities, core system entry points, key scenes, prefabs, ScriptableObjects, assemblies, and config mappings
- Record verified asset relationships, runtime entry points, key dependencies, and common lookup paths
- Remove temporary investigation traces, one-off task residue, unverified guesses, and expired cache
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
- Inscryption-style render pipeline validation is now in `Assets/Scenes/SampleScene.unity/Inscryption_RenderPipeline_Validation`: dark table block, black crush room shell, 5 physical multiplier slots, glowing labels/grooves, physical cards, heart candles, soul jar, red eyes, and one placed prefab instance from `Assets/PolygonFantasyHeroCharacters/Prefabs/Characters_Presets/Chr_FantasyHero_Preset_1.prefab` hidden in darkness.
- Retro full-screen shader lives at `Assets/Shaders/CardDungeon_RetroComposite.shader`; material preset lives at `Assets/VisualPrototypes/InscryptionRetro/Materials/M_RetroComposite_Inscryption.mat`.
- The retro shader now includes CRT-specific barrel curvature, soft edge mask, horizontal jitter, stronger scanlines, subtle high-light bleed, chromatic aberration, noise, palette/posterization, black crush, and vignette. Tune via material floats `_CrtCurvature`, `_CrtEdgeSoftness`, `_CrtGlowBleed`, `_HorizontalJitter`, `_ScanlineStrength`, `_ChromaticAberration`, and related color controls.
- Current active URP asset is `Assets/Settings/URP-HighFidelity.asset`; it was adjusted for retro validation: render scale 0.5, point/nearest upscaling enum value 1, MSAA disabled, depth texture enabled, shorter shadow distance, fewer additional lights.
- `Assets/Settings/URP-HighFidelity-Renderer.asset` includes a FullScreenPassRendererFeature named `CardDungeon Retro Composite`, injected After Rendering Post Processing with Color requirement and using `M_RetroComposite_Inscryption`.
- `Assets/Settings/SampleSceneProfile.asset` is used by `Global Volume` and now contains Tonemapping, Bloom, Vignette, and ColorAdjustments for the dark low-res validation look.
- Creating TextMeshPro 3D labels caused Unity to import TMP essentials under `Assets/TextMesh Pro/`; keep this if the scene labels or future card text use TextMeshPro.
<!-- locus:body:end -->
