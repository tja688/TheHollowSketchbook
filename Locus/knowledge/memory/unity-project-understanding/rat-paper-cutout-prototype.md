---
id: kd_f2d82562-b735-4f74-802b-df83d8a786ce
type: memory
path: unity-project-understanding/rat-paper-cutout-prototype.md
title: rat-paper-cutout-prototype
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779261693245
updatedAt: 1779261693246
---

# rat-paper-cutout-prototype

## Summary
TA prototype setup for the rat paper-cutout visual test in SampleScene.

<!-- locus:body:start -->
- `Assets/Scenes/SampleScene.unity` contains a TA prototype object `PaperCutout_Rat_Prototype` for the rat cutout look test.
- Source image: `Assets/Arts/Pictures/Monsters/老鼠.png`; processed paper/noise/posterized texture generated at `Assets/Arts/Pictures/Monsters/Prototype/老鼠_PaperProcessed.png`.
- Main material `Assets/Arts/Pictures/Monsters/Materials/老鼠_PaperCutout.mat` uses URP Lit alpha cutout (`_Cutoff` 0.38), double-sided rendering, low smoothness, and the processed texture.
- Fake paper thickness is made from an offset dark back quad using `Assets/Arts/Pictures/Monsters/Materials/老鼠_PaperBack_Edge.mat`, plus a thin bottom edge quad and a painted contact shadow.
- Runtime feedback script `Assets/Arts/Pictures/Monsters/PaperCutoutFeedback.cs` adds simple idle sway and pop motion in Play Mode.
<!-- locus:body:end -->
